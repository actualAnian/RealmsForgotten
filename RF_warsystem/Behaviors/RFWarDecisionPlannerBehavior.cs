using System;
using System.Collections.Generic;
using System.Linq;
using RF_warsystem.Diagnostics;
using RF_warsystem.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RF_warsystem.Behaviors;

public sealed class RFWarDecisionPlannerBehavior : CampaignBehaviorBase
{
    private const float CampaignStartGateDays = 5f;
    private const float ProposalCooldownDays = 3f;
    private const float MinimumWarLikelihood = 0.32f;
    private const float MinimumPeaceLikelihood = 0.26f;
    private const float ProposalReplacementBiasLead = 45f;

    private readonly Dictionary<string, float> _lastProposalDayByKingdom = new(StringComparer.Ordinal);
    private bool _suppressDecisionAudit;
    internal static RFWarDecisionPlannerBehavior? Instance { get; private set; }

    private sealed class WarCandidateSnapshot
    {
        public DeclareWarDecision? Decision;
        public Clan? Sponsor;
        public float StrategicScore;
        public float Threshold;
        public float Support;
        public float Likelihood;
        public float Bias;
    }

    private sealed class PeaceCandidateSnapshot
    {
        public MakePeaceKingdomDecision? Decision;
        public Clan? Sponsor;
        public float PeaceScore;
        public float Threshold;
        public float Support;
        public float Likelihood;
        public float Bias;
        public int TributePerDay;
    }

    public override void RegisterEvents()
    {
        Instance = this;
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnKingdomDecisionAdded);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    internal static bool TryPromoteSpecialWarProposal(Kingdom kingdom, Kingdom target)
    {
        return Instance?.TryPromoteSpecialWarProposalInternal(kingdom, target) == true;
    }

    internal static bool TryPromoteSpecialPeaceProposal(Kingdom kingdom, Kingdom target)
    {
        return Instance?.TryPromoteSpecialPeaceProposalInternal(kingdom, target) == true;
    }

    private void OnDailyTick()
    {
        if ((float)(int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow < CampaignStartGateDays)
        {
            return;
        }

        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsKingdomEligible(kingdom) || IsOnCooldown(kingdom))
            {
                continue;
            }

            if (TryAddBestPeaceDecision(kingdom))
            {
                MarkProposal(kingdom);
                continue;
            }

            if (TryAddBestWarDecision(kingdom))
            {
                MarkProposal(kingdom);
            }
        }
    }

    private void OnKingdomDecisionAdded(KingdomDecision decision, bool isPlayerInvolved)
    {
        if (_suppressDecisionAudit || isPlayerInvolved || decision?.Kingdom == null || decision.ProposerClan == null || decision.ProposerClan == Clan.PlayerClan)
        {
            return;
        }

        switch (decision)
        {
            case DeclareWarDecision declareWarDecision:
                AuditWarDecision(declareWarDecision);
                break;
            case MakePeaceKingdomDecision makePeaceDecision:
                AuditPeaceDecision(makePeaceDecision);
                break;
        }
    }

    private static bool IsKingdomEligible(Kingdom kingdom)
    {
        return kingdom != null && !kingdom.IsEliminated && kingdom.RulingClan != null;
    }

    private bool IsOnCooldown(Kingdom kingdom)
    {
        if (!_lastProposalDayByKingdom.TryGetValue(GetKingdomKey(kingdom), out float lastDay))
        {
            return false;
        }

        return GetCurrentDay() - lastDay < ProposalCooldownDays;
    }

    private void MarkProposal(Kingdom kingdom)
    {
        _lastProposalDayByKingdom[GetKingdomKey(kingdom)] = GetCurrentDay();
    }

    private static float GetCurrentDay()
    {
        return (float)CampaignTime.Now.ToDays;
    }

    private static string GetKingdomKey(Kingdom kingdom)
    {
        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    private static List<Clan> GetEligibleSponsors(Kingdom kingdom, int requiredInfluence)
    {
        return kingdom.Clans
            .Where(clan =>
                clan != null &&
                clan != Clan.PlayerClan &&
                !clan.IsEliminated &&
                !clan.IsBanditFaction &&
                clan.Kingdom == kingdom &&
                clan.CurrentTotalStrength > 0f &&
                clan.Influence >= requiredInfluence)
            .OrderByDescending(clan => clan.Influence)
            .ThenByDescending(clan => clan.CurrentTotalStrength)
            .ToList();
    }

    private static bool HasPendingWarDecision(Kingdom kingdom)
    {
        return kingdom.UnresolvedDecisions.Any(decision => decision is DeclareWarDecision);
    }

    private static bool HasPendingPeaceDecision(Kingdom kingdom)
    {
        return kingdom.UnresolvedDecisions.Any(decision => decision is MakePeaceKingdomDecision);
    }

    private static bool TryGetBestWarCandidate(
        Kingdom kingdom,
        out WarCandidateSnapshot? bestCandidate,
        out float secondBestBias,
        bool allowPendingDecision = false)
    {
        bestCandidate = null;
        secondBestBias = float.MinValue;
        if (kingdom.RulingClan == null)
        {
            return false;
        }

        if (!allowPendingDecision && HasPendingWarDecision(kingdom))
        {
            return false;
        }

        DiplomacyModel diplomacy = Campaign.Current.Models.DiplomacyModel;
        int influenceCost = diplomacy.GetInfluenceCostOfProposingWar(kingdom.RulingClan);
        List<Clan> sponsors = GetEligibleSponsors(kingdom, influenceCost);
        if (sponsors.Count == 0)
        {
            return false;
        }

        if (kingdom.FactionsAtWarWith.OfType<Kingdom>().Any() && RFWarCampaignDirectorBehavior.GetActiveCampaignLockFactor(kingdom) >= 0.82f)
        {
            return false;
        }

        float bestScore = float.MinValue;
        foreach (Kingdom target in Kingdom.All)
        {
            foreach (Clan sponsor in sponsors)
            {
                if (!TryEvaluateWarCandidate(kingdom, sponsor, target, out WarCandidateSnapshot? candidate) || candidate == null)
                {
                    continue;
                }

                if (candidate.Bias > bestScore)
                {
                    secondBestBias = bestScore;
                    bestScore = candidate.Bias;
                    bestCandidate = candidate;
                }
                else if (candidate.Bias > secondBestBias)
                {
                    secondBestBias = candidate.Bias;
                }
            }
        }

        if (bestCandidate != null && !HasDominantWarWindow(kingdom, bestCandidate, secondBestBias))
        {
            bestCandidate = null;
            return false;
        }

        return bestCandidate != null;
    }

    private static bool TryGetBestWarCandidateForTarget(
        Kingdom kingdom,
        Kingdom target,
        out WarCandidateSnapshot? bestCandidate)
    {
        bestCandidate = null;
        if (kingdom.RulingClan == null)
        {
            return false;
        }

        DiplomacyModel diplomacy = Campaign.Current.Models.DiplomacyModel;
        int influenceCost = diplomacy.GetInfluenceCostOfProposingWar(kingdom.RulingClan);
        List<Clan> sponsors = GetEligibleSponsors(kingdom, influenceCost);
        if (sponsors.Count == 0)
        {
            return false;
        }

        float bestScore = float.MinValue;
        foreach (Clan sponsor in sponsors)
        {
            if (!TryEvaluateWarCandidate(kingdom, sponsor, target, out WarCandidateSnapshot? candidate) || candidate == null)
            {
                continue;
            }

            if (candidate.Bias > bestScore)
            {
                bestScore = candidate.Bias;
                bestCandidate = candidate;
            }
        }

        return bestCandidate != null;
    }

    private static bool IsValidWarTarget(Kingdom kingdom, Kingdom target)
    {
        if (target == null || target == kingdom || target.IsEliminated)
        {
            return false;
        }

        if (target.IsAtWarWith(kingdom))
        {
            return false;
        }

        return target.GetStanceWith(kingdom).PeaceDeclarationDate.ElapsedDaysUntilNow > 20f;
    }

    private static bool TryEvaluateWarCandidate(Kingdom kingdom, Clan sponsor, Kingdom target, out WarCandidateSnapshot? candidate)
    {
        candidate = null;

        if (sponsor == null || target == null || !IsValidWarTarget(kingdom, target))
        {
            return false;
        }

        DiplomacyModel diplomacy = Campaign.Current.Models.DiplomacyModel;
        TextObject reason;
        float strategicScore = diplomacy.GetScoreOfDeclaringWar(kingdom, target, sponsor, out reason, includeReason: false);
        if (strategicScore <= -999999f)
        {
            return false;
        }

        float threshold = diplomacy.GetDecisionMakingThreshold(kingdom);
        if (strategicScore < threshold)
        {
            return false;
        }

        float barterValue = new DeclareWarBarterable(kingdom, target).GetValueForFaction(sponsor);
        if (barterValue < threshold)
        {
            return false;
        }

        DeclareWarDecision decision = new(sponsor, target);
        if (!decision.CanMakeDecision(out _))
        {
            return false;
        }

        float support = decision.CalculateSupport(sponsor);
        if (support <= 50f)
        {
            return false;
        }

        float likelihood = new KingdomElection(decision).GetLikelihoodForSponsor(sponsor);
        if (likelihood < MinimumWarLikelihood)
        {
            return false;
        }

        float candidateScore =
            (strategicScore - threshold) +
            support * 0.18f +
            likelihood * 35f +
            GetWarSelectionBias(kingdom, target);

        candidate = new WarCandidateSnapshot
        {
            Decision = decision,
            Sponsor = sponsor,
            StrategicScore = strategicScore,
            Threshold = threshold,
            Support = support,
            Likelihood = likelihood,
            Bias = candidateScore
        };
        return true;
    }

    private static bool HasDominantWarWindow(Kingdom kingdom, WarCandidateSnapshot bestCandidate, float secondBestBias)
    {
        if (secondBestBias <= float.MinValue + 1f)
        {
            return true;
        }

        Kingdom? bestTarget = bestCandidate.Decision?.FactionToDeclareWarOn as Kingdom;
        if (bestTarget == null)
        {
            return true;
        }

        float lead = bestCandidate.Bias - secondBestBias;
        return lead >= GetRequiredWarCandidateLead(kingdom, bestTarget);
    }

    private static float GetRequiredWarCandidateLead(Kingdom kingdom, Kingdom bestTarget)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(kingdom);
        float requiredLead = 12f;

        if (kingdom.FactionsAtWarWith.OfType<Kingdom>().Any())
        {
            requiredLead += 8f;
        }

        requiredLead += RFWarCampaignDirectorBehavior.GetActiveCampaignLockFactor(kingdom) * 14f;
        requiredLead += GetOwnMultiFrontPressure(kingdom) * 8f;
        requiredLead += GetOffAxisWarPenalty(kingdom, bestTarget) * 14f;
        requiredLead += Math.Max(0f, profile.Caution) * 10f;
        requiredLead += Math.Max(0f, profile.HomeGuardBias) * 8f;
        requiredLead += Math.Max(0f, profile.FrontierParanoia) * 6f;
        requiredLead -= Math.Max(0f, profile.OffensiveDrive) * 4f;
        requiredLead -= Math.Max(0f, profile.Opportunism) * 3f;

        if (RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom) == bestTarget)
        {
            requiredLead -= 7f;
        }

        requiredLead -= GetSpecialWarBias(kingdom, bestTarget) * 6f;
        requiredLead -= GetSacredTargetBias(kingdom, bestTarget) * 4f;

        return Math.Max(6f, Math.Min(30f, requiredLead));
    }

    private bool TryAddBestWarDecision(Kingdom kingdom)
    {
        if (!TryGetBestWarCandidate(kingdom, out WarCandidateSnapshot? bestCandidate, out _, allowPendingDecision: false) || bestCandidate == null)
        {
            return false;
        }

        AddManagedDecision(kingdom, bestCandidate.Decision!);
        RFWarSystemTraceLog.WarProposal(
            kingdom,
            bestCandidate.Decision!.FactionToDeclareWarOn as Kingdom,
            bestCandidate.Sponsor!,
            bestCandidate.StrategicScore,
            bestCandidate.Threshold,
            bestCandidate.Support,
            bestCandidate.Likelihood,
            bestCandidate.Bias);
        return true;
    }

    private static bool TryGetBestPeaceCandidate(
        Kingdom kingdom,
        out PeaceCandidateSnapshot? bestCandidate,
        bool allowPendingDecision = false)
    {
        bestCandidate = null;
        if (kingdom.RulingClan == null)
        {
            return false;
        }

        if (!allowPendingDecision && HasPendingPeaceDecision(kingdom))
        {
            return false;
        }

        DiplomacyModel diplomacy = Campaign.Current.Models.DiplomacyModel;
        int influenceCost = diplomacy.GetInfluenceCostOfProposingPeace(kingdom.RulingClan);
        List<Clan> sponsors = GetEligibleSponsors(kingdom, influenceCost);
        if (sponsors.Count == 0)
        {
            return false;
        }

        float threshold = diplomacy.GetDecisionMakingThreshold(kingdom);
        float bestScore = float.MinValue;
        foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            foreach (Clan sponsor in sponsors)
            {
                if (!TryEvaluatePeaceCandidate(kingdom, sponsor, enemy, out PeaceCandidateSnapshot? candidate) || candidate == null)
                {
                    continue;
                }

                if (candidate.Bias > bestScore)
                {
                    bestScore = candidate.Bias;
                    bestCandidate = candidate;
                }
            }
        }

        return bestCandidate != null;
    }

    private static bool TryGetBestPeaceCandidateForTarget(
        Kingdom kingdom,
        Kingdom target,
        out PeaceCandidateSnapshot? bestCandidate)
    {
        bestCandidate = null;
        if (kingdom.RulingClan == null)
        {
            return false;
        }

        DiplomacyModel diplomacy = Campaign.Current.Models.DiplomacyModel;
        int influenceCost = diplomacy.GetInfluenceCostOfProposingPeace(kingdom.RulingClan);
        List<Clan> sponsors = GetEligibleSponsors(kingdom, influenceCost);
        if (sponsors.Count == 0)
        {
            return false;
        }

        float bestScore = float.MinValue;
        foreach (Clan sponsor in sponsors)
        {
            if (!TryEvaluatePeaceCandidate(kingdom, sponsor, target, out PeaceCandidateSnapshot? candidate) || candidate == null)
            {
                continue;
            }

            if (candidate.Bias > bestScore)
            {
                bestScore = candidate.Bias;
                bestCandidate = candidate;
            }
        }

        return bestCandidate != null;
    }

    private static bool TryEvaluatePeaceCandidate(Kingdom kingdom, Clan sponsor, Kingdom enemy, out PeaceCandidateSnapshot? candidate)
    {
        candidate = null;

        DiplomacyModel diplomacy = Campaign.Current.Models.DiplomacyModel;
        if (sponsor == null || enemy == null || enemy.IsEliminated || enemy.RulingClan == null || !kingdom.IsAtWarWith(enemy) || diplomacy.IsAtConstantWar(kingdom, enemy))
        {
            return false;
        }

        float threshold = diplomacy.GetDecisionMakingThreshold(kingdom);
        float peaceScore = diplomacy.GetScoreOfDeclaringPeace(kingdom, enemy);
        if (peaceScore < threshold)
        {
            return false;
        }

        int tributeDurationInDays;
        int dailyTributeToPay = diplomacy.GetDailyTributeToPay(sponsor, enemy.RulingClan, out tributeDurationInDays);
        if (dailyTributeToPay < 0)
        {
            return false;
        }

        MakePeaceKingdomDecision decision = new(sponsor, enemy, dailyTributeToPay, tributeDurationInDays);
        if (!decision.CanMakeDecision(out _))
        {
            return false;
        }

        DecisionOutcome supportOutcome = decision
            .DetermineInitialCandidates()
            .First(outcome => outcome is MakePeaceKingdomDecision.MakePeaceDecisionOutcome peaceOutcome && peaceOutcome.ShouldPeaceBeDeclared);

        float support = decision.DetermineSupport(sponsor, supportOutcome);
        if (support <= 0f)
        {
            return false;
        }

        float likelihood = new KingdomElection(decision).GetLikelihoodForSponsor(sponsor);
        if (likelihood < MinimumPeaceLikelihood)
        {
            return false;
        }

        float candidateScore =
            (peaceScore - threshold) +
            support * 0.12f +
            likelihood * 28f +
            GetPeaceSelectionBias(kingdom, enemy);

        candidate = new PeaceCandidateSnapshot
        {
            Decision = decision,
            Sponsor = sponsor,
            PeaceScore = peaceScore,
            Threshold = threshold,
            Support = support,
            Likelihood = likelihood,
            Bias = candidateScore,
            TributePerDay = dailyTributeToPay
        };
        return true;
    }

    private bool TryAddBestPeaceDecision(Kingdom kingdom)
    {
        if (!TryGetBestPeaceCandidate(kingdom, out PeaceCandidateSnapshot? bestCandidate) || bestCandidate == null)
        {
            return false;
        }

        AddManagedDecision(kingdom, bestCandidate.Decision!);
        RFWarSystemTraceLog.PeaceProposal(
            kingdom,
            bestCandidate.Decision!.FactionToMakePeaceWith as Kingdom,
            bestCandidate.Sponsor!,
            bestCandidate.PeaceScore,
            bestCandidate.Threshold,
            bestCandidate.Support,
            bestCandidate.Likelihood,
            bestCandidate.Bias,
            bestCandidate.TributePerDay);
        return true;
    }

    private void AuditWarDecision(DeclareWarDecision decision)
    {
        Kingdom? kingdom = decision.Kingdom;
        Kingdom? currentTarget = decision.FactionToDeclareWarOn as Kingdom;
        Clan? sponsor = decision.ProposerClan;
        if (kingdom == null || currentTarget == null || sponsor == null)
        {
            return;
        }

        if (!TryGetBestWarCandidate(kingdom, out WarCandidateSnapshot? bestCandidate, out _, allowPendingDecision: true) || bestCandidate?.Decision == null)
        {
            return;
        }

        if (bestCandidate.Decision.FactionToDeclareWarOn == currentTarget)
        {
            return;
        }

        bool currentValid = TryEvaluateWarCandidate(kingdom, sponsor, currentTarget, out WarCandidateSnapshot? currentCandidate);
        float currentBias = currentCandidate?.Bias ?? float.MinValue;
        if (!ShouldReplaceProposal(kingdom, currentTarget, bestCandidate.Decision.FactionToDeclareWarOn as Kingdom, currentValid, currentBias, bestCandidate.Bias))
        {
            return;
        }

        ReplaceDecision(kingdom, decision, bestCandidate.Decision);
        RFWarSystemTraceLog.ProposalReplaced(
            "war",
            kingdom,
            currentTarget,
            bestCandidate.Decision.FactionToDeclareWarOn as Kingdom,
            currentBias,
            bestCandidate.Bias);
    }

    private void AuditPeaceDecision(MakePeaceKingdomDecision decision)
    {
        Kingdom? kingdom = decision.Kingdom;
        Kingdom? currentTarget = decision.FactionToMakePeaceWith as Kingdom;
        Clan? sponsor = decision.ProposerClan;
        if (kingdom == null || currentTarget == null || sponsor == null)
        {
            return;
        }

        if (!TryGetBestPeaceCandidate(kingdom, out PeaceCandidateSnapshot? bestCandidate, allowPendingDecision: true) || bestCandidate?.Decision == null)
        {
            return;
        }

        if (bestCandidate.Decision.FactionToMakePeaceWith == currentTarget)
        {
            return;
        }

        bool currentValid = TryEvaluatePeaceCandidate(kingdom, sponsor, currentTarget, out PeaceCandidateSnapshot? currentCandidate);
        float currentBias = currentCandidate?.Bias ?? float.MinValue;
        if (!ShouldReplaceProposal(kingdom, currentTarget, bestCandidate.Decision.FactionToMakePeaceWith as Kingdom, currentValid, currentBias, bestCandidate.Bias))
        {
            return;
        }

        ReplaceDecision(kingdom, decision, bestCandidate.Decision);
        RFWarSystemTraceLog.ProposalReplaced(
            "peace",
            kingdom,
            currentTarget,
            bestCandidate.Decision.FactionToMakePeaceWith as Kingdom,
            currentBias,
            bestCandidate.Bias);
    }

    private static bool ShouldReplaceProposal(Kingdom kingdom, Kingdom currentTarget, Kingdom? bestTarget, bool currentValid, float currentBias, float bestBias)
    {
        if (bestTarget == null)
        {
            return false;
        }

        if (!currentValid)
        {
            return true;
        }

        float biasLead = bestBias - currentBias;
        if (biasLead < ProposalReplacementBiasLead)
        {
            Kingdom? primaryEnemy = RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom);
            return primaryEnemy == bestTarget && primaryEnemy != currentTarget && biasLead >= ProposalReplacementBiasLead * 0.5f;
        }

        return true;
    }

    private void ReplaceDecision(Kingdom kingdom, KingdomDecision oldDecision, KingdomDecision newDecision)
    {
        // Only replace a decision that is GENUINELY PENDING. For AI kingdoms,
        // vanilla AddDecision runs the election synchronously right after the
        // KingdomDecisionAdded event (MEGA_010:243602) and never adds the
        // decision to UnresolvedDecisions — so RemoveDecision(old) is a no-op
        // and AddDecision(new) declares a SECOND war (double influence charge)
        // on top of the original that resolves the instant our handler returns.
        // Redirecting AI war targets is the score model's job (GetScoreOf-
        // DeclaringWar), not a reentrant decision swap.
        if (!kingdom.UnresolvedDecisions.Contains(oldDecision))
        {
            return;
        }

        _suppressDecisionAudit = true;
        try
        {
            kingdom.RemoveDecision(oldDecision);
            kingdom.AddDecision(newDecision);
            MarkProposal(kingdom);
        }
        finally
        {
            _suppressDecisionAudit = false;
        }
    }

    private void AddManagedDecision(Kingdom kingdom, KingdomDecision decision)
    {
        _suppressDecisionAudit = true;
        try
        {
            kingdom.AddDecision(decision);
        }
        finally
        {
            _suppressDecisionAudit = false;
        }
    }

    private bool TryPromoteSpecialWarProposalInternal(Kingdom kingdom, Kingdom target)
    {
        if (kingdom == null || target == null || !TryGetBestWarCandidateForTarget(kingdom, target, out WarCandidateSnapshot? candidate) || candidate?.Decision == null)
        {
            return false;
        }

        DeclareWarDecision? currentDecision = kingdom.UnresolvedDecisions.OfType<DeclareWarDecision>().FirstOrDefault();
        if (currentDecision != null)
        {
            Kingdom? currentTarget = currentDecision.FactionToDeclareWarOn as Kingdom;
            if (currentTarget == target)
            {
                return true;
            }

            if (currentTarget == null)
            {
                return false;
            }

            Clan? sponsor = currentDecision.ProposerClan;
            WarCandidateSnapshot? currentCandidate = null;
            bool currentValid = sponsor != null &&
                TryEvaluateWarCandidate(kingdom, sponsor, currentTarget, out currentCandidate);
            float currentBias = currentValid && currentCandidate != null ? currentCandidate.Bias : float.MinValue;
            if (!ShouldReplaceProposal(kingdom, currentTarget, target, currentValid, currentBias, candidate.Bias))
            {
                return false;
            }

            ReplaceDecision(kingdom, currentDecision, candidate.Decision);
            return true;
        }

        AddManagedDecision(kingdom, candidate.Decision);
        MarkProposal(kingdom);
        return true;
    }

    private bool TryPromoteSpecialPeaceProposalInternal(Kingdom kingdom, Kingdom target)
    {
        if (kingdom == null || target == null || !TryGetBestPeaceCandidateForTarget(kingdom, target, out PeaceCandidateSnapshot? candidate) || candidate?.Decision == null)
        {
            return false;
        }

        MakePeaceKingdomDecision? currentDecision = kingdom.UnresolvedDecisions.OfType<MakePeaceKingdomDecision>().FirstOrDefault();
        if (currentDecision != null)
        {
            Kingdom? currentTarget = currentDecision.FactionToMakePeaceWith as Kingdom;
            if (currentTarget == target)
            {
                return true;
            }

            if (currentTarget == null)
            {
                return false;
            }

            Clan? sponsor = currentDecision.ProposerClan;
            PeaceCandidateSnapshot? currentCandidate = null;
            bool currentValid = sponsor != null &&
                TryEvaluatePeaceCandidate(kingdom, sponsor, currentTarget, out currentCandidate);
            float currentBias = currentValid && currentCandidate != null ? currentCandidate.Bias : float.MinValue;
            if (!ShouldReplaceProposal(kingdom, currentTarget, target, currentValid, currentBias, candidate.Bias))
            {
                return false;
            }

            ReplaceDecision(kingdom, currentDecision, candidate.Decision);
            return true;
        }

        AddManagedDecision(kingdom, candidate.Decision);
        MarkProposal(kingdom);
        return true;
    }

    private static float GetWarSelectionBias(Kingdom attacker, Kingdom defender)
    {
        float bias = 0f;
        float frontierBias = GetFrontierBias(attacker, defender);
        float specialWarBias = GetSpecialWarBias(attacker, defender);
        float authorityBias = RFWarSpecialAuthorityBehavior.GetPendingWarPriority(attacker, defender);
        float sacredTargetBias = GetSacredTargetBias(attacker, defender);
        float primaryEnemyBias = GetPrimaryEnemyEscalationBias(attacker, defender);
        float enemyCollapseBias = GetEnemyCollapseOpportunityBias(attacker, defender);
        float activeDecisivePressure = RFWarCampaignDirectorBehavior.GetActiveDecisiveCampaignPressure(attacker);
        bias += 140f * RFWarStrategicIntent.GetWarIntentFactor(attacker, defender);
        bias += 120f * RFWarStrategicIntent.GetOpportunityWindow(attacker, defender);
        bias += 130f * RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(attacker, defender);
        bias += 90f * RFWarCampaignDirectorBehavior.GetCoalitionPullFactor(attacker, defender);
        bias += 110f * specialWarBias;
        bias += 160f * authorityBias;
        bias += 85f * sacredTargetBias;
        bias += 125f * primaryEnemyBias;
        bias += 135f * RFWarFrontEvaluator.GetWarFrontOpportunity(attacker, defender);
        bias += 120f * RFWarExternalFrontContext.GetEnemyPriority(attacker, defender);
        // Greed: the defender's resource zones within the attacker's reach
        // (provider installed by RF_ResourceZones; 0 when absent).
        bias += 115f * RFWarExternalFrontContext.GetResourceGreedFactor(attacker, defender);
        bias += 110f * frontierBias;
        bias += 90f * GetClaimBias(attacker, defender);
        bias += 80f * GetEnemyMultiFrontOpportunity(defender);
        bias += 110f * enemyCollapseBias;
        bias += 95f * GetBorderQualityBias(attacker, defender);
        bias += 100f * GetFrontBreakthroughBias(attacker, defender);
        bias += 105f * GetEnemyExposureBias(attacker, defender);
        bias += 85f * GetCoalitionReadinessBias(attacker, defender);
        bias += 70f * GetAttackerDoctrineTargetBias(attacker, defender, frontierBias, specialWarBias, sacredTargetBias);
        bias += 75f * GetStrategicPostureWarBias(attacker, defender, frontierBias, specialWarBias, sacredTargetBias);
        bias += 60f * GetRivalryBias(attacker, defender);
        bias -= 110f * GetOwnMultiFrontPressure(attacker);
        bias -= 125f * GetHomePressure(attacker);
        bias -= 115f * GetOffAxisWarPenalty(attacker, defender);
        bias -= 135f * RFWarCampaignDirectorBehavior.GetNewWarPenalty(attacker, defender);
        bias -= 150f * RFWarCampaignDirectorBehavior.GetActiveCampaignLockFactor(attacker);
        bias -= 105f * activeDecisivePressure;
        bias -= 85f * GetTreasuryDistress(attacker);
        return bias;
    }

    private static float GetPeaceSelectionBias(Kingdom attacker, Kingdom defender)
    {
        float bias = 0f;
        float authorityBias = RFWarSpecialAuthorityBehavior.GetPendingPeacePriority(attacker, defender);
        float collapseBias = GetEnemyCollapseOpportunityBias(attacker, defender);
        bias += 95f * GetNegativeMomentumBias(attacker, defender);
        bias -= 115f * Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(attacker, defender));
        bias -= 120f * GetSpecialWarBias(attacker, defender);
        bias -= 95f * GetSacredTargetBias(attacker, defender);
        bias += 110f * RFWarFrontEvaluator.GetPeacePressure(attacker, defender);
        bias += 105f * GetOwnMultiFrontPressure(attacker);
        bias += 130f * GetHomePressure(attacker);
        bias += 90f * GetTreasuryDistress(attacker);
        bias -= 100f * GetWarCommitmentBias(attacker, defender);
        bias -= 95f * RFWarCampaignDirectorBehavior.GetPeaceHoldFactor(attacker, defender);
        bias -= 105f * RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, defender);
        bias -= 105f * RFWarExternalFrontContext.GetEnemyPriority(attacker, defender);
        bias -= 70f * GetEnemyExposureBias(attacker, defender);
        bias -= 60f * GetCoalitionReadinessBias(attacker, defender);
        bias -= 70f * Math.Max(0f, RFWarStrategicIntent.GetWarIntentFactor(attacker, defender));
        bias -= 140f * collapseBias;
        bias -= 95f * GetLiveCampaignPeaceResistanceBias(attacker, defender);
        bias += 85f * GetCampaignContinuationPeaceBias(attacker, defender);
        bias += 165f * authorityBias;
        return bias;
    }

    private static float GetPrimaryEnemyEscalationBias(Kingdom attacker, Kingdom defender)
    {
        if (RFWarCampaignDirectorBehavior.GetPrimaryEnemy(attacker) != defender)
        {
            return 0f;
        }

        float focus = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(attacker, defender));
        float lockFactor = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, defender));
        float decisivePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(attacker, defender));
        float externalPriority = Math.Max(0f, RFWarExternalFrontContext.GetEnemyPriority(attacker, defender));
        float coalitionRole = Math.Max(0f, RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(attacker, defender));
        float unresolvedFront = Math.Max(0f, RFWarCampaignDirectorBehavior.GetUnresolvedFrontPressure(attacker, defender));
        return Math.Min(1f, focus * 0.38f + lockFactor * 0.22f + unresolvedFront * 0.16f + decisivePressure * 0.18f + externalPriority * 0.14f + coalitionRole * 0.1f);
    }

    private static float GetEnemyCollapseOpportunityBias(Kingdom attacker, Kingdom defender)
    {
        if (!defender.Fiefs.Any())
        {
            return 1f;
        }

        float lowFiefBias = defender.Fiefs.Count switch
        {
            <= 1 => 0.85f,
            2 => 0.62f,
            3 => 0.38f,
            4 => 0.18f,
            _ => 0f
        };

        float siegeBias = defender.Fiefs.Count(town => town.Settlement != null && town.Settlement.IsUnderSiege) / (float)Math.Max(1, defender.Fiefs.Count());
        float pressureBias = Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(defender) / 2.4f);
        float enemyDistraction = GetEnemyMultiFrontOpportunity(defender);
        float exposure = Math.Max(0f, GetEnemyExposureBias(attacker, defender));
        float focusedEnemyBias = RFWarCampaignDirectorBehavior.GetPrimaryEnemy(attacker) == defender
            ? Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, defender)) * 0.18f
            : 0f;
        float continuationBias = RFWarCampaignPhaseBehavior.GetPhase(attacker, defender) switch
        {
            RFWarCampaignPhase.PressCastle => 0.12f,
            RFWarCampaignPhase.PressTown => 0.18f,
            RFWarCampaignPhase.DeepStrike => 0.08f,
            _ => 0f
        };
        return Math.Min(1f, lowFiefBias + siegeBias * 0.24f + pressureBias * 0.18f + enemyDistraction * 0.14f + exposure * 0.18f + focusedEnemyBias + continuationBias);
    }

    private static float GetOffAxisWarPenalty(Kingdom attacker, Kingdom defender)
    {
        Kingdom? primaryEnemy = RFWarCampaignDirectorBehavior.GetPrimaryEnemy(attacker);
        if (primaryEnemy == null || primaryEnemy == defender || !attacker.IsAtWarWith(primaryEnemy))
        {
            return 0f;
        }

        float focus = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyFocusFactor(attacker, primaryEnemy));
        float lockFactor = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, primaryEnemy));
        float unresolvedFront = Math.Max(0f, RFWarCampaignDirectorBehavior.GetUnresolvedFrontPressure(attacker, primaryEnemy));
        float decisivePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(attacker, primaryEnemy));
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(attacker, primaryEnemy);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(attacker, primaryEnemy);
        float operationalCommitment = RFWarOperationalRhythmBehavior.GetState(attacker, primaryEnemy) switch
        {
            RFWarOperationalState.Besiege => 0.32f,
            RFWarOperationalState.Advance => 0.24f,
            RFWarOperationalState.Exploit => 0.18f,
            RFWarOperationalState.Defend => 0.16f,
            _ => 0f
        };

        return Math.Min(1f, focus * 0.22f + lockFactor * 0.24f + unresolvedFront * 0.16f + decisivePressure * 0.18f + frontCommitment * 0.14f + objectiveCommitment * 0.14f + operationalCommitment);
    }

    private static float GetLiveCampaignPeaceResistanceBias(Kingdom attacker, Kingdom defender)
    {
        float lockFactor = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCampaignLockFactor(attacker, defender));
        float unresolvedFront = Math.Max(0f, RFWarCampaignDirectorBehavior.GetUnresolvedFrontPressure(attacker, defender));
        float decisivePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetDecisiveCampaignPressure(attacker, defender));
        float collapsePressure = Math.Max(0f, RFWarCampaignDirectorBehavior.GetEnemyCollapseFactor(attacker, defender));
        float frontCommitment = RFWarFrontlineBehavior.GetFrontCommitmentFactor(attacker, defender);
        float objectiveCommitment = RFWarObjectiveChainBehavior.GetObjectiveCommitmentFactor(attacker, defender);
        float theaterCommitment = RFWarTheaterBehavior.GetFocusMode(attacker, defender) switch
        {
            RFWarTheaterFocusMode.OffensivePush => 0.18f,
            RFWarTheaterFocusMode.HomelandDefense => 0.08f,
            _ => 0f
        };
        float phaseCommitment = RFWarCampaignPhaseBehavior.GetPhase(attacker, defender) switch
        {
            RFWarCampaignPhase.BreakFront => 0.16f,
            RFWarCampaignPhase.PressCastle => 0.22f,
            RFWarCampaignPhase.PressTown => 0.28f,
            RFWarCampaignPhase.StripSupport => 0.12f,
            RFWarCampaignPhase.DeepStrike => 0.08f,
            _ => 0f
        };
        float operationalCommitment = RFWarOperationalRhythmBehavior.GetState(attacker, defender) switch
        {
            RFWarOperationalState.Besiege => 0.28f,
            RFWarOperationalState.Advance => 0.18f,
            RFWarOperationalState.Exploit => 0.1f,
            _ => 0f
        };

        return Math.Min(1f, lockFactor * 0.22f + unresolvedFront * 0.16f + decisivePressure * 0.18f + collapsePressure * 0.28f + frontCommitment * 0.14f + objectiveCommitment * 0.16f + theaterCommitment + phaseCommitment + operationalCommitment);
    }

    private static float GetStrategicPostureWarBias(Kingdom attacker, Kingdom defender, float frontierBias, float specialWarBias, float sacredTargetBias)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float borderQuality = Math.Max(0f, GetBorderQualityBias(attacker, defender));
        float exposure = Math.Max(0f, GetEnemyExposureBias(attacker, defender));
        float coalitionReadiness = Math.Max(0f, GetCoalitionReadinessBias(attacker, defender));
        float specialContext = Math.Max(specialWarBias, sacredTargetBias);
        float homePressure = GetHomePressure(attacker);

        float spearheadPosture =
            Math.Max(0f, profile.OffensiveDrive) * 0.28f +
            Math.Max(0f, profile.SiegePreference) * borderQuality * 0.22f +
            Math.Max(0f, profile.Opportunism) * exposure * 0.24f +
            Math.Max(0f, profile.CoalitionLoyalty) * coalitionReadiness * 0.14f +
            Math.Max(0f, profile.SacredZeal) * specialContext * 0.18f;

        float shieldPosture =
            Math.Max(0f, profile.DefensiveDiscipline) * 0.16f +
            Math.Max(0f, profile.HomeGuardBias) * homePressure * 0.24f +
            Math.Max(0f, profile.FrontierParanoia) * Math.Max(0f, frontierBias) * 0.18f +
            Math.Max(0f, profile.Caution) * (GetOwnMultiFrontPressure(attacker) * 0.16f + homePressure * 0.18f);

        return Math.Max(-1f, Math.Min(1f, spearheadPosture - shieldPosture));
    }

    private static float GetCampaignContinuationPeaceBias(Kingdom attacker, Kingdom defender)
    {
        RFWarCampaignPhase phase = RFWarCampaignPhaseBehavior.GetPhase(attacker, defender);
        float phaseBias = phase switch
        {
            RFWarCampaignPhase.BreakFront => -0.18f,
            RFWarCampaignPhase.StripSupport => -0.08f,
            RFWarCampaignPhase.PressCastle => -0.2f,
            RFWarCampaignPhase.PressTown => -0.26f,
            RFWarCampaignPhase.DeepStrike => -0.1f,
            RFWarCampaignPhase.Stabilize => 0.2f,
            _ => 0f
        };

        float operationalBias = RFWarOperationalRhythmBehavior.GetState(attacker, defender) switch
        {
            RFWarOperationalState.Besiege => -0.22f,
            RFWarOperationalState.Advance => -0.12f,
            RFWarOperationalState.Exploit => -0.08f,
            RFWarOperationalState.Defend => 0.08f,
            RFWarOperationalState.Regroup => 0.18f,
            _ => 0f
        };

        float coalitionRoleBias = RFWarCoalitionRoleBehavior.GetCampaignRoleFactor(attacker, defender) switch
        {
            > 0f => -0.12f,
            < 0f => 0.1f,
            _ => 0f
        };
        float collapseBias = -GetEnemyCollapseOpportunityBias(attacker, defender) * 0.62f;
        return Math.Max(-1f, Math.Min(1f, phaseBias + operationalBias + coalitionRoleBias + collapseBias));
    }

    private static float GetEnemyMultiFrontOpportunity(Kingdom kingdom)
    {
        int warCount = kingdom.FactionsAtWarWith.OfType<Kingdom>().Count();
        return Math.Min(1f, Math.Max(0, warCount - 1) / 3f);
    }

    private static float GetOwnMultiFrontPressure(Kingdom kingdom)
    {
        int warCount = kingdom.FactionsAtWarWith.OfType<Kingdom>().Count();
        return Math.Min(1f, Math.Max(0, warCount - 1) / 3f);
    }

    private static float GetHomePressure(Kingdom kingdom)
    {
        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetHomeFrontPressure(kingdom) / 2.5f);
    }

    private static float GetRivalryBias(Kingdom attacker, Kingdom defender)
    {
        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetRivalry(attacker, defender) / 2.5f);
    }

    private static float GetWarCommitmentBias(Kingdom attacker, Kingdom defender)
    {
        return Math.Min(1f, RFWarStrategicMemoryBehavior.GetWarCommitment(attacker, defender));
    }

    private static float GetNegativeMomentumBias(Kingdom attacker, Kingdom defender)
    {
        return Math.Min(1f, Math.Max(0f, -RFWarStrategicMemoryBehavior.GetMomentum(attacker, defender)) / 2.2f);
    }

    private static float GetTreasuryDistress(Kingdom kingdom)
    {
        float gold = kingdom.RulingClan?.Gold ?? 0f;
        if (gold <= 8000f)
        {
            return 1f;
        }

        if (gold >= 50000f)
        {
            return 0f;
        }

        return 1f - ((gold - 8000f) / 42000f);
    }

    private static float GetClaimBias(Kingdom attacker, Kingdom defender)
    {
        string attackerCulture = attacker.Culture?.StringId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(attackerCulture))
        {
            return 0f;
        }

        int sharedCultureFiefs = defender.Fiefs.Count(town =>
            string.Equals(town.Settlement.Culture?.StringId, attackerCulture, StringComparison.OrdinalIgnoreCase));

        if (sharedCultureFiefs <= 0)
        {
            return 0f;
        }

        return Math.Min(1f, sharedCultureFiefs / 3f);
    }

    private static float GetFrontierBias(Kingdom attacker, Kingdom defender)
    {
        if (!attacker.Fiefs.Any() || !defender.Fiefs.Any())
        {
            return -0.25f;
        }

        float minDistanceSquared = float.MaxValue;
        foreach (Town ownTown in attacker.Fiefs)
        {
            foreach (Town enemyTown in defender.Fiefs)
            {
                float distanceSquared = ownTown.Settlement.GatePosition.DistanceSquared(enemyTown.Settlement.GatePosition);
                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                }
            }
        }

        float distance = (float)Math.Sqrt(minDistanceSquared);
        if (distance <= 120f)
        {
            return 1f;
        }

        if (distance >= 360f)
        {
            return -0.6f;
        }

        return Math.Max(-0.6f, Math.Min(1f, 0.6f - ((distance - 120f) / 240f)));
    }

    private static float GetBorderQualityBias(Kingdom attacker, Kingdom defender)
    {
        if (!attacker.Fiefs.Any() || !defender.Fiefs.Any())
        {
            return 0f;
        }

        float score = 0f;
        int contacts = 0;

        foreach (Town ownTown in attacker.Fiefs)
        {
            Settlement? ownSettlement = ownTown?.Settlement;
            if (ownSettlement == null)
            {
                continue;
            }

            foreach (Town enemyTown in defender.Fiefs)
            {
                Settlement? enemySettlement = enemyTown?.Settlement;
                if (enemySettlement == null)
                {
                    continue;
                }

                float distanceSquared = ownSettlement.GatePosition.DistanceSquared(enemySettlement.GatePosition);
                if (distanceSquared > 32400f)
                {
                    continue;
                }

                contacts++;
                float local = enemySettlement.IsTown ? 0.4f : enemySettlement.IsCastle ? 0.3f : 0.18f;
                local += GetSettlementSoftness(enemySettlement) * 0.35f;
                local += IsSameStrategicCulture(attacker.Culture?.StringId, enemySettlement.Culture?.StringId) ? 0.2f : 0f;
                score += local;
            }
        }

        if (contacts == 0)
        {
            return 0f;
        }

        return Math.Min(1f, score / Math.Max(1f, contacts * 0.75f));
    }

    private static float GetEnemyExposureBias(Kingdom attacker, Kingdom defender)
    {
        if (!defender.Fiefs.Any())
        {
            return 0f;
        }

        float score = 0f;
        int considered = 0;

        foreach (Town enemyTown in defender.Fiefs)
        {
            Settlement? settlement = enemyTown?.Settlement;
            if (settlement == null)
            {
                continue;
            }

            if (!IsFrontierRelevant(attacker, settlement))
            {
                continue;
            }

            considered++;
            float local = GetSettlementSoftness(settlement) * 0.5f;
            local += Math.Max(0f, RFWarStrategicMemoryBehavior.GetSettlementHeat(attacker, settlement) / 2.8f) * 0.22f;
            local += settlement.IsUnderSiege ? 0.2f : 0f;
            score += local;
        }

        if (considered == 0)
        {
            float threatened = defender.Fiefs.Count(town => town?.Settlement != null && town.Settlement.IsUnderSiege) / (float)Math.Max(1, defender.Fiefs.Count());
            return Math.Min(1f, threatened * 0.75f);
        }

        float average = score / considered;
        float overstretch = GetEnemyMultiFrontOpportunity(defender) * 0.22f;
        return Math.Min(1f, average + overstretch);
    }

    private static float GetFrontBreakthroughBias(Kingdom attacker, Kingdom defender)
    {
        if (!attacker.Fiefs.Any() || !defender.Fiefs.Any())
        {
            return 0f;
        }

        float total = 0f;
        float best = 0f;
        int considered = 0;

        foreach (Town enemyTown in defender.Fiefs)
        {
            Settlement? settlement = enemyTown?.Settlement;
            if (settlement == null || !IsFrontierRelevant(attacker, settlement))
            {
                continue;
            }

            considered++;
            float local = settlement.IsTown ? 0.42f : settlement.IsCastle ? 0.3f : 0.16f;
            local += GetSettlementSoftness(settlement) * 0.32f;
            local += settlement.IsUnderSiege ? 0.14f : 0f;
            local += IsSameStrategicCulture(attacker.Culture?.StringId, settlement.Culture?.StringId) ? 0.12f : 0f;

            int adjacentEnemyFiefs = 0;
            bool directBorderContact = false;
            foreach (Town ownTown in attacker.Fiefs)
            {
                Settlement? ownSettlement = ownTown?.Settlement;
                if (ownSettlement == null)
                {
                    continue;
                }

                float ownDistanceSquared = ownSettlement.GatePosition.DistanceSquared(settlement.GatePosition);
                if (ownDistanceSquared <= 19600f)
                {
                    directBorderContact = true;
                }
            }

            foreach (Town otherEnemyTown in defender.Fiefs)
            {
                Settlement? otherSettlement = otherEnemyTown?.Settlement;
                if (otherSettlement == null || otherSettlement == settlement)
                {
                    continue;
                }

                float distanceSquared = otherSettlement.GatePosition.DistanceSquared(settlement.GatePosition);
                if (distanceSquared <= 22500f)
                {
                    adjacentEnemyFiefs++;
                }
            }

            if (directBorderContact)
            {
                local += 0.16f;
            }

            local += Math.Min(2, adjacentEnemyFiefs) * 0.1f;
            local = Math.Min(1f, local);

            total += local;
            if (local > best)
            {
                best = local;
            }
        }

        if (considered == 0)
        {
            return 0f;
        }

        float average = total / considered;
        return Math.Min(1f, best * 0.68f + average * 0.4f);
    }

    private static float GetCoalitionReadinessBias(Kingdom attacker, Kingdom defender)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float coalitionPull = Math.Max(0f, RFWarCampaignDirectorBehavior.GetCoalitionPullFactor(attacker, defender));
        if (coalitionPull <= 0f && profile.CoalitionLoyalty <= 0f)
        {
            return 0f;
        }

        int alignedPeers = 0;
        int convergingPeers = 0;
        foreach (Kingdom otherKingdom in Kingdom.All)
        {
            if (otherKingdom == null || otherKingdom == attacker || otherKingdom.IsEliminated || otherKingdom.Culture == null)
            {
                continue;
            }

            if (!AreKingdomsStrategicallyAligned(attacker, otherKingdom))
            {
                continue;
            }

            alignedPeers++;
            if (otherKingdom.IsAtWarWith(defender) || RFWarCampaignDirectorBehavior.GetPrimaryEnemy(otherKingdom) == defender)
            {
                convergingPeers++;
            }
        }

        float convergence = alignedPeers == 0 ? 0f : convergingPeers / (float)alignedPeers;
        return Math.Min(1f, coalitionPull * 0.6f + convergence * Math.Max(0f, profile.CoalitionLoyalty + 0.2f));
    }

    private static float GetAttackerDoctrineTargetBias(Kingdom attacker, Kingdom defender, float frontierBias, float specialWarBias, float sacredTargetBias)
    {
        RFWarStrategicProfile profile = RFWarStrategicProfiles.Get(attacker);
        float value = 0f;
        value += Math.Max(0f, profile.Opportunism) * GetEnemyExposureBias(attacker, defender) * 0.45f;
        value += Math.Max(0f, profile.FrontierParanoia) * Math.Max(0f, frontierBias) * 0.35f;
        value += Math.Max(0f, profile.SacredZeal) * Math.Max(specialWarBias, sacredTargetBias) * 0.45f;
        value += Math.Max(0f, profile.RevengeBias) * GetRivalryBias(attacker, defender) * 0.25f;
        return Math.Min(1f, value);
    }

    private static float GetSpecialWarBias(Kingdom attacker, Kingdom defender)
    {
        float holy = Math.Max(0f, RFWarExternalFrontContext.GetHolyWarPressure(attacker, defender));
        float defense = Math.Max(0f, RFWarExternalFrontContext.GetCollectiveDefensePressure(attacker, defender));
        float alignment = Math.Max(0f, RFWarExternalFrontContext.GetAlignmentWarPressure(attacker, defender));
        return Math.Min(1f, Math.Max(holy, Math.Max(defense * 1.05f, alignment * 0.92f)));
    }

    private static float GetSacredTargetBias(Kingdom attacker, Kingdom defender)
    {
        if (!defender.Fiefs.Any())
        {
            return 0f;
        }

        float best = 0f;
        foreach (Town town in defender.Fiefs)
        {
            Settlement? settlement = town?.Settlement;
            if (settlement == null)
            {
                continue;
            }

            best = Math.Max(best, Math.Max(0f, RFWarExternalFrontContext.GetSacredTargetFactor(attacker, defender, settlement)));
        }

        return Math.Min(1f, best);
    }

    private static float GetSettlementSoftness(Settlement settlement)
    {
        int garrison = settlement.Town?.GarrisonParty?.Party.NumberOfHealthyMembers ?? 0;
        int militia = (int)settlement.Militia;
        int defenders = garrison + militia;
        int threshold = settlement.IsTown ? 220 : settlement.IsCastle ? 140 : 60;
        if (defenders >= threshold)
        {
            return 0f;
        }

        return Math.Min(1f, Math.Max(0f, (threshold - defenders) / (float)threshold));
    }

    private static bool IsFrontierRelevant(Kingdom attacker, Settlement enemySettlement)
    {
        foreach (Town ownTown in attacker.Fiefs)
        {
            Settlement? ownSettlement = ownTown?.Settlement;
            if (ownSettlement == null)
            {
                continue;
            }

            if (ownSettlement.GatePosition.DistanceSquared(enemySettlement.GatePosition) <= 57600f)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameStrategicCulture(string? sourceCultureId, string? targetCultureId)
    {
        if (string.IsNullOrWhiteSpace(sourceCultureId) || string.IsNullOrWhiteSpace(targetCultureId))
        {
            return false;
        }

        if (string.Equals(sourceCultureId, targetCultureId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        bool sourceImperial = sourceCultureId.StartsWith("empire", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceCultureId, "south_realm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceCultureId, "west_realm", StringComparison.OrdinalIgnoreCase);
        bool targetImperial = targetCultureId.StartsWith("empire", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetCultureId, "empire", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetCultureId, "south_realm", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetCultureId, "west_realm", StringComparison.OrdinalIgnoreCase);
        return sourceImperial && targetImperial;
    }

    private static bool AreKingdomsStrategicallyAligned(Kingdom left, Kingdom right)
    {
        // Alignment blocs (good/evil) only exist after the quest-driven global
        // alignment war starts; before that no coalition-convergence bonus.
        if (!Logic.RFWarExternalFrontContext.AlignmentDoctrineActive)
        {
            return false;
        }

        if (left.Culture == null || right.Culture == null)
        {
            return false;
        }

        bool leftGood = IsGoodCulture(left.Culture.StringId);
        bool rightGood = IsGoodCulture(right.Culture.StringId);
        bool leftEvil = IsEvilCulture(left.Culture.StringId);
        bool rightEvil = IsEvilCulture(right.Culture.StringId);
        return (leftGood && rightGood) || (leftEvil && rightEvil);
    }

    private static bool IsGoodCulture(string? cultureId)
    {
        return string.Equals(cultureId, "battania", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cultureId, "giant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cultureId, "dwarf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cultureId, "grimwatch", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEvilCulture(string? cultureId)
    {
        return string.Equals(cultureId, "sturgia", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cultureId, "urkhai", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cultureId, "aserai", StringComparison.OrdinalIgnoreCase)
            || string.Equals(cultureId, "mage", StringComparison.OrdinalIgnoreCase);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.ClanAlignment;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.InciteBreak;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.RumorCampaigns;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.SecretPacts;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Campaign;

public sealed class StrategicIntrigueCampaignBehavior : CampaignBehaviorBase
{
    private enum CrackdownPunishmentOutcome
    {
        None,
        Exile,
        Imprisonment,
        Execution
    }

    private Dictionary<Clan, ClanIntrigueState> _clanStates = new();
    private Dictionary<Kingdom, KingdomIntrigueState> _kingdomStates = new();
    private List<SecretPact> _secretPacts = new();
    private List<SecretAllianceCompact> _secretAlliances = new();
    private List<IntrigueOperation> _pendingOperations = new();
    private bool _isInitialized;

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, OnDailyTickClan);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
        CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
        CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
        CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
        CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, OnClanInfluenceChanged);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, OnTownRebelliosStateChanged);
        CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_clanStates", ref _clanStates);
        dataStore.SyncData("_kingdomStates", ref _kingdomStates);
        dataStore.SyncData("_secretPacts", ref _secretPacts);
        dataStore.SyncData("_secretAlliances", ref _secretAlliances);
        dataStore.SyncData("_pendingOperations", ref _pendingOperations);
        dataStore.SyncData("_isInitialized", ref _isInitialized);
    }

    public ClanIntrigueState GetState(Clan clan)
    {
        if (clan == null)
        {
            return null;
        }

        EnsureInitialized();
        return _clanStates.TryGetValue(clan, out ClanIntrigueState state) ? state : null;
    }

    public KingdomIntrigueState GetKingdomState(Kingdom kingdom)
    {
        if (kingdom == null)
        {
            return null;
        }

        EnsureInitialized();
        return _kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState state) ? state : null;
    }

    public bool HasActivePact(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _secretPacts.Any(x => !x.IsExposed && x.MemberClan == clan);
    }

    public bool HasPlayerPact(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _secretPacts.Any(x => !x.IsExposed && x.MemberClan == clan && x.SponsorClan == Clan.PlayerClan);
    }

    public IntriguePactGoal? GetActivePactGoal(Clan clan)
    {
        EnsureInitialized();
        SecretPact pact = clan == null ? null : _secretPacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == clan);
        return pact?.Goal;
    }

    public bool HasPendingRumorCampaign(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _pendingOperations.Any(x =>
            x.Status == IntrigueOperationStatus.Pending
            && x.TargetClan == clan
            && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
    }

    public bool HasPendingPlayerRumorCampaign(Clan clan)
    {
        EnsureInitialized();
        return clan != null && _pendingOperations.Any(x =>
            x.Status == IntrigueOperationStatus.Pending
            && x.InstigatorClan == Clan.PlayerClan
            && x.TargetClan == clan
            && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
    }

    public float? GetPendingRumorDaysRemaining(Clan clan)
    {
        EnsureInitialized();
        IntrigueOperation operation = clan == null
            ? null
            : _pendingOperations.FirstOrDefault(x =>
                x.Status == IntrigueOperationStatus.Pending
                && x.TargetClan == clan
                && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
        if (operation == null)
        {
            return null;
        }

        return MathF.Max(0f, (float)(operation.ResolveAt - CampaignTime.Now).ToDays);
    }

    public float? GetPendingPlayerRumorDaysRemaining(Clan clan)
    {
        EnsureInitialized();
        IntrigueOperation operation = clan == null
            ? null
            : _pendingOperations.FirstOrDefault(x =>
                x.Status == IntrigueOperationStatus.Pending
                && x.InstigatorClan == Clan.PlayerClan
                && x.TargetClan == clan
                && (x.Type == IntrigueOperationType.RumorCampaign || x.Type == IntrigueOperationType.SponsorDissidence));
        if (operation == null)
        {
            return null;
        }

        return MathF.Max(0f, (float)(operation.ResolveAt - CampaignTime.Now).ToDays);
    }

    public bool TryCreateSecretPact(Clan sponsorClan, Clan memberClan, out TextObject reason)
    {
        EnsureInitialized();
        return ApplySecretPactAction.TryApply(_clanStates, _secretPacts, sponsorClan, memberClan, out reason);
    }

    public SecretAllianceCompact GetPlayerAllianceWithClan(Clan clan)
    {
        EnsureInitialized();
        return _secretAlliances.FirstOrDefault(x =>
            IsAllianceActive(x)
            && x.InstigatorClan == Clan.PlayerClan
            && x.AllyClan == clan
            && x.SupportedClan == clan);
    }

    public SecretAllianceCompact GetPlayerForeignAllianceWithClan(Clan clan)
    {
        EnsureInitialized();
        return _secretAlliances.FirstOrDefault(x =>
            IsAllianceActive(x)
            && x.InstigatorClan == Clan.PlayerClan
            && x.AllyClan == clan
            && x.SupportedClan != clan);
    }

    public Clan GetPreferredPlayerConspiracyTargetForForeignAlly(Clan allyClan)
    {
        EnsureInitialized();
        return GetPreferredPlayerConspiracyTarget(allyClan?.Kingdom);
    }

    public bool TryCreatePlayerAlliance(Clan memberClan, out TextObject reason, out SecretAllianceCompact alliance)
    {
        EnsureInitialized();
        alliance = null;
        reason = TextObject.GetEmpty();

        if (memberClan?.Kingdom == null || memberClan == memberClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=rf_si_alliance_invalid_target}This lord cannot bind a meaningful alliance here.");
            return false;
        }

        SecretPact pact = GetActivePact(memberClan);
        if (pact == null || pact.SponsorClan != Clan.PlayerClan)
        {
            reason = new TextObject("{=rf_si_alliance_requires_pact}A firmer secret understanding must come first.");
            return false;
        }

        if (GetPlayerAllianceWithClan(memberClan) != null)
        {
            reason = new TextObject("{=rf_si_alliance_already_exists}You already have terms with this conspirator.");
            return false;
        }

        if (!_clanStates.TryGetValue(memberClan, out ClanIntrigueState state)
            || state.TrustToPlayer < StrategicIntrigueConstants.AllianceTrustThreshold)
        {
            reason = new TextObject("{=rf_si_alliance_low_trust}They are not ready to tie hard promises to your cause.");
            return false;
        }

        Settlement promisedSettlement = ChoosePromisedSettlement(memberClan.Kingdom, memberClan, Clan.PlayerClan);
        if (promisedSettlement == null)
        {
            reason = new TextObject("{=rf_si_alliance_no_settlement}There is no credible prize to pledge from this realm right now.");
            return false;
        }

        alliance = new SecretAllianceCompact(
            Clan.PlayerClan,
            memberClan,
            memberClan,
            memberClan.Kingdom,
            MapAllianceObjective(pact.Goal),
            IntrigueAllianceRewardType.PromisedSettlement,
            promisedSettlement,
            Clan.PlayerClan,
            CampaignTime.DaysFromNow(StrategicIntrigueConstants.AllianceDurationDays));
        _secretAlliances.Add(alliance);
        pact.Commitment += 10f;
        pact.Secrecy -= 4f;
        pact.ClampValues();
        state.TrustToPlayer += 6f;
        state.ClampValues();
        return true;
    }

    public bool TryCreateForeignAlliance(Clan allyClan, out TextObject reason, out SecretAllianceCompact alliance)
    {
        EnsureInitialized();
        alliance = null;
        reason = TextObject.GetEmpty();

        if (allyClan?.Kingdom == null || allyClan != allyClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_invalid}Only a ruling clan can promise the weight of a foreign realm.");
            return false;
        }

        if (allyClan == Clan.PlayerClan || allyClan.Kingdom == Clan.PlayerClan?.Kingdom)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_same_realm}That is not a foreign hand to bargain with.");
            return false;
        }

        if (GetPlayerForeignAllianceWithClan(allyClan) != null)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_exists}You already have a discreet understanding with this ruler.");
            return false;
        }

        Clan targetClan = GetPreferredPlayerConspiracyTarget(allyClan.Kingdom);
        SecretPact pact = GetActivePact(targetClan);
        if (targetClan?.Kingdom == null || pact?.SponsorClan != Clan.PlayerClan)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_no_target}You have no live conspiracy worth selling to a foreign ruler.");
            return false;
        }

        Settlement promisedSettlement = ChoosePromisedSettlement(targetClan.Kingdom, targetClan, allyClan);
        if (promisedSettlement == null)
        {
            reason = new TextObject("{=rf_si_foreign_alliance_no_prize}There is no believable border prize to offer for outside help.");
            return false;
        }

        alliance = new SecretAllianceCompact(
            Clan.PlayerClan,
            allyClan,
            targetClan,
            targetClan.Kingdom,
            IntrigueAllianceObjective.ForeignIntervention,
            IntrigueAllianceRewardType.PromisedSettlement,
            promisedSettlement,
            allyClan,
            CampaignTime.DaysFromNow(StrategicIntrigueConstants.AllianceDurationDays));
        _secretAlliances.Add(alliance);

        if (_clanStates.TryGetValue(targetClan, out ClanIntrigueState targetState))
        {
            targetState.SoftDefectionPressure += 6f;
            targetState.ClaimantAmbition += 4f;
            targetState.ClampValues();
        }

        return true;
    }

    public bool TryStartRumorCampaign(Clan instigatorClan, Clan targetClan, out TextObject reason, float power = 30f, float risk = 25f)
    {
        EnsureInitialized();
        reason = TextObject.GetEmpty();

        if (instigatorClan == null || targetClan == null || targetClan.Kingdom?.RulingClan == null)
        {
            reason = new TextObject("{=si_invalid_rumor_target}There is no valid intrigue target here.");
            return false;
        }

        if (targetClan == targetClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=si_rumor_target_ruler}You need a dissatisfied vassal, not the ruler directly.");
            return false;
        }

        if (!_clanStates.TryGetValue(targetClan, out ClanIntrigueState state))
        {
            reason = new TextObject("{=si_missing_intrigue_state}This clan has no intrigue state yet.");
            return false;
        }

        if (HasPendingRumorCampaign(targetClan))
        {
            reason = new TextObject("{=si_existing_rumor_campaign}A rumor campaign is already underway for this clan.");
            return false;
        }

        if (state.Dissidence < 45f)
        {
            reason = new TextObject("{=si_rumor_not_needed}This clan is not unsettled enough for whispers to spread.");
            return false;
        }

        if (state.TrustToPlayer < StrategicIntrigueConstants.RumorCampaignTrustThreshold)
        {
            reason = new TextObject("{=si_rumor_low_trust}They do not trust your hand in this matter yet.");
            return false;
        }

        _pendingOperations.Add(new IntrigueOperation(
            IntrigueOperationType.RumorCampaign,
            instigatorClan,
            targetClan,
            targetClan.Kingdom.RulingClan,
            power,
            risk,
            CampaignTime.DaysFromNow(3f)));
        return true;
    }

    public bool TryInciteBreak(Clan targetClan, out TextObject reason, out IntrigueBreakOutcome outcome)
    {
        EnsureInitialized();
        reason = TextObject.GetEmpty();
        outcome = IntrigueBreakOutcome.None;

        if (targetClan?.Kingdom == null || targetClan == targetClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=si_invalid_break_target}This clan cannot be pushed into open rebellion.");
            return false;
        }

        if (!HasActivePact(targetClan))
        {
            reason = new TextObject("{=si_missing_pact_break}You need a secret pact before attempting a break.");
            return false;
        }

        if (!_clanStates.TryGetValue(targetClan, out ClanIntrigueState state))
        {
            reason = new TextObject("{=si_missing_intrigue_state}This clan has no intrigue state yet.");
            return false;
        }

        if (!state.IsBreakawayReady)
        {
            reason = new TextObject("{=si_breakaway_not_ready}The clan is not angry enough to abandon the realm.");
            return false;
        }

        if (state.TrustToPlayer < StrategicIntrigueConstants.SecretPactTrustThreshold)
        {
            reason = new TextObject("{=si_breakaway_low_trust}They still distrust your timing.");
            return false;
        }

        IntriguePactGoal? goal = GetActivePactGoal(targetClan);
        if (goal == IntriguePactGoal.SupportFutureClaimant && !CanTriggerClaimantCoup(targetClan, out reason))
        {
            return false;
        }

        outcome = ApplyInciteBreakAction.Apply(_clanStates, _kingdomStates, _secretPacts, targetClan);
        if (outcome == IntrigueBreakOutcome.None)
        {
            reason = new TextObject("{=si_breakaway_failed}The breakaway attempt fizzled before it began.");
            return false;
        }

        ProcessAllianceBreakOutcome(targetClan, outcome);
        return true;
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        EnsureInitialized();
    }

    private void OnDailyTickClan(Clan clan)
    {
        if (!IsValidIntrigueClan(clan))
        {
            return;
        }

        EnsureInitialized();
        RefreshKingdomState(clan.Kingdom);

        ClanIntrigueState state = GetOrCreateState(clan);
        RefreshTrustToPlayer(state);
        RefreshDerivedClanState(state);
        ClanAlignmentService.Recalculate(state, GetOrCreateKingdomState(clan.Kingdom));
    }

    private void OnDailyTick()
    {
        EnsureInitialized();
        Dictionary<Clan, bool> breakawayReadinessBeforeTick = CaptureActivePactBreakawayReadiness();
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (IsValidIntrigueKingdom(kingdom))
            {
                RefreshKingdomState(kingdom);
            }
        }

        foreach (Clan clan in Clan.All)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            ClanAlignmentService.Recalculate(state, GetOrCreateKingdomState(clan.Kingdom));
        }

        DecayDailyValues();
        ProcessRulerCountermoves();
        ScheduleAutomaticEscalations();
        List<IntrigueOperationResolution> resolutions = IntrigueOperationResolver.ResolveDueOperations(_clanStates, _kingdomStates, _secretPacts, _pendingOperations);
        ProcessAllianceBreakOutcomes(resolutions);
        ShowOperationResolutionNotifications(resolutions);
        ShowNewBreakawayReadyNotifications(
            breakawayReadinessBeforeTick,
            resolutions
                .Where(x => x.BecameBreakawayReady && x.TargetClan != null)
                .Select(x => x.TargetClan)
                .ToHashSet());
        GenerateOrganicIntrigueMoves();
        ProcessAllianceDeadlines();
    }

    private void OnHeroRelationChanged(
        Hero hero1,
        Hero hero2,
        int relationChange,
        bool showQuickNotification,
        ChangeRelationAction.ChangeRelationDetail detail,
        Hero originalHero,
        Hero originalGainedRelationWith)
    {
        if (hero1?.Clan == null || hero2?.Clan == null || hero1.Clan == hero2.Clan)
        {
            return;
        }

        if (hero1.Clan.Kingdom != null
            && hero1.Clan.Kingdom == hero2.Clan.Kingdom
            && hero2.Clan == hero1.Clan.Kingdom.RulingClan
            && IsValidIntrigueClan(hero1.Clan))
        {
            ClanIntrigueState state = GetOrCreateState(hero1.Clan);
            KingdomIntrigueState kingdomState = GetOrCreateKingdomState(hero1.Clan.Kingdom);
            if (relationChange < 0)
            {
                state.Dissidence += -relationChange * 0.75f;
                state.VoteResentment += -relationChange * 0.45f;
                state.ClaimantAmbition += -relationChange * 0.35f;
                kingdomState.CourtFragmentation += -relationChange * 0.25f;
            }
            else
            {
                state.RoyalFavor += relationChange * 0.5f;
                state.VoteResentment -= relationChange * 0.2f;
                kingdomState.RulerLegitimacy += relationChange * 0.08f;
            }

            state.ClampValues();
            kingdomState.ClampValues();
        }

        if (Hero.MainHero == null)
        {
            return;
        }

        Clan affectedClan = hero1 == Hero.MainHero ? hero2.Clan : hero2 == Hero.MainHero ? hero1.Clan : null;
        if (affectedClan != null && IsValidIntrigueClan(affectedClan))
        {
            ClanIntrigueState state = GetOrCreateState(affectedClan);
            state.TrustToPlayer += relationChange * 0.75f;
            if (relationChange > 0)
            {
                state.SoftDefectionPressure += relationChange * 0.12f;
            }

            state.ClampValues();
        }
    }

    private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isCancelled)
    {
        if (isCancelled || decision?.Kingdom == null || !IsValidIntrigueKingdom(decision.Kingdom))
        {
            return;
        }

        Clan favoredClan = chosenOutcome?.SponsorClan;
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(decision.Kingdom);

        ApplyGeneralDecisionPressure(decision.Kingdom, favoredClan, favoredClan == decision.Kingdom.RulingClan ? 2.5f : 1.25f);

        switch (decision)
        {
            case SettlementClaimantDecision settlementDecision:
                ApplySettlementClaimantDecisionEffects(settlementDecision, favoredClan, kingdomState);
                break;
            case KingdomPolicyDecision policyDecision:
                ApplyPolicyDecisionEffects(policyDecision, favoredClan, kingdomState);
                break;
            case KingSelectionKingdomDecision kingSelectionDecision:
                ApplyKingSelectionDecisionEffects(kingSelectionDecision, favoredClan, kingdomState);
                break;
            case ExpelClanFromKingdomDecision expelClanDecision:
                ApplyExpelClanDecisionEffects(expelClanDecision, favoredClan, kingdomState);
                break;
            default:
                kingdomState.CourtFragmentation += favoredClan == decision.Kingdom.RulingClan ? 2f : 0.75f;
                break;
        }

        kingdomState.LastUpdated = CampaignTime.Now;
        kingdomState.ClampValues();
    }

    private void OnClanChangedKingdom(
        Clan clan,
        Kingdom oldKingdom,
        Kingdom newKingdom,
        ChangeKingdomAction.ChangeKingdomActionDetail detail,
        bool showNotification = true)
    {
        if (clan == null)
        {
            return;
        }

        RemoveDeadReferencesForClan(clan);

        if (oldKingdom != null && IsValidIntrigueKingdom(oldKingdom))
        {
            RefreshKingdomState(oldKingdom);
        }

        if (newKingdom != null && IsValidIntrigueKingdom(newKingdom))
        {
            RefreshKingdomState(newKingdom);
        }

        if (IsValidIntrigueClan(clan))
        {
            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            ClanAlignmentService.Recalculate(state, GetOrCreateKingdomState(clan.Kingdom));
        }
    }

    private void OnRulingClanChanged(Kingdom kingdom, Clan newRulingClan)
    {
        if (kingdom == null || !IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom!);
        kingdomState.RulerLegitimacy = 60f;
        kingdomState.CourtFragmentation += 12f;
        kingdomState.WarExhaustion *= 0.85f;
        kingdomState.ClampValues();

        foreach (Clan clan in kingdom.Clans)
        {
            if (IsValidIntrigueClan(clan))
            {
                ClanIntrigueState state = GetOrCreateState(clan);
                if (clan == newRulingClan)
                {
                    state.Dissidence = 0f;
                    state.ClaimantAmbition = 0f;
                    state.VoteResentment *= 0.5f;
                }

                RefreshTrustToPlayer(state);
                RefreshDerivedClanState(state);
                ClanAlignmentService.Recalculate(state, kingdomState);
            }
        }
    }

    private void OnClanDestroyed(Clan clan)
    {
        if (clan == null)
        {
            return;
        }

        _clanStates.Remove(clan);
        RemoveDeadReferencesForClan(clan);

        if (clan.Kingdom != null && IsValidIntrigueKingdom(clan.Kingdom))
        {
            RefreshKingdomState(clan.Kingdom);
        }
    }

    private void OnClanInfluenceChanged(Clan clan, float influence)
    {
        if (!IsValidIntrigueClan(clan))
        {
            return;
        }

        ClanIntrigueState state = GetOrCreateState(clan);
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(clan.Kingdom);
        float expectedInfluence = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan) * 0.05f;

        if (influence < expectedInfluence * 0.75f)
        {
            state.VoteResentment += 1.75f;
            state.FiefGrievance += 0.75f;
        }
        else if (influence > expectedInfluence * 1.5f)
        {
            state.RoyalFavor += 1.25f;
            state.ClaimantAmbition += 0.75f;
            kingdomState.CourtFragmentation += clan == clan.Kingdom.RulingClan ? -0.5f : 1f;
        }

        state.ClampValues();
        kingdomState.ClampValues();
    }

    private void OnSettlementOwnerChanged(
        Settlement settlement,
        bool openToClaim,
        Hero newOwner,
        Hero oldOwner,
        Hero capturerHero,
        ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        Clan? oldClan = oldOwner?.Clan;
        Clan? newClan = newOwner?.Clan;
        if (settlement == null || oldClan == newClan)
        {
            return;
        }

        float lossWeight = settlement.IsTown ? 18f : 12f;

        if (oldClan?.Kingdom != null && IsValidIntrigueKingdom(oldClan.Kingdom))
        {
            KingdomIntrigueState oldKingdomState = GetOrCreateKingdomState(oldClan.Kingdom);
            oldKingdomState.RecentLosses += lossWeight;
            oldKingdomState.WarExhaustion += settlement.IsTown ? 8f : 5f;
            oldKingdomState.RulerLegitimacy -= settlement.IsTown ? 10f : 6f;
            oldKingdomState.CourtFragmentation += settlement.IsTown ? 4f : 2f;
            oldKingdomState.ClampValues();

            foreach (Clan clan in oldClan.Kingdom.Clans)
            {
                if (!IsValidIntrigueClan(clan))
                {
                    continue;
                }

                ClanIntrigueState state = GetOrCreateState(clan);
                state.MilitaryFrustration += settlement.IsTown ? 6f : 4f;
                if (clan == oldClan)
                {
                    state.FiefGrievance += settlement.IsTown ? 16f : 10f;
                    state.SoftDefectionPressure += settlement.IsTown ? 10f : 6f;
                }

                state.ClampValues();
            }
        }

        if (newClan?.Kingdom != null && IsValidIntrigueKingdom(newClan.Kingdom))
        {
            KingdomIntrigueState newKingdomState = GetOrCreateKingdomState(newClan.Kingdom);
            newKingdomState.RulerLegitimacy += settlement.IsTown ? 4f : 2f;
            newKingdomState.RecentLosses -= 4f;
            newKingdomState.CourtFragmentation += newClan == newClan.Kingdom.RulingClan ? 2f : -1f;
            newKingdomState.ClampValues();

            if (IsValidIntrigueClan(newClan))
            {
                ClanIntrigueState state = GetOrCreateState(newClan);
                state.RoyalFavor += settlement.IsTown ? 8f : 5f;
                state.FiefGrievance -= settlement.IsTown ? 10f : 6f;
                state.ClampValues();
            }

            if (oldClan?.Kingdom != null && oldClan.Kingdom == newClan.Kingdom && newClan == newClan.Kingdom.RulingClan)
            {
                foreach (Clan clan in newClan.Kingdom.Clans)
                {
                    if (!IsValidIntrigueClan(clan) || clan == newClan)
                    {
                        continue;
                    }

                    ClanIntrigueState state = GetOrCreateState(clan);
                    state.FiefGrievance += settlement.IsTown ? 6f : 3f;
                    state.VoteResentment += settlement.IsTown ? 4f : 2f;
                    state.ClampValues();
                }
            }
        }

        ProcessAllianceSettlementPromises(settlement);
    }

    private void OnTownRebelliosStateChanged(Town town, bool isRebellious)
    {
        if (town?.OwnerClan?.Kingdom == null || !IsValidIntrigueKingdom(town.OwnerClan.Kingdom))
        {
            return;
        }

        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(town.OwnerClan.Kingdom);
        if (isRebellious)
        {
            kingdomState.RebellionPressure += 18f;
            kingdomState.RulerLegitimacy -= 12f;
            kingdomState.CourtFragmentation += 8f;
        }
        else
        {
            kingdomState.RebellionPressure -= 8f;
            kingdomState.RulerLegitimacy += 3f;
            kingdomState.CourtFragmentation -= 4f;
        }

        kingdomState.ClampValues();

        if (IsValidIntrigueClan(town.OwnerClan))
        {
            ClanIntrigueState state = GetOrCreateState(town.OwnerClan);
            state.FiefGrievance += isRebellious ? 8f : -4f;
            state.MilitaryFrustration += isRebellious ? 6f : -2f;
            state.ClampValues();
        }
    }

    private void OnRebellionFinished(Settlement settlement, Clan rebelClan)
    {
        Kingdom? kingdom = settlement?.OwnerClan?.Kingdom ?? rebelClan?.Kingdom;
        if (!IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom);
        kingdomState.RebellionPressure -= 14f;
        kingdomState.RulerLegitimacy += 5f;
        kingdomState.CourtFragmentation -= 4f;
        kingdomState.ClampValues();
    }

    private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        ApplyWarStateShift(faction1, 6f, -2f);
        ApplyWarStateShift(faction2, 6f, -2f);
        RefreshFactionStates(faction1);
        RefreshFactionStates(faction2);
    }

    private void OnMakePeace(IFaction faction1, IFaction faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        ApplyWarStateShift(faction1, -10f, 4f);
        ApplyWarStateShift(faction2, -10f, 4f);
        RefreshFactionStates(faction1);
        RefreshFactionStates(faction2);
    }

    private void ApplyGeneralDecisionPressure(Kingdom kingdom, Clan favoredClan, float baseResentment)
    {
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            if (clan == favoredClan)
            {
                state.VoteResentment -= 6f;
                state.RoyalFavor += 4f;
            }
            else
            {
                state.VoteResentment += baseResentment;
                if (favoredClan == kingdom.RulingClan)
                {
                    state.FiefGrievance += 1f;
                }
            }

            state.ClampValues();
        }
    }

    private void ApplySettlementClaimantDecisionEffects(
        SettlementClaimantDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            bool isDispossessed = clan.Fiefs.Count() <= 1;
            if (clan == favoredClan)
            {
                state.FiefGrievance -= decision.Settlement?.IsTown == true ? 18f : 12f;
                state.VoteResentment -= 10f;
                state.RoyalFavor += 6f;
            }
            else
            {
                float resentment = isDispossessed ? 8f : 3f;
                if (favoredClan == decision.Kingdom.RulingClan)
                {
                    resentment += 4f;
                }

                if (clan == decision.ClanToExclude)
                {
                    resentment += 10f;
                }

                state.FiefGrievance += resentment;
                state.VoteResentment += resentment * 0.8f;
            }

            state.ClampValues();
        }

        kingdomState.CourtFragmentation += favoredClan == decision.Kingdom.RulingClan ? 8f : 3f;
        kingdomState.RulerLegitimacy += favoredClan == decision.Kingdom.RulingClan ? -4f : 1.5f;
    }

    private void ApplyPolicyDecisionEffects(
        KingdomPolicyDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            float support = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.ClanPoliticsModel.CalculateSupportForPolicyInClan(clan, decision.Policy);
            if (support < 0f)
            {
                state.VoteResentment += -support * 12f;
                state.Dissidence += -support * 3f;
            }
            else
            {
                state.RoyalFavor += support * 4f;
                state.VoteResentment -= support * 3f;
            }

            if (favoredClan == decision.Kingdom.RulingClan && support < 0f)
            {
                state.FiefGrievance += 2f;
            }

            state.ClampValues();
        }

        kingdomState.CourtFragmentation += favoredClan == decision.Kingdom.RulingClan ? 5f : 1.5f;
    }

    private void ApplyKingSelectionDecisionEffects(
        KingSelectionKingdomDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        float rulerStrength = favoredClan == null
            ? 0f
            : global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(favoredClan);

        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);

            if (clan == favoredClan)
            {
                state.ClaimantAmbition *= 0.35f;
                state.VoteResentment *= 0.5f;
                state.RoyalFavor += 8f;
            }
            else
            {
                state.ClaimantAmbition += clanStrength >= rulerStrength * 0.75f ? 14f : 6f;
                state.VoteResentment += 6f;
                if (favoredClan?.Leader != null && clan.Leader.GetRelation(favoredClan.Leader) < 0)
                {
                    state.ClaimantAmbition += 4f;
                }
            }

            state.ClampValues();
        }

        kingdomState.RulerLegitimacy = 60f;
        kingdomState.CourtFragmentation += 10f;
    }

    private void ApplyExpelClanDecisionEffects(
        ExpelClanFromKingdomDecision decision,
        Clan favoredClan,
        KingdomIntrigueState kingdomState)
    {
        if (IsValidIntrigueClan(decision.ClanToExpel))
        {
            ClanIntrigueState expelledState = GetOrCreateState(decision.ClanToExpel);
            expelledState.Dissidence += 25f;
            expelledState.VoteResentment += 25f;
            expelledState.SoftDefectionPressure += 25f;
            expelledState.ClampValues();
        }

        foreach (Clan clan in decision.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == decision.ClanToExpel)
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            state.VoteResentment += 4f;
            if (clan.Fiefs.Count() <= 1)
            {
                state.FiefGrievance += 4f;
            }

            if (favoredClan == decision.Kingdom.RulingClan)
            {
                state.ClaimantAmbition += 2f;
            }

            state.ClampValues();
        }

        kingdomState.RulerLegitimacy -= 12f;
        kingdomState.CourtFragmentation += 10f;
    }

    private void ApplyWarStateShift(IFaction faction, float warExhaustionDelta, float legitimacyDelta)
    {
        if (faction is not Kingdom kingdom || !IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState state = GetOrCreateKingdomState(kingdom);
        state.WarExhaustion += warExhaustionDelta;
        state.RulerLegitimacy += legitimacyDelta;
        state.ClampValues();
    }

    private void RefreshFactionStates(IFaction faction)
    {
        if (faction is not Kingdom kingdom || !IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        RefreshKingdomState(kingdom);
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom);
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            ClanAlignmentService.Recalculate(state, kingdomState);
        }
    }

    private void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (IsValidIntrigueKingdom(kingdom))
            {
                GetOrCreateKingdomState(kingdom);
            }
        }

        foreach (Clan clan in Clan.All)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetOrCreateState(clan);
            RefreshTrustToPlayer(state);
            RefreshDerivedClanState(state);
            state.ClampValues();
        }

        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (IsValidIntrigueKingdom(kingdom))
            {
                RefreshKingdomState(kingdom);
            }
        }

        _isInitialized = true;
    }

    private ClanIntrigueState GetOrCreateState(Clan clan)
    {
        if (!_clanStates.TryGetValue(clan, out ClanIntrigueState state))
        {
            state = new ClanIntrigueState(clan);
            _clanStates.Add(clan, state);
        }

        return state;
    }

    private KingdomIntrigueState GetOrCreateKingdomState(Kingdom kingdom)
    {
        if (!_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState state))
        {
            state = new KingdomIntrigueState(kingdom);
            _kingdomStates.Add(kingdom, state);
        }

        return state;
    }

    private void RefreshTrustToPlayer(ClanIntrigueState state)
    {
        if (state?.Clan?.Leader == null || Hero.MainHero == null)
        {
            return;
        }

        int relation = state.Clan.Leader.GetRelation(Hero.MainHero);
        float relationTrust = relation <= 0
            ? MBMath.Map(relation, -100f, 0f, 0f, 22f)
            : MBMath.Map(relation, 0f, 100f, 22f, 100f);
        state.TrustToPlayer = MBMath.ClampFloat((state.TrustToPlayer * 0.4f) + (relationTrust * 0.6f), 0f, 100f);
    }

    private void RefreshDerivedClanState(ClanIntrigueState state)
    {
        if (state?.Clan == null || state.Clan.Kingdom == null || state.Clan == state.Clan.Kingdom.RulingClan)
        {
            return;
        }

        Clan clan = state.Clan;
        Kingdom kingdom = clan.Kingdom;
        KingdomIntrigueState kingdomState = GetOrCreateKingdomState(kingdom);
        float ownStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
        float averageStrength = GetAverageClanStrength(kingdom);
        float averageInfluence = kingdom.Clans.Average(x => x.Influence);
        float averageFiefs = kingdom.Clans.Average(x => (float)x.Fiefs.Count());
        int relationToRuler = clan.Leader.GetRelation(kingdom.RulingClan.Leader);

        float baseFiefGrievance = 0f;
        if (clan.Fiefs.Count() == 0)
        {
            baseFiefGrievance += 24f;
        }
        else if (clan.Fiefs.Count() == 1)
        {
            baseFiefGrievance += 12f;
        }

        if (clan.Fiefs.Count() < averageFiefs)
        {
            baseFiefGrievance += (averageFiefs - clan.Fiefs.Count()) * 4.5f;
        }

        if (clan.Fiefs.Count() == 0 && ownStrength > averageStrength * 0.85f)
        {
            baseFiefGrievance += 8f;
        }

        state.FiefGrievance = MBMath.ClampFloat((state.FiefGrievance * 0.6f) + baseFiefGrievance, 0f, 100f);

        float expectedInfluence = ownStrength * 0.05f;
        float baseVoteResentment = expectedInfluence > clan.Influence
            ? (expectedInfluence - clan.Influence) * 0.35f
            : 0f;
        if (relationToRuler < 0)
        {
            baseVoteResentment += -relationToRuler * 0.05f;
        }

        if (clan.Influence < averageInfluence * 0.65f)
        {
            baseVoteResentment += 4f;
        }

        state.VoteResentment = MBMath.ClampFloat((state.VoteResentment * 0.78f) + baseVoteResentment, 0f, 100f);
        float baseMilitaryFrustration = kingdom.FactionsAtWarWith.Count(x => x.IsKingdomFaction) * 6f;
        foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            float progress = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(kingdom, enemy).ResultNumber;
            if (progress < 0f)
            {
                baseMilitaryFrustration += MBMath.ClampFloat(-progress / 4f, 0f, 10f);
            }
        }

        if (clan.Fiefs.Count() == 0)
        {
            baseMilitaryFrustration += 4f;
        }

        state.MilitaryFrustration = MBMath.ClampFloat((state.MilitaryFrustration * 0.74f) + baseMilitaryFrustration, 0f, 100f);

        float baseClaimantAmbition = 0f;
        if (ownStrength > averageStrength)
        {
            baseClaimantAmbition += ((ownStrength - averageStrength) / Math.Max(averageStrength, 1f)) * 25f;
        }

        if (clan.Influence > averageInfluence)
        {
            baseClaimantAmbition += MBMath.Map(clan.Influence - averageInfluence, 0f, Math.Max(averageInfluence, 1f), 0f, 12f);
        }

        if (relationToRuler < 0)
        {
            baseClaimantAmbition += -relationToRuler * 0.08f;
        }

        state.ClaimantAmbition = MBMath.ClampFloat((state.ClaimantAmbition * 0.72f) + baseClaimantAmbition, 0f, 100f);

        float defectionPressure = (100f - kingdomState.RulerLegitimacy) * 0.55f;
        defectionPressure += state.FiefGrievance * 0.2f;
        defectionPressure += state.VoteResentment * 0.25f;
        defectionPressure += state.MilitaryFrustration * 0.15f;
        defectionPressure += state.ClaimantAmbition * 0.15f;
        if (HasActivePact(clan))
        {
            defectionPressure += 12f;
        }

        if (Hero.MainHero != null)
        {
            int relationToPlayer = clan.Leader.GetRelation(Hero.MainHero);
            if (relationToPlayer > 0)
            {
                defectionPressure += relationToPlayer * 0.15f;
            }
        }

        state.SoftDefectionPressure = MBMath.ClampFloat((state.SoftDefectionPressure * 0.68f) + defectionPressure, 0f, 100f);
        state.ClampValues();
    }

    private void DecayDailyValues()
    {
        foreach (ClanIntrigueState state in _clanStates.Values)
        {
            state.Suspicion -= StrategicIntrigueConstants.DailySuspicionDecay;
            state.RoyalFavor -= StrategicIntrigueConstants.DailyRoyalFavorDecay;
            state.Infiltration -= StrategicIntrigueConstants.DailyInfiltrationDecay;
            state.FiefGrievance -= 0.35f;
            state.VoteResentment -= 0.9f;
            state.MilitaryFrustration -= 0.65f;
            state.ClaimantAmbition -= 0.25f;
            state.SoftDefectionPressure -= 0.45f;
            state.ClampValues();
        }

        foreach (KingdomIntrigueState state in _kingdomStates.Values)
        {
            state.WarExhaustion -= 1.2f;
            state.RecentLosses -= 2.5f;
            state.RebellionPressure -= 0.8f;
            state.CourtFragmentation -= 0.65f;
            state.ClaimantPressure -= 0.6f;
            state.ClampValues();
        }

        foreach (SecretPact pact in _secretPacts)
        {
            if (!pact.IsExposed)
            {
                pact.Secrecy -= 0.5f;
                pact.Commitment += 0.35f;
                pact.ClampValues();
            }
        }
    }

    private Dictionary<Clan, bool> CaptureActivePactBreakawayReadiness()
    {
        Dictionary<Clan, bool> readiness = new();
        foreach (SecretPact pact in _secretPacts)
        {
            if (pact.IsExposed || pact.MemberClan == null || readiness.ContainsKey(pact.MemberClan))
            {
                continue;
            }

            readiness[pact.MemberClan] = GetState(pact.MemberClan)?.IsBreakawayReady == true;
        }

        return readiness;
    }

    private void ScheduleAutomaticEscalations()
    {
        foreach (SecretPact pact in _secretPacts.Where(x => !x.IsExposed).ToList())
        {
            Clan clan = pact.MemberClan;
            if (clan == null
                || !_clanStates.TryGetValue(clan, out ClanIntrigueState state)
                || !state.IsBreakawayReady
                || pact.Commitment < StrategicIntrigueConstants.AutoEscalationCommitmentThreshold
                || HasPendingBreakOperation(clan))
            {
                continue;
            }

            if (pact.Goal == IntriguePactGoal.SupportFutureClaimant && !CanTriggerClaimantCoup(clan, out _))
            {
                continue;
            }

            _pendingOperations.Add(new IntrigueOperation(
                IntrigueOperationType.PrepareBreakaway,
                pact.SponsorClan,
                clan,
                clan.Kingdom?.RulingClan,
                pact.Commitment,
                35f,
                CampaignTime.DaysFromNow(StrategicIntrigueConstants.AutoEscalationDelayDays)));
        }
    }

    private void GenerateOrganicIntrigueMoves()
    {
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsValidIntrigueKingdom(kingdom) || !_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState kingdomState))
            {
                continue;
            }

            float crisis = GetOrganicCrisisScore(kingdomState);
            if (crisis < StrategicIntrigueConstants.OrganicRumorCrisisThreshold)
            {
                continue;
            }

            TryGenerateOrganicRumor(kingdom, kingdomState, crisis);

            if (crisis >= StrategicIntrigueConstants.OrganicPactCrisisThreshold)
            {
                TryGenerateOrganicPact(kingdom, kingdomState, crisis);
            }
        }
    }

    private void TryGenerateOrganicRumor(Kingdom kingdom, KingdomIntrigueState kingdomState, float crisis)
    {
        if ((CampaignTime.Now - kingdomState.LastOrganicRumorAt).ToDays < StrategicIntrigueConstants.OrganicRumorCooldownDays)
        {
            return;
        }

        List<(Clan Clan, float Weight)> candidates = new();
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == kingdom.RulingClan || HasPendingRumorCampaign(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetState(clan);
            if (state == null)
            {
                continue;
            }

            float weight = GetOrganicRumorTargetWeight(state);
            if (weight >= 28f)
            {
                candidates.Add((clan, weight));
            }
        }

        Clan targetClan = ChooseWeightedClan(candidates);
        if (targetClan == null)
        {
            return;
        }

        Clan sponsorClan = FindOrganicRumorSponsor(targetClan);
        if (sponsorClan == null)
        {
            return;
        }

        ClanIntrigueState targetState = GetState(targetClan);
        float chance = MBMath.ClampFloat(
            0.04f + ((crisis - StrategicIntrigueConstants.OrganicRumorCrisisThreshold) * 0.003f) + ((targetState.Dissidence - 45f) * 0.0035f),
            0f,
            StrategicIntrigueConstants.OrganicRumorMaxChance);
        if (MBRandom.RandomFloat > chance)
        {
            return;
        }

        float power = MBMath.ClampFloat(18f + (crisis * 0.28f), 18f, 48f);
        float risk = sponsorClan.Kingdom == targetClan.Kingdom ? 22f : 28f;
        _pendingOperations.Add(new IntrigueOperation(
            IntrigueOperationType.RumorCampaign,
            sponsorClan,
            targetClan,
            targetClan.Kingdom?.RulingClan,
            power,
            risk,
            CampaignTime.DaysFromNow(2f + (MBRandom.RandomFloat * 2f))));
        kingdomState.LastOrganicRumorAt = CampaignTime.Now;
    }

    private void TryGenerateOrganicPact(Kingdom kingdom, KingdomIntrigueState kingdomState, float crisis)
    {
        if ((CampaignTime.Now - kingdomState.LastOrganicPactAt).ToDays < StrategicIntrigueConstants.OrganicPactCooldownDays)
        {
            return;
        }

        List<(Clan Clan, float Weight)> candidates = new();
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == kingdom.RulingClan || HasActivePact(clan))
            {
                continue;
            }

            ClanIntrigueState state = GetState(clan);
            if (state == null || !state.IsConspirable || state.Suspicion >= 78f)
            {
                continue;
            }

            float weight = GetOrganicPactTargetWeight(state);
            if (weight >= 40f)
            {
                candidates.Add((clan, weight));
            }
        }

        Clan targetClan = ChooseWeightedClan(candidates);
        if (targetClan == null)
        {
            return;
        }

        Clan sponsorClan = FindOrganicPactSponsor(targetClan);
        if (sponsorClan == null)
        {
            return;
        }

        ClanIntrigueState targetState = GetState(targetClan);
        float chance = MBMath.ClampFloat(
            0.025f + ((crisis - StrategicIntrigueConstants.OrganicPactCrisisThreshold) * 0.0025f) + ((targetState.Dissidence - StrategicIntrigueConstants.ConspirableDissidenceThreshold) * 0.003f),
            0f,
            StrategicIntrigueConstants.OrganicPactMaxChance);
        if (MBRandom.RandomFloat > chance)
        {
            return;
        }

        if (ApplySecretPactAction.TryApplyOrganic(_clanStates, _secretPacts, sponsorClan, targetClan, out _))
        {
            kingdomState.LastOrganicPactAt = CampaignTime.Now;
            if (sponsorClan.Kingdom != null && sponsorClan.Kingdom != kingdom && _kingdomStates.TryGetValue(sponsorClan.Kingdom, out KingdomIntrigueState sponsorKingdomState))
            {
                sponsorKingdomState.LastOrganicPactAt = CampaignTime.Now;
            }
        }
    }

    private Clan FindOrganicRumorSponsor(Clan targetClan)
    {
        List<(Clan Clan, float Weight)> sponsors = new();
        foreach (Clan clan in targetClan.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == targetClan || clan == targetClan.Kingdom.RulingClan)
            {
                continue;
            }

            float weight = GetInternalSponsorWeight(clan, targetClan);
            if (weight >= 26f)
            {
                sponsors.Add((clan, weight));
            }
        }

        return ChooseWeightedClan(sponsors);
    }

    private Clan FindOrganicPactSponsor(Clan targetClan)
    {
        ClanIntrigueState targetState = GetState(targetClan);
        bool preferExternal = targetState != null
            && targetState.SoftDefectionPressure >= StrategicIntrigueConstants.OrganicExternalPactSoftDefectionThreshold
            && targetState.ClaimantAmbition < 60f;

        Clan sponsor = preferExternal
            ? FindExternalPactSponsor(targetClan)
            : FindInternalPactSponsor(targetClan);
        if (sponsor != null)
        {
            return sponsor;
        }

        return preferExternal
            ? FindInternalPactSponsor(targetClan)
            : FindExternalPactSponsor(targetClan);
    }

    private Clan FindInternalPactSponsor(Clan targetClan)
    {
        List<(Clan Clan, float Weight)> sponsors = new();
        foreach (Clan clan in targetClan.Kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || clan == targetClan || clan == targetClan.Kingdom.RulingClan || HasActivePact(clan))
            {
                continue;
            }

            float weight = GetInternalSponsorWeight(clan, targetClan) + (GetState(clan)?.ClaimantAmbition ?? 0f) * 0.18f;
            if (weight >= 34f)
            {
                sponsors.Add((clan, weight));
            }
        }

        return ChooseWeightedClan(sponsors);
    }

    private Clan FindExternalPactSponsor(Clan targetClan)
    {
        ClanIntrigueState targetState = GetState(targetClan);
        if (targetState == null || targetState.SoftDefectionPressure < StrategicIntrigueConstants.OrganicExternalPactSoftDefectionThreshold)
        {
            return null;
        }

        List<(Clan Clan, float Weight)> sponsors = new();
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null || kingdom == targetClan.Kingdom || kingdom.IsEliminated || kingdom.RulingClan?.Leader == null)
            {
                continue;
            }

            Hero sponsorLeader = kingdom.RulingClan.Leader;
            Hero targetLeader = targetClan.Leader;
            int relation = targetLeader?.GetRelation(sponsorLeader) ?? 0;
            if (relation < -10 && kingdom.Culture != targetClan.Culture)
            {
                continue;
            }

            float weight = MathF.Max(0f, (float)relation) * 0.85f;
            if (kingdom.Culture == targetClan.Culture)
            {
                weight += 14f;
            }

            if (_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState sponsorKingdomState))
            {
                weight += sponsorKingdomState.RulerLegitimacy * 0.12f;
            }

            weight += targetState.SoftDefectionPressure * 0.32f;
            if (weight >= 38f)
            {
                sponsors.Add((kingdom.RulingClan, weight));
            }
        }

        return ChooseWeightedClan(sponsors);
    }

    private float GetOrganicCrisisScore(KingdomIntrigueState kingdomState)
    {
        return ((100f - kingdomState.RulerLegitimacy) * 0.42f)
            + (kingdomState.CourtFragmentation * 0.24f)
            + (kingdomState.RebellionPressure * 0.17f)
            + (kingdomState.ClaimantPressure * 0.17f);
    }

    private static float GetOrganicRumorTargetWeight(ClanIntrigueState state)
    {
        return (state.Dissidence * 0.5f)
            + (state.VoteResentment * 0.18f)
            + (state.FiefGrievance * 0.14f)
            + (state.ClaimantAmbition * 0.08f)
            + (state.SoftDefectionPressure * 0.1f)
            - (state.FearOfRuler * 0.18f)
            - (state.Suspicion * 0.08f);
    }

    private static float GetOrganicPactTargetWeight(ClanIntrigueState state)
    {
        return (state.Dissidence * 0.54f)
            + (state.ClaimantAmbition * 0.18f)
            + (state.SoftDefectionPressure * 0.22f)
            + (state.Infiltration * 0.08f)
            - (state.FearOfRuler * 0.12f)
            - (state.Suspicion * 0.1f);
    }

    private float GetInternalSponsorWeight(Clan sponsorClan, Clan targetClan)
    {
        ClanIntrigueState sponsorState = GetState(sponsorClan);
        ClanIntrigueState targetState = GetState(targetClan);
        if (sponsorState == null || targetState == null || sponsorClan.Leader == null || targetClan.Leader == null)
        {
            return 0f;
        }

        Hero ruler = targetClan.Kingdom?.RulingClan?.Leader;
        int relationToTarget = sponsorClan.Leader.GetRelation(targetClan.Leader);
        int sponsorToRuler = ruler == null ? 0 : sponsorClan.Leader.GetRelation(ruler);
        int targetToRuler = ruler == null ? 0 : targetClan.Leader.GetRelation(ruler);
        if (relationToTarget < -18)
        {
            return 0f;
        }

        float weight = MathF.Max(0f, (float)relationToTarget) * 0.85f;
        weight += MathF.Max(0f, (float)(-sponsorToRuler)) * 0.45f;
        weight += MathF.Max(0f, (float)(-targetToRuler)) * 0.35f;
        weight += sponsorState.Dissidence * 0.28f;
        weight += sponsorState.ClaimantAmbition * 0.16f;
        if (sponsorClan.Culture == targetClan.Culture)
        {
            weight += 8f;
        }

        return weight;
    }

    private static Clan ChooseWeightedClan(List<(Clan Clan, float Weight)> options)
    {
        if (options.Count == 0)
        {
            return null;
        }

        float totalWeight = options.Sum(x => MathF.Max(0.01f, x.Weight));
        float roll = MBRandom.RandomFloat * totalWeight;
        foreach ((Clan clan, float weight) in options)
        {
            roll -= MathF.Max(0.01f, weight);
            if (roll <= 0f)
            {
                return clan;
            }
        }

        return options[options.Count - 1].Clan;
    }

    private SecretPact GetActivePact(Clan clan)
    {
        return clan == null
            ? null
            : _secretPacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == clan);
    }

    private Clan GetPreferredPlayerConspiracyTarget(Kingdom excludedKingdom)
    {
        return _secretPacts
            .Where(x =>
                !x.IsExposed
                && x.SponsorClan == Clan.PlayerClan
                && x.MemberClan?.Kingdom != null
                && x.MemberClan.Kingdom != excludedKingdom)
            .OrderByDescending(x =>
            {
                ClanIntrigueState state = GetState(x.MemberClan);
                if (state == null)
                {
                    return 0f;
                }

                float score = state.Dissidence
                    + (state.IsBreakawayReady ? 40f : 0f)
                    + (state.IsConspirable ? 15f : 0f)
                    + (state.SoftDefectionPressure * 0.2f)
                    + (state.ClaimantAmbition * 0.18f);
                return score;
            })
            .Select(x => x.MemberClan)
            .FirstOrDefault();
    }

    private static bool IsAllianceActive(SecretAllianceCompact alliance)
    {
        return alliance != null
            && alliance.IsActive
            && alliance.AllyClan != null
            && alliance.SupportedClan != null
            && alliance.TargetKingdom != null;
    }

    private static IntrigueAllianceObjective MapAllianceObjective(IntriguePactGoal goal)
    {
        return goal == IntriguePactGoal.SupportFutureClaimant
            ? IntrigueAllianceObjective.BackClaimant
            : IntrigueAllianceObjective.BackBreakaway;
    }

    private static Settlement ChoosePromisedSettlement(Kingdom targetKingdom, Clan supportedClan, Clan recipientClan)
    {
        if (targetKingdom == null)
        {
            return null;
        }

        Town promisedTown = targetKingdom.Fiefs
            .Where(x => x.OwnerClan != null && x.OwnerClan != recipientClan)
            .OrderByDescending(x => x.OwnerClan == targetKingdom.RulingClan)
            .ThenBy(x => x.IsTown ? 1 : 0)
            .ThenByDescending(x => x.OwnerClan != supportedClan)
            .FirstOrDefault();

        return promisedTown?.Settlement;
    }

    private void ProcessAllianceBreakOutcomes(IEnumerable<IntrigueOperationResolution> resolutions)
    {
        foreach (IntrigueOperationResolution resolution in resolutions)
        {
            if (resolution.BreakOutcome != IntrigueBreakOutcome.None && resolution.TargetClan != null)
            {
                ProcessAllianceBreakOutcome(resolution.TargetClan, resolution.BreakOutcome);
            }
        }
    }

    private void ProcessAllianceBreakOutcome(Clan targetClan, IntrigueBreakOutcome outcome)
    {
        foreach (SecretAllianceCompact alliance in _secretAlliances
                     .Where(x => IsAllianceActive(x) && x.SupportedClan == targetClan)
                     .ToList())
        {
            TriggerAllianceSupport(alliance, outcome);
            TryFulfillSettlementPromise(alliance);
        }
    }

    private void TriggerAllianceSupport(SecretAllianceCompact alliance, IntrigueBreakOutcome outcome)
    {
        if (alliance == null || alliance.IsSupportTriggered)
        {
            return;
        }

        alliance.IsSupportTriggered = true;
        alliance.ResolveBy = CampaignTime.DaysFromNow(StrategicIntrigueConstants.AllianceSettlementGraceDays);

        Kingdom allyKingdom = alliance.AllyClan?.Kingdom;
        if (alliance.Objective == IntrigueAllianceObjective.ForeignIntervention
            && outcome != IntrigueBreakOutcome.ClaimantCoup
            && allyKingdom != null
            && alliance.TargetKingdom != null
            && allyKingdom != alliance.TargetKingdom
            && !allyKingdom.IsAtWarWith(alliance.TargetKingdom))
        {
            DeclareWarAction.ApplyByKingdomDecision(allyKingdom, alliance.TargetKingdom);
        }

        if (alliance.InstigatorClan == Clan.PlayerClan)
        {
            TextObject title = new TextObject("{=rf_si_alliance_support_title}Secret Alliance Moved");
            TextObject body = alliance.Objective == IntrigueAllianceObjective.ForeignIntervention
                ? new TextObject("{=rf_si_alliance_support_body_foreign}{ALLY} has begun to move openly against {KINGDOM}. The secret bargain is now in motion.")
                : new TextObject("{=rf_si_alliance_support_body_internal}Your secret alliance with {ALLY} has moved beyond whispers. The promised terms are now due if the plot succeeds.");
            body.SetTextVariable("ALLY", alliance.AllyClan?.Name ?? new TextObject("{=rf_si_unknown_ally}your ally"));
            body.SetTextVariable("KINGDOM", alliance.TargetKingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
            ShowIntrigueInquiry(title, body);
        }
    }

    private void ProcessAllianceSettlementPromises(Settlement settlement)
    {
        if (settlement == null)
        {
            return;
        }

        foreach (SecretAllianceCompact alliance in _secretAlliances
                     .Where(x => IsAllianceActive(x) && x.PromisedSettlement == settlement)
                     .ToList())
        {
            TryFulfillSettlementPromise(alliance);
        }
    }

    private bool TryFulfillSettlementPromise(SecretAllianceCompact alliance)
    {
        if (!IsAllianceActive(alliance)
            || alliance.RewardType != IntrigueAllianceRewardType.PromisedSettlement
            || alliance.PromisedSettlement == null)
        {
            return false;
        }

        Settlement settlement = alliance.PromisedSettlement;
        Clan recipientClan = alliance.SettlementRecipientClan;
        if (settlement.OwnerClan == recipientClan)
        {
            alliance.IsFulfilled = true;
            return true;
        }

        Hero recipientHero = recipientClan == Clan.PlayerClan ? Hero.MainHero : recipientClan?.Leader;
        if (recipientHero == null)
        {
            return false;
        }

        bool claimantCanPay = alliance.TargetKingdom?.RulingClan == alliance.SupportedClan
            && settlement.OwnerClan?.Kingdom == alliance.TargetKingdom;
        bool controllerCanPay = settlement.OwnerClan == alliance.SupportedClan
            || settlement.OwnerClan == alliance.InstigatorClan
            || settlement.OwnerClan == Clan.PlayerClan;
        if (!claimantCanPay && !controllerCanPay)
        {
            return false;
        }

        alliance.IsFulfilled = true;
        ChangeOwnerOfSettlementAction.ApplyByGift(settlement, recipientHero);

        if (alliance.InstigatorClan == Clan.PlayerClan || recipientClan == Clan.PlayerClan)
        {
            TextObject title = new TextObject("{=rf_si_alliance_reward_title}Promise Honored");
            TextObject body = new TextObject("{=rf_si_alliance_reward_body}{SETTLEMENT} has changed hands in accordance with your secret bargain.");
            body.SetTextVariable("SETTLEMENT", settlement.Name);
            ShowIntrigueInquiry(title, body);
        }

        Hero allyLeader = alliance.AllyClan?.Leader;
        if (allyLeader != null && recipientHero != null && allyLeader != recipientHero)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(allyLeader, recipientHero, 8, false);
        }

        return true;
    }

    private void ProcessAllianceDeadlines()
    {
        foreach (SecretAllianceCompact alliance in _secretAlliances.Where(IsAllianceActive).ToList())
        {
            if ((CampaignTime.Now - alliance.ResolveBy).ToDays <= 0f)
            {
                continue;
            }

            alliance.IsBroken = true;
            ApplyBrokenAllianceConsequences(alliance);
        }
    }

    private void ApplyBrokenAllianceConsequences(SecretAllianceCompact alliance)
    {
        Hero instigator = alliance.InstigatorClan == Clan.PlayerClan ? Hero.MainHero : alliance.InstigatorClan?.Leader;
        Hero ally = alliance.AllyClan?.Leader;
        if (instigator != null && ally != null && instigator != ally)
        {
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(instigator, ally, alliance.IsSupportTriggered ? -18 : -10, false);
        }

        if (_clanStates.TryGetValue(alliance.SupportedClan, out ClanIntrigueState state))
        {
            state.Suspicion += alliance.IsSupportTriggered ? 12f : 6f;
            state.TrustToPlayer -= alliance.InstigatorClan == Clan.PlayerClan ? 10f : 0f;
            state.ClampValues();
        }

        if (alliance.InstigatorClan == Clan.PlayerClan || alliance.SettlementRecipientClan == Clan.PlayerClan)
        {
            TextObject title = new TextObject("{=rf_si_alliance_broken_title}Secret Bargain Collapsed");
            TextObject body = new TextObject("{=rf_si_alliance_broken_body}The promised terms with {ALLY} have soured. The reward was not delivered in time, and trust has collapsed.");
            body.SetTextVariable("ALLY", alliance.AllyClan?.Name ?? new TextObject("{=rf_si_unknown_ally}your ally"));
            ShowIntrigueInquiry(title, body);
        }
    }

    private bool HasPendingBreakOperation(Clan clan)
    {
        return clan != null && _pendingOperations.Any(x =>
            x.Status == IntrigueOperationStatus.Pending
            && x.TargetClan == clan
            && x.Type == IntrigueOperationType.PrepareBreakaway);
    }

    private bool CanTriggerClaimantCoup(Clan targetClan, out TextObject reason)
    {
        reason = TextObject.GetEmpty();
        if (targetClan?.Kingdom == null || !_kingdomStates.TryGetValue(targetClan.Kingdom, out KingdomIntrigueState kingdomState))
        {
            reason = new TextObject("{=si_claimant_no_kingdom}There is no viable royal crisis to exploit here.");
            return false;
        }

        float rulingStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(targetClan.Kingdom.RulingClan);
        float claimantStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(targetClan);
        bool claimantHasStanding = targetClan.Influence >= targetClan.Kingdom.RulingClan.Influence * 0.55f
            || claimantStrength >= rulingStrength * 0.8f;

        if (kingdomState.RulerLegitimacy > StrategicIntrigueConstants.ClaimantCoupLegitimacyThreshold)
        {
            reason = new TextObject("{=si_claimant_legitimacy_too_high}The crown is still too secure for a claimant move.");
            return false;
        }

        if (kingdomState.CourtFragmentation < StrategicIntrigueConstants.ClaimantCoupFragmentationThreshold)
        {
            reason = new TextObject("{=si_claimant_court_too_orderly}The court has not fractured enough to back a new ruler.");
            return false;
        }

        if (!claimantHasStanding)
        {
            reason = new TextObject("{=si_claimant_not_strong_enough}This lord lacks the weight to seize the crown, even now.");
            return false;
        }

        return true;
    }

    private void ProcessRulerCountermoves()
    {
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (!IsValidIntrigueKingdom(kingdom) || !_kingdomStates.TryGetValue(kingdom, out KingdomIntrigueState kingdomState))
            {
                continue;
            }

            if ((CampaignTime.Now - kingdomState.LastCountermoveAt).ToDays < StrategicIntrigueConstants.RulerCountermoveCooldownDays)
            {
                continue;
            }

            ClanIntrigueState threatenedState = null;
            SecretPact activePact = null;
            Clan threatenedClan = null;
            float highestThreat = 0f;

            foreach (Clan clan in kingdom.Clans)
            {
                if (!IsValidIntrigueClan(clan) || !_clanStates.TryGetValue(clan, out ClanIntrigueState state))
                {
                    continue;
                }

                SecretPact pact = _secretPacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == clan);
                float threat = state.Dissidence
                    + (state.Suspicion * 0.55f)
                    + (state.SoftDefectionPressure * 0.25f)
                    + (state.ClaimantAmbition * 0.2f)
                    + (pact != null ? 18f : 0f);
                if (threat <= highestThreat || threat < 80f)
                {
                    continue;
                }

                highestThreat = threat;
                threatenedState = state;
                threatenedClan = clan;
                activePact = pact;
            }

            if (threatenedState == null || threatenedClan == null)
            {
                continue;
            }

            bool useCrackdown = threatenedState.Suspicion >= 50f || activePact != null;
            if (useCrackdown)
            {
                threatenedState.FearOfRuler += 18f;
                threatenedState.Dissidence -= 8f;
                threatenedState.TrustToPlayer -= 14f;
                threatenedState.Suspicion += 12f;
                threatenedState.RoyalFavor += 3f;
                kingdomState.CourtFragmentation += 4f;
                kingdomState.RulerLegitimacy += 2f;

                if (activePact != null)
                {
                    activePact.Secrecy -= 22f;
                    activePact.ClampValues();
                    if (activePact.Secrecy <= 35f)
                    {
                        activePact.IsExposed = true;
                        threatenedState.Suspicion = 100f;
                        if (activePact.SponsorClan == Clan.PlayerClan)
                        {
                            TextObject title = new TextObject("{=rf_si_exposed_pact_title}Secret Pact Exposed");
                            TextObject body = new TextObject("{=rf_si_exposed_pact_body}Agents of {KINGDOM} have uncovered your private understanding with {CLAN}. The court is now alert to your hand in their politics.");
                            body.SetTextVariable("KINGDOM", kingdom.Name);
                            body.SetTextVariable("CLAN", threatenedClan.Name);
                            ShowIntrigueInquiry(title, body);
                        }
                    }
                }

                bool shouldApplyHeavyPunishment =
                    kingdom.RulingClan != Clan.PlayerClan
                    && ((activePact?.IsExposed ?? false)
                        || threatenedState.Suspicion >= StrategicIntrigueConstants.SeverePunishmentSuspicionThreshold
                        || highestThreat >= StrategicIntrigueConstants.SeverePunishmentThreatThreshold);

                if (shouldApplyHeavyPunishment)
                {
                    CrackdownPunishmentOutcome punishmentOutcome = ApplyCrackdownPunishment(kingdom, threatenedClan, threatenedState);
                    if (punishmentOutcome != CrackdownPunishmentOutcome.None)
                    {
                        RemoveDeadReferencesForClan(threatenedClan);

                        switch (punishmentOutcome)
                        {
                            case CrackdownPunishmentOutcome.Execution:
                                kingdomState.RulerLegitimacy -= 4f;
                                kingdomState.CourtFragmentation += 12f;
                                break;
                            case CrackdownPunishmentOutcome.Imprisonment:
                                kingdomState.CourtFragmentation += 8f;
                                break;
                            default:
                                kingdomState.RulerLegitimacy -= 1.5f;
                                kingdomState.CourtFragmentation += 6f;
                                break;
                        }

                        if ((activePact?.SponsorClan == Clan.PlayerClan) || threatenedClan == Clan.PlayerClan)
                        {
                            ShowCrackdownPunishmentNotification(kingdom, threatenedClan, punishmentOutcome);
                        }
                    }
                }
            }
            else
            {
                threatenedState.RoyalFavor += 15f;
                threatenedState.Dissidence -= 12f;
                threatenedState.VoteResentment -= 10f;
                threatenedState.FiefGrievance -= 8f;
                threatenedState.ClaimantAmbition -= 6f;
                kingdomState.RulerLegitimacy += 6f;
                kingdomState.CourtFragmentation -= 4f;
                kingdomState.ClaimantPressure -= 8f;
            }

            threatenedState.ClampValues();
            kingdomState.LastCountermoveAt = CampaignTime.Now;
            kingdomState.ClampValues();
        }
    }

    private CrackdownPunishmentOutcome ApplyCrackdownPunishment(
        Kingdom kingdom,
        Clan threatenedClan,
        ClanIntrigueState threatenedState)
    {
        Hero ruler = kingdom?.RulingClan?.Leader;
        Hero dissident = threatenedClan?.Leader;
        if (ruler == null || dissident == null)
        {
            return CrackdownPunishmentOutcome.None;
        }

        int relationToRuler = dissident.GetRelation(ruler);
        ChangeRelationAction.ApplyRelationChangeBetweenHeroes(dissident, ruler, -30);

        threatenedState.TrustToPlayer -= 28f;
        threatenedState.Dissidence -= 18f;
        threatenedState.Suspicion = 100f;
        threatenedState.FearOfRuler += 30f;
        threatenedState.RoyalFavor = 0f;

        if (dissident != Hero.MainHero
            && relationToRuler <= StrategicIntrigueConstants.ExecutionRelationThreshold
            && dissident.IsAlive
            && !dissident.IsChild)
        {
            KillCharacterAction.ApplyByExecution(dissident, ruler, showNotification: true, isForced: true);
            threatenedState.Dissidence = 0f;
            threatenedState.TrustToPlayer = 0f;
            threatenedState.ClampValues();
            return CrackdownPunishmentOutcome.Execution;
        }

        if (relationToRuler <= StrategicIntrigueConstants.ImprisonmentRelationThreshold
            && TryImprisonDissident(kingdom, dissident))
        {
            threatenedState.Dissidence -= 10f;
            threatenedState.ClampValues();
            return CrackdownPunishmentOutcome.Imprisonment;
        }

        if (TryExileClan(threatenedClan))
        {
            threatenedState.Dissidence = 0f;
            threatenedState.TrustToPlayer = 0f;
            threatenedState.ClampValues();
            return CrackdownPunishmentOutcome.Exile;
        }

        if (dissident != Hero.MainHero && TryImprisonDissident(kingdom, dissident))
        {
            threatenedState.ClampValues();
            return CrackdownPunishmentOutcome.Imprisonment;
        }

        threatenedState.ClampValues();
        return CrackdownPunishmentOutcome.None;
    }

    private static bool TryImprisonDissident(Kingdom kingdom, Hero dissident)
    {
        if (dissident == null || dissident == Hero.MainHero || !dissident.IsAlive || dissident.IsPrisoner)
        {
            return false;
        }

        PartyBase capturerParty = GetRulerCapturerParty(kingdom);
        if (capturerParty == null)
        {
            return false;
        }

        TakePrisonerAction.Apply(capturerParty, dissident);
        return true;
    }

    private static bool TryExileClan(Clan clan)
    {
        if (clan?.Kingdom == null)
        {
            return false;
        }

        ChangeKingdomAction.ApplyByLeaveKingdom(clan, false);
        return true;
    }

    private static PartyBase GetRulerCapturerParty(Kingdom kingdom)
    {
        Hero ruler = kingdom?.RulingClan?.Leader;
        if (ruler?.PartyBelongedTo?.Party != null)
        {
            return ruler.PartyBelongedTo.Party;
        }

        if (ruler?.CurrentSettlement?.Party != null)
        {
            return ruler.CurrentSettlement.Party;
        }

        return kingdom?.RulingClan?.Settlements?.FirstOrDefault()?.Party;
    }

    private void ShowCrackdownPunishmentNotification(
        Kingdom kingdom,
        Clan threatenedClan,
        CrackdownPunishmentOutcome outcome)
    {
        TextObject title;
        TextObject body;
        switch (outcome)
        {
            case CrackdownPunishmentOutcome.Execution:
                title = new TextObject("{=rf_si_crackdown_execute_title}Dissident Executed");
                body = new TextObject("{=rf_si_crackdown_execute_body}{CLAN} has been crushed completely. {RULER} chose the axe over mercy once the conspiracy was laid bare.");
                break;
            case CrackdownPunishmentOutcome.Imprisonment:
                title = new TextObject("{=rf_si_crackdown_prison_title}Dissident Imprisoned");
                body = new TextObject("{=rf_si_crackdown_prison_body}{CLAN} has been seized by the crown. The suspected dissident now sits in chains while the court turns on your scheme.");
                break;
            default:
                title = new TextObject("{=rf_si_crackdown_exile_title}Dissident Banished");
                body = new TextObject("{=rf_si_crackdown_exile_body}{CLAN} has been cast out of {KINGDOM}. Old ties were not enough to save them once disloyalty was exposed.");
                body.SetTextVariable("KINGDOM", kingdom?.Name ?? new TextObject("{=rf_si_unknown_kingdom}the realm"));
                break;
        }

        body.SetTextVariable("CLAN", threatenedClan?.Name ?? new TextObject("{=rf_si_unknown_clan}the clan"));
        body.SetTextVariable("RULER", kingdom?.RulingClan?.Leader?.Name ?? new TextObject("{=rf_si_unknown_ruler}the ruler"));
        ShowIntrigueInquiry(title, body);
    }

    private void ShowOperationResolutionNotifications(IEnumerable<IntrigueOperationResolution> resolutions)
    {
        foreach (IntrigueOperationResolution resolution in resolutions)
        {
            if (resolution.TargetClan == null)
            {
                continue;
            }

            if (resolution.Type is not IntrigueOperationType.RumorCampaign and not IntrigueOperationType.SponsorDissidence)
            {
                if (resolution.BreakOutcome != IntrigueBreakOutcome.None)
                {
                    ShowBreakOutcomeNotification(resolution);
                }

                continue;
            }

            bool playerInvolved = resolution.InstigatorClan == Clan.PlayerClan
                || resolution.TargetClan == Clan.PlayerClan
                || resolution.TargetRulerClan == Clan.PlayerClan;
            if (!playerInvolved)
            {
                continue;
            }

            TextObject title = new TextObject("{=rf_si_rumor_popup_title}Rumor Network Report");
            TextObject body = resolution.WasExposed
                ? new TextObject("{=rf_si_rumor_popup_exposed}The whispers around {CLAN} were noticed at court. Suspicion is rising, but the strain inside the realm is still real.")
                : new TextObject("{=rf_si_rumor_popup_resolved}The whispers around {CLAN} have taken hold. Court opinion has shifted against the current order.");
            body.SetTextVariable("CLAN", resolution.TargetClan.Name);
            string baseBody = body.ToString();

            if (resolution.BecameBreakawayReady)
            {
                body = new TextObject("{=rf_si_rumor_popup_breakaway}{BASE}\n\nThis has pushed {CLAN} into open breakaway territory. Your private understanding can now be turned into action.");
                body.SetTextVariable("BASE", baseBody);
                body.SetTextVariable("CLAN", resolution.TargetClan.Name);
            }
            else if (resolution.BecameConspirable)
            {
                body = new TextObject("{=rf_si_rumor_popup_conspirable}{BASE}\n\n{CLAN} is now ready for a deeper private arrangement against their ruler.");
                body.SetTextVariable("BASE", baseBody);
                body.SetTextVariable("CLAN", resolution.TargetClan.Name);
            }

            ShowIntrigueInquiry(title, body);
        }
    }

    private void ShowNewBreakawayReadyNotifications(
        Dictionary<Clan, bool> readinessBeforeTick,
        HashSet<Clan> alreadyReported)
    {
        foreach (KeyValuePair<Clan, bool> entry in readinessBeforeTick)
        {
            Clan clan = entry.Key;
            bool wasReady = entry.Value;
            if (clan == null
                || wasReady
                || alreadyReported.Contains(clan)
                || !_secretPacts.Any(x => !x.IsExposed && x.MemberClan == clan && x.SponsorClan == Clan.PlayerClan)
                || GetState(clan)?.IsBreakawayReady != true)
            {
                continue;
            }

            TextObject title = new TextObject("{=rf_si_breakaway_popup_title}Conspiracy Hardened");
            TextObject body = new TextObject("{=rf_si_breakaway_popup_body}{CLAN} is now prepared to move from secret understanding to open rupture. If you speak with them again, you can press for decisive action.");
            body.SetTextVariable("CLAN", clan.Name);
            ShowIntrigueInquiry(title, body);
        }
    }

    private static void ShowIntrigueInquiry(TextObject title, TextObject body)
    {
        InformationManager.ShowInquiry(new InquiryData(
            title.ToString(),
            body.ToString(),
            true,
            false,
            new TextObject("{=rf_si_popup_ack}Understood").ToString(),
            string.Empty,
            null,
            null));
    }

    private void ShowBreakOutcomeNotification(IntrigueOperationResolution resolution)
    {
        if (resolution.TargetClan == null)
        {
            return;
        }

        TextObject title;
        TextObject body;
        switch (resolution.BreakOutcome)
        {
            case IntrigueBreakOutcome.Defection:
                title = new TextObject("{=rf_si_defection_popup_title}Secret Defection");
                body = resolution.InstigatorClan == Clan.PlayerClan
                    ? new TextObject("{=rf_si_defection_popup_body}{CLAN} has abandoned its old liege and entered your orbit as planned.")
                    : new TextObject("{=rf_si_defection_popup_body_ai}{CLAN} has abandoned its old liege and entered the protection of {SPONSOR}.");
                body.SetTextVariable("SPONSOR", resolution.InstigatorClan?.Name ?? new TextObject("{=rf_si_unknown_sponsor}another power"));
                break;
            case IntrigueBreakOutcome.ClaimantCoup:
                title = new TextObject("{=rf_si_claimant_popup_title}Claimant Rising");
                body = new TextObject("{=rf_si_claimant_popup_body}{CLAN} has moved from conspiracy to coup and now claims the crown within {KINGDOM}.");
                body.SetTextVariable("KINGDOM", resolution.TargetClan.Kingdom?.Name ?? new TextObject("{=rf_si_claimant_unknown_kingdom}the realm"));
                break;
            default:
                title = new TextObject("{=rf_si_break_popup_title}Realm Fractured");
                body = new TextObject("{=rf_si_break_popup_body}{CLAN} has broken openly with its realm. The conspiracy has turned into rebellion.");
                break;
        }

        body.SetTextVariable("CLAN", resolution.TargetClan.Name);
        ShowIntrigueInquiry(title, body);
    }

    private void RemoveDeadReferencesForClan(Clan clan)
    {
        _secretPacts.RemoveAll(x => x.MatchesClan(clan));
        _secretAlliances.RemoveAll(x => x.MatchesClan(clan));
        _pendingOperations.RemoveAll(x =>
            x.TargetClan == clan || x.InstigatorClan == clan || x.TargetRulerClan == clan);
    }

    private void RefreshKingdomState(Kingdom kingdom)
    {
        if (!IsValidIntrigueKingdom(kingdom))
        {
            return;
        }

        KingdomIntrigueState state = GetOrCreateKingdomState(kingdom);
        state.WarExhaustion = MBMath.ClampFloat((state.WarExhaustion * 0.65f) + GetWarExhaustionFactor(kingdom), 0f, 100f);
        state.RecentLosses = MBMath.ClampFloat(state.RecentLosses * 0.82f, 0f, 100f);
        state.RebellionPressure = MBMath.ClampFloat((state.RebellionPressure * 0.9f) + GetRebellionFactor(kingdom), 0f, 100f);
        state.CourtFragmentation = MBMath.ClampFloat((state.CourtFragmentation * 0.78f) + GetCourtFragmentationFactor(kingdom), 0f, 100f);
        state.ClaimantPressure = MBMath.ClampFloat((state.ClaimantPressure * 0.74f) + GetClaimantPressureFactor(kingdom), 0f, 100f);

        float legitimacy = 65f;
        legitimacy += GetRulerSupportFactor(kingdom);
        legitimacy += GetFiefSecurityFactor(kingdom);
        legitimacy -= state.WarExhaustion * 0.25f;
        legitimacy -= state.RecentLosses * 0.3f;
        legitimacy -= state.RebellionPressure * 0.32f;
        legitimacy -= state.CourtFragmentation * 0.18f;
        legitimacy -= state.ClaimantPressure * 0.22f;
        if (kingdom.Fiefs.Count() <= 2)
        {
            legitimacy -= 8f;
        }

        state.RulerLegitimacy = MBMath.ClampFloat(legitimacy, 0f, 100f);
        state.LastUpdated = CampaignTime.Now;
        state.ClampValues();
    }

    private float GetWarExhaustionFactor(Kingdom kingdom)
    {
        float pressure = kingdom.FactionsAtWarWith.Count(x => x.IsKingdomFaction) * 8f;
        foreach (Kingdom enemy in kingdom.FactionsAtWarWith.OfType<Kingdom>())
        {
            float progress = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetWarProgressScore(kingdom, enemy).ResultNumber;
            if (progress < 0f)
            {
                pressure += MBMath.ClampFloat(-progress / 3f, 0f, 14f);
            }
            else
            {
                pressure -= MBMath.ClampFloat(progress / 6f, 0f, 5f);
            }
        }

        return MBMath.ClampFloat(pressure, 0f, 100f);
    }

    private float GetRebellionFactor(Kingdom kingdom)
    {
        float pressure = 0f;
        foreach (Town fief in kingdom.Fiefs)
        {
            if (!fief.IsTown)
            {
                continue;
            }

            pressure += Math.Max(0f, 40f - fief.Loyalty) * 0.12f;
            pressure += Math.Max(0f, 35f - fief.Security) * 0.08f;
        }

        return MBMath.ClampFloat(pressure, 0f, 100f);
    }

    private float GetCourtFragmentationFactor(Kingdom kingdom)
    {
        if (kingdom.RulingClan?.Leader == null)
        {
            return 0f;
        }

        float fragmentation = 0f;
        float averageStrength = GetAverageClanStrength(kingdom);
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan))
            {
                continue;
            }

            int relation = clan.Leader.GetRelation(kingdom.RulingClan.Leader);
            if (relation < 0)
            {
                fragmentation += -relation * 0.08f;
            }

            if (clan.Fiefs.Count() <= 1)
            {
                fragmentation += 2.5f;
            }

            float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
            if (clanStrength > averageStrength * 1.1f)
            {
                fragmentation += 2f;
            }

            if (clan.Influence > kingdom.RulingClan.Influence * 0.65f)
            {
                fragmentation += 1.5f;
            }
        }

        return MBMath.ClampFloat(fragmentation, 0f, 100f);
    }

    private float GetClaimantPressureFactor(Kingdom kingdom)
    {
        if (kingdom.RulingClan?.Leader == null)
        {
            return 0f;
        }

        float pressure = 0f;
        float rulerStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(kingdom.RulingClan);
        foreach (Clan clan in kingdom.Clans)
        {
            if (!IsValidIntrigueClan(clan) || !_clanStates.TryGetValue(clan, out ClanIntrigueState state))
            {
                continue;
            }

            float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
            if (clanStrength >= rulerStrength * 0.8f)
            {
                pressure += 4f;
            }

            if (state.ClaimantAmbition >= 50f)
            {
                pressure += (state.ClaimantAmbition - 45f) * 0.18f;
            }

            if (clan.Leader.GetRelation(kingdom.RulingClan.Leader) < -10)
            {
                pressure += 3f;
            }
        }

        return MBMath.ClampFloat(pressure, 0f, 100f);
    }

    private float GetRulerSupportFactor(Kingdom kingdom)
    {
        if (kingdom.RulingClan?.Leader == null)
        {
            return 0f;
        }

        List<Clan> vassals = kingdom.Clans.Where(IsValidIntrigueClan).ToList();
        if (vassals.Count == 0)
        {
            return 0f;
        }

        float averageRelation = (float)vassals.Average(x => x.Leader.GetRelation(kingdom.RulingClan.Leader));
        return MBMath.ClampFloat(averageRelation * 0.18f, -18f, 18f);
    }

    private float GetFiefSecurityFactor(Kingdom kingdom)
    {
        List<Town> fiefs = kingdom.Fiefs.ToList();
        if (fiefs.Count == 0)
        {
            return -12f;
        }

        float averageLoyalty = fiefs.Where(x => x.IsTown).DefaultIfEmpty().Average(x => x?.Loyalty ?? 50f);
        float averageSecurity = fiefs.Where(x => x.IsTown).DefaultIfEmpty().Average(x => x?.Security ?? 50f);
        float loyaltyFactor = (averageLoyalty - 50f) * 0.16f;
        float securityFactor = (averageSecurity - 50f) * 0.12f;
        return MBMath.ClampFloat(loyaltyFactor + securityFactor, -12f, 12f);
    }

    private float GetAverageClanStrength(Kingdom kingdom)
    {
        return Math.Max(
            kingdom.Clans.Average(x => global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(x)),
            1f);
    }

    private static bool IsValidIntrigueClan(Clan clan)
    {
        if (clan == null || clan.Leader == null || clan.Kingdom == null)
        {
            return false;
        }

        if (clan.IsMinorFaction || clan.IsUnderMercenaryService || clan.IsEliminated)
        {
            return false;
        }

        return clan.Kingdom.Clans.Count > 1;
    }

    private static bool IsValidIntrigueKingdom(Kingdom kingdom)
    {
        return kingdom != null
            && !kingdom.IsEliminated
            && kingdom.RulingClan?.Leader != null
            && kingdom.Clans.Count > 1;
    }
}

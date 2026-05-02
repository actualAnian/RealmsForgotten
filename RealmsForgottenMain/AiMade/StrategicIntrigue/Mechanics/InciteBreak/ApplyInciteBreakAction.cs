using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.InciteBreak;

public enum IntrigueBreakOutcome
{
    None = 0,
    Rebellion = 1,
    Defection = 2,
    ClaimantCoup = 3
}

public static class ApplyInciteBreakAction
{
    public static IntrigueBreakOutcome Apply(
        Dictionary<Clan, ClanIntrigueState> states,
        Dictionary<Kingdom, KingdomIntrigueState> kingdomStates,
        List<SecretPact> pacts,
        Clan targetClan)
    {
        if (targetClan?.Kingdom == null
            || targetClan == targetClan.Kingdom.RulingClan
            || !states.TryGetValue(targetClan, out ClanIntrigueState state))
        {
            return IntrigueBreakOutcome.None;
        }

        SecretPact pact = pacts.FirstOrDefault(x => !x.IsExposed && x.MemberClan == targetClan);
        if (pact == null)
        {
            return IntrigueBreakOutcome.None;
        }

        if (state.Dissidence < StrategicIntrigueConstants.BreakawayDissidenceThreshold
            || !HasSufficientCommitment(state, pact, targetClan))
        {
            return IntrigueBreakOutcome.None;
        }

        Kingdom originKingdom = targetClan.Kingdom;
        Clan oldRulingClan = originKingdom.RulingClan;
        KingdomIntrigueState originKingdomState = kingdomStates.TryGetValue(originKingdom, out KingdomIntrigueState existingState)
            ? existingState
            : null;

        IntrigueBreakOutcome outcome;
        Kingdom sponsorKingdom = pact.SponsorClan?.Kingdom;
        if (pact.Goal == IntriguePactGoal.PrepareProtectedVassalage
            && sponsorKingdom != null
            && sponsorKingdom != targetClan.Kingdom)
        {
            ChangeKingdomAction.ApplyByJoinToKingdomByDefection(
                targetClan,
                targetClan.Kingdom,
                sponsorKingdom,
                CampaignTime.DaysFromNow(45f),
                true);
            outcome = IntrigueBreakOutcome.Defection;
            if (originKingdomState != null)
            {
                originKingdomState.RulerLegitimacy -= 14f;
                originKingdomState.CourtFragmentation += 16f;
                originKingdomState.RebellionPressure += 10f;
                originKingdomState.ClaimantPressure += 6f;
                originKingdomState.ClampValues();
            }

            if (kingdomStates.TryGetValue(sponsorKingdom, out KingdomIntrigueState sponsorKingdomState))
            {
                sponsorKingdomState.RulerLegitimacy += 6f;
                sponsorKingdomState.CourtFragmentation -= 3f;
                sponsorKingdomState.ClampValues();
            }
        }
        else if (pact.Goal == IntriguePactGoal.SupportFutureClaimant)
        {
            ChangeRulingClanAction.Apply(originKingdom, targetClan);
            outcome = IntrigueBreakOutcome.ClaimantCoup;
            if (originKingdomState != null)
            {
                originKingdomState.RulerLegitimacy = 58f;
                originKingdomState.CourtFragmentation += 20f;
                originKingdomState.RebellionPressure = MBMath.ClampFloat(originKingdomState.RebellionPressure - 8f, 0f, 100f);
                originKingdomState.ClaimantPressure = 18f;
                originKingdomState.ClampValues();
            }

            foreach (Clan clan in originKingdom.Clans.ToList())
            {
                if (!states.TryGetValue(clan, out ClanIntrigueState clanState))
                {
                    continue;
                }

                if (clan == targetClan)
                {
                    clanState.Dissidence = 8f;
                    clanState.RoyalFavor += 20f;
                    clanState.ClaimantAmbition = 0f;
                    clanState.VoteResentment *= 0.45f;
                }
                else if (clan == oldRulingClan)
                {
                    clanState.Dissidence += 22f;
                    clanState.RoyalFavor = 0f;
                    clanState.SoftDefectionPressure += 12f;
                }
                else
                {
                    int relationToClaimant = clan.Leader?.GetRelation(targetClan.Leader) ?? 0;
                    if (relationToClaimant >= 0)
                    {
                        clanState.Dissidence -= 6f;
                        clanState.VoteResentment -= 4f;
                    }
                    else
                    {
                        clanState.Dissidence += 8f;
                        clanState.ClaimantAmbition += 6f;
                        clanState.VoteResentment += 5f;
                    }
                }

                clanState.ClampValues();
            }

            HandleDeposedRulerAfterClaimantCoup(kingdomStates, originKingdom, oldRulingClan, targetClan);
        }
        else
        {
            ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(targetClan, true);
            outcome = IntrigueBreakOutcome.Rebellion;
            if (originKingdomState != null)
            {
                originKingdomState.RulerLegitimacy -= 18f;
                originKingdomState.CourtFragmentation += 18f;
                originKingdomState.RebellionPressure += 14f;
                originKingdomState.ClaimantPressure += 10f;
                originKingdomState.ClampValues();
            }
        }

        state.Dissidence = outcome == IntrigueBreakOutcome.ClaimantCoup ? 10f : 25f;
        state.Suspicion = 100f;
        state.SoftDefectionPressure = 0f;
        pact.IsExposed = true;
        state.ClampValues();
        return outcome;
    }

    private static void HandleDeposedRulerAfterClaimantCoup(
        Dictionary<Kingdom, KingdomIntrigueState> kingdomStates,
        Kingdom originKingdom,
        Clan oldRulingClan,
        Clan newRulingClan)
    {
        if (originKingdom == null
            || oldRulingClan == null
            || newRulingClan == null
            || oldRulingClan == newRulingClan
            || oldRulingClan.Kingdom != originKingdom)
        {
            return;
        }

        int deposedFiefCount = oldRulingClan.Fiefs.Count();
        int totalKingdomFiefCount = originKingdom.Fiefs.Count();
        bool shouldRaiseIndependentWar = deposedFiefCount >= StrategicIntrigueConstants.DeposedRulerIndependentWarFiefThreshold
            || (totalKingdomFiefCount > 0
                && ((float)deposedFiefCount / totalKingdomFiefCount) >= StrategicIntrigueConstants.DeposedRulerIndependentWarFiefShareThreshold);

        if (shouldRaiseIndependentWar && deposedFiefCount > 0)
        {
            ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(oldRulingClan, true);
            return;
        }

        Kingdom asylumKingdom = FindRestorationAsylum(originKingdom, oldRulingClan, newRulingClan);
        if (asylumKingdom == null)
        {
            if (deposedFiefCount > 0)
            {
                ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(oldRulingClan, true);
            }

            return;
        }

        ChangeKingdomAction.ApplyByJoinToKingdomByDefection(
            oldRulingClan,
            originKingdom,
            asylumKingdom,
            CampaignTime.DaysFromNow(StrategicIntrigueConstants.DeposedRulerAsylumWarDurationDays),
            true);

        if (!asylumKingdom.IsAtWarWith(originKingdom))
        {
            DeclareWarAction.ApplyByKingdomDecision(asylumKingdom, originKingdom);
        }

        if (kingdomStates.TryGetValue(asylumKingdom, out KingdomIntrigueState asylumState))
        {
            asylumState.RulerLegitimacy += 4f;
            asylumState.CourtFragmentation += 2f;
            asylumState.ClampValues();
        }

        if (kingdomStates.TryGetValue(originKingdom, out KingdomIntrigueState originState))
        {
            originState.RulerLegitimacy -= 6f;
            originState.CourtFragmentation += 8f;
            originState.RebellionPressure += 6f;
            originState.ClampValues();
        }
    }

    private static Kingdom FindRestorationAsylum(Kingdom originKingdom, Clan oldRulingClan, Clan newRulingClan)
    {
        Hero formerRuler = oldRulingClan.Leader;
        Hero newRuler = newRulingClan.Leader;
        if (formerRuler == null)
        {
            return null;
        }

        Kingdom bestKingdom = null;
        float bestScore = float.MinValue;
        foreach (Kingdom kingdom in Kingdom.All)
        {
            if (kingdom == null
                || kingdom == originKingdom
                || kingdom.IsEliminated
                || kingdom.RulingClan?.Leader == null)
            {
                continue;
            }

            Hero sponsorLeader = kingdom.RulingClan.Leader;
            float score = 0f;
            if (kingdom.IsAtWarWith(originKingdom))
            {
                score += 30f;
            }

            if (kingdom.Culture == oldRulingClan.Culture)
            {
                score += 12f;
            }

            int relationToFormerRuler = formerRuler.GetRelation(sponsorLeader);
            int relationToNewRuler = newRuler == null ? 0 : sponsorLeader.GetRelation(newRuler);
            score += relationToFormerRuler * 0.85f;
            score += MathF.Max(0f, (float)(-relationToNewRuler)) * 0.65f;
            score += kingdom.Fiefs.Count() * 1.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestKingdom = kingdom;
            }
        }

        return bestScore >= 12f ? bestKingdom : null;
    }

    private static bool HasSufficientCommitment(ClanIntrigueState state, SecretPact pact, Clan targetClan)
    {
        if (pact.SponsorClan == Clan.PlayerClan)
        {
            return state.TrustToPlayer >= StrategicIntrigueConstants.SecretPactTrustThreshold;
        }

        Hero sponsorLeader = pact.SponsorClan?.Leader;
        Hero targetLeader = targetClan?.Leader;
        if (sponsorLeader == null || targetLeader == null)
        {
            return state.Dissidence >= 92f;
        }

        int relationToSponsor = targetLeader.GetRelation(sponsorLeader);
        return relationToSponsor >= 5
            || (pact.Goal == IntriguePactGoal.SupportFutureClaimant && state.ClaimantAmbition >= 65f)
            || (pact.Goal == IntriguePactGoal.PrepareProtectedVassalage && state.SoftDefectionPressure >= 72f)
            || state.Dissidence >= 92f;
    }
}

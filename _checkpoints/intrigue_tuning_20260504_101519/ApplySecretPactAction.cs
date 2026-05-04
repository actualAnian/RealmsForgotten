using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.Core;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.SecretPacts;

public static class ApplySecretPactAction
{
    public static bool TryApply(
        Dictionary<Clan, ClanIntrigueState> states,
        List<SecretPact> pacts,
        Clan sponsorClan,
        Clan memberClan,
        out TextObject reason)
    {
        reason = TextObject.GetEmpty();

        if (sponsorClan == null || memberClan == null || sponsorClan == memberClan)
        {
            reason = new TextObject("{=si_invalid_pact_target}Invalid secret pact target.");
            return false;
        }

        if (memberClan.Kingdom == null || memberClan == memberClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=si_invalid_ruler_target}The ruling clan cannot be bound this way.");
            return false;
        }

        if (!states.TryGetValue(memberClan, out ClanIntrigueState state))
        {
            reason = new TextObject("{=si_missing_intrigue_state}This clan has no intrigue state yet.");
            return false;
        }

        if (pacts.Any(x => !x.IsExposed && x.MemberClan == memberClan))
        {
            reason = new TextObject("{=si_existing_secret_pact}This clan is already tied to a secret pact.");
            return false;
        }

        if (state.Dissidence < StrategicIntrigueConstants.ConspirableDissidenceThreshold)
        {
            reason = new TextObject("{=si_low_dissidence}This clan is not dissatisfied enough to conspire.");
            return false;
        }

        if (state.TrustToPlayer < StrategicIntrigueConstants.SecretPactTrustThreshold)
        {
            reason = new TextObject("{=si_low_trust}This clan does not trust you enough yet.");
            return false;
        }

        IntriguePactGoal goal = DetermineGoal(state, sponsorClan, memberClan);

        SecretPact pact = new SecretPact(sponsorClan, memberClan, goal);
        pacts.Add(pact);

        state.TrustToPlayer += 10f;
        state.Suspicion += 5f;
        state.ClampValues();
        return true;
    }

    public static bool TryApplyOrganic(
        Dictionary<Clan, ClanIntrigueState> states,
        List<SecretPact> pacts,
        Clan sponsorClan,
        Clan memberClan,
        out TextObject reason)
    {
        reason = TextObject.GetEmpty();

        if (sponsorClan == null || memberClan == null || sponsorClan == memberClan)
        {
            reason = new TextObject("{=si_invalid_pact_target}Invalid secret pact target.");
            return false;
        }

        if (memberClan.Kingdom == null || memberClan == memberClan.Kingdom.RulingClan)
        {
            reason = new TextObject("{=si_invalid_ruler_target}The ruling clan cannot be bound this way.");
            return false;
        }

        if (!states.TryGetValue(memberClan, out ClanIntrigueState state))
        {
            reason = new TextObject("{=si_missing_intrigue_state}This clan has no intrigue state yet.");
            return false;
        }

        if (pacts.Any(x => !x.IsExposed && x.MemberClan == memberClan))
        {
            reason = new TextObject("{=si_existing_secret_pact}This clan is already tied to a secret pact.");
            return false;
        }

        if (state.Dissidence < StrategicIntrigueConstants.ConspirableDissidenceThreshold)
        {
            reason = new TextObject("{=si_low_dissidence}This clan is not dissatisfied enough to conspire.");
            return false;
        }

        float affinity = GetOrganicAffinity(states, sponsorClan, memberClan, state);
        if (affinity < 45f)
        {
            reason = new TextObject("{=si_organic_pact_no_alignment}There is not enough private alignment between these clans to sustain a real conspiracy.");
            return false;
        }

        IntriguePactGoal goal = DetermineGoal(state, sponsorClan, memberClan);
        SecretPact pact = new SecretPact(sponsorClan, memberClan, goal)
        {
            Commitment = MBMath.ClampFloat(18f + affinity * 0.10f, 18f, 48f), // was (30+affinity*0.35, max 92) — capped below AutoEscalationThreshold(68)
            Secrecy = sponsorClan.Kingdom != null && sponsorClan.Kingdom != memberClan.Kingdom ? 80f : 68f
        };
        pact.ClampValues();
        pacts.Add(pact);

        state.Suspicion += 4f;
        state.Infiltration += 6f;
        if (sponsorClan.Kingdom != null && sponsorClan.Kingdom != memberClan.Kingdom)
        {
            state.SoftDefectionPressure += 8f;
        }

        state.ClampValues();
        if (states.TryGetValue(sponsorClan, out ClanIntrigueState sponsorState))
        {
            sponsorState.Suspicion += 2f;
            sponsorState.ClampValues();
        }

        return true;
    }

    private static IntriguePactGoal DetermineGoal(ClanIntrigueState state, Clan sponsorClan, Clan memberClan)
    {
        if (state.SoftDefectionPressure >= 70f && sponsorClan.Kingdom != null && sponsorClan.Kingdom != memberClan.Kingdom)
        {
            return IntriguePactGoal.PrepareProtectedVassalage;
        }

        if (state.ClaimantAmbition >= 60f)
        {
            return IntriguePactGoal.SupportFutureClaimant;
        }

        if (state.Dissidence >= StrategicIntrigueConstants.BreakawayDissidenceThreshold)
        {
            return IntriguePactGoal.BreakAwayFromKingdom;
        }

        return IntriguePactGoal.UndermineRuler;
    }

    private static float GetOrganicAffinity(
        Dictionary<Clan, ClanIntrigueState> states,
        Clan sponsorClan,
        Clan memberClan,
        ClanIntrigueState memberState)
    {
        Hero sponsorLeader = sponsorClan.Leader;
        Hero memberLeader = memberClan.Leader;
        if (sponsorLeader == null || memberLeader == null)
        {
            return 0f;
        }

        int relationToSponsor = memberLeader.GetRelation(sponsorLeader);
        float affinity = MathF.Max(0f, (float)relationToSponsor) * 0.9f;

        if (sponsorClan.Kingdom != null && sponsorClan.Kingdom == memberClan.Kingdom)
        {
            Hero ruler = memberClan.Kingdom?.RulingClan?.Leader;
            int memberToRuler = ruler == null ? 0 : memberLeader.GetRelation(ruler);
            int sponsorToRuler = ruler == null ? 0 : sponsorLeader.GetRelation(ruler);
            affinity += MathF.Max(0f, (float)(-memberToRuler)) * 0.7f;
            affinity += MathF.Max(0f, (float)(-sponsorToRuler)) * 0.45f;
            affinity += memberState.ClaimantAmbition * 0.15f;

            if (states.TryGetValue(sponsorClan, out ClanIntrigueState sponsorState))
            {
                affinity += sponsorState.Dissidence * 0.35f;
            }
        }
        else
        {
            if (sponsorClan.Culture == memberClan.Culture)
            {
                affinity += 18f;
            }

            affinity += memberState.SoftDefectionPressure * 0.45f;
            affinity += memberState.Dissidence * 0.15f;
        }

        return affinity;
    }
}

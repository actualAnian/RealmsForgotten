using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.ClanAlignment;

public static class ClanAlignmentService
{
    public static void Recalculate(ClanIntrigueState state, KingdomIntrigueState kingdomState = null)
    {
        if (state?.Clan == null || state.Clan.Kingdom == null || state.Clan.Leader == null)
        {
            return;
        }

        Clan clan = state.Clan;
        Kingdom kingdom = clan.Kingdom;
        Clan rulerClan = kingdom.RulingClan;

        if (rulerClan?.Leader == null || clan == rulerClan)
        {
            state.Dissidence = 0f;
            state.FearOfRuler = 0f;
            state.ClampValues();
            return;
        }

        float fearOfRuler = GetFearOfRuler(clan, rulerClan);
        float dissidence = 0f;
        dissidence += GetRulerRelationFactor(clan, rulerClan);
        dissidence += GetPolicyMismatchFactor(clan);
        dissidence += GetInfluencePressureFactor(clan);
        dissidence += GetRelativePowerAmbitionFactor(clan, kingdom);
        dissidence += GetPlayerLeverageFactor(clan);
        dissidence += GetKingdomLegitimacyFactor(kingdomState);
        dissidence += state.FiefGrievance * 0.16f;
        dissidence += state.VoteResentment * 0.18f;
        dissidence += state.MilitaryFrustration * 0.16f;
        dissidence += state.ClaimantAmbition * 0.12f;
        dissidence += state.SoftDefectionPressure * 0.08f;
        dissidence += state.Infiltration * 0.35f;
        dissidence -= state.RoyalFavor * 0.6f;
        dissidence -= GetWarPressureFactor(kingdom);
        dissidence -= fearOfRuler * 0.08f;

        state.Dissidence = MBMath.ClampFloat(dissidence, 0f, 100f);
        state.FearOfRuler = fearOfRuler;
        state.LastUpdated = CampaignTime.Now;
        state.ClampValues();
    }

    private static float GetRulerRelationFactor(Clan clan, Clan rulerClan)
    {
        int relation = clan.Leader.GetRelation(rulerClan.Leader);
        return relation >= 0 ? 0f : MBMath.Map(-relation, 0f, 100f, 0f, 35f);
    }

    private static float GetPolicyMismatchFactor(Clan clan)
    {
        if (clan.Kingdom == null || clan.Kingdom.ActivePolicies.Count == 0)
        {
            return 0f;
        }

        float mismatch = 0f;
        foreach (PolicyObject policy in clan.Kingdom.ActivePolicies)
        {
            float support = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.ClanPoliticsModel.CalculateSupportForPolicyInClan(clan, policy);
            if (support < 0f)
            {
                mismatch += -support * 4f;
            }
        }

        return MBMath.ClampFloat(mismatch, 0f, 20f);
    }

    private static float GetInfluencePressureFactor(Clan clan)
    {
        float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
        float expectedInfluence = clanStrength * 0.05f;
        if (clan.Influence >= expectedInfluence)
        {
            return 0f;
        }

        float deficit = expectedInfluence - clan.Influence;
        return MBMath.ClampFloat(deficit * 0.2f, 0f, 15f);
    }

    private static float GetRelativePowerAmbitionFactor(Clan clan, Kingdom kingdom)
    {
        if (kingdom.Clans.Count <= 1)
        {
            return 0f;
        }

        float averageStrength = kingdom.Clans.Average(x => global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(x));
        float ownStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
        if (ownStrength <= averageStrength)
        {
            return 0f;
        }

        return MBMath.ClampFloat((ownStrength - averageStrength) / 40f, 0f, 15f);
    }

    private static float GetPlayerLeverageFactor(Clan clan)
    {
        if (Hero.MainHero == null)
        {
            return 0f;
        }

        int relation = clan.Leader.GetRelation(Hero.MainHero);
        return relation <= 0 ? 0f : MBMath.Map(relation, 0f, 100f, 0f, 15f);
    }

    private static float GetKingdomLegitimacyFactor(KingdomIntrigueState kingdomState)
    {
        if (kingdomState == null)
        {
            return 0f;
        }

        float legitimacyLoss = 100f - kingdomState.RulerLegitimacy;
        float fragmentationPressure = kingdomState.CourtFragmentation * 0.08f;
        float rebellionPressure = kingdomState.RebellionPressure * 0.12f;
        return MBMath.ClampFloat((legitimacyLoss * 0.22f) + fragmentationPressure + rebellionPressure, 0f, 28f);
    }

    private static float GetWarPressureFactor(Kingdom kingdom)
    {
        int activeKingdomWars = kingdom.FactionsAtWarWith.Count(x => x.IsKingdomFaction);
        return MBMath.ClampFloat(activeKingdomWars * 3f, 0f, 10f);
    }

    private static float GetFearOfRuler(Clan clan, Clan rulerClan)
    {
        float rulerStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(rulerClan);
        float clanStrength = global::TaleWorlds.CampaignSystem.Campaign.Current.Models.DiplomacyModel.GetClanStrength(clan);
        if (clanStrength <= 0f)
        {
            return 100f;
        }

        float ratio = rulerStrength / clanStrength;
        float influencePressure = MBMath.ClampFloat(rulerClan.Influence / 20f, 0f, 30f);
        return MBMath.ClampFloat((ratio * 20f) + influencePressure, 0f, 100f);
    }
}

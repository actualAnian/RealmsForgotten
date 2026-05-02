using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.RumorCampaigns;

public static class ApplyRumorCampaignAction
{
    public static void Apply(ClanIntrigueState targetState, Clan rulerClan, float power)
    {
        if (targetState?.Clan?.Leader == null)
        {
            return;
        }

        float clampedPower = MBMath.ClampFloat(power, 5f, 100f);
        targetState.Dissidence += clampedPower * 0.45f;
        targetState.Suspicion += clampedPower * 0.35f;
        targetState.Infiltration += clampedPower * 0.25f;

        if (rulerClan?.Leader != null && rulerClan != targetState.Clan)
        {
            int relationLoss = MBMath.ClampInt((int)(clampedPower / 12f), 1, 4);
            ChangeRelationAction.ApplyRelationChangeBetweenHeroes(targetState.Clan.Leader, rulerClan.Leader, -relationLoss, false);
        }

        targetState.ClampValues();
    }
}

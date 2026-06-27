using RF_warsystem.Behaviors;
using RF_warsystem.Models;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace RF_warsystem;

public static class RFWarSystemRegistrar
{
    public static void RegisterBehaviors(CampaignGameStarter campaignGameStarter)
    {
        campaignGameStarter.AddBehavior(new RFWarStrategicMemoryBehavior());
        campaignGameStarter.AddBehavior(new RFWarCoalitionRoleBehavior());
        campaignGameStarter.AddBehavior(new RFWarOperationalRhythmBehavior());
        campaignGameStarter.AddBehavior(new RFWarTheaterBehavior());
        campaignGameStarter.AddBehavior(new RFWarFrontlineBehavior());
        campaignGameStarter.AddBehavior(new RFWarCampaignPhaseBehavior());
        campaignGameStarter.AddBehavior(new RFWarObjectiveChainBehavior());
        campaignGameStarter.AddBehavior(new RFWarCampaignDirectorBehavior());
        campaignGameStarter.AddBehavior(new RFWarDecisionPlannerBehavior());
        campaignGameStarter.AddBehavior(new RFWarSpecialAuthorityBehavior());
    }

    public static void Register(CampaignGameStarter campaignGameStarter)
    {
        DiplomacyModel? diplomacyModel = RFWarSystemGameModelHelper.GetGameModel<DiplomacyModel>(campaignGameStarter);
        if (diplomacyModel != null)
        {
            campaignGameStarter.AddModel(new RFWarSystemDiplomacyModel(diplomacyModel));
        }

        TargetScoreCalculatingModel? targetScoreModel = RFWarSystemGameModelHelper.GetGameModel<TargetScoreCalculatingModel>(campaignGameStarter);
        if (targetScoreModel != null)
        {
            campaignGameStarter.AddModel(new RFWarSystemTargetScoreCalculatingModel(targetScoreModel));
        }
    }
}

internal static class RFWarSystemGameModelHelper
{
    public static T? GetGameModel<T>(IGameStarter gameStarterObject) where T : GameModel
    {
        GameModel[] array = gameStarterObject.Models.ToArray();
        for (int index = array.Length - 1; index >= 0; --index)
        {
            if (array[index] is T gameModel)
            {
                return gameModel;
            }
        }

        return default;
    }
}

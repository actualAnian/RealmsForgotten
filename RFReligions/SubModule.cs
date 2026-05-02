using HarmonyLib;
using RealmsForgotten.Behaviors;
using RealmsForgotten.Patches;
using RealmsForgotten.Quest;
using RealmsForgotten.RFReligions.Behavior;
using RealmsForgotten.RFReligions.Core;
using RealmsForgotten.RFReligions.Models;
using RFReligions.Behavior;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using static RealmsForgotten.RFReligions.Patches.MainPatch;

namespace RealmsForgotten.RFReligions;

public class SubModule : MBSubModuleBase
{
    protected override void OnApplicationTick(float _deltaTime)
    {
        if (Campaign.Current != null && Mission.Current == null && Input.IsKeyReleased(InputKey.R))
            ReligionBehavior.Instance?.TriggerReligionMenuEvent();
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnCampaignStart(game, gameStarterObject);
        if (gameStarterObject is CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddBehavior(new ReligionBehavior());
            campaignGameStarter.AddBehavior(new ReligiousWarBehavior());
            campaignGameStarter.AddBehavior(new CrusadeBehavior());

            campaignGameStarter.AddModel(new ReligionPartyMoraleModel());
            campaignGameStarter.AddModel(new ReligionSettlementLoyaltyModel());
            campaignGameStarter.AddModel(new ReligionPartySpeedModel());
        }
    }
    Harmony harmony = new("com.realmsforgotten.religion");

    protected override void OnSubModuleLoad()
    {
        harmony.PatchAll();
    }

    bool manualPatchesHaveFired = false;
    public override void OnGameInitializationFinished(Game game)
    {
        base.OnGameInitializationFinished(game);
        //Globals.SetRacesIds();
        if (!manualPatchesHaveFired)
        {
            manualPatchesHaveFired = true;
            RunManualPatches();
        }
    }
    private void RunManualPatches()
    {
#pragma warning disable BHA0003 // Type was not found
        MethodInfo originalMethod = AccessTools.Method("EncyclopediaHeroPageVM:Refresh");
#pragma warning restore BHA0003 // Type was not found
        harmony.Patch(originalMethod, postfix: new HarmonyMethod(typeof(EncyclopediaHeroPageVMPatch), nameof(EncyclopediaHeroPageVMPatch.Postfix)));
        QuestPatches.PatchAll();
    }

}
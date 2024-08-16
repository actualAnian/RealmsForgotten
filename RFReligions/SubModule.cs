using System;
using HarmonyLib;
using RealmsForgotten.RFReligions.Behavior;
using RealmsForgotten.RFReligions.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFReligions;

public class SubModule : MBSubModuleBase
{
    protected override void OnApplicationTick(float _deltaTime)
    {
        if (Campaign.Current != null && Mission.Current == null && Input.IsKeyReleased((InputKey)19))
            ReligionBehavior.Instance.TriggerReligionMenuEvent();
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnCampaignStart(game, gameStarterObject);
        if (gameStarterObject is CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddBehavior(new ReligionBehavior());

            campaignGameStarter.AddModel(new ReligionPartyMoraleModel());
            campaignGameStarter.AddModel(new ReligionSettlementLoyaltyModel());
        }
    }

    protected override void OnSubModuleLoad()
    {
        var harmony = new Harmony("com.realmsforgotten.religion");
        harmony.PatchAll();
    }
}
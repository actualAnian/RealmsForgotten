// Decompiled with JetBrains decompiler
// Type: RFSmithing.SubModule
// Assembly: RFSmithing.1.2.9, Version=1.1.5.0, Culture=neutral, PublicKeyToken=null
// MVID: 996038D3-2903-49B0-AC80-6C36395EF6AC
// Assembly location: C:\Users\Pedro\Desktop\RFSmithing.1.2.9.dll

using System;
using System.Linq;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using RealmsForgotten.Smithing.Behavior;
using RealmsForgotten.Smithing.Mixins;
using RealmsForgotten.Smithing.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Smithing
{
  public class SubModule : MBSubModuleBase
  {
    private static readonly string Namespace = typeof (SubModule).Namespace;
    private readonly UIExtender _extender = new (Namespace);
    private readonly Harmony _harmony = new (Namespace);

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
      base.OnGameStart(game, gameStarterObject);
      if (gameStarterObject is not CampaignGameStarter campaignGameStarter)
        return;
      
      SmithingModel smithingModel = GetGameModel<SmithingModel>(gameStarterObject) ?? throw new InvalidOperationException("Default SmithingModel was not found.");
      SettlementEconomyModel settlementEconomyModel = GetGameModel<SettlementEconomyModel>(gameStarterObject) ?? throw new InvalidOperationException("Default SettlementEconomyModel was not found.");

      
      campaignGameStarter.AddModel(new RFSmithingModel(smithingModel));
      campaignGameStarter.AddModel(new RFSettlementEconomyModel(settlementEconomyModel));

      campaignGameStarter.AddBehavior(new TownKardrathiumBehavior());
    }

    public override void OnGameLoaded(Game game, object initializerObject)
    {
      base.OnGameLoaded(game, initializerObject);
      if (initializerObject is not CampaignGameStarter campaignGameStarter)
        return;
      InitializeObjects(campaignGameStarter);
    }

    public override void OnNewGameCreated(Game game, object initializerObject)
    {
      base.OnNewGameCreated(game, initializerObject);
      if (initializerObject is not CampaignGameStarter campaignGameStarter)
        return;
      InitializeObjects(campaignGameStarter);
    }

    private void InitializeObjects(CampaignGameStarter campaignGameStarter)
    {
      new RFItems().RegisterAll();
      
    }

    protected override void OnSubModuleLoad()
    {
      base.OnSubModuleLoad();
      CraftingMixin.ApplyPatches(_harmony);
      _extender.Register(typeof (SubModule).Assembly);
      _extender.Enable();
      _harmony.PatchAll();
    }

    private static T? GetGameModel<T>(IGameStarter gameStarterObject) where T : GameModel
    {
      GameModel[] array = gameStarterObject.Models.ToArray<GameModel>();
      for (int index = array.Length - 1; index >= 0; --index)
      {
        if (array[index] is T gameModel)
          return gameModel;
      }
      return default;
    }
  }
}

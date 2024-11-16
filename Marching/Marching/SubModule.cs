// Decompiled with JetBrains decompiler
// Type: Marching.SubModule
// Assembly: Marching, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: FAB07C52-9EF1-4E87-B983-D3A51612112E
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Marching\bin\Win64_Shipping_Client\Marching.dll

using HarmonyLib;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;


#nullable enable
namespace Marching
{
  public class SubModule : MBSubModuleBase
  {
    protected virtual void OnSubModuleLoad() => base.OnSubModuleLoad();

    protected virtual void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
      base.OnGameStart(game, gameStarterObject);
      if (GlobalSettings<MarchGlobalConfig>.Instance.ArtemisSupport)
        new Harmony("com.marching").PatchAll();
      if (gameStarterObject is CampaignGameStarter campaignGameStarter)
        campaignGameStarter.AddModel((GameModel) new MarchingAgentStatCalculateModel(((IGameStarter) campaignGameStarter).GetExistingModel<AgentStatCalculateModel>()));
      else
        gameStarterObject.AddModel((GameModel) new CustomMarchingAgentStatCalculateModel(gameStarterObject.GetExistingModel<AgentStatCalculateModel>()));
    }

    public virtual void OnMissionBehaviorInitialize(Mission mission)
    {
      base.OnMissionBehaviorInitialize(mission);
      mission.AddMissionBehavior((MissionBehavior) new MarchMissionBehavior());
    }
  }
}

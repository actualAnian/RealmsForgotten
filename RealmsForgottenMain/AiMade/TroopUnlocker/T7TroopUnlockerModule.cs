// Decompiled with JetBrains decompiler
// Type: T7TroopUnlocker.T7TroopUnlockerModule
// Assembly: T7TroopUnlocker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A0557F34-96D3-471D-9F55-0537187F801E
// Assembly location: F:\Nexus Mods\1.2.11\T7TroopUnlockerHarmony for v1.1.X and 1.2.X ONLY-4205-1-0-3-1715852778\T7TroopUnlocker\bin\Win64_Shipping_Client\T7TroopUnlocker.dll

using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace T7TroopUnlocker
{
  public class T7TroopUnlockerModule : MBSubModuleBase
  {
    protected virtual void OnSubModuleLoad() => base.OnSubModuleLoad();

    protected virtual void OnSubModuleUnloaded() => base.OnSubModuleUnloaded();

    protected virtual void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
      base.OnGameStart(game, gameStarterObject);
      if (!(gameStarterObject is CampaignGameStarter starter))
        return;
      if (this.ReplaceTroopPartyModel(starter))
        InformationManager.DisplayMessage(new InformationMessage("Tier 7 Troop Unlocker Mod : Mod Successfully loaded", new Color(0.0f, 1f, 0.0f, 1f)));
      else
        InformationManager.DisplayMessage(new InformationMessage("Tier 7 Troop Unlocker Mod : Mod Partially loaded, Tier 7 won't work as expected (see mod post for more info)", new Color(1f, 0.0f, 0.0f, 1f)));
    }

    private bool ReplaceTroopPartyModel(CampaignGameStarter starter)
    {
      int configTroopMaxTier = this.getConfigTroopMaxTier();
      bool flag;
      if (!(starter.Models is IList<GameModel> models))
      {
        flag = false;
      }
      else
      {
        for (int index = 0; index < models.Count; ++index)
        {
          if (models[index] is DefaultCharacterStatsModel)
          {
            models[index] = (GameModel) new CustomDefaultCharacterStatsModel(configTroopMaxTier);
            return true;
          }
        }
        flag = false;
      }
      return flag;
    }

    private int getConfigTroopMaxTier() => 7;
  }
}

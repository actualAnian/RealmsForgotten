// Decompiled with JetBrains decompiler
// Type: T7TroopUnlocker.CustomDefaultCharacterStatsModel
// Assembly: T7TroopUnlocker, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A0557F34-96D3-471D-9F55-0537187F801E
// Assembly location: F:\Nexus Mods\1.2.11\T7TroopUnlockerHarmony for v1.1.X and 1.2.X ONLY-4205-1-0-3-1715852778\T7TroopUnlocker\bin\Win64_Shipping_Client\T7TroopUnlocker.dll

using TaleWorlds.CampaignSystem.GameComponents;

namespace T7TroopUnlocker
{
  public class CustomDefaultCharacterStatsModel : DefaultCharacterStatsModel
  {
    private readonly int troopMaxTier;

    public CustomDefaultCharacterStatsModel(int troopMaxTier) => this.troopMaxTier = troopMaxTier;

    public virtual int MaxCharacterTier => 7;
  }
}

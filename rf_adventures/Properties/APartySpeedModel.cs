// Decompiled with JetBrains decompiler
// Type: Adventurer.Properties.APartySpeedModel
// Assembly: Adventurer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 43A1DA2D-A2AB-4DDA-92AE-88A9DCF8A904
// Assembly location: F:\Nexus Mods\1.2.12\1.2.10-6594-1-2-10-1721143941\Adventurer\bin\Win64_Shipping_Client\Adventurer.dll

using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

#nullable disable
namespace Adventurer.Properties;

public class APartySpeedModel : PartySpeedModel
{
  private PartySpeedModel _previousModel;

  public APartySpeedModel(PartySpeedModel previousModel)
  {
    this._previousModel = previousModel;
    if (this._previousModel != null)
      return;
    this._previousModel = (PartySpeedModel) new DefaultPartySpeedCalculatingModel();
  }

  public virtual ExplainedNumber CalculateBaseSpeed(
    MobileParty party,
    bool includeDescriptions = false,
    int additionalTroopOnFootCount = 0,
    int additionalTroopOnHorseCount = 0)
  {
    ExplainedNumber baseSpeed = this._previousModel.CalculateBaseSpeed(party, includeDescriptions, additionalTroopOnFootCount, additionalTroopOnHorseCount);
    if (party.IsBandit)
      ((ExplainedNumber) ref baseSpeed).AddFactor(0.1f, new TextObject("{=ZKdiaaFo}Brigand", (Dictionary<string, object>) null));
    return baseSpeed;
  }

  public virtual ExplainedNumber CalculateFinalSpeed(
    MobileParty mobileParty,
    ExplainedNumber finalSpeed)
  {
    return this._previousModel.CalculateFinalSpeed(mobileParty, finalSpeed);
  }

  public virtual float BaseSpeed => this._previousModel.BaseSpeed;

  public virtual float MinimumSpeed => this._previousModel.MinimumSpeed;
}

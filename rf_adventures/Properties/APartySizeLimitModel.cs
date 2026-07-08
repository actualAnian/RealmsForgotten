// Decompiled with JetBrains decompiler
// Type: Adventurer.Properties.APartySizeLimitModel
// Assembly: Adventurer, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 43A1DA2D-A2AB-4DDA-92AE-88A9DCF8A904
// Assembly location: F:\Nexus Mods\1.2.12\1.2.10-6594-1-2-10-1721143941\Adventurer\bin\Win64_Shipping_Client\Adventurer.dll

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;

#nullable disable
namespace Adventurer.Properties;

public class APartySizeLimitModel : PartySizeLimitModel
{
  private PartySizeLimitModel _previousModel;
  private readonly TextObject _baseSizeText = GameTexts.FindText("str_base_size", (string) null);

  public APartySizeLimitModel(PartySizeLimitModel previousModel)
  {
    this._previousModel = previousModel;
    if (this._previousModel != null)
      return;
    this._previousModel = (PartySizeLimitModel) new DefaultPartySizeLimitModel();
  }

  public virtual ExplainedNumber GetPartyMemberSizeLimit(PartyBase party, bool includeDescriptions = false)
  {
    ExplainedNumber partyMemberSizeLimit = this._previousModel.GetPartyMemberSizeLimit(party, includeDescriptions);
    if (party.MobileParty.IsCaravan || party.MobileParty.IsVillager)
      ((ExplainedNumber) ref partyMemberSizeLimit).AddFactor(-0.4f, this._baseSizeText);
    return partyMemberSizeLimit;
  }

  public virtual ExplainedNumber GetPartyPrisonerSizeLimit(
    PartyBase party,
    bool includeDescriptions = false)
  {
    return this._previousModel.GetPartyPrisonerSizeLimit(party, includeDescriptions);
  }

  public virtual int GetTierPartySizeEffect(int tier)
  {
    return this._previousModel.GetTierPartySizeEffect(tier);
  }

  public virtual int GetAssumedPartySizeForLordParty(
    Hero leaderHero,
    IFaction partyMapFaction,
    Clan actualClan)
  {
    this._previousModel.GetAssumedPartySizeForLordParty(leaderHero, partyMapFaction, actualClan);
    return 0;
  }
}

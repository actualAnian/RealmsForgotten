using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.Smithing.Mixins;
using RealmsForgotten.Smithing.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static TaleWorlds.Core.ArmorComponent;

namespace RealmsForgotten.Smithing.Models;

public class RFSettlementEconomyModel : DefaultSettlementEconomyModel
{
    private SettlementEconomyModel _model;

    public RFSettlementEconomyModel(SettlementEconomyModel model) => _model = model;
    
    public override float GetDailyDemandForCategory(Town town, ItemCategory category, int extraProsperity)
    {
        if (category == RFItems.KardrathiumCategory)
        {
            return 0f;
        }
        return _model.GetDailyDemandForCategory(town, category, extraProsperity);
    }
}
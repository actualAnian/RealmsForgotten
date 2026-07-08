using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Models
{
    public class RFSettlementValueModel : SettlementValueModel
    {
        SettlementValueModel _previousModel;
        public RFSettlementValueModel(SettlementValueModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override float CalculateSettlementBaseValue(Settlement settlement)
        {
            return _previousModel.CalculateSettlementBaseValue(settlement);
        }

        public override float CalculateSettlementValueForEnemyHero(Settlement settlement, Hero hero)
        {
            return _previousModel.CalculateSettlementValueForEnemyHero(settlement, hero);
        }

        public override float CalculateSettlementValueForFaction(Settlement settlement, IFaction faction)
        {
            return _previousModel.CalculateSettlementValueForFaction(settlement, faction);
        }

        public override Settlement FindMostSuitableHomeSettlement(Clan clan)
        {
            if (clan.Culture?.StringId == "mage")
            {
                Settlement settlement = Settlement.All.First(s => s.Culture.StringId == "mage");
                if (settlement != null) return settlement;        
            } 
            return _previousModel.FindMostSuitableHomeSettlement(clan);
        }
    }
}

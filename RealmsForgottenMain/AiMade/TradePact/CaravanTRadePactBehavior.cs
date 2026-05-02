using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.TradePact
{
    public class CaravanTradePactBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnCaravanTick);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnCaravanTick(MobileParty party)
        {
            if (!party.IsCaravan || party.LeaderHero?.MapFaction == null || party.TargetSettlement == null)
                return;

            var faction = party.LeaderHero.MapFaction;

            // Se já está indo para cidade aliada com pacto, não muda
            if (EconomicPactManager.TryGetPact(faction, party.TargetSettlement.OwnerClan?.Kingdom, out var activePact))
                return;

            // Busca cidades com pacto e ordena pela prosperidade
            var candidate = Town.AllTowns
                .Where(t => EconomicPactManager.HasActivePact(faction, t.OwnerClan?.Kingdom))
                .OrderByDescending(t => t.Prosperity)
                .FirstOrDefault();

            if (candidate != null && candidate.Settlement != party.TargetSettlement)
            {
                party.SetMoveGoToSettlement(candidate.Settlement, MobileParty.NavigationType.Default, party.TargetSettlement.HasPort);
            }
        }
    }
}
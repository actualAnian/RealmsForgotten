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

        // Cache por hora de campanha: a lista de cidades com pacto, ordenada por
        // prosperidade, é a MESMA para todas as caravanas de uma facção dentro da mesma
        // hora. Antes cada caravana refazia o LINQ sobre Town.AllTowns no seu tick
        // horário (~100 caravanas × ~100 cidades, com sort, toda hora de jogo).
        private static readonly Dictionary<IFaction, Town> _bestPactTownCache = new Dictionary<IFaction, Town>();
        private static CampaignTime _cacheBuiltAt = CampaignTime.Zero;

        private void OnCaravanTick(MobileParty party)
        {
            if (!party.IsCaravan || party.LeaderHero?.MapFaction == null || party.TargetSettlement == null)
                return;

            var faction = party.LeaderHero.MapFaction;

            // Se já está indo para cidade aliada com pacto, não muda
            if (EconomicPactManager.TryGetPact(faction, party.TargetSettlement.OwnerClan?.Kingdom, out var activePact))
                return;

            var candidate = FindBestPactTown(faction);

            if (candidate != null && candidate.Settlement != party.TargetSettlement)
            {
                party.SetMoveGoToSettlement(candidate.Settlement, MobileParty.NavigationType.Default, party.TargetSettlement.HasPort);
            }
        }

        private static Town FindBestPactTown(IFaction faction)
        {
            if ((CampaignTime.Now - _cacheBuiltAt).ToHours >= 1d)
            {
                _bestPactTownCache.Clear();
                _cacheBuiltAt = CampaignTime.Now;
            }

            if (_bestPactTownCache.TryGetValue(faction, out Town cached))
            {
                return cached;
            }

            Town best = Town.AllTowns
                .Where(t => EconomicPactManager.HasActivePact(faction, t.OwnerClan?.Kingdom))
                .OrderByDescending(t => t.Prosperity)
                .FirstOrDefault();

            // null tambem entra no cache: "sem pacto" e o caso comum e era o que mais varria.
            _bestPactTownCache[faction] = best;
            return best;
        }
    }
}
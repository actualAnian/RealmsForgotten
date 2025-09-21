using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using static TaleWorlds.CampaignSystem.Army;


namespace RealmsForgotten.AiMade
{
    public class AIBreakInBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        private void OnHourlyTick(MobileParty party)
        {
            if (party == null || !party.IsLordParty || party.IsMainParty || party.IsBandit || party.LeaderHero == null || party.MapFaction == null)
                return;

            // Find nearby sieges
            var nearbySieges = Settlement.All
                .Where(s => s.IsUnderSiege && party.Position.DistanceSquared(s.GatePosition) < 900f) // 30*30 radius
                .ToList();

            if (!nearbySieges.Any())
                return;

            foreach (var settlement in nearbySieges)
            {
                // Is the settlement's faction the same as the party's faction?
                bool isFriendlyDefense = settlement.MapFaction == party.MapFaction;

                // Is the party friendly to the player? (Handles neutral parties helping the player)
                bool isFriendlyToPlayer = party.LeaderHero.Clan == Clan.PlayerClan ||
                                          party.MapFaction == Hero.MainHero.MapFaction ||
                                          Hero.MainHero.GetRelation(party.LeaderHero) >= 50;

                // Is the player the one besieging this settlement?
                bool isPlayerSieging = MobileParty.MainParty.BesiegedSettlement == settlement;

                // Determine if the party should intervene
                if (!isFriendlyDefense && !(isPlayerSieging && isFriendlyToPlayer))
                    continue;

                // Find the besieging army
                var siegeEvent = settlement.SiegeEvent;
                if (siegeEvent == null)
                    continue;

                // Get the lead party of the besieging army
                var besiegingParty = siegeEvent.BesiegerCamp.LeaderParty;
                if (besiegingParty == null || besiegingParty == party)
                    continue;

                float aiStrength = party.Party.EstimatedStrength;
                float enemyStrength = besiegingParty.Party.EstimatedStrength;

                // If stronger, attack the besieger. If not, move to the settlement to support.
                if (aiStrength > enemyStrength)
                {
                    // **Corrected Move Command**
                    party.SetMoveEngageParty(besiegingParty, MobileParty.NavigationType.All);
                }
                else
                {
                    // **Corrected Move Command**
                    party.SetMoveGoToSettlement(settlement, MobileParty.NavigationType.All, false);
                }
            }
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
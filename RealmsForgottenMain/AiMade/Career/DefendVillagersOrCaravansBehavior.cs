using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class DefendVillagersOrCaravansBehavior : CampaignBehaviorBase
    {
        private bool hasDefended;
        private int chivalryPointsGained;
        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            PartyBase attackerParty = mapEvent.AttackerSide.LeaderParty;
            PartyBase defenderParty = mapEvent.DefenderSide.LeaderParty;

            if (mapEvent.WinningSide == mapEvent.PlayerSide)
            {
                if (attackerParty != null && IsBanditParty(attackerParty) && IsVillagerOrCaravanParty(defenderParty))
                {
                    ChivalryManager.AddChivalry(Hero.MainHero, 10, true); // Award 10 chivalry points for defending
                    InformationManager.DisplayMessage(new InformationMessage("You have successfully defended the villagers/caravan and gained chivalry points!"));
                }
            }
        }

        private bool IsBanditParty(PartyBase party)
        {
            return party.MobileParty != null && party.MobileParty.PartyComponent.GetType().Name == "BanditPartyComponent";
        }

        private bool IsVillagerOrCaravanParty(PartyBase party)
        {
            if (party.MobileParty != null)
            {
                var componentType = party.MobileParty.PartyComponent.GetType().Name;
                return componentType == "VillagerPartyComponent" || componentType == "CaravanPartyComponent";
            }
            return false;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("hasDefended", ref hasDefended);
            dataStore.SyncData("chivalryPointsGained", ref chivalryPointsGained);
        }
    }
}
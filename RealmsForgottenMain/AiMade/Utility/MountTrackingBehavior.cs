using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Utility
{
    public class MountTrackingBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            // Register to the event when the campaign session is launched
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);

            // Optionally, you can also track when parties are initialized or heroes spawn
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
        }

        // Triggered when the campaign session starts
        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            InformationManager.DisplayMessage(new InformationMessage("Campaign session launched. Tracking parties for mounts."));

            // Check all currently active parties
            foreach (MobileParty party in MobileParty.All)
            {
                LogPartyMounts(party);
            }
        }

        // Triggered when a new hero is created
        private void OnHeroCreated(Hero hero, bool isPlayerHero)
        {
            if (hero != null && hero.PartyBelongedTo != null)
            {
                LogPartyMounts(hero.PartyBelongedTo);
            }
        }

        // Method to log the mounts of all party members
        private void LogPartyMounts(MobileParty party)
        {
            try
            {
                foreach (TroopRosterElement troop in party.MemberRoster.GetTroopRoster())
                {
                    if (troop.Character != null && troop.Character.IsHero)
                    {
                        // Log the hero's mount details
                        LogHeroMountDetails(troop.Character.HeroObject);
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error logging mount details: {ex.Message}"));
            }
        }

        // Method to log a hero's mount details
        private void LogHeroMountDetails(Hero hero)
        {
            if (hero != null && hero.BattleEquipment != null)
            {
                // Check the hero's horse slot (EquipmentIndex.Horse)
                EquipmentElement mountEquipment = hero.BattleEquipment[EquipmentIndex.Horse];
                ItemObject mountItem = mountEquipment.Item;

                if (mountItem != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Hero {hero.Name}'s Mount: {mountItem.Name} (ID: {mountItem.StringId})"));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Hero {hero.Name} has no mount equipped."));
                }
            }
        }

        public override void SyncData(IDataStore dataStore) { }
    }

}



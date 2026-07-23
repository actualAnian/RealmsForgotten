using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Patches
{
    public class RaceCraftingStaminaBehavior : CampaignBehaviorBase
    {
        private const int BaseMaxStamina = 100;
        private const int CustomRaceMaxStamina = 200;  // Max stamina for your custom race
        private const float BaseRecoveryRate = 1.0f;
        private const float CustomRaceRecoveryRateInside = 2.0f;  // Faster recovery inside towns
        private const float CustomRaceRecoveryRateOutside = 1.5f; // Faster recovery outside towns

        private Dictionary<string, int> heroStaminaData = new Dictionary<string, int>();
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("heroStaminaData", ref heroStaminaData);
        }

        // This method is called every hour in the game to apply stamina recovery
        private void OnHourlyTick()
        {
            // Resolve the behavior ONCE: GetCampaignBehavior is a linear scan of
            // ~150 behaviors, and this loop visits ~2000 heroes every game hour.
            ICraftingCampaignBehavior craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
            if (craftingBehavior == null) return;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                // No restriction to player character, now applies to all heroes (both player and NPCs)
                bool isInsideTown = hero.CurrentSettlement != null;
                ApplyCraftingStaminaRecovery(craftingBehavior, hero, isInsideTown);
            }
        }

       
        public void SetMaxCraftingStaminaForRace(Hero hero)
        {
            ICraftingCampaignBehavior craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
            if (craftingBehavior == null || hero == null) return;

           
            if (IsCustomRace(hero.CharacterObject)) // Custom race check
            {
              
                craftingBehavior.SetHeroCraftingStamina(hero, CustomRaceMaxStamina);
            }
            else
            {
               
                craftingBehavior.SetHeroCraftingStamina(hero, BaseMaxStamina);
            }
        }

        
        public float GetCraftingStaminaRecoveryRate(Hero hero, bool isInsideTown)
        {
            if (IsCustomRace(hero.CharacterObject)) // Custom race check
            {
               
                return isInsideTown ? CustomRaceRecoveryRateInside : CustomRaceRecoveryRateOutside;
            }
            else
            {
            
                return BaseRecoveryRate;
            }
        }

       
        public void ApplyCraftingStaminaRecovery(Hero hero, bool isInsideTown)
        {
            ApplyCraftingStaminaRecovery(Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>(), hero, isInsideTown);
        }

        private void ApplyCraftingStaminaRecovery(ICraftingCampaignBehavior craftingBehavior, Hero hero, bool isInsideTown)
        {
            if (craftingBehavior == null || hero == null) return;

            // Get current and max stamina
            int currentStamina = craftingBehavior.GetHeroCraftingStamina(hero);
            int maxStamina = craftingBehavior.GetMaxHeroCraftingStamina(hero);

            // Already full — skip the rate math and the interface write.
            if (currentStamina >= maxStamina) return;

            // Get the appropriate recovery rate for the hero's race
            float recoveryRate = GetCraftingStaminaRecoveryRate(hero, isInsideTown);

            // Calculate stamina to recover (adjust formula as needed)
            int staminaRecovered = (int)(recoveryRate * 10); // Adjust recovery amount here
            int newStamina = Math.Min(currentStamina + staminaRecovered, maxStamina);

            // Apply the recovered stamina
            craftingBehavior.SetHeroCraftingStamina(hero, newStamina);
        }

        // Method to check if a character belongs to the custom race
        public bool IsCustomRace(BasicCharacterObject character)
        {
            // Replace "your_custom_race" with your custom race ID
            return character.Race == FaceGen.GetRaceOrDefault("dwarf");
        }
    }
}
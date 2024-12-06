using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Roster;

namespace RealmsForgotten.AiMade.Models
{
    public class CustomInventoryCapacityModel : DefaultInventoryCapacityModel
    {
        private string specificItemId = "dwarf_backpack"; // Replace with the actual item ID of the custom item

        public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool includeDescriptions = false, int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
        {
            // Get base value from the default calculation
            ExplainedNumber result = base.CalculateInventoryCapacity(mobileParty, includeDescriptions, additionalTroops, additionalSpareMounts, additionalPackAnimals, includeFollowers);

            // Check if the player hero has the specific item equipped
            if (mobileParty.LeaderHero != null)
            {
                Equipment leaderEquipment = mobileParty.LeaderHero.BattleEquipment;
                if (HasSpecificItemEquipped(leaderEquipment))
                {
                    // Increase inventory capacity by a set amount (example: 50) when the specific item is equipped
                    result.Add(50, new TaleWorlds.Localization.TextObject("Bonus from hero equipped item"));
                }
            }

            // Check if any of the troops in the party have the specific item equipped
            foreach (TroopRosterElement troop in mobileParty.MemberRoster.GetTroopRoster())
            {
                // Check each individual troop's equipment
                Equipment troopEquipment = troop.Character.Equipment;
                if (HasSpecificItemEquipped(troopEquipment))
                {
                    // Increase inventory capacity by a set amount for each troop that has the specific item equipped
                    result.Add(20, new TaleWorlds.Localization.TextObject("Bonus from troop equipped item")); // Example: 20 for each troop
                }
            }

            return result;
        }

        private bool HasSpecificItemEquipped(Equipment equipment)
        {
            // Iterate over all equipment slots using EquipmentIndex enum
            foreach (EquipmentIndex index in System.Enum.GetValues(typeof(EquipmentIndex)))
            {
                EquipmentElement equipmentElement = equipment[index];

                if (!equipmentElement.IsEmpty && equipmentElement.Item.StringId == specificItemId)
                {
                    return true; // The specific item is equipped
                }
            }
            return false; // The specific item is not equipped
        }
    }
}

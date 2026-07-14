using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models
{
    public class RFInventoryCapacityModel : InventoryCapacityModel
    {
        readonly InventoryCapacityModel _baseModel;
        public RFInventoryCapacityModel(InventoryCapacityModel baseModel) { _baseModel = baseModel; }
        public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false, int additionalManOnFoot = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
        {
            // Get base capacity from DefaultInventoryCapacityModel (base class)
            ExplainedNumber result = _baseModel.CalculateInventoryCapacity(mobileParty, isCurrentlyAtSea, includeDescriptions, additionalManOnFoot, additionalSpareMounts, additionalPackAnimals, includeFollowers);

            // Add slave bonus if in main party
            if (mobileParty.IsMainParty)
            {
                int index = mobileParty.MemberRoster.FindIndexOfTroop(CulturesCampaignBehavior.SlaveCharacter);
                if (index != -1)
                {
                    var troopElement = mobileParty.MemberRoster.GetElementCopyAtIndex(index);
                    int slaveCount = troopElement.Number;
                    if (slaveCount > 0)
                    {
                        result.Add(slaveCount * 5, new TextObject("{=slaves}Slaves"));
                    }
                }
            }

            if (mobileParty.LeaderHero != null)
            {
                Equipment leaderEquipment = mobileParty.LeaderHero.BattleEquipment;
                if (HasSpecificItemEquipped(leaderEquipment))
                {
                    // Increase inventory capacity by a set amount (example: 50) when the specific item is equipped
                    result.Add(50, new TaleWorlds.Localization.TextObject("Bonus from hero equipped item"));
                }
            }

            // Only scan troop equipment for the MAIN party — this ran the full
            // roster ×4 equipment slots for EVERY party each capacity query
            // (hot path), and only the player's inventory capacity matters here.
            if (mobileParty.IsMainParty)
            {
                foreach (TroopRosterElement troop in mobileParty.MemberRoster.GetTroopRoster())
                {
                    Equipment troopEquipment = troop.Character.Equipment;
                    if (HasSpecificItemEquipped(troopEquipment))
                    {
                        result.Add(20, description: new TaleWorlds.Localization.TextObject("Bonus from troop equipped item"));
                    }
                }
            }



            return result;
        }


        private readonly string specificItemId = "dwarf_backpack";
        private bool HasSpecificItemEquipped(Equipment equipment)
        {
            for (EquipmentIndex equipmentIndex = EquipmentIndex.ArmorItemBeginSlot; equipmentIndex <= EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
            {
                EquipmentElement equipmentElement = equipment[equipmentIndex];

                if (!equipmentElement.IsEmpty && equipmentElement.Item.StringId == specificItemId)
                    return true;
            }
            return false;
        }

        public override ExplainedNumber CalculateTotalWeightCarried(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false) => _baseModel.CalculateTotalWeightCarried(mobileParty, isCurrentlyAtSea, includeDescriptions);
        public override int GetItemAverageWeight() => _baseModel.GetItemAverageWeight();
        public override float GetItemEffectiveWeight(EquipmentElement equipmentElement, MobileParty mobileParty, bool isCurrentlyAtSea, out TextObject description)
        {
            return _baseModel.GetItemEffectiveWeight(equipmentElement, mobileParty, isCurrentlyAtSea, out description);
        }
    }
}

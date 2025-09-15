using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace RealmsForgotten.Models
{
    public class RFInventoryCapacityModel : DefaultInventoryCapacityModel
    {
        public override ExplainedNumber CalculateInventoryCapacity(
            MobileParty mobileParty,
            bool includeDescriptions = false,
            int additionalTroops = 0,
            int additionalSpareMounts = 0,
            int additionalPackAnimals = 0,
            bool includeFollowers = false)
        {
            // Get base capacity from DefaultInventoryCapacityModel (base class)
            ExplainedNumber baseValue = base.CalculateInventoryCapacity(
                mobileParty,
                includeDescriptions,
                additionalTroops,
                additionalSpareMounts,
                additionalPackAnimals,
                includeFollowers);

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
                        baseValue.Add(slaveCount * 5, new TextObject("{=slaves}Slaves"));
                    }
                }
            }

            return baseValue;
        }
    }
}

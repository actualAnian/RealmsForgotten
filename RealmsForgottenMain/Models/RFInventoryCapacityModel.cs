using RealmsForgotten.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace RealmsForgotten.Models;

internal class RFInventoryCapacityModel : DefaultInventoryCapacityModel
{
    public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool includeDescriptions = false,
        int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0,
        bool includeFollowers = false)
    {
        ExplainedNumber baseValue = base.CalculateInventoryCapacity(mobileParty, includeDescriptions, additionalTroops, additionalSpareMounts, additionalPackAnimals, includeFollowers);
        if (mobileParty.IsMainParty)
        {
            int index = mobileParty.MemberRoster.FindIndexOfTroop(CulturesCampaignBehavior.SlaveCharacter);
            if (index != -1)
            {
                var troopElement = mobileParty.MemberRoster.GetElementCopyAtIndex(index);
                baseValue.Add(troopElement.Number * 5, new TextObject("{=slaves}Slaves"));
            }
        }

        return baseValue;
    }
}
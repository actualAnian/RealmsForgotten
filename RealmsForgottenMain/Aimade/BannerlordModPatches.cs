using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade
{
    public static class VassalAndMercenaryOfferCampaignBehaviorPatch
    {
        // If modifying or replacing the return value, return type must be bool
        public static bool MercenaryKingdomSelectionConditionsHoldPostfix(bool result, Kingdom kingdom)
        {
            if (kingdom == null || kingdom.IsEliminated)
            {
                return false; // Modify the return value based on some condition
            }
            return result; // Or just pass through the original result
        }
    }

}
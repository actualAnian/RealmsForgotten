using TaleWorlds.CampaignSystem;

namespace RF_Promoted;

public static class PromotedHelper
{
    public static int GetAdditionalCompanionLimit(Clan clan)
    {
        if (clan != Clan.PlayerClan)
        {
            return 0;
        }

        int bonus = 0;

        if (PromotedSettings.Current.PromotedCompanionsIncreaseLimit)
        {
            PromotedCampaignBehavior? behavior = Campaign.Current?.GetCampaignBehavior<PromotedCampaignBehavior>();
            if (behavior != null)
            {
                bonus += behavior.GetActivePromotedCompanionCount();
            }
        }

        if (PromotedSettings.Current.EnableBonusCompanionLimit)
        {
            bonus += PromotedSettings.Current.BonusCompanionLimitValue;
        }

        return bonus;
    }
}

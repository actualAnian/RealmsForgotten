using TaleWorlds.CampaignSystem;

namespace RF_warsystem
{
    /// <summary>
    /// One-way bridge from RealmsForgotten's Young World system into the war
    /// director. RealmsForgottenMain references RF_warsystem (not the reverse),
    /// so the Young World behavior PUSHES its state here and the war director
    /// reads it locally — no reflection, no back-reference.
    ///
    /// Additive and inert by default: until <see cref="YoungWorldActive"/> is
    /// pushed true, <see cref="GetWarScoreMultiplier"/> always returns 1 and the
    /// director behaves exactly as before.
    /// </summary>
    public static class RFYoungWorldWarBridge
    {
        /// <summary>Days from campaign start during which a young world keeps the
        /// war director calm.</summary>
        public const int CalmPeriodDays = 80;

        /// <summary>Multiplier applied to the war-declaration / objective-push
        /// score during the calm period.</summary>
        public const float CalmMultiplier = 0.35f;

        /// <summary>
        /// Pushed by RealmsForgotten's RFYoungWorldBehavior: true only when the
        /// Young World toggle is on AND it was actually applied to this campaign.
        /// Pushed false otherwise (toggle off, or a non-young saved campaign), so
        /// the calm never leaks into a normal game.
        /// </summary>
        public static bool YoungWorldActive;

        /// <summary>
        /// Returns <see cref="CalmMultiplier"/> while a young world is within its
        /// first <see cref="CalmPeriodDays"/> days, otherwise 1. Never throws.
        /// </summary>
        public static float GetWarScoreMultiplier()
        {
            if (!YoungWorldActive || Campaign.Current == null)
            {
                return 1f;
            }

            // CampaignStartTime.ElapsedDaysUntilNow is the CORRECT elapsed-days
            // source. CampaignTime.Now.ElapsedDaysUntilNow is always 0 in this
            // codebase — do not use it here.
            float days = Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow;
            return days < CalmPeriodDays ? CalmMultiplier : 1f;
        }
    }
}

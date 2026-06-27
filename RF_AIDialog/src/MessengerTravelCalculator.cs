using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    public static class MessengerTravelCalculator
    {
        public static int EstimateDays(Hero? sender, Hero? recipient)
        {
            int minDays = AIConfig.LettersMinDelayDays;
            int maxDays = AIConfig.LettersMaxDelayDays;

            try
            {
                Vec2? from = ResolvePosition(sender);
                Vec2? to = ResolvePosition(recipient);
                if (!from.HasValue || !to.HasValue)
                    return Math.Max(minDays, 3);

                float distance = (from.Value - to.Value).Length;
                int days = Math.Max(minDays, (int)Math.Ceiling(distance / 18f));

                if (sender?.MapFaction != null &&
                    recipient?.MapFaction != null &&
                    FactionManager.IsAtWarAgainstFaction(sender.MapFaction, recipient.MapFaction))
                {
                    days += 1;
                }

                if (sender?.CurrentSettlement != null && recipient?.CurrentSettlement != null &&
                    sender.CurrentSettlement.StringId == recipient.CurrentSettlement.StringId)
                {
                    days = 1;
                }

                return Math.Min(maxDays, Math.Max(minDays, days));
            }
            catch
            {
                return Math.Max(minDays, 3);
            }
        }

        public static bool IsLikelyNearby(Hero? sender, Hero? recipient)
        {
            try
            {
                return EstimateDays(sender, recipient) <= 1;
            }
            catch
            {
                return false;
            }
        }

        private static Vec2? ResolvePosition(Hero? hero)
        {
            if (hero == null)
                return null;

            MobileParty? party = hero.PartyBelongedTo;
            if (party != null)
                return party.GetPosition2D;

            Settlement? settlement = hero.CurrentSettlement ?? hero.HomeSettlement ?? hero.BornSettlement;
            if (settlement != null)
                return settlement.GetPosition2D;

            return null;
        }
    }
}

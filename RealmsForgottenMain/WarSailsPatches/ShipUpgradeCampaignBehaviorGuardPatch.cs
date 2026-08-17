using HarmonyLib;
using NavalDLC;
using NavalDLC.CampaignBehaviors;
using RealmsForgotten.AiMade;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.WarSailsPatches
{
    internal static class ShipUpgradeCampaignBehaviorGuardPatch
    {
        private static readonly HashSet<string> LoggedSettlements = new HashSet<string>();

        internal static bool TryApply(Harmony harmony)
        {
            MethodInfo target = AccessTools.Method(
                typeof(ShipUpgradeCampaignBehavior),
                "OnSettlementEntered",
                new[] { typeof(MobileParty), typeof(Settlement), typeof(Hero) });

            if (target == null)
            {
                RFLogger.Log("[Lifecycle] Optional naval patch skipped | ShipUpgradeCampaignBehavior.OnSettlementEntered target not found.");
                return false;
            }

            harmony.Patch(target, prefix: new HarmonyMethod(typeof(ShipUpgradeCampaignBehaviorGuardPatch), nameof(Prefix)));
            return true;
        }

        public static bool Prefix(MobileParty mobileParty, Settlement settlement)
        {
            if (mobileParty?.IsCaravan != true || settlement?.HasPort != true || !settlement.IsTown)
                return true;

            if (settlement.Town?.GetShipyard() != null)
                return true;

            string settlementId = settlement.StringId ?? settlement.Name?.ToString() ?? "unknown";
            if (LoggedSettlements.Add(settlementId))
                RFLogger.Log($"[NavalShipUpgradeGuard] Skipped caravan ship upgrade at port without shipyard | settlement={settlementId} | party={mobileParty.StringId}");

            return false;
        }
    }
}

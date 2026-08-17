using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(AiMilitaryBehavior), "OnMapEventEnded")]
    internal static class AiMilitaryBehaviorMapEventEndedGuardPatch
    {
        private static bool Prefix(MapEvent mapEvent)
        {
            if (mapEvent == null)
                return false;

            if (mapEvent.RetreatingSide != BattleSideEnum.None ||
                !mapEvent.IsRaid ||
                mapEvent.BattleState != BattleState.AttackerVictory)
                return true;

            MobileParty attacker = mapEvent.AttackerSide?.LeaderParty?.MobileParty;
            if (attacker == MobileParty.MainParty || attacker?.Ai != null)
                return true;

            Debug.Print(
                $"[RF AiMilitaryGuard] Skipped post-raid movement for party without AI | party={attacker?.StringId ?? "null"} | settlement={mapEvent.MapEventSettlement?.StringId ?? "null"}",
                0,
                Debug.DebugColor.Red);

            return false;
        }
    }
}

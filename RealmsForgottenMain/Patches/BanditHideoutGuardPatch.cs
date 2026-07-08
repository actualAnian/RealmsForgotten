using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Patches
{
    // Vanilla GetInfestedHideoutCount indexes _hideouts[banditFaction.Culture] directly,
    // but that dictionary only contains cultures that own at least one hideout on the map.
    // A bandit clan whose culture has no hideout here (e.g. cs_undead_horde, whose parties
    // are spawned by UndeadHordeBehavior instead of hideouts) crashes new-game bandit
    // spawning with KeyNotFoundException. Report zero infested hideouts instead; the
    // vanilla callers all handle a zero count gracefully.
    [HarmonyPatch(typeof(BanditSpawnCampaignBehavior), "GetInfestedHideoutCount")]
    public static class BanditHideoutGuardPatch
    {
        public static bool Prefix(Clan banditFaction, ref int __result, Dictionary<CultureObject, List<Hideout>> ____hideouts)
        {
            if (banditFaction?.Culture == null || ____hideouts == null || !____hideouts.ContainsKey(banditFaction.Culture))
            {
                __result = 0;
                return false;
            }
            return true;
        }
    }
}

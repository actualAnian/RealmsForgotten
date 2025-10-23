using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Patches
{
    class BanditSpawnPatch
    {
        public static bool Prefix(IFaction faction, ref bool __result)
        {
            __result = faction.StringId == "looters";
            return false;
        }
    }
}
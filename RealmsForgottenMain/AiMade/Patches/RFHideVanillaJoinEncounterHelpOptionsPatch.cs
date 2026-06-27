using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.AiMade.Patches
{
    public static class RFHideVanillaJoinEncounterHelpOptionsPatch
    {
        private static bool ShouldHideHelpOptions()
        {
            MapEvent battle = PlayerEncounter.EncounteredBattle;

            if (battle == null)
                return false;

            if (battle.IsFinalized)
                return false;

            return battle.IsRaid ||
                   battle.IsFieldBattle ||
                   battle.IsSiegeOutside ||
                   battle.IsSiegeAssault ||
                   battle.IsSiegeAmbush ||
                   battle.IsSallyOut;
        }
        public static bool HideVanillaHelpAttackersInVillageRaid(MenuCallbackArgs args, ref bool __result)
        {
            if (ShouldHideHelpOptions())
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static bool HideVanillaHelpDefendersInVillageRaid(MenuCallbackArgs args, ref bool __result)
        {
            if (ShouldHideHelpOptions())
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}

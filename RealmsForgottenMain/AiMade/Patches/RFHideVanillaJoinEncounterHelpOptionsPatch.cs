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

            // Se não tem batalha, não faz sentido mostrar "help"
            if (battle == null)
                return true;

            // Evento já finalizado -> não mexe mais
            if (battle.IsFinalized)
                return true;

            // Raid em vila -> é exatamente o caso do seu sistema
            if (battle.IsRaid)
            {
                Settlement settlement = battle.MapEventSettlement;
                if (settlement != null && settlement.IsVillage)
                    return true;
            }

            return false;
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
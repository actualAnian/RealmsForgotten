using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.AiMade.Patches
{
    /// <summary>
    /// Esconde as opções vanilla "Help {ATTACKER}" e "Help {DEFENDER}" no menu join_encounter
    /// quando o encontro é um raid de vila (ou o MapEvent já está em estado inválido).
    /// Assim o jogador só usa as opções do mod e evitamos o crash que você viu.
    /// </summary>
    //[HarmonyPatch(typeof(EncounterGameMenuBehavior))]
    public static class RFHideVanillaJoinEncounterHelpOptionsPatch
    {
        /// <summary>
        /// True quando NÃO queremos que as opções vanilla apareçam:
        /// - raid em vila, ou
        /// - batalha nula/finalizada (estado estranho pós-combate).
        /// </summary>
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

        // Esconde "Help {ATTACKER}."
        [HarmonyPatch("game_menu_join_encounter_help_attackers_on_condition")]
        [HarmonyPrefix]
        public static bool HideVanillaHelpAttackersInVillageRaid(MenuCallbackArgs args, ref bool __result)
        {
            if (ShouldHideHelpOptions())
            {
                __result = false;
                return false;
            }
            return true;
        }

        // Esconde "Help {DEFENDER}." / "Help {village}"
        [HarmonyPatch("game_menu_join_encounter_help_defenders_on_condition")]
        [HarmonyPrefix]
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

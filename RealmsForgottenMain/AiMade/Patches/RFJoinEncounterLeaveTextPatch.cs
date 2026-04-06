using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Patches
{
    public static class RFJoinEncounterLeaveTextPatch
    {
        private static bool IsVillageRaidAlreadyOver(out MapEvent raidEvent)
        {
            raidEvent = PlayerEncounter.EncounteredBattle;
            if (raidEvent == null)
                return false;

            if (!raidEvent.IsRaid)
                return false;

            Settlement settlement = raidEvent.MapEventSettlement;
            if (settlement == null || !settlement.IsVillage)
                return false;

            // Nenhum atacante saudável => raid acabou de fato
            if (raidEvent.AttackerSide == null ||
                raidEvent.AttackerSide.GetTotalHealthyTroopCountOfSide() <= 0)
                return true;

            return false;
        }
        public static bool JoinEncounterLeaveConditionPrefix(MenuCallbackArgs args, ref bool __result)
        {
            // Caso especial: raid de vila que já acabou
            if (IsVillageRaidAlreadyOver(out MapEvent raidEvent))
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;

                // Texto mais lógico que "Don't get involved."
                MBTextManager.SetTextVariable(
                    "LEAVE_TEXT",
                    new TextObject("{=rf_raid_over_leave}The raid is already over. Leave the area.")
                );

                __result = true;   // condição OK, opção aparece com esse texto
                return false;      // pula o método vanilla
            }

            // Caso normal -> deixa o código original rodar (mostra "Don't get involved.")
            return true;
        }
    }
}

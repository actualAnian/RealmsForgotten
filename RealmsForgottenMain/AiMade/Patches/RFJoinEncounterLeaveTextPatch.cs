using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Patches
{
    public static class RFJoinEncounterLeaveTextPatch
    {
        private static bool IsEncounterBattleAlreadyOver(out MapEvent battleEvent)
        {
            battleEvent = PlayerEncounter.EncounteredBattle;
            if (battleEvent == null)
                return false;

            if (!(battleEvent.IsRaid || battleEvent.IsFieldBattle || battleEvent.IsSiegeOutside || battleEvent.IsSiegeAssault || battleEvent.IsSiegeAmbush || battleEvent.IsSallyOut))
                return false;

            return battleEvent.AttackerSide == null ||
                   battleEvent.DefenderSide == null ||
                   battleEvent.AttackerSide.GetTotalHealthyTroopCountOfSide() <= 0 ||
                   battleEvent.DefenderSide.GetTotalHealthyTroopCountOfSide() <= 0;
        }
        public static bool JoinEncounterLeaveConditionPrefix(MenuCallbackArgs args, ref bool __result)
        {
            if (IsEncounterBattleAlreadyOver(out _))
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;

                MBTextManager.SetTextVariable(
                    "LEAVE_TEXT",
                    new TextObject("{=rf_battle_over_leave}The battle is already over. Leave the area.")
                );

                __result = true;
                return false;
            }

            return true;
        }
    }
}

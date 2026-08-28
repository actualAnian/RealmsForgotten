using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using Helpers;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using System.Reflection;

namespace RealmsForgotten.AiMade
{
    public class RFJoinRaidEncounterBehavior : CampaignBehaviorBase
    {
        private const int AttackersOptionIndex = 3;
        private const int DefendersOptionIndex = 4;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddGameMenus(starter);
        }

        private void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "town_outside",
                "rf_open_join_siege_event",
                "Take part in the siege",
                OpenJoinSiegeEventCondition,
                OpenJoinSiegeEventConsequence,
                isLeave: false,
                index: 3
            );

            starter.AddGameMenuOption(
                "castle_outside",
                "rf_open_join_siege_event",
                "Take part in the siege",
                OpenJoinSiegeEventCondition,
                OpenJoinSiegeEventConsequence,
                isLeave: false,
                index: 3
            );

            starter.AddGameMenuOption(
                "join_encounter",
                "rf_join_raid_as_attacker",
                "Join the attackers",
                JoinRaidAttackerCondition,
                JoinRaidAttackerConsequence,
                isLeave: false,
                index: AttackersOptionIndex
            );

            starter.AddGameMenuOption(
                "join_encounter",
                "rf_join_raid_as_defender",
                "Join the defenders",
                JoinRaidDefenderCondition,
                JoinRaidDefenderConsequence,
                isLeave: false,
                index: DefendersOptionIndex
            );

            starter.AddGameMenuOption(
                "join_siege_event",
                "rf_join_siege_as_attacker",
                "Join the attackers",
                JoinSiegeAttackerCondition,
                JoinSiegeAttackerConsequence,
                isLeave: false,
                index: 0
            );

            starter.AddGameMenuOption(
                "join_siege_event",
                "rf_join_siege_as_defender",
                "Join the defenders",
                JoinSiegeDefenderCondition,
                JoinSiegeDefenderConsequence,
                isLeave: false,
                index: 1
            );
        }

        private static bool HasJoinableSiegeSettlement(out Settlement settlement)
        {
            settlement = PlayerEncounter.EncounterSettlement ?? Settlement.CurrentSettlement;
            return settlement != null
                   && settlement.IsFortification
                   && settlement.IsUnderSiege
                   && settlement.SiegeEvent != null;
        }

        private static bool TryGetJoinableBattle(out MapEvent battleEvent)
        {
            battleEvent = PlayerEncounter.EncounteredBattle;
            if (battleEvent == null || battleEvent.IsFinalized)
                return false;

            bool isSupportedBattle =
                battleEvent.IsRaid ||
                battleEvent.IsFieldBattle ||
                battleEvent.IsSiegeOutside ||
                battleEvent.IsSiegeAssault ||
                battleEvent.IsSiegeAmbush ||
                battleEvent.IsSallyOut;

            if (!isSupportedBattle)
                return false;

            if (battleEvent.AttackerSide == null || battleEvent.DefenderSide == null)
                return false;

            if (battleEvent.AttackerSide.GetTotalHealthyTroopCountOfSide() <= 0 ||
                battleEvent.DefenderSide.GetTotalHealthyTroopCountOfSide() <= 0)
                return false;

            return true;
        }

        private static bool IsVillageRaid(MapEvent battleEvent)
        {
            if (battleEvent == null || !battleEvent.IsRaid)
                return false;

            Settlement settlement = battleEvent.MapEventSettlement;
            return settlement != null && settlement.IsVillage;
        }

        private bool OpenJoinSiegeEventCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

            if (!HasJoinableSiegeSettlement(out Settlement settlement))
                return false;

            args.IsEnabled = true;
            args.Tooltip = new TextObject("{=rf_join_siege_entry_tooltip}Choose whether to support the attackers or defenders in the siege around this settlement.");
            MBTextManager.SetTextVariable("SETTLEMENT", settlement.Name);
            return true;
        }

        private void OpenJoinSiegeEventConsequence(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("join_siege_event");
        }

        private static void EnsureVillageDefenders(MapEvent battleEvent)
        {
            Settlement settlement = battleEvent.MapEventSettlement;
            if (settlement == null || !settlement.IsVillage)
                return;

            MapEventSide defenderSide = battleEvent.DefenderSide;
            if (defenderSide == null)
                return;

            if (defenderSide.TroopCount > 0)
                return;

            PartyBase defenderLeader = defenderSide.LeaderParty ?? settlement.Party;
            if (defenderLeader == null)
                return;

            var roster = defenderLeader.MemberRoster;
            if (roster == null)
                return;

            if (settlement.Culture?.BasicTroop == null)
                return;

            if (roster.TotalManCount > 0)
                return;

            float militiaValue = settlement.Village?.Militia ?? 0f;
            int militiaCount = (int)(militiaValue / 5f);

            if (militiaCount < 10)
                militiaCount = 10;
            if (militiaCount > 80)
                militiaCount = 80;

            if (militiaCount <= 0)
                return;

            roster.AddToCounts(settlement.Culture.BasicTroop, militiaCount);
        }

        private bool JoinRaidAttackerCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;

            if (!TryGetJoinableBattle(out MapEvent battleEvent))
                return false;

            if (IsVillageRaid(battleEvent))
            {
                args.Tooltip = new TextObject("{=rf_join_custom_attackers_raid}Throw in with the raiders and share their spoils if they prevail.");
            }
            else if (battleEvent.IsFieldBattle)
            {
                args.Tooltip = new TextObject("{=rf_join_custom_attackers_field}Ride in to reinforce the attacking side of the battle.");
            }
            else
            {
                args.Tooltip = new TextObject("{=rf_join_custom_attackers_siege}Join the attacking side of this siege or assault in progress.");
            }

            args.IsEnabled = true;
            return true;
        }

        private void JoinRaidAttackerConsequence(MenuCallbackArgs args)
        {
            if (!TryGetJoinableBattle(out MapEvent battleEvent))
                return;

            if (IsVillageRaid(battleEvent))
                EnsureVillageDefenders(battleEvent);

            if (PlayerEncounter.InsideSettlement &&
                PlayerEncounter.EncounterSettlement != null &&
                PlayerEncounter.EncounterSettlement.IsUnderSiege)
            {
                PlayerEncounter.LeaveSettlement();
            }

            PlayerEncounter.JoinBattle(BattleSideEnum.Attacker);
            // Menu de encounter vanilla, NAO direto pra batalha: e la que moram
            // "Attack!" E "Send your troops" — pular pro EncounterAttackConsequence
            // roubava a opcao de mandar so as tropas (feedback do autor 2026-08-27).
            // Mesmo idioma do caminho de cerco logo abaixo.
            GameMenu.SwitchToMenu("encounter");
        }

        private bool JoinRaidDefenderCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;

            if (!TryGetJoinableBattle(out MapEvent battleEvent))
                return false;

            if (IsVillageRaid(battleEvent))
            {
                args.Tooltip = new TextObject("{=rf_join_custom_defenders_raid}Protect the village and answer for any blood you spill on either side.");
            }
            else if (battleEvent.IsFieldBattle)
            {
                args.Tooltip = new TextObject("{=rf_join_custom_defenders_field}Ride in to reinforce the defending side of the battle.");
            }
            else
            {
                args.Tooltip = new TextObject("{=rf_join_custom_defenders_siege}Join the defenders and fight to break or hold the siege.");
            }

            args.IsEnabled = true;
            return true;
        }

        private void JoinRaidDefenderConsequence(MenuCallbackArgs args)
        {
            if (!TryGetJoinableBattle(out MapEvent battleEvent))
                return;

            if (IsVillageRaid(battleEvent))
                EnsureVillageDefenders(battleEvent);

            if (PlayerEncounter.InsideSettlement &&
                PlayerEncounter.EncounterSettlement != null &&
                PlayerEncounter.EncounterSettlement.IsUnderSiege)
            {
                PlayerEncounter.LeaveSettlement();
            }

            PlayerEncounter.JoinBattle(BattleSideEnum.Defender);
            // Ver comentario no lado atacante: o menu "encounter" preserva o
            // "Send your troops" alem do "Attack!".
            GameMenu.SwitchToMenu("encounter");
        }

        private bool JoinSiegeAttackerCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;

            if (!HasJoinableSiegeSettlement(out _))
                return false;

            args.IsEnabled = true;
            args.Tooltip = new TextObject("{=rf_join_custom_attackers_siege_long}Take the attackers' side. If the assault has already begun, you will enter the battle immediately. Otherwise you will join the siege camp and preparations.");
            return true;
        }

        private void JoinSiegeAttackerConsequence(MenuCallbackArgs args)
        {
            if (!HasJoinableSiegeSettlement(out Settlement settlement))
                return;

            if (settlement.Party.MapEvent != null)
            {
                PlayerEncounter.JoinBattle((!settlement.Party.MapEvent.IsSallyOut) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
                GameMenu.SwitchToMenu("encounter");
                return;
            }

            if (Hero.MainHero.CurrentSettlement != null)
                PlayerEncounter.LeaveSettlement();

            PlayerEncounter.Finish();
            MobileParty.MainParty.BesiegerCamp = settlement.SiegeEvent.BesiegerCamp;
            if (TryStartPlayerSiege(BattleSideEnum.Attacker, settlement))
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.UnstoppablePlay;
        }

        private bool JoinSiegeDefenderCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;

            if (!HasJoinableSiegeSettlement(out _))
                return false;

            args.IsEnabled = true;
            args.Tooltip = new TextObject("{=rf_join_custom_defenders_siege_long}Take the defenders' side. If the assault has already begun, you will enter the battle immediately. Otherwise you will attempt to break in and support the garrison.");
            return true;
        }

        private void JoinSiegeDefenderConsequence(MenuCallbackArgs args)
        {
            if (!HasJoinableSiegeSettlement(out Settlement settlement))
                return;

            if (settlement.Party.MapEvent != null)
            {
                PlayerEncounter.JoinBattle((!settlement.Party.MapEvent.IsSallyOut) ? BattleSideEnum.Defender : BattleSideEnum.Attacker);
                GameMenu.SwitchToMenu("encounter");
                return;
            }

            GameMenu.SwitchToMenu("break_in_menu");
        }

        private static bool TryStartPlayerSiege(BattleSideEnum side, Settlement settlement)
        {
            try
            {
                Type playerSiegeType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a =>
                    {
                        try { return a.GetType("TaleWorlds.CampaignSystem.PlayerSiege", false); }
                        catch { return null; }
                    })
                    .FirstOrDefault(t => t != null);

                if (playerSiegeType == null)
                    return false;

                MethodInfo startPlayerSiege = playerSiegeType.GetMethod(
                    "StartPlayerSiege",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(BattleSideEnum), typeof(bool), typeof(Settlement) },
                    null);

                MethodInfo startSiegePreparation = playerSiegeType.GetMethod(
                    "StartSiegePreparation",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (startPlayerSiege == null || startSiegePreparation == null)
                    return false;

                startPlayerSiege.Invoke(null, new object[] { side, false, settlement });
                startSiegePreparation.Invoke(null, Array.Empty<object>());
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

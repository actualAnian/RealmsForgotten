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

namespace RealmsForgotten.AiMade
{
    public class RFJoinRaidEncounterBehavior : CampaignBehaviorBase
    {
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
            // Join on the raiders' side
            starter.AddGameMenuOption(
                "join_encounter",
                "rf_join_raid_as_attacker",
                "Join the raid (attackers)",
                JoinRaidAttackerCondition,
                JoinRaidAttackerConsequence,
                isLeave: false,
                index: 3
            );

            // Join defending the village
            starter.AddGameMenuOption(
                "join_encounter",
                "rf_join_raid_as_defender",
                "Defend the village",
                JoinRaidDefenderCondition,
                JoinRaidDefenderConsequence,
                isLeave: false,
                index: 4
            );
        }

        /// <summary>
        /// Same source vanilla uses: PlayerEncounter.EncounteredBattle.
        /// We only care if it's a raid on a village.
        /// </summary>
        private static bool TryGetRaidBattle(out MapEvent raidEvent)
        {
            raidEvent = PlayerEncounter.EncounteredBattle;
            if (raidEvent == null || !raidEvent.IsRaid)
                return false;

            Settlement settlement = raidEvent.MapEventSettlement;
            if (settlement == null || !settlement.IsVillage)
                return false;

            return true;
        }

        /// <summary>
        /// Ensure there are some defending troops for the village in this raid.
        /// This is used for BOTH attacker and defender options, so defenders exist either way.
        /// </summary>
        private static void EnsureVillageDefenders(MapEvent raidEvent)
        {
            Settlement settlement = raidEvent.MapEventSettlement;
            if (settlement == null || !settlement.IsVillage)
                return;

            MapEventSide defenderSide = raidEvent.DefenderSide;
            if (defenderSide == null)
                return;

            // If there are already defenders, we don't touch it
            if (defenderSide.TroopCount > 0)
                return;

            // Prefer the defender leader's party, fall back to the settlement party
            PartyBase defenderLeader = defenderSide.LeaderParty ?? settlement.Party;
            if (defenderLeader == null)
                return;

            var roster = defenderLeader.MemberRoster;
            if (roster == null)
                return;

            if (settlement.Culture?.BasicTroop == null)
                return;

            // Also don't spam if the party already has troops for some reason
            if (roster.TotalManCount > 0)
                return;

            // Turn some of the village militia value into actual battle troops
            float militiaValue = settlement.Village?.Militia ?? 0f;

            // Tune this to taste – 1/5 of militia as combatants
            int militiaCount = (int)(militiaValue / 5f);

            // Clamp to reasonable min/max
            if (militiaCount < 10)
                militiaCount = 10;
            if (militiaCount > 80)
                militiaCount = 80;

            if (militiaCount <= 0)
                return;

            roster.AddToCounts(settlement.Culture.BasicTroop, militiaCount);
        }

        // =======================
        //   ATTACKER OPTION
        // =======================

        private bool JoinRaidAttackerCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;

            if (!TryGetRaidBattle(out _))
                return false;

            // We want this always clickable whenever it's a village raid
            args.IsEnabled = true;
            return true;
        }

        private void JoinRaidAttackerConsequence(MenuCallbackArgs args)
        {
            if (!TryGetRaidBattle(out MapEvent raidEvent))
                return;

            // Make sure there are *some* defenders in this raid
            EnsureVillageDefenders(raidEvent);

            // Handle edge case: player "inside" a besieged settlement
            if (PlayerEncounter.InsideSettlement &&
                PlayerEncounter.EncounterSettlement != null &&
                PlayerEncounter.EncounterSettlement.IsUnderSiege)
            {
                PlayerEncounter.LeaveSettlement();
            }

            // Join the existing MapEvent on the attacker side
            PlayerEncounter.JoinBattle(BattleSideEnum.Attacker);

            // Use the same logic vanilla uses for "Attack" – this opens the battle mission.
            MenuHelper.EncounterAttackConsequence(args);
        }

        // =======================
        //   DEFENDER OPTION
        // =======================

        private bool JoinRaidDefenderCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Mission;

            if (!TryGetRaidBattle(out _))
                return false;

            args.IsEnabled = true;
            return true;
        }

        private void JoinRaidDefenderConsequence(MenuCallbackArgs args)
        {
            if (!TryGetRaidBattle(out MapEvent raidEvent))
                return;

            // Also ensure defenders exist when we defend
            EnsureVillageDefenders(raidEvent);

            if (PlayerEncounter.InsideSettlement &&
                PlayerEncounter.EncounterSettlement != null &&
                PlayerEncounter.EncounterSettlement.IsUnderSiege)
            {
                PlayerEncounter.LeaveSettlement();
            }

            // Join the defender side
            PlayerEncounter.JoinBattle(BattleSideEnum.Defender);

            // Same attack flow -> proper mission starts
            MenuHelper.EncounterAttackConsequence(args);
        }
    }
}

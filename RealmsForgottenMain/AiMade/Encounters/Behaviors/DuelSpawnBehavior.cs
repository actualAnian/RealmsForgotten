using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Encounters.Behaviors
{
    public class DuelSpawnBehavior : CampaignBehaviorBase
    {
        private Hero _duelTarget;

        // ------------------------------  Boilerplate  ------------------------------
        public override void RegisterEvents()
        {
            // pick a target when you ENTER a town
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);

            // add menu item & dialogue AFTER all XML and menus are loaded
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_duelTarget", ref _duelTarget);
        }

        // ------------------------------  Event handlers  ---------------------------
        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party != MobileParty.MainParty || !settlement.IsTown)
                return;

            // first lord in town who is not the player
            _duelTarget = settlement.HeroesWithoutParty?
                                    .FirstOrDefault(h => h.IsLord && h != Hero.MainHero);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // --------------  Town‑menu option  --------------
            starter.AddGameMenuOption(
                "town",                        // menu id
                "rf_duel_option",              // option id
                "Challenge the local lord to a duel", // generic text (e1.1.x can’t change per‑frame)
                DuelOptionCondition,
                DuelOptionConsequence,
                isLeave: false,
                index: 5);

            // --------------  Dialogue flow  ----------------
            starter.AddDialogLine("rf_duel_intro", "start", "rf_duel_player_choice",
                "So, you dare challenge me?", DuelIntroCondition, null);

            starter.AddPlayerLine("rf_duel_accept", "rf_duel_player_choice", "rf_duel_end",
                "Prepare yourself!", null, DuelAcceptConsequence);

            starter.AddPlayerLine("rf_duel_decline", "rf_duel_player_choice", "close_window",
                "Perhaps another time…", null, DuelDeclineConsequence);

            starter.AddDialogLine("rf_duel_end", "rf_duel_end", "close_window",
                "Then let steel decide our fates!", null, null);
        }

        // ------------------------------  Menu logic  ------------------------------
        private bool DuelOptionCondition(MenuCallbackArgs args)
        {
            if (_duelTarget == null)
                return false; // hide option if no lord

            // In ≤e1.1.x you can’t rename the option per‑frame; just keep generic text
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private void DuelOptionConsequence(MenuCallbackArgs args)
        {
            if (_duelTarget == null)
                return; // safety

            // open a standard conversation from inside the town menu (safe)
            CampaignMapConversation.OpenConversation(
                new ConversationCharacterData(CharacterObject.PlayerCharacter),
                new ConversationCharacterData(_duelTarget.CharacterObject));
        }

        // ------------------------------  Conversation logic  -----------------------
        private bool DuelIntroCondition() => Hero.OneToOneConversationHero == _duelTarget;

        private void DuelAcceptConsequence()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[TEST] Duel with {_duelTarget.Name} would start now."));
            _duelTarget = null; // clear so the menu item disappears next time
        }

        private void DuelDeclineConsequence() => _duelTarget = null;
    }
}

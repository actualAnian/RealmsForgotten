using Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Quest
{
    public class QuestHelperCampaignBehavior : CampaignBehaviorBase
    {
        const int _maxTroopCountInHideout = 40;
        const int _maxBanditsInHideout = 80;
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }
        private void OnGameLoaded(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("hideout_place", "attack", "Storm the hideout",
                new GameMenuOption.OnConditionDelegate(this.game_menu_attack_hideout_parties_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_attack_on_consequence), false, -1, false, null);
            starter.AddGameMenuOption("hideout_place", "send_troops", "{=qPwxYFQS}Send troops to clear",
                new GameMenuOption.OnConditionDelegate(this.game_menu_send_troops_hideout_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_attack_on_consequence), false, -1, false, null);
        }
        public static void TryAddQuest2HideoutText()
        {
            if (IsInHideoutForQuest2())
                GameTexts.SetVariable("HIDEOUT_TEXT", "{=rf_quest_reached_hideout}You have reached the site indicated by The Owl. The bandits occupying this area are believed to possess a fragment of the map you were tasked to obtain.");
        }
        public static bool IsInHideoutForQuest2()
        {
            if (Settlement.CurrentSettlement == null) return false;
            foreach (var quest in Campaign.Current.QuestManager.Quests)
            {
                if (quest.StringId == "rf_queen_quest")
                {
                    foreach (KeyValuePair<ITrackableCampaignObject, List<QuestBase>> obj in Campaign.Current.QuestManager.TrackedObjects)
                    {
                        if (obj.Value[0].StringId == "rf_queen_quest"
                            && obj.Key is Settlement sett
                            && sett.StringId == Settlement.CurrentSettlement.StringId)
                            return true;
                    }
                    return false;
                }
            }
            return false;
        }

        private void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args)
        {
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }

            TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
            TroopRoster strongestAndPriorTroops = MobilePartyHelper.GetStrongestAndPriorTroops(MobileParty.MainParty, _maxTroopCountInHideout, true);
            troopRoster.Add(strongestAndPriorTroops);
            args.MenuContext.OpenTroopSelection(MobileParty.MainParty.MemberRoster, troopRoster, new Func<CharacterObject, bool>(CanChangeStatusOfTroop), new Action<TroopRoster>(OnTroopRosterManageDone), _maxTroopCountInHideout, 1);
        }
        private void OnTroopRosterManageDone(TroopRoster roster)
        {
            int currentBandits = 0;
            foreach (var party in Settlement.CurrentSettlement.Parties)
            {
                if (!party.IsBandit) continue;
                if (currentBandits + party.MemberRoster.TotalHealthyCount > _maxBanditsInHideout)
                {
                    var troopRoster = party.MemberRoster.GetTroopRoster();
                    for (int i = 0; i < troopRoster.Count; i++)
                    {
                        if (currentBandits >= _maxBanditsInHideout)
                        {
                            party.MemberRoster.AddToCountsAtIndex(i, 1 - troopRoster[i].Number);
                            currentBandits += 1;
                        }
                        else if (currentBandits + troopRoster[i].Number > _maxBanditsInHideout)
                        {
                            int numToAdd = _maxBanditsInHideout - currentBandits;
                            party.MemberRoster.AddToCountsAtIndex(i, numToAdd - troopRoster[i].Number);
                            currentBandits += numToAdd;
                        }
                        else
                        {
                            currentBandits += troopRoster[i].Number;
                        }
                    }
                }
                else
                    currentBandits += party.MemberRoster.TotalHealthyCount;
            }
            CampaignMission.OpenHideoutBattleMission(Settlement.CurrentSettlement.LocationComplex.GetScene("hideout_center", 0), roster.ToFlattenedRoster(), false);
        }
        private bool CanChangeStatusOfTroop(CharacterObject character)
        {
            return !character.IsPlayerCharacter && !character.IsNotTransferableInHideouts;
        }

        private bool game_menu_attack_hideout_parties_on_condition(MenuCallbackArgs args)
        {
            if (!IsInHideoutForQuest2()) return false;
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction; 
            if (Hero.MainHero.IsWounded)
            {
                args.IsEnabled = false;
                args.Tooltip = new TextObject("{=pM9GOxrV}You are wounded, you can't sneak in!", null);
            }
            return true;
        }
        private bool game_menu_send_troops_hideout_on_condition(MenuCallbackArgs args)
        {
            if (!IsInHideoutForQuest2()) return false;
            args.IsEnabled = false;
            args.Tooltip = new TextObject("send troop option is unavailable for this hideout", null);
            args.optionLeaveType = GameMenuOption.LeaveType.OrderTroopsToAttack;
            return true;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}

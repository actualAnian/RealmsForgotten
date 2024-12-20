﻿using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using RealmsForgotten.RFCustomSettlements;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Actions;
using RealmsForgotten;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace RFCustomSettlements
{
    public class ArenaCampaignBehavior : CampaignBehaviorBase
    {
        private static Settlement? arenaSettlement;
        private ArenaSettlementStateHandler? _arenaSettlementStateHandler;
        private ArenaSettlementStateHandler ArenaSettlementStateHandler { 
            get
            { 
                if(_arenaSettlementStateHandler == null)
                {
                    _arenaSettlementStateHandler = (ArenaSettlementStateHandler)(arenaSettlement?.SettlementComponent as RFCustomSettlement).StateHandler;
                }
                return _arenaSettlementStateHandler;
            }
        }
        private readonly string playerArenMasterTalkEquipmentId = "rf_arena_prisoner";
        private readonly string playerArenaLostEquipmentId = "rf_arena_prisoner";

        internal static string? currentChallengeToSync;
        internal static ArenaSettlementStateHandler.ArenaState currentArenaState;
        internal static bool isWaiting;

        public ArenaCampaignBehavior() 
        {
            currentArenaState = ArenaSettlementStateHandler.ArenaState.Visiting;
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
        }
        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            //this.AddDialogs(campaignGameStarter); 
            this.AddGameMenus(campaignGameStarter); 
            arenaSettlement = Settlement.All.FirstOrDefault(s => s.SettlementComponent is RFCustomSettlement settlement && settlement.StateHandler is ArenaSettlementStateHandler);
        }
        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenu("rf_taken_to_arena", "The camp of your captors stirs, as the captives are rounded and you hear rumours - you are to be taken to the infamous colossum, where slaves fight for the enjoyment of the masses!", delegate(MenuCallbackArgs args) { args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale? "arena_captured_female" : "arena_male_captured_b"); },
                GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("rf_taken_to_arena", "rf_taken_to_arena_continue", "I will need all my strength to survive what's to come...", delegate(MenuCallbackArgs args) { return true; },
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_taken_to_arena_on_consequence), false, -1, false, null);
            campaignGameStarter.AddGameMenu("rf_arena_finish", "You are victorious yet again! As a clear audience favourite, the arena master returns your freedom, amidst a grand ceremony. There are more ways to keep the public engaged than just through bloodshed, eh?", new OnInitDelegate(rf_arena_finish_init), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("rf_arena_finish", "rf_start_battle_continue", "Now I know the value of freedom.", null, new GameMenuOption.OnConsequenceDelegate(rf_arena_finish_consequence));
            campaignGameStarter.AddGameMenu("rf_arena_player_lost", "{=!}{RF_ARENA_LOSE_TEXT}", new OnInitDelegate(rf_arena_lost_on_init), GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("rf_arena_player_lost", "rf_arena_player_lost_continue", "{=!}{RF_ARENA_LOSE_CONTINUE_TEXT}", null, new GameMenuOption.OnConsequenceDelegate(rf_arena_player_lost_consequence));
        }

        private void rf_arena_finish_init(MenuCallbackArgs args)
        {
            args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale ? "arena_female_win" : "arena_male_win");
            Equipment playerEquipment = ArenaSettlementStateHandler.PlayerArenaRewardEquipment != null ? ArenaSettlementStateHandler.PlayerArenaRewardEquipment : MBObjectManager.Instance.GetObject<CharacterObject>("aserai_infantry").Equipment;
            if(playerEquipment != null) Hero.MainHero.BattleEquipment.FillFrom(playerEquipment);
            int amount = 20;
            Hero.MainHero.Clan.AddRenown(amount);
            InformationManager.DisplayMessage(new InformationMessage("Your renown increases by: " + amount));
        }

        private void rf_arena_lost_on_init(MenuCallbackArgs args)
        {
            if (Globals.Settings.PunishingArenaDefeats)
            {
                GameTexts.SetVariable("RF_ARENA_LOSE_TEXT", "Your fate is in the hands of your opponent... the crowd is silent as he approaches you, you wouldn't give him any mercy, why would he? The blade rises before a strike...");
                GameTexts.SetVariable("RF_ARENA_LOSE_CONTINUE_TEXT", "My journey ends here");
            }
            else
            {
                args.MenuContext.SetBackgroundMeshName(Hero.MainHero.IsFemale ? "arena_looser_female" : "arena_looser_male");
                GameTexts.SetVariable("RF_ARENA_LOSE_TEXT", "Your fate is in the hands of your opponent... the crowd is silent as he approaches you, 'Pathetic' you hear him say, as he spits and walks away from you. You may have been spared, but not for long. As a disgraced warrior, you are taken into the great desert, to perish under the scorching sun");
                GameTexts.SetVariable("RF_ARENA_LOSE_CONTINUE_TEXT", "You haven't heard the last word from me...");
                int amount = -20;
                Hero.MainHero.Clan.AddRenown(amount);
                InformationManager.DisplayMessage(new InformationMessage("Your renown decreases by: " + MathF.Abs(amount)));
            }
        }

        private void rf_arena_player_lost_consequence(MenuCallbackArgs args)
        {
            if (Globals.Settings.PunishingArenaDefeats)
            {
                PlayerEncounter.LeaveSettlement();
                PlayerEncounter.Finish(true);
                KillCharacterAction.ApplyByWounds(Hero.MainHero, true);
            }
            else ExpelPlayerFromArena();
        }

        private void ExpelPlayerFromArena()
        {
            MBEquipmentRoster equipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(playerArenaLostEquipmentId);
            try { Hero.MainHero.BattleEquipment.FillFrom(equipmentRoster.DefaultEquipment); }
            catch { RealmsForgotten.HuntableHerds.SubModule.PrintDebugMessage($"Error giving the player equipment {playerArenMasterTalkEquipmentId}", 255, 0, 0); }
            Hero.MainHero.HitPoints = Hero.MainHero.MaxHitPoints * 1 / 10;
            PlayerEncounter.LeaveSettlement();
            PlayerEncounter.Finish(true);
            Clan.PlayerClan.AddRenown(-10);
            ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.Visiting;
        }

        private void rf_arena_finish_consequence(MenuCallbackArgs args)
        {
            ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.Visiting;
            GameMenu.SwitchToMenu("rf_settlement_start");
            
        }

        private void game_menu_taken_to_arena_on_consequence(MenuCallbackArgs args)
        {
            if (arenaSettlement != null)
            {
                PlayerCaptivity.EndCaptivity();
                Hero.MainHero.PartyBelongedTo.Position2D = arenaSettlement.GatePosition;

                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, arenaSettlement);
                ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.Captured;
                GameMenu.ActivateGameMenu("rf_settlement_start");
                try { 
                MBEquipmentRoster equipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(playerArenMasterTalkEquipmentId);
                Hero.MainHero.BattleEquipment.FillFrom(equipmentRoster.DefaultEquipment);
                }
                catch { RealmsForgotten.HuntableHerds.SubModule.PrintDebugMessage($"Error giving the player equipment {playerArenMasterTalkEquipmentId}", 255, 0, 0); }
            }
        }

        //private void AddDialogs(CampaignGameStarter campaignGameStarter)
        //{
        //    campaignGameStarter.AddDialogLine("rf_arena_explain", "start", "rf_arena_understood", "Explain what happens in the arena", new ConversationSentence.OnConditionDelegate(this.condition_start_arena_talk), null, 100, null);
        //    campaignGameStarter.AddPlayerLine("rf_arena_understood", "rf_arena_understood", "close_window", "I see", null, new ConversationSentence.OnConsequenceDelegate(this.StartArenaMission), 100, null, null);
        //}
        private void StartArenaMission()
        {
            Mission.Current.EndMission();
            ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.FightStage1;
        }
#pragma warning disable IDE1006 // Naming Styles
        private bool condition_start_arena_talk()
#pragma warning restore IDE1006 // Naming Styles
        {
            //return false;
            return Settlement.CurrentSettlement != null &&  Settlement.CurrentSettlement.SettlementComponent != null && Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement && CharacterObject.OneToOneConversationCharacter.StringId == "caravan_master_aserai";
        }
        public override void SyncData(IDataStore dataStore)
        {
            if(dataStore.IsSaving)
            { 
                currentArenaState = ArenaSettlementStateHandler.currentState;
                if (ArenaSettlementStateHandler.currentChallenge != null)
                    currentChallengeToSync = ArenaSettlementStateHandler.currentChallenge.ChallengeName;
                isWaiting = ArenaSettlementStateHandler.hasToWait;
            }
            dataStore.SyncData("current_arena_state", ref currentArenaState);
            dataStore.SyncData("current_challenge_name", ref currentChallengeToSync);
            dataStore.SyncData("arena_is_waiting", ref isWaiting);
        }
        public static void TeleportCapturedPlayerToArena()
        {
            if(arenaSettlement == null) GameMenu.SwitchToMenu("menu_captivity_end_no_more_enemies");
            else
            {
                GameMenu.SwitchToMenu("rf_taken_to_arena");
            }

        }
    }
}

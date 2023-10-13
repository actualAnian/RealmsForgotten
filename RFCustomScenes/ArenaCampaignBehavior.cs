using System;
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

namespace RFCustomSettlements
{
    public class ArenaCampaignBehavior : CampaignBehaviorBase
    {
        private static Settlement? arenaSettlement;
        private static ArenaSettlementStateHandler? arenaSettlementStateHandler;
        public ArenaCampaignBehavior() 
        {
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
        }
        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            this.AddDialogs(campaignGameStarter); 
            this.AddGameMenus(campaignGameStarter);
            try
            {
                arenaSettlement = Settlement.All.Single(s => s.SettlementComponent is RFCustomSettlement && ((RFCustomSettlement)s.SettlementComponent).StateHandler is ArenaSettlementStateHandler);
                arenaSettlementStateHandler = (ArenaSettlementStateHandler)(arenaSettlement.SettlementComponent as RFCustomSettlement).StateHandler;
            }
            catch { RealmsForgotten.HuntableHerds.SubModule.PrintDebugMessage("Error, could not find arena settlement"); }

        }
        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenu("rf_taken_to_arena", "The camp of your captors stirs, as the captives are rounded and you hear rumours - you are to be taken to the infamous colossum, where slaves fight for the enjoyment of the masses!", delegate(MenuCallbackArgs args) { args.MenuContext.SetBackgroundMeshName("wait_captive_female"); },
                GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("rf_taken_to_arena", "rf_taken_to_arena_continue", "I will need all my strength to survive what's to come...", delegate(MenuCallbackArgs args) { return true; },
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_taken_to_arena_on_consequence), false, -1, false, null);
            campaignGameStarter.AddGameMenu("rf_arena_finish", "You are victorious yet again! As a clear audience favourite, the arena master returns your freedom, amidst a grand ceremony. There are more ways to keep the public engaged than just through bloodshed, eh?", delegate(MenuCallbackArgs args) { }, GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.None, null);
            campaignGameStarter.AddGameMenuOption("rf_arena_finish", "rf_start_battle_continue", "Now I know the value of freedom.", null, new GameMenuOption.OnConsequenceDelegate(rf_arena_finish_consequence));
        }

        private void rf_arena_finish_consequence(MenuCallbackArgs args)
        {
            arenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.Visiting;
            GameMenu.SwitchToMenu("rf_settlement_start");
        }

        private void game_menu_taken_to_arena_on_consequence(MenuCallbackArgs args)
        {
            if (arenaSettlement != null)
            {
                PlayerCaptivity.EndCaptivity();
                Hero.MainHero.PartyBelongedTo.Position2D = arenaSettlement.GatePosition;

                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, arenaSettlement);
                arenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.Captured;
                GameMenu.ActivateGameMenu("rf_settlement_start");
            }
        }

        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddDialogLine("rf_arena_explain", "start", "rf_arena_understood", "Explain what happens in the arena", new ConversationSentence.OnConditionDelegate(this.test), null, 100, null);
            campaignGameStarter.AddPlayerLine("rf_arena_understood", "rf_arena_understood", "close_window", "I see", null, new ConversationSentence.OnConsequenceDelegate(this.StartArenaMission), 100, null, null);
        }
        private void StartArenaMission()
        {
            Mission.Current.EndMission();
            arenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.FightStage1;
        }
        private bool test()
        {
            return Settlement.CurrentSettlement != null &&  Settlement.CurrentSettlement.SettlementComponent != null && Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement && CharacterObject.OneToOneConversationCharacter.StringId == "caravan_master_aserai";
        }
        public override void SyncData(IDataStore dataStore)
        {
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

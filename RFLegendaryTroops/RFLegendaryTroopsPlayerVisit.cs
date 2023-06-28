using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Settlements;

namespace RFLegendaryTroops
{
    public class RFLegendaryTroopsPlayerVisit
    //    {
    //        [GameMenuInitializationHandler("castle")]

    //        [GameMenuEventHandler("castle", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
    //        private static void game_menu_recruit_volunteers_on_consequence(MenuCallbackArgs args) => args.MenuContext.OpenRecruitVolunteers();
    //    }




    {
        [GameMenuInitializationHandler("town")]
        [GameMenuInitializationHandler("castle")]
        private static void game_menu_town_on_init(MenuCallbackArgs args)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            args.MenuContext.SetBackgroundMeshName(currentSettlement.Town.WaitMeshName);
        }

        [GameMenuInitializationHandler("town_wait")]
        [GameMenuInitializationHandler("town_guard")]
        [GameMenuInitializationHandler("menu_tournament_withdraw_verify")]
        [GameMenuInitializationHandler("menu_tournament_bet_confirm")]
        [GameMenuInitializationHandler("settlement_alley_after_battle")]
        [GameMenuInitializationHandler("settlement_alley_fight_won")]
        [GameMenuInitializationHandler("settlement_alley_after_wait")]
        [GameMenuInitializationHandler("settlement_alley_after_battle_lose")]
        public static void game_menu_town_menu_on_init(MenuCallbackArgs args) => args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);

        [GameMenuEventHandler("town", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("town", "manage_production_cheat", GameMenuEventHandler.EventType.OnConsequence)]
        public static void game_menu_town_manage_town_on_consequence(MenuCallbackArgs args) => args.MenuContext.OpenTownManagement();

        [GameMenuEventHandler("town_keep", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        public static void game_menu_town_castle_manage_town_on_consequence(MenuCallbackArgs args) => args.MenuContext.OpenTownManagement();

        [GameMenuEventHandler("castle", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        public static void game_menu_castle_manage_castle_on_consequence(MenuCallbackArgs args) => args.MenuContext.OpenTownManagement();

        [GameMenuEventHandler("tutorial", "mno_go_back_dot", GameMenuEventHandler.EventType.OnConsequence)]
        private static void mno_go_back_dot(MenuCallbackArgs args)
        {
        }

        [GameMenuEventHandler("village", "buy_goods", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_village_buy_good_on_consequence(MenuCallbackArgs args) => InventoryManager.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, (SettlementComponent)Settlement.CurrentSettlement.Village);

        [GameMenuEventHandler("village", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_village_manage_village_on_consequence(MenuCallbackArgs args) => args.MenuContext.OpenTownManagement();

        [GameMenuEventHandler("village", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("town_backstreet", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("town", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("castle", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_recruit_volunteers_on_consequence(MenuCallbackArgs args) => args.MenuContext.OpenRecruitVolunteers();
    }








}



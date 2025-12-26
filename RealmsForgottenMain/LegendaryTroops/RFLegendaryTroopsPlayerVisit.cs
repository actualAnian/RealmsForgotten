using Helpers;
using System;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.LegendaryTroops
{
    public class RFLegendaryTroopsPlayerVisit
    {
        [GameMenuInitializationHandler("town")]
        [GameMenuInitializationHandler("castle")]
        private static void game_menu_town_on_init(MenuCallbackArgs args)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;

            if (currentSettlement == null || currentSettlement.Town == null)
            {
                Console.WriteLine("Error: CurrentSettlement or Town is null in game_menu_town_on_init.");
                return; // Exit the method to prevent further errors
            }

            string waitMeshName = currentSettlement.Town.WaitMeshName ?? "default_mesh";
            args.MenuContext.SetBackgroundMeshName(waitMeshName);
        }

        [GameMenuInitializationHandler("town_wait")]
        [GameMenuInitializationHandler("town_guard")]
        [GameMenuInitializationHandler("menu_tournament_withdraw_verify")]
        [GameMenuInitializationHandler("menu_tournament_bet_confirm")]
        [GameMenuInitializationHandler("settlement_alley_after_battle")]
        [GameMenuInitializationHandler("settlement_alley_fight_won")]
        [GameMenuInitializationHandler("settlement_alley_after_wait")]
        [GameMenuInitializationHandler("settlement_alley_after_battle_lose")]
        public static void game_menu_town_menu_on_init(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement.SettlementComponent == null)
            {
                Console.WriteLine("Error: CurrentSettlement or SettlementComponent is null in game_menu_town_menu_on_init.");
                return; // Exit the method to prevent further errors
            }

            string waitMeshName = Settlement.CurrentSettlement.SettlementComponent.WaitMeshName ?? "default_mesh";
            args.MenuContext.SetBackgroundMeshName(waitMeshName);
        }

        [GameMenuEventHandler("town", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("town", "manage_production_cheat", GameMenuEventHandler.EventType.OnConsequence)]
        public static void game_menu_town_manage_town_on_consequence(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement == null)
            {
                Console.WriteLine("Error: CurrentSettlement is null in game_menu_town_manage_town_on_consequence.");
                return;
            }

            args.MenuContext.OpenTownManagement();
        }

        [GameMenuEventHandler("castle", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        public static void game_menu_castle_manage_castle_on_consequence(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement == null)
            {
                Console.WriteLine("Error: CurrentSettlement is null in game_menu_castle_manage_castle_on_consequence.");
                return;
            }

            args.MenuContext.OpenTownManagement();
        }

        [GameMenuEventHandler("village", "buy_goods", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_village_buy_good_on_consequence(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement.Village == null)
            {
                Console.WriteLine("Error: CurrentSettlement or Village is null in game_menu_village_buy_good_on_consequence.");
                return;
            }

            InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Village);
        }

        [GameMenuEventHandler("village", "manage_production", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_village_manage_village_on_consequence(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement == null)
            {
                Console.WriteLine("Error: CurrentSettlement is null in game_menu_village_manage_village_on_consequence.");
                return;
            }

            args.MenuContext.OpenTownManagement();
        }

        [GameMenuEventHandler("village", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("town_backstreet", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("town", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        [GameMenuEventHandler("castle", "recruit_volunteers", GameMenuEventHandler.EventType.OnConsequence)]
        private static void game_menu_recruit_volunteers_on_consequence(MenuCallbackArgs args)
        {
            if (Settlement.CurrentSettlement == null)
            {
                Console.WriteLine("Error: CurrentSettlement is null in game_menu_recruit_volunteers_on_consequence.");
                return;
            }

            args.MenuContext.OpenRecruitVolunteers();
        }
    }
}



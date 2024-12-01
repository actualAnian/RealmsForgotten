using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party;

namespace RealmsForgotten.AiMade
{
    public class DocksMenuBehavior : CampaignBehaviorBase
    {
        private const int BaseTravelCost = 50;
        private const int CostPerDistanceUnit = 5;
        private readonly string docksMenuPrefix = "town_"; // Prefix for dock menus
        private readonly string chooseDestinationOptionSuffix = "_choose_destination"; // Suffix for choosing destination option
        private readonly string leaveOptionSuffix = "_leave"; // Suffix for leaving option

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        private void OnSessionLaunched(CampaignGameStarter gameStarter)
        {
            AddDocksMenusToAllTowns(gameStarter);
        }

        private void OnGameLoaded(CampaignGameStarter gameStarter)
        {
            AddDocksMenusToAllTowns(gameStarter);
        }

        private void AddDocksMenusToAllTowns(CampaignGameStarter gameStarter)
        {
            foreach (var settlement in Settlement.All)
            {
                if (settlement.IsTown && IsSpecificTown(settlement))
                {
                    AddDocksMenu(gameStarter, settlement);
                }
            }
        }

        private bool IsSpecificTown(Settlement settlement)
        {
            var townsWithDocks = new List<string> { "town_EM2", "town_V7", "town_S4", "town_EN2", "town_EW2", "town_ES2" }; // Replace with actual town IDs or names
            return townsWithDocks.Contains(settlement.Name.ToString());
        }

        private void AddDocksMenu(CampaignGameStarter gameStarter, Settlement settlement)
        {
            string menuId = $"{docksMenuPrefix}{settlement.Name}_docks";
            gameStarter.AddGameMenu(menuId, "You arrive at the docks. Where would you like to go?", (args) => { });

            gameStarter.AddGameMenuOption(menuId, $"{menuId}{chooseDestinationOptionSuffix}", "Choose a destination", (args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }, (args) => ShowDestinationSubmenu(gameStarter, args), false);

            gameStarter.AddGameMenuOption(menuId, $"{menuId}{leaveOptionSuffix}", "Leave the docks", (args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (args) => GameMenu.SwitchToMenu("town"), true);
        }

        private void ShowDestinationSubmenu(CampaignGameStarter gameStarter, MenuCallbackArgs args)
        {
            var destinationTowns = new List<Settlement>
            {
                Settlement.Find("town_EM2"),
                Settlement.Find("town_V7"),
                Settlement.Find("town_S4"),
                Settlement.Find("town_EN2"),
                Settlement.Find("town_EW2"),
                Settlement.Find("town_ES2")
            };

            foreach (var town in destinationTowns)
            {
                gameStarter.AddGameMenuOption($"{docksMenuPrefix}{town.Name}_submenu", $"travel_to_{town.Name}", $"Travel to {town.Name} (Cost: {CalculateTravelCost(Settlement.CurrentSettlement, town)} gold)",
                (menuCallbackArgs) => true,
                (menuCallbackArgs) => AttemptTravelToTown(town), false);
            }
        }

        private int CalculateTravelCost(Settlement currentTown, Settlement destinationTown)
        {
            float distance = currentTown.Position2D.Distance(destinationTown.Position2D);
            int cost = BaseTravelCost + (int)(distance * CostPerDistanceUnit);
            return cost;
        }

        private void AttemptTravelToTown(Settlement destinationTown)
        {
            int travelCost = CalculateTravelCost(Settlement.CurrentSettlement, destinationTown);
            Hero mainHero = Hero.MainHero;

            if (mainHero.Gold >= travelCost)
            {
                mainHero.ChangeHeroGold(-travelCost);
                TravelToTown(destinationTown);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("You do not have enough gold to travel.", Colors.Red));
            }
        }

        private void TravelToTown(Settlement destinationTown)
        {
            if (destinationTown != null && destinationTown.IsTown)
            {
                MobileParty.MainParty.Position2D = destinationTown.GatePosition;
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, destinationTown);
                InformationManager.DisplayMessage(new InformationMessage($"You have arrived at {destinationTown.Name}.", Colors.Green));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Invalid destination.", Colors.Red));
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No data to sync for persistence since we're only re-adding menus on load
        }
    }
}



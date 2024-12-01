using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RealmsForgotten.RFLegendaryTroops
{
    public class RFLegendaryTroopsPlayerVisitTownCampaignBehavior: CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterNewGameCreated));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnAfterNewGameCreated);
        }

        public void OnAfterNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            this.AddGameMenus(campaignGameStarter);
        }
        public override void SyncData(IDataStore dataStore)
        {
        }
        protected void AddGameMenus(CampaignGameStarter campaignGameSystemStarter)
        {
            campaignGameSystemStarter.AddGameMenuOption("castle", "recruit_volunteers", "{=E31IJyqs}Recruit legendary troops",
                game_menu_castle_recruit_troops_on_condition, game_menu_recruit_volunteers_on_consequence);
        }
        private static bool game_menu_castle_recruit_troops_on_condition(MenuCallbackArgs args)
        {
            bool canPlayerDo = Helper.CanMainHeroRecruit(Settlement.CurrentSettlement, out bool shouldBeDisabled, out TextObject disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }
        private static void game_menu_recruit_volunteers_on_consequence(MenuCallbackArgs args)
        {
        }


    }

}
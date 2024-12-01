using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Religions
{
    public class SettlementReligionBehavior : CampaignBehaviorBase
    {
        private ReligionsManager _religionsManager;

        public SettlementReligionBehavior(ReligionsManager religionsManager)
        {
            _religionsManager = religionsManager;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            AddSettlementReligionsMenu(starter);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddSettlementReligionsMenu(starter);
        }

        private void AddSettlementReligionsMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "show_settlement_religion", "Show Religion Info",
                args => true,
                args =>
                {
                    ShowReligionInfo();
                    args.MenuContext.SwitchToMenu("town");
                },
                index: 2);
        }

        private void ShowReligionInfo()
        {
            var settlement = Settlement.CurrentSettlement;
            if (settlement != null)
            {
                if (_religionsManager.SettlementReligions.TryGetValue(settlement, out var religionData))
                {
                    var religion = religionData.DominantReligion;
                    if (religion != null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"Settlement Religion: {religion.Name}"));
                        DisplayBonusesAndPenalties(religion.Bonuses);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("No dominant religion found."));
                    }
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("No religion data found for this settlement."));
                }
            }
        }

        private void DisplayBonusesAndPenalties(ReligionBonuses bonuses)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Loyalty Bonus: {bonuses.LoyaltyBonus}"));
            InformationManager.DisplayMessage(new InformationMessage($"Loyalty Penalty: {bonuses.LoyaltyPenalty}"));
            InformationManager.DisplayMessage(new InformationMessage($"Prosperity Bonus: {bonuses.ProsperityBonus}"));
            InformationManager.DisplayMessage(new InformationMessage($"Prosperity Penalty: {bonuses.ProsperityPenalty}"));
            InformationManager.DisplayMessage(new InformationMessage($"Growth Bonus: {bonuses.GrowthBonus}"));
            InformationManager.DisplayMessage(new InformationMessage($"Growth Penalty: {bonuses.GrowthPenalty}"));
            InformationManager.DisplayMessage(new InformationMessage($"Recruitment Bonus: {bonuses.RecruitmentBonus}"));
            InformationManager.DisplayMessage(new InformationMessage($"Recruitment Penalty: {bonuses.RecruitmentPenalty}"));
            InformationManager.DisplayMessage(new InformationMessage($"Workshop Production Penalty: {bonuses.WorkshopProductionPenalty}"));
            InformationManager.DisplayMessage(new InformationMessage($"Military Building Speed Bonus: {bonuses.MilitaryBuildingSpeedBonus}"));
            InformationManager.DisplayMessage(new InformationMessage($"Building Speed Penalty: {bonuses.BuildingSpeedPenalty}"));
        }
    }
}


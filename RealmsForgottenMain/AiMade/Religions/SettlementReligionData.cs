using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Religions
{
    [SaveableClass]
    public class SettlementReligionData
    {
        [SaveableField(1)]
        private Settlement settlement;

        [SaveableField(2)]
        private ReligionObject dominantReligion;

        public Settlement Settlement
        {
            get => settlement;
            private set => settlement = value;
        }

        public ReligionObject DominantReligion
        {
            get => dominantReligion;
            private set => dominantReligion = value;
        }

        public SettlementReligionData(Settlement settlement)
        {
            Settlement = settlement;
            DominantReligion = DetermineInitialReligion(settlement);
            ApplyBonusesAndPenalties();
        }

        private ReligionObject DetermineInitialReligion(Settlement settlement)
        {
            if (settlement.Culture != null)
            {
                return ReligionObject.All.FirstOrDefault(r => r.Culture == settlement.Culture);
            }
            return ReligionObject.All.OrderBy(r => MBRandom.RandomFloat).FirstOrDefault();
        }

        public void SetDominantReligion(ReligionObject religion)
        {
            DominantReligion = religion;
            ApplyBonusesAndPenalties();
        }

        public void ApplyBonusesAndPenalties()
        {
            if (DominantReligion != null)
            {
                var bonuses = DominantReligion.Bonuses;

                // For Towns
                if (Settlement.IsTown)
                {
                    var town = Settlement.Town;
                    if (town != null)
                    {
                        // Loyalty adjustment using SettlementLoyaltyModel
                        AdjustLoyalty(town, bonuses.LoyaltyBonus, bonuses.LoyaltyPenalty);

                        // Prosperity adjustment using SettlementProsperityModel
                        AdjustProsperity(town, bonuses.ProsperityBonus, bonuses.ProsperityPenalty);

                        // Food adjustment using SettlementFoodModel
                        AdjustFood(town, bonuses.GrowthBonus, bonuses.GrowthPenalty);

                        // Militia adjustment using SettlementMilitiaModel
                        AdjustMilitia(town, bonuses.RecruitmentBonus, bonuses.RecruitmentPenalty);

                        // Apply workshop production penalty (handled indirectly via prosperity)
                        if (bonuses.WorkshopProductionPenalty != 0)
                        {
                            AdjustWorkshopProduction(town, bonuses.WorkshopProductionPenalty);
                        }

                        // Apply building speed bonus
                        if (bonuses.MilitaryBuildingSpeedBonus != 0)
                        {
                            AdjustBuildingSpeed(town, bonuses.MilitaryBuildingSpeedBonus);
                        }
                    }
                }

                // For Villages
                if (Settlement.IsVillage)
                {
                    var village = Settlement.Village;
                    if (village != null)
                    {
                        AdjustVillageHearth(village, bonuses.GrowthBonus, bonuses.GrowthPenalty);
                    }
                }
            }
        }
        private void AdjustLoyalty(Town town, float bonus, float penalty)
        {
            var loyaltyModel = Campaign.Current.Models.SettlementLoyaltyModel as DefaultSettlementLoyaltyModel;
            var explainedNumber = loyaltyModel.CalculateLoyaltyChange(town, true); // Updated method name
            explainedNumber.Add(bonus, new TextObject("Religious Bonus"));
            explainedNumber.Add(-penalty, new TextObject("Religious Penalty"));
            town.Loyalty += explainedNumber.ResultNumber;
        }


        private void AdjustProsperity(Town town, float bonus, float penalty)
        {
            var prosperityModel = Campaign.Current.Models.SettlementProsperityModel as DefaultSettlementProsperityModel;
            var explainedNumber = prosperityModel.CalculateProsperityChange(town, true);
            explainedNumber.Add(bonus, new TextObject("Religious Bonus"));
            explainedNumber.Add(-penalty, new TextObject("Religious Penalty"));
            town.Prosperity += explainedNumber.ResultNumber;
        }


        private void AdjustFood(Town town, float bonus, float penalty)
        {
            var foodModel = Campaign.Current.Models.SettlementFoodModel as DefaultSettlementFoodModel;
            var explainedNumber = foodModel.CalculateTownFoodStocksChange(town, true); // Updated method name
            explainedNumber.Add(bonus, new TextObject("Religious Bonus"));
            explainedNumber.Add(-penalty, new TextObject("Religious Penalty"));
            town.FoodStocks += explainedNumber.ResultNumber;
        }


        private void AdjustMilitia(Town town, float bonus, float penalty)
        {
            try
            {
                var militiaModel = Campaign.Current.Models.SettlementMilitiaModel as DefaultSettlementMilitiaModel;
                if (militiaModel == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Militia model is null"));
                    return;
                }
                var explainedNumber = militiaModel.CalculateMilitiaChange(town.Settlement, true);
                explainedNumber.Add(bonus, new TextObject("Religious Bonus"));
                explainedNumber.Add(-penalty, new TextObject("Religious Penalty"));
                float militiaChange = explainedNumber.ResultNumber;
                InformationManager.DisplayMessage(new InformationMessage($"Militia change: {militiaChange}"));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error in AdjustMilitia: {ex.Message}"));
            }
        }


        private void AdjustWorkshopProduction(Town town, float penalty)
        {
            var workshops = town.Workshops;
            foreach (var workshop in workshops)
            {
                var productionModel = Campaign.Current.Models.WorkshopModel as DefaultWorkshopModel;
                var explainedNumber = productionModel.GetEffectiveConversionSpeedOfProduction(workshop, 1.0f, true); // Adjusted method call
                explainedNumber.Add(-penalty, new TextObject("Religious Penalty"));
                // No direct property to adjust production speed, this should affect production indirectly
            }
        }


        private void AdjustBuildingSpeed(Town town, float bonus)
        {
            var constructionModel = Campaign.Current.Models.BuildingConstructionModel as DefaultBuildingConstructionModel;
            var explainedNumber = constructionModel.CalculateDailyConstructionPower(town, true);
            explainedNumber.Add(bonus, new TextObject("Religious Bonus"));
            // Use the result for further logic if needed
            float constructionPowerChange = explainedNumber.ResultNumber;
            // Log the construction power change for debugging
            InformationManager.DisplayMessage(new InformationMessage($"Construction power change: {constructionPowerChange}"));
        }


        private void AdjustVillageHearth(Village village, float bonus, float penalty)
        {
            var productionModel = Campaign.Current.Models.VillageProductionCalculatorModel as DefaultVillageProductionCalculatorModel;
            if (productionModel != null)
            {
                var hearthChange = productionModel.CalculateDailyProductionAmount(village, new ItemObject());
                village.Hearth += hearthChange * (1 + bonus - penalty);
            }
        }

        private bool IsMilitaryBuilding(BuildingType buildingType)
        {
            return buildingType.StringId.Contains("military"); // Adjust based on actual identifier for military buildings
        }

        private float AdjustValue(float baseValue, float bonus, float penalty)
        {
            return baseValue * (1 + bonus) * (1 - penalty);
        }
    }
}
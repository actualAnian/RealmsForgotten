using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Extensions;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(DefaultHeroCreationModel), "GetCivilianEquipment")]
    public class GetCivilianEquipmentPatch
    {
        /// <summary>
        /// Prefix patch to handle the case where GetEquipmentRostersForDeliveredOffspring returns empty collection
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(DefaultHeroCreationModel __instance, Hero hero, ref Equipment __result)
        {
            // Only patch for offspring heroes (those with a mother)
            if (hero.Mother == null)
            {
                return true; // Continue with original method
            }

            try
            {
                MBList<MBEquipmentRoster> equipmentRosters = Campaign.Current.Models.EquipmentSelectionModel
                    .GetEquipmentRostersForDeliveredOffspring(hero);

                // Check if we got an empty collection
                if (equipmentRosters == null || equipmentRosters.Count == 0)
                {
                    LogFallback(hero, "GetEquipmentRostersForDeliveredOffspring returned empty collection");
                    __result = GetCultureAppropriateCivilianEquipment(hero);
                    return false; // Skip original method
                }

                // Check if the random roster is null
                MBEquipmentRoster randomRoster = equipmentRosters.GetRandomElementInefficiently();
                if (randomRoster == null)
                {
                    LogFallback(hero, "GetRandomElementInefficiently returned null roster");
                    __result = GetCultureAppropriateCivilianEquipment(hero);
                    return false;
                }

                // Get civilian equipments from the roster
                IEnumerable<Equipment> civilianEquipments = randomRoster.GetCivilianEquipments();
                if (civilianEquipments == null || !civilianEquipments.Any())
                {
                    LogFallback(hero, "No civilian equipments found in roster");
                    __result = GetCultureAppropriateCivilianEquipment(hero);
                    return false;
                }

                // Get random equipment
                Equipment civilianEquipment = civilianEquipments.GetRandomElementInefficiently();
                if (civilianEquipment == null)
                {
                    LogFallback(hero, "GetRandomElementInefficiently returned null equipment");
                    __result = GetCultureAppropriateCivilianEquipment(hero);
                    return false;
                }

                __result = civilianEquipment;
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                // Last resort exception handling
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Error getting civilian equipment for {hero.Name}. Using culture fallback: {ex.Message}",
                    Colors.Red));

                __result = GetCultureAppropriateCivilianEquipment(hero);
                return false;
            }
        }

        /// <summary>
        /// Provides culture-appropriate civilian equipment for offspring
        /// Prioritizes the child's actual culture over template equipment
        /// </summary>
        private static Equipment GetCultureAppropriateCivilianEquipment(Hero hero)
        {
            // Priority 1: Use child's assigned culture default civilian equipment
            if (hero.Culture?.DefaultCivilianEquipmentRoster != null)
            {
                IEnumerable<Equipment> childCultureEquipments = hero.Culture.DefaultCivilianEquipmentRoster.GetCivilianEquipments();
                if (childCultureEquipments != null && childCultureEquipments.Any())
                {
                    return childCultureEquipments.GetRandomElementInefficiently();
                }

                // If no civilian equipments, try all equipments from culture roster
                if (hero.Culture.DefaultCivilianEquipmentRoster.AllEquipments != null &&
                    hero.Culture.DefaultCivilianEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Culture.DefaultCivilianEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            // Priority 2: Use mother's culture (since child inherits culture from parents)
            if (hero.Mother?.Culture?.DefaultCivilianEquipmentRoster != null)
            {
                IEnumerable<Equipment> motherCultureEquipments = hero.Mother.Culture.DefaultCivilianEquipmentRoster.GetCivilianEquipments();
                if (motherCultureEquipments != null && motherCultureEquipments.Any())
                {
                    return motherCultureEquipments.GetRandomElementInefficiently();
                }

                if (hero.Mother.Culture.DefaultCivilianEquipmentRoster.AllEquipments != null &&
                    hero.Mother.Culture.DefaultCivilianEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Mother.Culture.DefaultCivilianEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            // Priority 3: Use father's culture
            if (hero.Father?.Culture?.DefaultCivilianEquipmentRoster != null)
            {
                IEnumerable<Equipment> fatherCultureEquipments = hero.Father.Culture.DefaultCivilianEquipmentRoster.GetCivilianEquipments();
                if (fatherCultureEquipments != null && fatherCultureEquipments.Any())
                {
                    return fatherCultureEquipments.GetRandomElementInefficiently();
                }

                if (hero.Father.Culture.DefaultCivilianEquipmentRoster.AllEquipments != null &&
                    hero.Father.Culture.DefaultCivilianEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Father.Culture.DefaultCivilianEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            // Priority 4: Use character template's civilian equipment
            if (hero.CharacterObject?.CivilianEquipments != null && hero.CharacterObject.CivilianEquipments.Any())
            {
                return hero.CharacterObject.CivilianEquipments.FirstOrDefault();
            }

            // Priority 5: Use hero's existing civilian equipment if already set
            if (hero.CivilianEquipment != null && !hero.CivilianEquipment.IsEmpty())
            {
                return hero.CivilianEquipment;
            }

            // Last resort: Create empty equipment
            return new Equipment(Equipment.EquipmentType.Civilian);
        }

        /// <summary>
        /// Logs when fallback equipment is used, showing parent cultures for debugging
        /// </summary>
        private static void LogFallback(Hero hero, string reason)
        {
            string motherCulture = hero.Mother?.Culture?.Name?.ToString() ?? "Unknown";
            string fatherCulture = hero.Father?.Culture?.Name?.ToString() ?? "Unknown";
            string childCulture = hero.Culture?.Name?.ToString() ?? "Unknown";

            string message = $"Using culture-appropriate fallback for {hero.Name} " +
                           $"(Culture: {childCulture}, Mother: {motherCulture}, Father: {fatherCulture}). " +
                           $"Reason: {reason}";

            // Display as yellow message for visibility without being alarming
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
        }
    }
}

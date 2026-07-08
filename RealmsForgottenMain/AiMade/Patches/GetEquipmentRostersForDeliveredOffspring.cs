using HarmonyLib;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Extensions;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(DefaultHeroCreationModel), "GetBattleEquipment")]
    public class GetBattleEquipmentPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(DefaultHeroCreationModel __instance, Hero hero, ref Equipment __result)
        {
            if (hero.Mother == null)
            {
                return true;
            }

            try
            {
                Equipment equipment =
                    Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentForDeliveredOffspring(hero);

                if (equipment == null || equipment.IsEmpty())
                {
                    EquipmentFallbackHelper.LogFallback(hero, "GetEquipmentForDeliveredOffspring returned null or empty battle equipment");
                    __result = EquipmentFallbackHelper.GetCultureAppropriateBattleEquipment(hero);
                    return false;
                }

                __result = equipment;
                return false;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Error getting battle equipment for {hero.Name}. Using culture fallback: {ex.Message}",
                    Colors.Red));

                __result = EquipmentFallbackHelper.GetCultureAppropriateBattleEquipment(hero);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(DefaultHeroCreationModel), "GetCivilianEquipment")]
    public class GetCivilianEquipmentPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(DefaultHeroCreationModel __instance, Hero hero, ref Equipment __result)
        {
            if (hero.Mother == null)
            {
                return true;
            }

            try
            {
                Equipment equipment =
                    Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentForDeliveredOffspring(hero);

                if (equipment == null || equipment.IsEmpty())
                {
                    EquipmentFallbackHelper.LogFallback(hero, "GetEquipmentForDeliveredOffspring returned null or empty civilian equipment");
                    __result = EquipmentFallbackHelper.GetCultureAppropriateCivilianEquipment(hero);
                    return false;
                }

                __result = equipment;
                return false;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"Error getting civilian equipment for {hero.Name}. Using culture fallback: {ex.Message}",
                    Colors.Red));

                __result = EquipmentFallbackHelper.GetCultureAppropriateCivilianEquipment(hero);
                return false;
            }
        }
    }

    internal static class EquipmentFallbackHelper
    {
        internal static Equipment GetCultureAppropriateBattleEquipment(Hero hero)
        {
            if (hero.CharacterObject?.BattleEquipments != null && hero.CharacterObject.BattleEquipments.Any())
            {
                return hero.CharacterObject.BattleEquipments.First();
            }

            if (hero.Culture?.DefaultBattleEquipmentRoster != null)
            {
                var childCultureEquipments = hero.Culture.DefaultBattleEquipmentRoster.GetBattleEquipments();
                if (childCultureEquipments != null && childCultureEquipments.Any())
                {
                    return childCultureEquipments.GetRandomElementInefficiently();
                }

                if (hero.Culture.DefaultBattleEquipmentRoster.AllEquipments != null &&
                    hero.Culture.DefaultBattleEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Culture.DefaultBattleEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            if (hero.Mother?.Culture?.DefaultBattleEquipmentRoster != null)
            {
                var motherCultureEquipments = hero.Mother.Culture.DefaultBattleEquipmentRoster.GetBattleEquipments();
                if (motherCultureEquipments != null && motherCultureEquipments.Any())
                {
                    return motherCultureEquipments.GetRandomElementInefficiently();
                }

                if (hero.Mother.Culture.DefaultBattleEquipmentRoster.AllEquipments != null &&
                    hero.Mother.Culture.DefaultBattleEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Mother.Culture.DefaultBattleEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            if (hero.Father?.Culture?.DefaultBattleEquipmentRoster != null)
            {
                var fatherCultureEquipments = hero.Father.Culture.DefaultBattleEquipmentRoster.GetBattleEquipments();
                if (fatherCultureEquipments != null && fatherCultureEquipments.Any())
                {
                    return fatherCultureEquipments.GetRandomElementInefficiently();
                }

                if (hero.Father.Culture.DefaultBattleEquipmentRoster.AllEquipments != null &&
                    hero.Father.Culture.DefaultBattleEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Father.Culture.DefaultBattleEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            if (hero.BattleEquipment != null && !hero.BattleEquipment.IsEmpty())
            {
                return hero.BattleEquipment;
            }

            return new Equipment(Equipment.EquipmentType.Battle);
        }

        internal static Equipment GetCultureAppropriateCivilianEquipment(Hero hero)
        {
            if (hero.Culture?.DefaultCivilianEquipmentRoster != null)
            {
                var childCultureEquipments = hero.Culture.DefaultCivilianEquipmentRoster.GetCivilianEquipments();
                if (childCultureEquipments != null && childCultureEquipments.Any())
                {
                    return childCultureEquipments.GetRandomElementInefficiently();
                }

                if (hero.Culture.DefaultCivilianEquipmentRoster.AllEquipments != null &&
                    hero.Culture.DefaultCivilianEquipmentRoster.AllEquipments.Count > 0)
                {
                    return hero.Culture.DefaultCivilianEquipmentRoster.AllEquipments.GetRandomElement();
                }
            }

            if (hero.Mother?.Culture?.DefaultCivilianEquipmentRoster != null)
            {
                var motherCultureEquipments = hero.Mother.Culture.DefaultCivilianEquipmentRoster.GetCivilianEquipments();
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

            if (hero.Father?.Culture?.DefaultCivilianEquipmentRoster != null)
            {
                var fatherCultureEquipments = hero.Father.Culture.DefaultCivilianEquipmentRoster.GetCivilianEquipments();
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

            if (hero.CharacterObject?.CivilianEquipments != null && hero.CharacterObject.CivilianEquipments.Any())
            {
                return hero.CharacterObject.CivilianEquipments.First();
            }

            if (hero.CivilianEquipment != null && !hero.CivilianEquipment.IsEmpty())
            {
                return hero.CivilianEquipment;
            }

            return new Equipment(Equipment.EquipmentType.Civilian);
        }

        internal static void LogFallback(Hero hero, string reason)
        {
            string motherCulture = hero.Mother?.Culture?.Name?.ToString() ?? "Unknown";
            string fatherCulture = hero.Father?.Culture?.Name?.ToString() ?? "Unknown";
            string childCulture = hero.Culture?.Name?.ToString() ?? "Unknown";

            string message = $"Using culture-appropriate fallback for {hero.Name} " +
                             $"(Culture: {childCulture}, Mother: {motherCulture}, Father: {fatherCulture}). " +
                             $"Reason: {reason}";

            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
        }
    }
}

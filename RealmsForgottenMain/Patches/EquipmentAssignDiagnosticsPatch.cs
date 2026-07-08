using HarmonyLib;
using System;
using System.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Patches
{
    internal static class MBEquipmentRoster_AddEquipmentRoster_Log_Prefix
    {
        private static void Prefix(
            MBEquipmentRoster __instance,
            MBEquipmentRoster equipmentRoster,
            Equipment.EquipmentType equipmentType)
        {
            try
            {
                string intoId = __instance?.StringId ?? "(null)";
                string refId = equipmentRoster?.StringId ?? "(null)";
                RFHardDebugLog.Write($"[EquipRoster.Add.Prefix] into={intoId} ref={refId} equipmentType={equipmentType}");
            }
            catch
            {
            }
        }
    }

    internal static class AssignHeroEquipmentFromEquipmentPatch
    {
        private static System.Reflection.MethodBase? TargetMethod()
        {
            Type? t =
                AccessTools.TypeByName("TaleWorlds.CampaignSystem.EquipmentHelper") ??
                AccessTools.TypeByName("TaleWorlds.CampaignSystem.Actions.EquipmentHelper") ??
                AccessTools.TypeByName("EquipmentHelper");

            if (t == null)
            {
                return null;
            }

            return AccessTools.Method(t, "AssignHeroEquipmentFromEquipment", new[] { typeof(Hero), typeof(Equipment) });
        }

        [HarmonyPrefix]
        private static bool Prefix(Hero hero, Equipment equipment)
        {
            if (equipment != null)
            {
                return true;
            }

            try
            {
                string heroName = hero?.Name?.ToString() ?? "NULL HERO";
                string heroId = hero?.StringId ?? "N/A";
                string culture = hero?.Culture?.StringId ?? "NULL CULTURE";
                string clan = hero?.Clan?.Name?.ToString() ?? "NULL CLAN";
                float age = hero?.Age ?? -1f;
                bool isFemale = hero?.IsFemale ?? false;
                bool isChild = hero?.IsChild ?? false;

                RFHardDebugLog.Write(
                    $"[AssignHeroEquipmentFromEquipment] NULL equipment detected! " +
                    $"hero={heroName} id={heroId} culture={culture} clan={clan} age={age} isFemale={isFemale} isChild={isChild}");
                RFHardDebugLog.Write($"[AssignHeroEquipmentFromEquipment] StackTrace:{Environment.NewLine}{new StackTrace(true)}");
            }
            catch (Exception ex)
            {
                try
                {
                    RFHardDebugLog.Write($"[AssignHeroEquipmentFromEquipment] Logging failed: {ex.GetType().FullName}: {ex.Message}");
                }
                catch
                {
                }
            }

            return false;
        }
    }
}

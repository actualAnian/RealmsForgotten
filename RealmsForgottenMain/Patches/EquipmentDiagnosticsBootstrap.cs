using HarmonyLib;
using System;
using System.Reflection;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Patches
{
    internal static class EquipmentDiagnosticsBootstrap
    {
        private static bool _applied;

        internal static void Apply()
        {
            if (_applied)
            {
                RFHardDebugLog.Write("[Startup] Equipment diagnostics bootstrap already applied");
                return;
            }

            var harmony = new Harmony("realmsforgotten.manual.equipmentdiagnostics");

            MethodInfo? addRoster = AccessTools.Method(
                typeof(MBEquipmentRoster),
                nameof(MBEquipmentRoster.AddEquipmentRoster),
                new[] { typeof(MBEquipmentRoster), typeof(Equipment.EquipmentType) });

            if (addRoster != null)
            {
                MethodInfo? addRosterPrefix = AccessTools.Method(
                    typeof(MBEquipmentRoster_AddEquipmentRoster_Log_Prefix),
                    "Prefix");

                harmony.Patch(addRoster, prefix: new HarmonyMethod(addRosterPrefix));
                RFHardDebugLog.Write("[Startup] Patched MBEquipmentRoster.AddEquipmentRoster");
            }
            else
            {
                RFHardDebugLog.Write("[Startup] FAILED to find MBEquipmentRoster.AddEquipmentRoster");
            }

            MethodInfo? deserializeMethod = AccessTools.Method(
                typeof(MBEquipmentRoster),
                nameof(MBEquipmentRoster.Deserialize),
                new[] { typeof(MBObjectManager), typeof(XmlNode) });

            if (deserializeMethod != null)
            {
                MethodInfo? deserializePrefix = AccessTools.Method(
                    typeof(EquipmentRosterXmlDiagnosticsPatch),
                    nameof(EquipmentRosterXmlDiagnosticsPatch.OnDeserializePrefix));
                MethodInfo? deserializeFinalizer = AccessTools.Method(
                    typeof(EquipmentRosterXmlDiagnosticsPatch),
                    nameof(EquipmentRosterXmlDiagnosticsPatch.OnDeserializeFinalizer));

                harmony.Patch(
                    deserializeMethod,
                    prefix: new HarmonyMethod(deserializePrefix),
                    finalizer: new HarmonyMethod(deserializeFinalizer));
                RFHardDebugLog.Write("[Startup] Patched MBEquipmentRoster.Deserialize");
            }
            else
            {
                RFHardDebugLog.Write("[Startup] FAILED to find MBEquipmentRoster.Deserialize");
            }

            MethodInfo? initEquipmentMethod = AccessTools.Method(
                typeof(MBEquipmentRoster),
                "InitEquipment",
                new[] { typeof(MBObjectManager), typeof(XmlNode) });

            if (initEquipmentMethod != null)
            {
                MethodInfo? initEquipmentPrefix = AccessTools.Method(
                    typeof(EquipmentRosterXmlDiagnosticsPatch),
                    nameof(EquipmentRosterXmlDiagnosticsPatch.OnInitEquipmentPrefix));
                MethodInfo? initEquipmentFinalizer = AccessTools.Method(
                    typeof(EquipmentRosterXmlDiagnosticsPatch),
                    nameof(EquipmentRosterXmlDiagnosticsPatch.OnInitEquipmentFinalizer));

                harmony.Patch(
                    initEquipmentMethod,
                    prefix: new HarmonyMethod(initEquipmentPrefix),
                    finalizer: new HarmonyMethod(initEquipmentFinalizer));
                RFHardDebugLog.Write("[Startup] Patched MBEquipmentRoster.InitEquipment");
            }
            else
            {
                RFHardDebugLog.Write("[Startup] FAILED to find MBEquipmentRoster.InitEquipment");
            }

            Type? equipmentHelperType =
                AccessTools.TypeByName("TaleWorlds.CampaignSystem.EquipmentHelper") ??
                AccessTools.TypeByName("TaleWorlds.CampaignSystem.Actions.EquipmentHelper") ??
                AccessTools.TypeByName("EquipmentHelper");

            if (equipmentHelperType != null)
            {
                MethodInfo? assignMethod = AccessTools.Method(
                    equipmentHelperType,
                    "AssignHeroEquipmentFromEquipment",
                    new[] { typeof(Hero), typeof(Equipment) });
                MethodInfo? assignPrefix = AccessTools.Method(
                    typeof(AssignHeroEquipmentFromEquipmentPatch),
                    "Prefix");

                if (assignMethod != null && assignPrefix != null)
                {
                    harmony.Patch(assignMethod, prefix: new HarmonyMethod(assignPrefix));
                    RFHardDebugLog.Write("[Startup] Patched AssignHeroEquipmentFromEquipment on " + equipmentHelperType.FullName);
                }
                else
                {
                    RFHardDebugLog.Write("[Startup] FAILED to patch AssignHeroEquipmentFromEquipment because method/prefix was null");
                }
            }
            else
            {
                RFHardDebugLog.Write("[Startup] FAILED to find EquipmentHelper type");
            }

            _applied = true;
        }
    }
}

using HarmonyLib;
using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Patches
{
    internal static class EquipmentRosterXmlDiagnosticsPatch
    {
        internal static void OnDeserializePrefix(MBEquipmentRoster __instance, XmlNode node)
        {
            try
            {
                string rosterId = __instance?.StringId ?? "<null>";
                string nodeName = node?.Name ?? "<null>";
                string xmlId = node?.Attributes?["id"]?.Value ?? "<no-id>";
                string culture = node?.Attributes?["culture"]?.Value ?? "<no-culture>";
                RFHardDebugLog.Write($"[EquipRoster.Deserialize.Prefix] roster={rosterId} node={nodeName} xmlId={xmlId} culture={culture}");
            }
            catch
            {
            }
        }

        internal static Exception OnDeserializeFinalizer(Exception __exception, MBEquipmentRoster __instance, XmlNode node)
        {
            if (__exception == null)
            {
                return null;
            }

            try
            {
                string rosterId = __instance?.StringId ?? "<null>";
                string xmlId = node?.Attributes?["id"]?.Value ?? "<no-id>";
                string outerXml = SafeOuterXml(node);
                RFHardDebugLog.Write($"[EquipRoster.Deserialize.Exception] roster={rosterId} xmlId={xmlId} ex={__exception.GetType().FullName}: {__exception.Message}");
                RFHardDebugLog.Write($"[EquipRoster.Deserialize.Exception.Xml] {outerXml}");
            }
            catch
            {
            }

            return __exception;
        }

        internal static void OnInitEquipmentPrefix(MBEquipmentRoster __instance, XmlNode node)
        {
            try
            {
                string rosterId = __instance?.StringId ?? "<null>";
                string nodeName = node?.Name ?? "<null>";
                string equipmentType = node?.Attributes?["equipmentType"]?.Value ?? "<none>";
                string civilian = node?.Attributes?["civilian"]?.Value ?? "<none>";
                RFHardDebugLog.Write($"[EquipRoster.InitEquipment.Prefix] roster={rosterId} node={nodeName} equipmentType={equipmentType} civilian={civilian}");
            }
            catch
            {
            }
        }

        internal static Exception OnInitEquipmentFinalizer(Exception __exception, MBEquipmentRoster __instance, XmlNode node)
        {
            if (__exception == null)
            {
                return null;
            }

            try
            {
                string rosterId = __instance?.StringId ?? "<null>";
                string equipmentType = node?.Attributes?["equipmentType"]?.Value ?? "<none>";
                string civilian = node?.Attributes?["civilian"]?.Value ?? "<none>";
                string outerXml = SafeOuterXml(node);
                RFHardDebugLog.Write($"[EquipRoster.InitEquipment.Exception] roster={rosterId} equipmentType={equipmentType} civilian={civilian} ex={__exception.GetType().FullName}: {__exception.Message}");
                RFHardDebugLog.Write($"[EquipRoster.InitEquipment.Exception.Xml] {outerXml}");
            }
            catch
            {
            }

            return __exception;
        }

        private static string SafeOuterXml(XmlNode node)
        {
            try
            {
                string xml = node?.OuterXml ?? "<null>";
                return xml.Length > 2000 ? xml.Substring(0, 2000) + "...<truncated>" : xml;
            }
            catch (Exception ex)
            {
                return $"<outerxml-failed:{ex.GetType().Name}>";
            }
        }
    }
}

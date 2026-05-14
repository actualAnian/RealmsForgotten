using HarmonyLib;
using RealmsForgotten.AiMade;
using System;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Patches
{
    internal static class HeroDeserializeDiagnosticsPatch
    {
        private static Exception? Finalizer(Exception? __exception, MBObjectManager objectManager, XmlNode node)
        {
            if (__exception == null)
            {
                return null;
            }

            try
            {
                string id = node?.Attributes?["id"]?.Value ?? "<null>";
                string name = node?.Attributes?["name"]?.Value ?? "<null>";
                string faction = node?.Attributes?["faction"]?.Value ?? "<null>";
                string clan = node?.Attributes?["clan"]?.Value ?? "<null>";
                string xml = node?.OuterXml ?? "<null>";
                if (xml.Length > 4000)
                {
                    xml = xml.Substring(0, 4000) + "...<truncated>";
                }

                RFLogger.Log(
                    $"[HeroDeserialize] Exception={__exception.GetType().FullName}: {__exception.Message} | id={id} | name={name} | faction={faction} | clan={clan} | xml={xml}");
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[HeroDeserialize] Logging failed: {ex.GetType().FullName}: {ex.Message}");
            }

            return __exception;
        }
    }
}

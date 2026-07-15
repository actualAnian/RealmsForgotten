using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RF_ResourceZones
{
    /// <summary>
    /// Loads ModuleData/rf_resource_zones.xml from the RealmsForgotten module.
    /// The file is read directly (NOT registered in SubModule.xml's Xmls list)
    /// so a machine missing it — another dev, a partial install — gets zero
    /// zones and a log line instead of a startup crash (the Homesteads-assets
    /// lesson of 2026-07-14).
    /// </summary>
    public static class ResourceZoneManifest
    {
        private const string ModuleId = "RealmsForgotten";
        private const string FileName = "rf_resource_zones.xml";

        public static List<ResourceZoneDefinition> Load()
        {
            List<ResourceZoneDefinition> definitions = new();
            string path;
            try
            {
                path = Path.Combine(ModuleHelper.GetModuleFullPath(ModuleId), "ModuleData", FileName);
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_ResourceZones] Module path lookup failed: {exception.Message}");
                return definitions;
            }

            if (!File.Exists(path))
            {
                Debug.Print($"[RF_ResourceZones] Manifest not found ({path}) — no resource zones this session.");
                return definitions;
            }

            try
            {
                XmlDocument document = new();
                document.Load(path);
                XmlNodeList? nodes = document.SelectNodes("//ResourceZones/Zone");
                if (nodes == null)
                {
                    return definitions;
                }

                foreach (XmlNode node in nodes)
                {
                    ResourceZoneDefinition? definition = ParseZone(node);
                    if (definition != null)
                    {
                        definitions.Add(definition);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_ResourceZones] Manifest parse failed (zones skipped): {exception}");
            }

            Debug.Print($"[RF_ResourceZones] Manifest loaded: {definitions.Count} zone(s).");
            return definitions;
        }

        private static ResourceZoneDefinition? ParseZone(XmlNode node)
        {
            string id = Attribute(node, "id");
            string typeText = Attribute(node, "type");
            if (string.IsNullOrEmpty(id) || !Enum.TryParse(typeText, ignoreCase: true, out ResourceZoneType type))
            {
                Debug.Print($"[RF_ResourceZones] Zone entry skipped (id='{id}', type='{typeText}').");
                return null;
            }

            ResourceZoneDefinition definition = new()
            {
                Id = id,
                Type = type,
                Name = Attribute(node, "name"),
                AnchorSettlementId = Attribute(node, "anchorSettlement"),
                BoundTownId = Attribute(node, "boundTown"),
                OffsetX = ParseFloat(Attribute(node, "offsetX")),
                OffsetY = ParseFloat(Attribute(node, "offsetY")),
            };

            string posX = Attribute(node, "posX");
            string posY = Attribute(node, "posY");
            if (!string.IsNullOrEmpty(posX) && !string.IsNullOrEmpty(posY))
            {
                definition.HasAbsolutePosition = true;
                definition.PosX = ParseFloat(posX);
                definition.PosY = ParseFloat(posY);
            }

            if (string.IsNullOrEmpty(definition.Name))
            {
                definition.Name = $"{type} Camp";
            }

            return definition;
        }

        private static string Attribute(XmlNode node, string name)
        {
            return node.Attributes?[name]?.Value ?? string.Empty;
        }

        private static float ParseFloat(string text)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
        }
    }
}

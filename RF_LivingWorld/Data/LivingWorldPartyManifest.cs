using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RF_LivingWorld
{
    public static class LivingWorldPartyManifest
    {
        public static List<LivingWorldPartyDefinition> Load()
        {
            List<LivingWorldPartyDefinition> definitions = new();
            string path;
            try
            {
                path = Path.Combine(ModuleHelper.GetModuleFullPath("RealmsForgotten"), "ModuleData", "living_world_parties.xml");
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_LivingWorld] Module path lookup failed: {exception.Message}");
                return definitions;
            }

            if (!File.Exists(path))
            {
                Debug.Print($"[RF_LivingWorld] Manifest not found ({path}); living parties are disabled.");
                return definitions;
            }

            try
            {
                XmlDocument document = new();
                document.Load(path);
                foreach (XmlNode node in document.SelectNodes("//LivingWorldParties/Party") ?? new XmlDocument().ChildNodes)
                {
                    LivingWorldPartyDefinition? definition = Parse(node);
                    if (definition != null)
                    {
                        definitions.Add(definition);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_LivingWorld] Manifest parse failed: {exception}");
            }

            Debug.Print($"[RF_LivingWorld] Manifest loaded: {definitions.Count} definition(s).");
            return definitions;
        }

        private static LivingWorldPartyDefinition? Parse(XmlNode node)
        {
            string id = Attribute(node, "id");
            if (string.IsNullOrWhiteSpace(id) || !Enum.TryParse(Attribute(node, "type"), true, out LivingWorldPartyType type))
            {
                Debug.Print($"[RF_LivingWorld] Party entry skipped (id='{id}').");
                return null;
            }

            LivingWorldPartyDefinition definition = new()
            {
                Id = id,
                Type = type,
                MaxInstances = Math.Max(1, ParseInt(Attribute(node, "maxInstances"), 1)),
                MinimumSize = Math.Max(1, ParseInt(Attribute(node, "minimumSize"), 3)),
                MaximumSize = Math.Max(1, ParseInt(Attribute(node, "maximumSize"), 6)),
                RumorReliability = MathF.Min(1f, MathF.Max(0f, ParseFloat(Attribute(node, "rumorReliability"), 0.65f))),
                IsAmbient = bool.TryParse(Attribute(node, "ambient"), out bool ambient) && ambient,
                AmbientBaseline = Math.Max(0, ParseInt(Attribute(node, "baseline"), 0))
            };
            definition.MaximumSize = Math.Max(definition.MinimumSize, definition.MaximumSize);
            if (!definition.IsAmbient)
            {
                definition.AmbientBaseline = 0;
            }

            string herd = Attribute(node, "herdVariant");
            if (!string.IsNullOrWhiteSpace(herd) && !Enum.TryParse(herd, true, out definition.HerdVariant))
            {
                Debug.Print($"[RF_LivingWorld] Party '{id}' skipped: unknown herd variant '{herd}'.");
                return null;
            }

            if (type == LivingWorldPartyType.Herder && definition.HerdVariant == LivingWorldHerdVariant.None)
            {
                Debug.Print($"[RF_LivingWorld] Party '{id}' skipped: herders require a herdVariant.");
                return null;
            }

            return definition;
        }

        private static string Attribute(XmlNode node, string name) => node.Attributes?[name]?.Value ?? string.Empty;
        private static int ParseInt(string text, int fallback) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        private static float ParseFloat(string text, float fallback) => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
    }
}

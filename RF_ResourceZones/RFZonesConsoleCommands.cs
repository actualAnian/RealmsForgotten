using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RF_ResourceZones
{
    /// <summary>
    /// Slot-authoring helper. Ride to the spot, open the console and run
    ///   rf_zones.mark_zone Gold Sunken Vein Gold Mine
    /// — the zone is APPENDED to the deployed rf_resource_zones.xml AND spawns
    /// on the map immediately. Authoring 20 slots becomes a horseback ride.
    /// (The repo copy of the manifest is synced by hand/at commit time — the
    /// game only writes to the deployed module.)
    /// </summary>
    public static class RFZonesConsoleCommands
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("disable", "rf_zones")]
        public static string Disable(List<string> strings)
        {
            return ResourceZonesCampaignBehavior.SetRuntimeEnabled(false);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("enable", "rf_zones")]
        public static string Enable(List<string> strings)
        {
            return ResourceZonesCampaignBehavior.SetRuntimeEnabled(true);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("mark_zone", "rf_zones")]
        public static string MarkZone(List<string> strings)
        {
            if (Campaign.Current == null || MobileParty.MainParty == null)
            {
                return "rf_zones.mark_zone: campaign not running.";
            }

            string type = strings.Count > 0 ? strings[0] : "Iron";
            if (!Enum.TryParse(type, ignoreCase: true, out ResourceZoneType parsedType))
            {
                return $"Unknown type '{type}'. Use: Gold, Iron, Wood, Charcoal, Silver, Karthradium.";
            }

            string name = strings.Count > 1
                ? string.Join(" ", strings.GetRange(1, strings.Count - 1)).Trim('"')
                : $"{parsedType} Camp";

            CampaignVec2 position = MobileParty.MainParty.Position;
            string id = $"rf_zone_{parsedType.ToString().ToLowerInvariant()}_{(long)CampaignTime.Now.ToMilliseconds % 100000}";

            ResourceZoneDefinition definition = new()
            {
                Id = id,
                Type = parsedType,
                Name = name,
                HasAbsolutePosition = true,
                PosX = position.X,
                PosY = position.Y,
            };

            string line =
                $"<Zone id=\"{id}\" type=\"{parsedType}\" name=\"{name}\" " +
                $"posX=\"{position.X.ToString("F2", CultureInfo.InvariantCulture)}\" " +
                $"posY=\"{position.Y.ToString("F2", CultureInfo.InvariantCulture)}\" />";
            Debug.Print("[RF_ResourceZones] " + line);

            string persistNote = AppendToManifest(definition)
                ? "Saved to ModuleData/rf_resource_zones.xml."
                : "COULD NOT write the manifest — paste this line by hand:\n" + line;

            string spawnNote;
            if (ResourceZonesCampaignBehavior.Instance != null)
            {
                ResourceZonesCampaignBehavior.Instance.RegisterRuntimeZone(definition);
                spawnNote = "Zone spawned at your position (bandit-held).";
            }
            else
            {
                spawnNote = "Zone will spawn on the next session load.";
            }

            return $"{spawnNote}\n{persistNote}\n{line}";
        }

        /// <summary>Appends the zone to the DEPLOYED manifest (creating the file
        /// with a root element if missing). Returns false on any I/O failure —
        /// the command output then tells the author to paste manually.</summary>
        private static bool AppendToManifest(ResourceZoneDefinition definition)
        {
            try
            {
                string path = Path.Combine(
                    ModuleHelper.GetModuleFullPath("RealmsForgotten"), "ModuleData", "rf_resource_zones.xml");

                XmlDocument document = new();
                if (File.Exists(path))
                {
                    document.Load(path);
                }

                XmlNode? root = document.SelectSingleNode("//ResourceZones");
                if (root == null)
                {
                    root = document.CreateElement("ResourceZones");
                    document.AppendChild(root);
                }

                XmlElement zone = document.CreateElement("Zone");
                zone.SetAttribute("id", definition.Id);
                zone.SetAttribute("type", definition.Type.ToString());
                zone.SetAttribute("name", definition.Name);
                zone.SetAttribute("posX", definition.PosX.ToString("F2", CultureInfo.InvariantCulture));
                zone.SetAttribute("posY", definition.PosY.ToString("F2", CultureInfo.InvariantCulture));
                root.AppendChild(zone);

                document.Save(path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_ResourceZones] mark_zone manifest write failed: {exception.Message}");
                return false;
            }
        }
    }
}

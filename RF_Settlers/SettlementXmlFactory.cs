using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;

namespace RF_Settlers
{
    /// <summary>
    /// Builds the settlement XML for a new settler village by CLONING a real
    /// village of the same culture from RF_Map's settlements.xml. Because the
    /// donor entry is live map data, its village scenes, background meshes and
    /// village_type are guaranteed valid for that culture — no hand-authored
    /// templates needed for the 19 RF cultures. Only identity attributes are
    /// substituted (id, name, position, bound, hearth).
    /// </summary>
    public static class SettlementXmlFactory
    {
        private const string MapModuleId = "RF_Map";
        private const float StartingHearth = 120f;

        private static XmlDocument _mapSettlementsDocument;

        public static bool TryBuildVillageXml(
            Kingdom kingdom,
            Vec2 position,
            Settlement bound,
            string stringId,
            string displayName,
            out string xml,
            out string villageTypeId)
        {
            xml = null;
            villageTypeId = null;
            try
            {
                XmlNode donor = PickDonorVillage(kingdom?.Culture?.StringId);
                if (donor == null || kingdom == null || bound == null)
                {
                    return false;
                }

                XmlNode clone = donor.CloneNode(deep: true);

                SetAttribute(clone, "id", stringId);
                SetAttribute(clone, "name", displayName);
                SetAttribute(clone, "posX", position.X.ToString("F3", CultureInfo.InvariantCulture));
                SetAttribute(clone, "posY", position.Y.ToString("F3", CultureInfo.InvariantCulture));
                SetAttribute(clone, "culture", "Culture." + kingdom.Culture.StringId);
                // The donor's lore text belongs to another village entirely.
                RemoveAttribute(clone, "text");

                XmlNode village = clone.SelectSingleNode("descendant::Village");
                if (village == null)
                {
                    return false;
                }

                SetAttribute(village, "id", "village_comp_" + stringId);
                SetAttribute(village, "bound", "Settlement." + bound.StringId);
                SetAttribute(village, "hearth", StartingHearth.ToString("F0", CultureInfo.InvariantCulture));

                // Deliberate village type by TERRAIN (fishing by water, mines in
                // mountains...) instead of the random donor's type. On failure
                // the donor's own (always valid) type stays in place.
                string pickedType = SettlerVillageTypePicker.Pick(position);
                if (pickedType != null)
                {
                    SetAttribute(village, "village_type", "VillageType." + pickedType);
                    villageTypeId = pickedType;
                }
                else
                {
                    string donorType = village.Attributes?["village_type"]?.Value;
                    villageTypeId = donorType != null && donorType.StartsWith("VillageType.", StringComparison.Ordinal)
                        ? donorType.Substring("VillageType.".Length)
                        : null;
                }

                xml = "<Settlements>" + clone.OuterXml + "</Settlements>";
                return true;
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_Settlers] Failed to build village XML: {exception}");
                return false;
            }
        }

        private static XmlNode PickDonorVillage(string cultureId)
        {
            XmlDocument document = LoadMapSettlements();
            if (document?.DocumentElement == null)
            {
                return null;
            }

            List<XmlNode> sameCulture = new();
            List<XmlNode> anyCulture = new();
            foreach (XmlNode node in document.DocumentElement.SelectNodes("Settlement"))
            {
                string id = node.Attributes?["id"]?.Value;
                if (id == null || !id.StartsWith("village_", StringComparison.Ordinal)
                    || node.SelectSingleNode("descendant::Village") == null)
                {
                    continue;
                }

                anyCulture.Add(node);
                if (cultureId != null
                    && node.Attributes?["culture"]?.Value == "Culture." + cultureId)
                {
                    sameCulture.Add(node);
                }
            }

            List<XmlNode> pool = sameCulture.Count > 0 ? sameCulture : anyCulture;
            return pool.Count > 0 ? pool[MBRandom.RandomInt(pool.Count)] : null;
        }

        private static XmlDocument LoadMapSettlements()
        {
            if (_mapSettlementsDocument != null)
            {
                return _mapSettlementsDocument;
            }

            string path = Path.Combine(ModuleHelper.GetModuleFullPath(MapModuleId), "ModuleData", "settlements.xml");
            if (!File.Exists(path))
            {
                Debug.Print($"[RF_Settlers] Map settlements file not found: {path}");
                return null;
            }

            XmlDocument document = new();
            document.Load(path);
            _mapSettlementsDocument = document;
            return document;
        }

        private static void SetAttribute(XmlNode node, string name, string value)
        {
            XmlAttribute attribute = node.Attributes[name];
            if (attribute == null)
            {
                attribute = node.OwnerDocument.CreateAttribute(name);
                node.Attributes.SetNamedItem(attribute);
            }

            attribute.Value = value;
        }

        private static void RemoveAttribute(XmlNode node, string name)
        {
            if (node.Attributes[name] != null)
            {
                node.Attributes.RemoveNamedItem(name);
            }
        }
    }
}

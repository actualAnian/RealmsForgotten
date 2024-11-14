using System.Xml;
using System.IO;
using TaleWorlds.Library;
using System.Linq;

namespace RealmsForgotten.AiMade.Patches
{
    public static class FaceGenHelper
    {
        private static readonly string SkinFilePath = Path.Combine(BasePath.Name, "Modules", "RF_Races", "ModuleData", "skins.xml");
        private static XmlDocument SkinDocument;

        public static string? GetBeardName(int index, int race, int gender)
        {
            if (SkinDocument == null)
                LoadSkinsXML();

            if (SkinDocument != null)
            {
                XmlNodeList raceList = SkinDocument.GetElementsByTagName("race");

                if (raceList.Count > race)
                {
                    var dwarfList = raceList.Cast<XmlNode>().Where(node => node.Attributes?["id"]?.Value == "dwarf");
                    if (dwarfList.Count() != 1) return null;
                    XmlNode selectedRace = dwarfList.First();
                    XmlNodeList genderNodes = selectedRace.ChildNodes;

                    if (genderNodes.Count > gender)
                    {
                        XmlNode selectedGender = genderNodes.Item(gender);
                        XmlNodeList beards = selectedGender.SelectNodes("beard_meshes/beard_mesh");

                        if (beards.Count > 0 && index < beards.Count && beards[index].Attributes.Count != 0)
                        {
                            return beards[index].Attributes["name"].Value;
                        }
                    }
                }
            }
            return null;
        }

        private static void LoadSkinsXML()
        {
            var settings = new XmlReaderSettings { IgnoreComments = true };
            using XmlReader reader = XmlReader.Create(SkinFilePath, settings);
            SkinDocument = new XmlDocument();
            SkinDocument.Load(reader);
        }
    }
}


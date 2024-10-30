using System.Xml;
using System.IO;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Patches
{
    public static class FaceGenHelper
    {
        private static readonly string SkinFilePath = Path.Combine(BasePath.Name, "Modules", "RF_Races", "ModuleData", "skins.xml");
        private static XmlDocument SkinDocument;

        public static string GetBeardName(int index, int race, int gender)
        {
            if (SkinDocument == null)
                LoadSkinsXML();

            if (SkinDocument != null)
            {
                var raceList = SkinDocument.GetElementsByTagName("race");

                if (raceList.Count > race)
                {
                    var selectedRace = raceList[race];
                    var genderNodes = selectedRace.ChildNodes;

                    if (genderNodes.Count > gender)
                    {
                        var selectedGender = genderNodes.Item(gender);
                        var beards = selectedGender.SelectNodes("beard_meshes/beard_mesh");

                        if (beards.Count > 0 && index < beards.Count)
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
            if (File.Exists(SkinFilePath))
            {
                var settings = new XmlReaderSettings { IgnoreComments = true };
                using (var reader = XmlReader.Create(SkinFilePath, settings))
                {
                    SkinDocument = new XmlDocument();
                    SkinDocument.Load(reader);
                }
            }
            else
            {
                // Handle file not found scenario
                // Optionally log or display an error message
            }
        }
    }
}


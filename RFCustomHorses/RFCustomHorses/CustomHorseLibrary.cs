using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace RFCustomHorses
{
    [Serializable]
    public class CustomHorseLibrary
    {
        internal const string LIBRARY_FILENAME = "CustomHorses.xml";

        public List<Horse> Horses = new List<Horse>();

        internal static CustomHorseLibrary LoadFromFile(string path)
        {
            CustomHorseLibrary result = null;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(CustomHorseLibrary));
            using (StreamReader streamReader = File.OpenText(path))
            {
                result = (CustomHorseLibrary)xmlSerializer.Deserialize(streamReader);
            }
            return result;
        }

        [Serializable]
        public class Horse
        {
            [XmlAttribute("id")]
            public string Id = null;
            [XmlAttribute("culture")]
            public string Culture = null;
            [XmlAttribute("chance_to_spawn")]
            public float SpawnChance = 0f;
        }
    }
}

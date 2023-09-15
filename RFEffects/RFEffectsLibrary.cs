using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace RealmsForgotten.RFEffects
{
    [Serializable]
    public class RFEffectsLibrary
    {
        internal const string LIBRARY_FILENAME = "weapons_effects.xml";

        public List<WeaponEffect> Effects = new List<WeaponEffect>();

        public static RFEffectsLibrary Instance;
        public static void Initialize()
        {
            Instance = LoadFromFile(Path.Combine(Assembly.GetExecutingAssembly().Location, LIBRARY_FILENAME));
        }
        internal static RFEffectsLibrary LoadFromFile(string path)
        {
            RFEffectsLibrary result = null;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(RFEffectsLibrary));
            using (StreamReader streamReader = File.OpenText(path))
            {
                result = (RFEffectsLibrary)xmlSerializer.Deserialize(streamReader);
            }
            return result;
        }

        [Serializable]
        public class WeaponEffect
        {
            [XmlAttribute("id")]
            public string Id = null;
            [XmlAttribute("item_effect")]
            public string ItemEffect = null;
            [XmlAttribute("victim_effect")]
            public string VictimEffect = null;
            [XmlAttribute("effect")]
            public string Effect = null;
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RealmsForgotten.Patches.CameraPositionWhenViewingCharacters
{
    public class JsonPatchConfig : object
    {
        public class JsonPatchConfigItem
        {
            public string Race { get; set; }

            public float Horizontal { get; set; }

            public float Vertical { get; set; }

            public float Zoom { get; set; }
            public bool Mount { get; set; }
        }

        public List<JsonPatchConfigItem> Items { get; private set; }

        private static string GetFileName<TType>(TType configOwner)
        {
            return Path.Combine(Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location), string.Concat(typeof(TType).Name, ".json"));
        }

        public JsonPatchConfig()
        {
            Items = new List<JsonPatchConfigItem>();
        }
        public JsonPatchConfigItem GetConfigItem(int race, bool isMount = false)
        {
            return Items.FirstOrDefault(item => TaleWorlds.Core.FaceGen.GetRaceOrDefault(item.Race) == race && item.Mount == isMount);
        }
        public static JsonPatchConfig LoadConfig<TType>(TType configOwner)
        {
            JsonPatchConfig config = null;

            try
            {
                string jsonString = File.ReadAllText(GetFileName(configOwner));

                if (jsonString != null)
                {
                    config = JsonConvert.DeserializeObject<JsonPatchConfig>(jsonString);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.PrintError(ex.Message);
            }

            return config ?? new JsonPatchConfig();
        }

        public static void WriteConfig<TType>(TType configOwner, JsonPatchConfig config)
        {
            if (config != null)
            {
                string jsonstring = JsonConvert.SerializeObject(config);
                File.WriteAllText(GetFileName(configOwner), jsonstring);
            }
        }
    }
}


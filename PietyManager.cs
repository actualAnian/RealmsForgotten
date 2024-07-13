using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.Module1.Religions
{
    public static class PietyManager
    {
        private static Dictionary<Hero, float> heroPiety = new Dictionary<Hero, float>();

        public static void AddPiety(Hero hero, float amount, bool notifyPlayer = false)
        {
            if (!heroPiety.ContainsKey(hero))
            {
                heroPiety[hero] = 0;
            }

            heroPiety[hero] += amount;

            if (notifyPlayer)
            {
                InformationManager.DisplayMessage(new InformationMessage($"{hero.Name} gained {amount} piety.", Colors.Green));
            }
        }

        public static float GetPiety(Hero hero)
        {
            return heroPiety.ContainsKey(hero) ? heroPiety[hero] : 0;
        }

        public static void SyncData(IDataStore dataStore)
        {
            // Convert the dictionary to a list of key-value pairs for serialization
            var heroPietyList = heroPiety.ToList();
            dataStore.SyncData("heroPiety", ref heroPietyList);

            // Convert the list back to a dictionary after deserialization
            heroPiety = heroPietyList.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}



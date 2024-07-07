using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public static class ChivalryManager
    {
        private static Dictionary<Hero, float> heroChivalry = new Dictionary<Hero, float>();

        public static void AddChivalry(Hero hero, float amount, bool notifyPlayer = false)
        {
            if (!heroChivalry.ContainsKey(hero))
            {
                heroChivalry[hero] = 0;
            }

            heroChivalry[hero] += amount;

            if (notifyPlayer)
            {
                InformationManager.DisplayMessage(new InformationMessage($"{hero.Name} gained {amount} chivalry points.", Colors.Green));
            }
        }

        public static float GetChivalry(Hero hero)
        {
            return heroChivalry.ContainsKey(hero) ? heroChivalry[hero] : 0;
        }

        public static void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("heroChivalry", ref heroChivalry);
        }

        public static void RegisterEvents()
        {
            // Register necessary events here if needed
       }
      
    }
}


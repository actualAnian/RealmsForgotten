using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Religions
{
    public static class HeroExtensions
    {
        private static Dictionary<Hero, ReligionObject> heroReligions = new Dictionary<Hero, ReligionObject>();

        public static void SetReligion(this Hero hero, ReligionObject religion)
        {
            heroReligions[hero] = religion;
        }

        public static ReligionObject GetReligion(this Hero hero)
        {
            return heroReligions.ContainsKey(hero) ? heroReligions[hero] : null;
        }

        public static bool HasReligion(this Hero hero)
        {
            return heroReligions.ContainsKey(hero);
        }
    }
}
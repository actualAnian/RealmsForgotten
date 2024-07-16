using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using System.Linq;

namespace Bannerlord.Module1.Religions
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
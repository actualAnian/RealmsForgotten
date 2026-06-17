using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public static class FactionExtensions
    {
        /// <summary>
        /// Checks if two factions are at war.
        /// </summary>
        public static bool IsAtWarWith(this IFaction faction, IFaction other)
        {
            return faction.GetStanceWith(other).IsAtWar;
        }

        /// <summary>
        /// Returns true if both factions share the same alignment (both good or both evil).
        /// </summary>
        public static bool IsSameAlignment(this IFaction faction, IFaction other)
        {
            return faction.Culture.IsGoodCulture() == other.Culture.IsGoodCulture();
        }

        /// <summary>
        /// Returns true if one faction is good and the other is evil.
        /// </summary>
        public static bool IsEnemyAlignment(this IFaction faction, IFaction other)
        {
            return faction.Culture.IsGoodCulture() != other.Culture.IsGoodCulture();
        }

        /// <summary>
        /// Returns all current enemies of the given faction.
        /// </summary>
        public static List<IFaction> GetEnemies(this IFaction faction)
        {
            return Kingdom.All
                .Where(k => k != faction && faction.GetStanceWith(k).IsAtWar)
                .Cast<IFaction>()
                .ToList();
        }
        /// <summary>
        /// Returns true if the faction belongs to a "good" culture.
        /// </summary>
        public static bool IsGood(this IFaction faction)
        {
            return faction.Culture.IsGoodCulture();
        }

        /// <summary>
        /// Returns true if the faction belongs to an "evil" culture.
        /// </summary>
        public static bool IsEvil(this IFaction faction)
        {
            return faction.Culture.IsEvilCulture();
        }

        /// <summary>
        /// Returns true if the faction has been eliminated from the game.
        /// </summary>
        public static bool IsDead(this IFaction faction)
        {
            return faction is Kingdom k && k.IsEliminated;
        }
    }
}

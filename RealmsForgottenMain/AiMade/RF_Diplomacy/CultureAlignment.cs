using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public static class CultureAlignment
    {
        // Baseline sides — never mutated. The live lists below start from these
        // and can be changed at runtime (momentum promotes neutral cultures).
        private static readonly string[] BaselineGoodCultures = { "battania", "giant", "dwarf", "grimwatch" };
        private static readonly string[] BaselineEvilCultures = { "sturgia", "urkhai", "aserai", "mage" };

        public static readonly List<string> GoodCultures = new(BaselineGoodCultures);

        public static readonly List<string> EvilCultures = new(BaselineEvilCultures);

        /// <summary>
        /// Restores the baseline lists. The lists are STATIC and are mutated by
        /// AlignmentMomentumBehavior (promoted cultures) — without this reset a
        /// new/loaded campaign in the same game session inherits the previous
        /// campaign's promotions. Call before ReapplyPromotedCultures.
        /// </summary>
        public static void ResetToBaseline()
        {
            GoodCultures.Clear();
            GoodCultures.AddRange(BaselineGoodCultures);
            EvilCultures.Clear();
            EvilCultures.AddRange(BaselineEvilCultures);
        }

        public static bool IsGoodCulture(this CultureObject culture)
            => GoodCultures.Contains(culture.StringId);

        public static bool IsEvilCulture(this CultureObject culture)
            => EvilCultures.Contains(culture.StringId);
    }
    public static class CultureAlignmentModifiers
    {
        public static void SetGoodCulture(this CultureObject culture, bool value)
        {
            if (value)
            {
                if (!CultureAlignment.GoodCultures.Contains(culture.StringId))
                    CultureAlignment.GoodCultures.Add(culture.StringId);
                CultureAlignment.EvilCultures.Remove(culture.StringId);
            }
            else
            {
                CultureAlignment.GoodCultures.Remove(culture.StringId);
            }
        }

        public static void SetEvilCulture(this CultureObject culture, bool value)
        {
            if (value)
            {
                if (!CultureAlignment.EvilCultures.Contains(culture.StringId))
                    CultureAlignment.EvilCultures.Add(culture.StringId);
                CultureAlignment.GoodCultures.Remove(culture.StringId);
            }
            else
            {
                CultureAlignment.EvilCultures.Remove(culture.StringId);
            }
        }
    }
}

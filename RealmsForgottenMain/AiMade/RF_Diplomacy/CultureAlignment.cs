using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public static class CultureAlignment
    {
        // Define your own cultures here
        public static readonly List<string> GoodCultures = new()
        {
            "battania",
            "giant",
            "dwarf",
            "grimwatch"
        };

        public static readonly List<string> EvilCultures = new()
        {
            "sturgia",
            "urkhai",
            "aserai",
            "mage"
        };

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

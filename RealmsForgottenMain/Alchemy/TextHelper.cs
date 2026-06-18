using System;
using TaleWorlds.Localization;

namespace RealmsForgotten.Alchemy
{
    public static class TextHelper
    {
        public static string GetBaseDecriptionAndOnProjectilePassed(TextObject baseDesc, TextObject onProPassed, bool showOnProPassed)
        {
            return baseDesc + (showOnProPassed ? Environment.NewLine + onProPassed : "");
        }

    }
}

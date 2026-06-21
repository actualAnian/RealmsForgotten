using HarmonyLib;
using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(GauntletLayer), "LoadMovie", new Type[] { typeof(string), typeof(ViewModel) })]
    public static class ReplaceUIPatch
    {
        public static void Prefix(ref string movieName, ViewModel dataSource)
        {
            if (movieName == "CharacterDeveloper")
                movieName = "RFCharacterDeveloper";
        }
    }
}

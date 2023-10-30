using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(GauntletLayer), "LoadMovie")]
    public static class ReplaceUIPatch
    {
        public static void Prefix(ref string movieName, ViewModel dataSource)
        {
            if (movieName == "CharacterDeveloper")
                movieName = "RFCharacterDeveloper";
        }
    }
}

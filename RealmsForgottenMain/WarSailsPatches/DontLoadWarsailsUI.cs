using HarmonyLib;
using NavalDLC.GauntletUI;
using System.Reflection;

namespace RealmsForgotten.WarSailsPatches
{
    public class DontLoadWarsailsUI
    {
        public static void Postfix(NavalDLCGauntletUISubModule __instance)
        {
            FieldInfo info = AccessTools.Field(typeof(NavalDLCGauntletUISubModule), "_initializedLoadingCategory");
            info.SetValue(__instance, false);
        }
    }
}

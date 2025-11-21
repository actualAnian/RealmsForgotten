using HarmonyLib;
using psai.net;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

/*
namespace RealmsForgotten.Patches
{
    [HarmonyPatch]
    public static class MBMusicManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBMusicManager), "Initialize")]
        public static void OverrideCreation()
        {
            if (!NativeConfig.DisableSound)
            {
                PsaiCore.Instance.LoadSoundtrackFromProjectFile(new List<string>() { "realmsforgotten" });
            }
        }
    }
}
*/

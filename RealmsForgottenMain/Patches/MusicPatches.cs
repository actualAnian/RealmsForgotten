using HarmonyLib;
using psai.net;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

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
                string path = ModuleHelper.GetModuleFullPath("realmsforgotten") + "music/soundtrack.xml";
                //if (IsPlatformSteamWorkshop())
                //{
                //    path = fullPath + "music/soundtrack_steam.xml";
                //}
                PsaiCore.Instance.LoadSoundtrackFromProjectFile(path);
            }
        }
    }
}

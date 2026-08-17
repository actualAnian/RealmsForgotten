using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace RF_DualWield
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            new Harmony("com.realmsforgotten.rfdualwield").PatchAll(typeof(SubModule).Assembly);
        }
    }
}

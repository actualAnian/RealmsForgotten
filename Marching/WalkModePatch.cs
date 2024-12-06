//using HarmonyLib;
//using TaleWorlds.MountAndBlade;

//namespace Marching
//{
//  [HarmonyPatch(typeof (Agent), "WalkMode", MethodType.Getter)]
//  public static class WalkModePatch
//  {
//    public static void Postfix(ref bool __result, Agent __instance)
//    {
//      if (!MarchingAgentStatCalculateModel.IsMarching(__instance))
//        return;
//      __result = true;
//    }
//  }
//}

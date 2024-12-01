// Decompiled with JetBrains decompiler
// Type: Marching.WalkModePatch
// Assembly: Marching, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: FAB07C52-9EF1-4E87-B983-D3A51612112E
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Marching\bin\Win64_Shipping_Client\Marching.dll

using HarmonyLib;
using TaleWorlds.MountAndBlade;


#nullable enable
namespace Marching
{
  [HarmonyPatch(typeof (Agent), "WalkMode", MethodType.Getter)]
  public static class WalkModePatch
  {
    public static void Postfix(ref bool __result, Agent __instance)
    {
      if (!MarchingAgentStatCalculateModel.IsMarching(__instance))
        return;
      __result = true;
    }
  }
}

using HarmonyLib;
using TaleWorlds.MountAndBlade.Objects;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(AreaMarker), "get_Tag")]
public static class AreaMarkerNullTagPatch
{
	[HarmonyPostfix]
	public static void Postfix(ref string __result)
	{
		if (__result == null)
		{
			__result = string.Empty;
		}
	}
}

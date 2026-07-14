using System.Reflection;
using HarmonyLib;
using Homesteads.Models;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.Localization;

namespace Homesteads.Patches;

[HarmonyPatch(typeof(PartyNameplateVM), "RefreshDynamicProperties")]
internal class HomesteadMapAppearanceNameplatePatch
{
	[HarmonyPostfix]
	private static void Postfix(PartyNameplateVM __instance)
	{
		Homestead homestead = Homestead.GetFor(__instance.Party);
		if (homestead != null)
		{
			FieldInfo fieldInfo = AccessTools.Field(typeof(PartyNameplateVM), "_latestNameTextObject");
			FieldInfo fieldInfo2 = AccessTools.Field(typeof(PartyNameplateVM), "_fullNameBind");
			TextObject textObject = (TextObject)fieldInfo.GetValue(__instance);
			string text = (string)fieldInfo2.GetValue(__instance);
			if (!(textObject == null) && text != null)
			{
				fieldInfo.SetValue(__instance, homestead.Name, BindingFlags.NonPublic, null, null);
				fieldInfo2.SetValue(__instance, homestead.Name.ToString(), BindingFlags.NonPublic, null, null);
			}
		}
	}
}

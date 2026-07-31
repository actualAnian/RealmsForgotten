using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Library;

namespace SOTOR.AbilitySystem;

[HarmonyPatch(typeof(CharacterDeveloperHeroItemVM), "RefreshValues")]
public static class SotorHideSpellcraftPatch
{
	public static void Postfix(CharacterDeveloperHeroItemVM __instance)
	{
		try
		{
			MBBindingList<SkillVM> mBBindingList = __instance?.Skills;
			if (mBBindingList == null)
			{
				return;
			}
			int num = 0;
			for (int num2 = mBBindingList.Count - 1; num2 >= 0; num2--)
			{
				if (mBBindingList[num2]?.Skill?.StringId == "SotorSpellcraft")
				{
					mBBindingList.RemoveAt(num2);
					num++;
				}
			}
			if (num > 0)
			{
				SotorLog.Info($"HIDEDIAG: removed {num} Spellcraft entr(ies) from skill grid ({mBBindingList.Count} remain).");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorHideSpellcraftPatch failed: " + ex.Message);
		}
	}
}

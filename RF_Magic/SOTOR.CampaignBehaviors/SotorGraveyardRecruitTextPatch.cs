using System;
using HarmonyLib;
using SandBox.ViewModelCollection;
using TaleWorlds.Localization;

namespace SOTOR.CampaignBehaviors;

[HarmonyPatch(typeof(SandBoxUIHelper), "GetRecruitNotificationText")]
public static class SotorGraveyardRecruitTextPatch
{
	private sealed class ScopedActivation : IDisposable
	{
		public ScopedActivation()
		{
			ActiveForGraveyardRaise = true;
		}

		public void Dispose()
		{
			ActiveForGraveyardRaise = false;
		}
	}

	public static bool ActiveForGraveyardRaise;

	public static IDisposable Scope()
	{
		return new ScopedActivation();
	}

	public static void Postfix(int recruitmentAmount, ref string __result)
	{
		try
		{
			if (ActiveForGraveyardRaise)
			{
				TextObject textObject = new TextObject("+{COUNT} Skeleton{?PLURAL}s{?}{\\?}");
				textObject.SetTextVariable("COUNT", recruitmentAmount);
				textObject.SetTextVariable("PLURAL", (recruitmentAmount > 1) ? 1 : 0);
				__result = textObject.ToString();
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorGraveyardRecruitTextPatch.Postfix failed: " + ex.Message);
		}
	}
}

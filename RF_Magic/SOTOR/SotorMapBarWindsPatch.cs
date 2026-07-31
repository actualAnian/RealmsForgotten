using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core.ViewModelCollection.Information;

using TaleWorlds.Localization;

namespace SOTOR;

[HarmonyPatch(typeof(MapInfoVM), "Refresh")]
public static class SotorMapBarWindsPatch
{
	private static readonly ConditionalWeakTable<MapInfoVM, MapInfoItemVM> _windsItems = new ConditionalWeakTable<MapInfoVM, MapInfoItemVM>();

	private static bool _loggedOnce;

	private static int _winds;

	private static int _maxWinds;

	private static float _rechargeRate;

	[HarmonyPostfix]
	public static void Postfix(MapInfoVM __instance)
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null || __instance?.SecondaryInfoItems == null)
			{
				return;
			}
			bool flag = mainHero.HasAttribute("SpellCaster");
			MapInfoItemVM value;
			bool flag2 = _windsItems.TryGetValue(__instance, out value);
			if (!_loggedOnce)
			{
				_loggedOnce = true;
				SotorLog.Info($"WINDSDIAG: MapInfoVM.Refresh postfix FIRED. caster={flag} secItems={__instance.SecondaryInfoItems.Count}.");
			}
			if (!flag)
			{
				if (flag2 && __instance.SecondaryInfoItems.Contains(value))
				{
					__instance.SecondaryInfoItems.Remove(value);
				}
				return;
			}
			if (!flag2)
			{
				value = new MapInfoItemVM("winds", GetWindsHintText);
				_windsItems.Add(__instance, value);
			}
			if (!__instance.SecondaryInfoItems.Contains(value))
			{
				__instance.SecondaryInfoItems.Add(value);
			}
			_winds = (int)Math.Round(mainHero.GetWindsOfMagic());
			_maxWinds = (int)Math.Round(mainHero.GetMaxWindsOfMagic());
			_rechargeRate = ExtendedInfoManager.GetWindsRechargePerHour(mainHero);
			value.HasWarning = _winds < 0;
			value.IntValue = _winds;
			value.Value = _winds.ToString();
		}
		catch (Exception ex)
		{
			SotorLog.Warn("WINDSDIAG: MapInfoVM.Refresh postfix failed: " + ex.Message);
		}
	}

	private static List<TooltipProperty> GetWindsHintText()
	{
		return new List<TooltipProperty>
		{
			new TooltipProperty(new TextObject("{=rf_mana}Mana").ToString(), _winds.ToString(), 0, onlyShowWhenExtended: false, TooltipProperty.TooltipPropertyFlags.Title),
			new TooltipProperty("Maximum:", _maxWinds.ToString(), 0),
			new TooltipProperty("Recharge Rate:", $"{_rechargeRate:0.00} / hour", 0)
		};
	}
}

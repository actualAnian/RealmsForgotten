using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SOTOR.GameManagers;

public static class SotorKeyInputManager
{
	public static void Initialize()
	{
		List<GameKeyContext> list = HotKeyManager.GetAllCategories().Cast<GameKeyContext>().ToList();
		if (!list.Any((GameKeyContext x) => x is SotorGameKeyContext))
		{
			list.Add(new SotorGameKeyContext());
		}
		HotKeyManager.RegisterInitialContexts((IEnumerable<GameKeyContext>)list); // [RF-A] 1.4.7: sobrecarga perdeu o bool
		RegisterKeybindStrings();
	}

	private static void RegisterKeybindStrings()
	{
		try
		{
			GameTextManager gameTextManager = Module.CurrentModule?.GlobalTextManager;
			if (gameTextManager != null)
			{
				gameTextManager.GetGameText("str_key_category_name").AddVariationWithId("SotorGameKeyContext", new TextObject("{=sotor_key_category_name}RF Magic"), new List<GameTextManager.ChoiceTag>());
				string variationId = "SotorGameKeyContext_" + 111;
				gameTextManager.GetGameText("str_key_name").AddVariationWithId(variationId, new TextObject("{=sotor_quickcast_key_name}Spellcasting Mode"), new List<GameTextManager.ChoiceTag>());
				gameTextManager.GetGameText("str_key_description").AddVariationWithId(variationId, new TextObject("{=sotor_quickcast_key_desc}Opens the spell selection menu in battle. Hold to pick a spell, release to enter aiming mode; left-click casts, right-click cancels."), new List<GameTextManager.ChoiceTag>());
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorKeyInputManager.RegisterKeybindStrings failed: " + ex.Message);
		}
	}
}

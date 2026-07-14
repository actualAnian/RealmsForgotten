namespace Homesteads;

public static class HomesteadTutorial
{
	public static int TutorialStage
	{
		get
		{
			if (HomesteadBehavior.Instance == null)
			{
				return 0;
			}
			return HomesteadBehavior.Instance.TutorialStage;
		}
		set
		{
			HomesteadBehavior.Instance.TutorialStage = value;
		}
	}

	public static void LaunchedMenu()
	{
		if (TutorialStage <= 0)
		{
			TutorialStage++;
			string localizedTag = "homestead_tutorial_launched_menu";
			string text = "Hello, you beautiful person. Welcome to Homesteads Reloaded! This tutorial will hopefully help you a little along this journey.\r\nTo the left of this message box, you will see your homestead's game menu. The two most important options are 'Walk around' and 'Manage homestead'.\r\nPlease click 'Manage homestead' to continue the tutorial, I won't pop up in 'Walk around' until you do. :)";
			Utils.ShowMessageBox(GetTitleLocalizedString(), GetTextLocalizedString(localizedTag, text));
		}
	}

	public static void ManagingHomestead()
	{
		if (TutorialStage <= 1)
		{
			TutorialStage++;
			string localizedTag = "homestead_tutorial_managing_homestead";
			string text = "Here you will find information about your homestead and some options to manage it. You might want to consider putting some starting food in the stash and depositing some gold.\r\nIn the information box, you will find that homesteads operate based on 3 main components; space, productivity, and leisure.\r\nSpace will increase the amount of troops you can have in your homestead.\r\nProductivity will increase the amount of gold your homestead generates, but it will decrease morale.\r\nLeisure does the opposite of productivity. It will decrease your gold made, but will increase the morale.\r\nYou will also find that your homestead has a tier level at the top! Your homestead, at maximum, can be tier 3. Upgrading your tier is important to many different systems in homesteads. A fully developed Tier 3 homestead can later be promoted into a true settlement.\r\nUpgrading your tier can be done by placing more troops in your homestead and assigning a leader with good Steward and Engineering skill.\r\nLet's go 'Walk around' a bit, yea? I'm feeling antsy :) also don't forget to store some gold and food before you leave!";
			Utils.ShowMessageBox(GetTitleLocalizedString(), GetTextLocalizedString(localizedTag, text));
		}
	}

	public static void WalkAround()
	{
		if (TutorialStage <= 2)
		{
			TutorialStage++;
			string localizedTag = "homestead_tutorial_walk_around";
			string text = "Welcome to the site of your new homestead! Hopefully this is a good spot — but if not, walk around and find a better one. You can also pack up and move the whole homestead elsewhere on the campaign map by talking to the leader you assigned.\r\nHere's where the fun begins! Press {EDIT_MODE_KEY}, by default, to cycle through your edit modes (build, destroy, move, and template). Press {SPAWN_KEY} at any time to set your spawn position.\r\nWhile in build mode, press {CATEGORY_KEY} to switch building categories and {CYCLE_LEFT_KEY} / {CYCLE_RIGHT_KEY} to cycle through the buildings in the current category — watch the bottom-left for details on the highlighted building.\r\nPress {ROTATE_LEFT_KEY} / {ROTATE_RIGHT_KEY} to turn it, or hold the right mouse button and move the mouse to freely tilt and spin it. Scroll the mouse wheel to raise or lower it, {SNAP_KEY} snaps it to the ground, and {RESET_KEY} resets its orientation. When you're happy, press {PLACE_KEY} to place!\r\nTip: build a Dog Kennel or a Market and you'll meet new faces — a Hound Master who can give you a loyal companion hound, and a Market Lady with trade tips.";
			MCMSettings settings = HomesteadsReloaded.Settings;
			Utils.ShowMessageBox(GetTitleLocalizedString(), GetTextLocalizedString(localizedTag, text, ("EDIT_MODE_KEY", settings?.GetEditModeKeyLabel() ?? "\\"), ("SPAWN_KEY", settings?.GetSetPlayerSpawnKeyLabel() ?? "O"), ("CATEGORY_KEY", settings?.GetSwitchBuilderModeCategoryKeyLabel() ?? "'"), ("CYCLE_LEFT_KEY", settings?.GetCycleLeftKeyLabel() ?? "["), ("CYCLE_RIGHT_KEY", settings?.GetCycleRightKeyLabel() ?? "]"), ("ROTATE_LEFT_KEY", settings?.GetRotateTurnLeftKeyLabel() ?? "Q"), ("ROTATE_RIGHT_KEY", settings?.GetRotateTurnRightKeyLabel() ?? "E"), ("SNAP_KEY", settings?.GetSnapToGroundKeyLabel() ?? "G"), ("RESET_KEY", settings?.GetResetRotationKeyLabel() ?? "Ctrl"), ("PLACE_KEY", settings?.GetPlaceKeyLabel() ?? "F")), pauseGameActiveState: false);
		}
	}

	private static string GetTextLocalizedString(string localizedTag, string text, params (string, string)[] textVars)
	{
		return Utils.GetLocalizedString("{=" + localizedTag + "}" + text, textVars);
	}

	private static string GetTitleLocalizedString()
	{
		return Utils.GetLocalizedString("{=homestead_tutorial_title}Homesteads Reloaded Tutorial");
	}
}

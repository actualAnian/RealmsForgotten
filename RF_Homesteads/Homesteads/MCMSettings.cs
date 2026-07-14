using System;
using System.Collections.Generic;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace Homesteads;

public class MCMSettings : AttributeGlobalSettings<MCMSettings>
{
	private static readonly Dictionary<string, string> KeyAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["-"] = "Minus",
		["_"] = "Minus",
		["="] = "Equals",
		["+"] = "Equals",
		["["] = "OpenBraces",
		["]"] = "CloseBraces",
		["'"] = "Apostrophe",
		["\""] = "Apostrophe",
		[";"] = "SemiColon",
		[","] = "Comma",
		["."] = "Period",
		["/"] = "Slash",
		["\\"] = "BackSlash",
		["`"] = "Tilde",
		["~"] = "Tilde"
	};

	private static readonly Dictionary<string, string> KeyDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["Minus"] = "-",
		["Equals"] = "=",
		["OpenBraces"] = "[",
		["CloseBraces"] = "]",
		["Apostrophe"] = "\"",
		["SemiColon"] = ";",
		["Comma"] = ",",
		["Period"] = ".",
		["Slash"] = "/",
		["BackSlash"] = "\\",
		["Tilde"] = "`",
		["D1"] = "1",
		["D2"] = "2",
		["D3"] = "3",
		["D4"] = "4",
		["D5"] = "5",
		["D6"] = "6",
		["D7"] = "7",
		["D8"] = "8",
		["LeftMouseButton"] = "LMB",
		["LeftControl"] = "Ctrl",
		["Space"] = "Space",
		["LeftAlt"] = "Alt",
		["LeftShift"] = "Shift"
	};

	private static readonly string[] CtrlButtons = new string[15]
	{
		"None", "ControllerLUp", "ControllerLDown", "ControllerLLeft", "ControllerLRight", "ControllerRUp", "ControllerRDown", "ControllerRLeft", "ControllerRRight", "ControllerLBumper",
		"ControllerRBumper", "ControllerLTrigger", "ControllerRTrigger", "ControllerLThumb", "ControllerRThumb"
	};

	[SettingPropertyBool("{=MCM_HR_ShowDailyProductionNotifications_Name}Daily Production Notifications", Order = 1, HintText = "{=MCM_HR_ShowDailyProductionNotifications_Hint}Show daily production reports for homesteads (gold, produced goods, training results, and the Tavern's daily road-toll income).", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Notifications}Notifications", GroupOrder = 0)]
	public bool ShowDailyProductionNotifications { get; set; } = true;

	[SettingPropertyBool("{=MCM_HR_ShowCaravanTradeNotifications_Name}Caravan Trade Notifications", Order = 2, HintText = "{=MCM_HR_ShowCaravanTradeNotifications_Hint}Show notifications when caravans trade with homesteads.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Notifications}Notifications", GroupOrder = 0)]
	public bool ShowCaravanTradeNotifications { get; set; } = true;

	[SettingPropertyBool("{=MCM_HR_ShowRecruitNotifications_Name}Recruit Arrival Notifications", Order = 3, HintText = "{=MCM_HR_ShowRecruitNotifications_Hint}Show notifications when recruits arrive at or depart for homesteads.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Notifications}Notifications", GroupOrder = 0)]
	public bool ShowRecruitNotifications { get; set; } = true;

	[SettingPropertyBool("{=MCM_HR_ShowNpcXpNotifications_Name}NPC XP & Relation Notifications", Order = 4, HintText = "{=MCM_HR_ShowNpcXpNotifications_Hint}Show daily homestead notifications for: leader & companion skill gains (Steward, Engineering, Leadership from the homestead; Trade from a Market or caravan trades; Charm from an Ambassador's Hall) and the Ambassador's diplomatic relation improvements with nearby lords, notables, and caravan owners.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Notifications}Notifications", GroupOrder = 0)]
	public bool ShowNpcXpNotifications { get; set; } = true;

	[SettingPropertyBool("{=MCM_HR_ShowPatrolNotifications_Name}Patrol Notifications", Order = 5, HintText = "{=MCM_HR_ShowPatrolNotifications_Hint}Show notifications about patrol dispatch and status.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Notifications}Notifications", GroupOrder = 0)]
	public bool ShowPatrolNotifications { get; set; } = true;

	[SettingPropertyBool("{=MCM_HR_AutoTroopUpgradesEnabled_Name}Automatic Troop Upgrades", Order = 11, HintText = "{=MCM_HR_AutoTroopUpgradesEnabled_Hint}When enabled, garrison and patrol troops automatically promote to the next tier once they accumulate enough XP (and your homestead has the required gold and items). Disable if you want to manage troop tiers yourself. Troops still earn XP either way.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Gameplay}Gameplay", GroupOrder = 10)]
	public bool AutoTroopUpgradesEnabled { get; set; } = true;

	[SettingPropertyBool("{=MCM_HR_ShowNavPointsInBattle_Name}Show Nav Points During Battle", Order = 21, HintText = "{=MCM_HR_ShowNavPointsInBattle_Hint}If enabled, Nav Point flags you have placed in the homestead will be visible (in green) during battles. Disabled by default to keep the battlefield visually clean.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Battle}Battle", GroupOrder = 20)]
	public bool ShowNavPointsInBattle { get; set; }

	[SettingPropertyInteger("{=MCM_HR_MaxBattleDogs_Name}Max Battle Dogs", 0, 20, "0", Order = 22, HintText = "{=MCM_HR_MaxBattleDogs_Hint}Maximum number of dogs from your homestead stash that will join you in battle. Set to 0 to disable battle dogs entirely.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Battle}Battle", GroupOrder = 20)]
	public int MaxBattleDogs { get; set; } = 5;

	[SettingPropertyInteger("{=MCM_HR_TradeSellFoodMin_Name}Sell Minimum: Food", 0, 2000, "0", Order = 31, HintText = "{=MCM_HR_TradeSellFoodMin_Hint}The minimum amount of food to keep in stash before selling the surplus.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Trade}Caravan Trade Thresholds", GroupOrder = 30)]
	public int TradeSellFoodMin { get; set; } = 50;

	[SettingPropertyInteger("{=MCM_HR_TradeSellBuildingMaterialsMin_Name}Sell Minimum: Building Materials", 0, 2000, "0", Order = 32, HintText = "{=MCM_HR_TradeSellBuildingMaterialsMin_Hint}The minimum amount of building materials to keep in stash before selling the surplus.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Trade}Caravan Trade Thresholds", GroupOrder = 30)]
	public int TradeSellBuildingMaterialsMin { get; set; } = 60;

	[SettingPropertyInteger("{=MCM_HR_TradeSellGeneralGoodsMin_Name}Sell Minimum: General Goods", 0, 2000, "0", Order = 33, HintText = "{=MCM_HR_TradeSellGeneralGoodsMin_Hint}The minimum amount of general goods to keep in stash before selling the surplus.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Trade}Caravan Trade Thresholds", GroupOrder = 30)]
	public int TradeSellGeneralGoodsMin { get; set; } = 5;

	[SettingPropertyInteger("{=MCM_HR_TradeBuyFoodMax_Name}Buy Maximum: Food", 0, 2000, "0", Order = 34, HintText = "{=MCM_HR_TradeBuyFoodMax_Hint}The maximum amount of food the homestead will stockpile by buying from caravans.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Trade}Caravan Trade Thresholds", GroupOrder = 30)]
	public int TradeBuyFoodMax { get; set; } = 100;

	[SettingPropertyInteger("{=MCM_HR_TradeBuyBuildingMaterialsMax_Name}Buy Maximum: Building Materials", 0, 2000, "0", Order = 35, HintText = "{=MCM_HR_TradeBuyBuildingMaterialsMax_Hint}The maximum amount of building materials the homestead will stockpile by buying from caravans.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Trade}Caravan Trade Thresholds", GroupOrder = 30)]
	public int TradeBuyBuildingMaterialsMax { get; set; } = 100;

	[SettingPropertyInteger("{=MCM_HR_TradeBuyHorsesMax_Name}Buy Maximum: Horses (per category)", 0, 100, "0", Order = 36, HintText = "{=MCM_HR_TradeBuyHorsesMax_Hint}The maximum number of riding horses or war horses the homestead will stockpile per upgrade category by buying from passing caravans. Set to 0 to disable.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Trade}Caravan Trade Thresholds", GroupOrder = 30)]
	public int TradeBuyHorsesMax { get; set; } = 15;

	[SettingPropertyText("{=MCM_HR_KeyBindEditMode_Name}Toggle Edit Mode Types", -1, true, "", Order = 41, HintText = "{=MCM_HR_KeyBindEditMode_Hint}The button that is used to switch to an edit mode or turn it off.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindEditMode { get; set; } = "\\";

	[SettingPropertyText("{=MCM_HR_KeyBindSwitchBuilderModeCategory_Name}Switch Builder Mode Category", -1, true, "", Order = 42, HintText = "{=MCM_HR_KeyBindSwitchBuilderModeCategory_Hint}The button that will switch your build menu's category of placeables to the next category.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindSwitchBuilderModeCategory { get; set; } = "Apostrophe";

	[SettingPropertyText("{=MCM_HR_KeyBindCycleLeft_Name}Cycle Placeables Left", -1, true, "", Order = 43, HintText = "{=MCM_HR_KeyBindCycleLeft_Hint}One of two buttons that are used to switch to a different placeable.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindCycleLeft { get; set; } = "OpenBraces";

	[SettingPropertyText("{=MCM_HR_KeyBindCycleRight_Name}Cycle Placeables Right", -1, true, "", Order = 44, HintText = "{=MCM_HR_KeyBindCycleRight_Hint}One of two buttons that are used to switch to a different placeable.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindCycleRight { get; set; } = "CloseBraces";

	[SettingPropertyText("{=MCM_HR_KeyBindPlace_Name}Place Highlighted Placeable", -1, true, "", Order = 45, HintText = "{=MCM_HR_KeyBindPlace_Hint}The button that is used to place the selected placeable.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindPlace { get; set; } = "F";

	[SettingPropertyText("{=MCM_HR_KeyBindSetPlayerSpawn_Name}Set Player Spawn", -1, true, "", Order = 46, HintText = "{=MCM_HR_KeyBindSetPlayerSpawn_Hint}The button that is used to set the player's spawn.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindSetPlayerSpawn { get; set; } = "O";

	[SettingPropertyText("{=MCM_HR_KeyBindResetRotation_Name}Reset Rotation", -1, true, "", Order = 47, HintText = "{=MCM_HR_KeyBindResetRotation_Hint}The button that is used to reset the rotation of the dummy placeable while in edit mode.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindResetRotation { get; set; } = "LeftControl";

	[SettingPropertyText("{=MCM_HR_KeyBindRotateTurnLeft_Name}Rotate Turn Left", -1, true, "", Order = 48, HintText = "{=MCM_HR_KeyBindRotateTurnLeft_Hint}The button that is used to turn the highlighted placeable left.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindRotateTurnLeft { get; set; } = "Q";

	[SettingPropertyText("{=MCM_HR_KeyBindRotateTurnRight_Name}Rotate Turn Right", -1, true, "", Order = 49, HintText = "{=MCM_HR_KeyBindRotateTurnRight_Hint}The button that is used to turn the highlighted placeable right.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindRotateTurnRight { get; set; } = "E";

	[SettingPropertyText("{=MCM_HR_KeyBindMoveUp_Name}Move Up", -1, true, "", Order = 50, HintText = "{=MCM_HR_KeyBindMoveUp_Hint}The button that is used to move the highlighted placeable upwards.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindMoveUp { get; set; } = "I";

	[SettingPropertyText("{=MCM_HR_KeyBindMoveDown_Name}Move Down", -1, true, "", Order = 51, HintText = "{=MCM_HR_KeyBindMoveDown_Hint}The button that is used to move the highlighted placeable downwards.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindMoveDown { get; set; } = "D8";

	[SettingPropertyText("{=MCM_HR_KeyBindSnapToGround_Name}Snap to Ground", -1, true, "", Order = 52, HintText = "{=MCM_HR_KeyBindSnapToGround_Hint}Snaps the building preview (or template) to terrain height at the cursor position.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindSnapToGround { get; set; } = "G";

	[SettingPropertyText("{=MCM_HR_KeyBindToggleHeightLock_Name}Toggle Height Lock", -1, true, "", Order = 53, HintText = "{=MCM_HR_KeyBindToggleHeightLock_Hint}Toggles between terrain-following mode (preview Z tracks the ground beneath the cursor) and height-lock mode (preview holds a fixed elevation; only scroll wheel and Move Up/Down change the height).", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindToggleHeightLock { get; set; } = "H";

	[SettingPropertyText("{=MCM_HR_KeyBindSicEm_Name}Sic 'Em (Companion Dog)", -1, true, "", Order = 54, HintText = "{=MCM_HR_KeyBindSicEm_Hint}In combat, press this key to toggle Sic 'Em mode — your companion dog will chase the nearest enemy regardless of distance. Press again to stand down. Avoid keys with existing Bannerlord battle bindings (B = banner window, G = drop weapon, etc.) as both actions will fire simultaneously.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindSicEm { get; set; } = "N";

	[SettingPropertyText("{=MCM_HR_KeyBindPetDog_Name}Pet Dog", -1, true, "", Order = 55, HintText = "{=MCM_HR_KeyBindPetDog_Hint}While walking around (not in battle), look at a nearby dog and press this key to give it a pat — the dog barks happily. Defaults to F, the standard interact key (only active for dogs when you are not building).", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindPetDog { get; set; } = "F";

	[SettingPropertyText("{=MCM_HR_KeyBindRaceFlagToggle_Name}Toggle Race Gate Flags", -1, true, "", Order = 56, HintText = "{=MCM_HR_KeyBindRaceFlagToggle_Hint}During a horse race, press this key to show/hide the gate flag-poles (e.g. for screenshots). Defaults to \\.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindRaceFlagToggle { get; set; } = "\\";

	[SettingPropertyText("{=MCM_HR_KeyBindOpenBuildMenu_Name}Open Build Menu", -1, true, "", Order = 57, HintText = "{=MCM_HR_KeyBindOpenBuildMenu_Hint}While in Build mode, press this key to open the building picker — a browsable list of every available placeable, grouped by category, with full details and one-click selection. Defaults to the backtick/tilde key (` ). On controller, D-pad Down opens it instead.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_KeyBinds}Change Key Binds", GroupOrder = 40)]
	public string KeyBindOpenBuildMenu { get; set; } = "Tilde";

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindEditMode_Name}Cycle Edit Mode", Order = 45, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindEditMode_Hint}Controller button that cycles through the build/edit modes (D-pad up by default). Also rotates the object's pitch while the modifier is held.")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindEditMode { get; set; } = CtrlDropdown("ControllerLUp");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindCategory_Name}Switch Category", Order = 46, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindCategory_Hint}Controller button that switches the builder category (D-pad down by default). Also rotates the object's pitch while the modifier is held.")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindCategory { get; set; } = CtrlDropdown("ControllerLDown");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindCycleLeft_Name}Cycle Placeables Left", Order = 47, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindCycleLeft_Hint}Controller button that cycles placeables/templates left (D-pad left by default). Also rotates the object left while the modifier is held.")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindCycleLeft { get; set; } = CtrlDropdown("ControllerLLeft");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindCycleRight_Name}Cycle Placeables Right", Order = 48, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindCycleRight_Hint}Controller button that cycles placeables/templates right (D-pad right by default). Also rotates the object right while the modifier is held.")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindCycleRight { get; set; } = CtrlDropdown("ControllerLRight");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindPlace_Name}Place / Confirm", Order = 49, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindPlace_Hint}Controller button that places the highlighted placeable (A by default — also drives the native cursor click).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindPlace { get; set; } = CtrlDropdown("ControllerRDown");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindHeightLockSnap_Name}Height Lock + Snap to Ground", Order = 50, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindHeightLockSnap_Hint}Dual-purpose button: toggles height lock AND snaps the preview to the ground in one press (X by default).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindHeightLockSnap { get; set; } = CtrlDropdown("ControllerRLeft");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindModifier_Name}Camera/Rotate Modifier (hold)", Order = 51, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindModifier_Hint}Hold this button (Y by default): the left stick flies the camera and the D-pad rotates the held object instead of cycling.")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindModifier { get; set; } = CtrlDropdown("ControllerRUp");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindResetRotation_Name}Reset Rotation", Order = 52, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindResetRotation_Hint}Controller button that resets the held object's rotation (right stick click by default).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindResetRotation { get; set; } = CtrlDropdown("ControllerRThumb");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindMoveUp_Name}Object Up", Order = 53, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindMoveUp_Hint}Controller button that raises the held object (right trigger by default).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindMoveUp { get; set; } = CtrlDropdown("ControllerRTrigger");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindMoveDown_Name}Object Down", Order = 54, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindMoveDown_Hint}Controller button that lowers the held object (left trigger by default).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindMoveDown { get; set; } = CtrlDropdown("ControllerLTrigger");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindCameraUp_Name}Camera Up", Order = 55, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindCameraUp_Hint}Controller button that raises the free camera (right bumper by default).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindCameraUp { get; set; } = CtrlDropdown("ControllerRBumper");

	[SettingPropertyDropdown("{=MCM_HR_CtrlBindCameraDown_Name}Camera Down", Order = 56, RequireRestart = false, HintText = "{=MCM_HR_CtrlBindCameraDown_Hint}Controller button that lowers the free camera (left bumper by default).")]
	[SettingPropertyGroup("{=MCM_HR_Group_CtrlBinds}Controller Binds", GroupOrder = 45)]
	public Dropdown<string> CtrlBindCameraDown { get; set; } = CtrlDropdown("ControllerLBumper");

	[SettingPropertyBool("{=MCM_HR_EnableDebugLogging_Name}Enable Debug Logging", Order = 61, HintText = "{=MCM_HR_EnableDebugLogging_Hint}Write detailed trace messages to the HomesteadsReloaded.trace.log file. Disable for better performance.", RequireRestart = false)]
	[SettingPropertyGroup("{=MCM_HR_Group_Debug}Debug", GroupOrder = 60)]
	public bool EnableDebugLogging { get; set; }

	public override string Id => "HomesteadsReloaded";

	public override string DisplayName => new TextObject("{=MCM_HR_Mod_Name}Homesteads Reloaded").ToString();

	public override string FolderName => "HomesteadsReloaded";

	public override string FormatType { get; } = "xml";

	public bool LoadMCMConfigFile { get; set; } = true;

	private static Dropdown<string> CtrlDropdown(string defaultButton)
	{
		return new Dropdown<string>(CtrlButtons, Math.Max(0, Array.IndexOf(CtrlButtons, defaultButton)));
	}

	public InputKey GetEditModeKey()
	{
		return GetKey(KeyBindEditMode, InputKey.BackSlash);
	}

	public InputKey GetSwitchBuilderModeCategoryKey()
	{
		return GetKey(KeyBindSwitchBuilderModeCategory, InputKey.Apostrophe);
	}

	public InputKey GetCycleLeftKey()
	{
		return GetKey(KeyBindCycleLeft, InputKey.OpenBraces);
	}

	public InputKey GetCycleRightKey()
	{
		return GetKey(KeyBindCycleRight, InputKey.CloseBraces);
	}

	public InputKey GetPlaceKey()
	{
		return GetKey(KeyBindPlace, InputKey.F);
	}

	public InputKey GetSetPlayerSpawnKey()
	{
		return GetKey(KeyBindSetPlayerSpawn, InputKey.O);
	}

	public InputKey GetResetRotationKey()
	{
		return GetKey(KeyBindResetRotation, InputKey.LeftControl);
	}

	public InputKey GetRotateTurnLeftKey()
	{
		InputKey key = GetKey(KeyBindRotateTurnLeft, InputKey.Q);
		if (key != InputKey.D5)
		{
			return key;
		}
		return InputKey.Q;
	}

	public InputKey GetRotateTurnRightKey()
	{
		InputKey key = GetKey(KeyBindRotateTurnRight, InputKey.E);
		if (key != InputKey.D6)
		{
			return key;
		}
		return InputKey.E;
	}

	public InputKey GetMoveUpKey()
	{
		InputKey key = GetKey(KeyBindMoveUp, InputKey.I);
		if (key != InputKey.D7)
		{
			return key;
		}
		return InputKey.I;
	}

	public InputKey GetMoveDownKey()
	{
		return GetKey(KeyBindMoveDown, InputKey.D8);
	}

	public InputKey GetSnapToGroundKey()
	{
		return GetKey(KeyBindSnapToGround, InputKey.G);
	}

	public InputKey GetToggleHeightLockKey()
	{
		return GetKey(KeyBindToggleHeightLock, InputKey.H);
	}

	private static InputKey GetCtrlKey(Dropdown<string>? dd, InputKey fallback)
	{
		try
		{
			string text = dd?.SelectedValue;
			if (string.IsNullOrEmpty(text))
			{
				return fallback;
			}
			if (text == "None")
			{
				return InputKey.Invalid;
			}
			return (InputKey)Enum.Parse(typeof(InputKey), text);
		}
		catch
		{
			return fallback;
		}
	}

	public InputKey GetCtrlEditModeKey()
	{
		return GetCtrlKey(CtrlBindEditMode, InputKey.ControllerLUp);
	}

	public InputKey GetCtrlCategoryKey()
	{
		return GetCtrlKey(CtrlBindCategory, InputKey.ControllerLDown);
	}

	public InputKey GetCtrlCycleLeftKey()
	{
		return GetCtrlKey(CtrlBindCycleLeft, InputKey.ControllerLLeft);
	}

	public InputKey GetCtrlCycleRightKey()
	{
		return GetCtrlKey(CtrlBindCycleRight, InputKey.ControllerLRight);
	}

	public InputKey GetCtrlPlaceKey()
	{
		return GetCtrlKey(CtrlBindPlace, InputKey.ControllerRDown);
	}

	public InputKey GetCtrlHeightLockSnapKey()
	{
		return GetCtrlKey(CtrlBindHeightLockSnap, InputKey.ControllerRLeft);
	}

	public InputKey GetCtrlModifierKey()
	{
		return GetCtrlKey(CtrlBindModifier, InputKey.ControllerRUp);
	}

	public InputKey GetCtrlResetRotationKey()
	{
		return GetCtrlKey(CtrlBindResetRotation, InputKey.ControllerRThumb);
	}

	public InputKey GetCtrlMoveUpKey()
	{
		return GetCtrlKey(CtrlBindMoveUp, InputKey.ControllerRTrigger);
	}

	public InputKey GetCtrlMoveDownKey()
	{
		return GetCtrlKey(CtrlBindMoveDown, InputKey.ControllerLTrigger);
	}

	public InputKey GetCtrlCameraUpKey()
	{
		return GetCtrlKey(CtrlBindCameraUp, InputKey.ControllerRBumper);
	}

	public InputKey GetCtrlCameraDownKey()
	{
		return GetCtrlKey(CtrlBindCameraDown, InputKey.ControllerLBumper);
	}

	public InputKey GetSicEmKey()
	{
		return GetKey(KeyBindSicEm, InputKey.N);
	}

	public InputKey GetPetDogKey()
	{
		return GetKey(KeyBindPetDog, InputKey.F);
	}

	public InputKey GetRaceFlagToggleKey()
	{
		return GetKey(KeyBindRaceFlagToggle, InputKey.BackSlash);
	}

	public InputKey GetOpenBuildMenuKey()
	{
		return GetKey(KeyBindOpenBuildMenu, InputKey.Tilde);
	}

	public string GetOpenBuildMenuKeyLabel()
	{
		return GetKeyLabel(KeyBindOpenBuildMenu, "Tilde");
	}

	public string GetEditModeKeyLabel()
	{
		return GetKeyLabel(KeyBindEditMode, "BackSlash");
	}

	public string GetSetPlayerSpawnKeyLabel()
	{
		return GetKeyLabel(KeyBindSetPlayerSpawn, "O");
	}

	public string GetSwitchBuilderModeCategoryKeyLabel()
	{
		return GetKeyLabel(KeyBindSwitchBuilderModeCategory, "Apostrophe");
	}

	public string GetCycleLeftKeyLabel()
	{
		return GetKeyLabel(KeyBindCycleLeft, "OpenBraces");
	}

	public string GetCycleRightKeyLabel()
	{
		return GetKeyLabel(KeyBindCycleRight, "CloseBraces");
	}

	public string GetPlaceKeyLabel()
	{
		return GetKeyLabel(KeyBindPlace, "F");
	}

	public string GetResetRotationKeyLabel()
	{
		return GetKeyLabel(KeyBindResetRotation, "LeftControl");
	}

	public string GetRotateTurnLeftKeyLabel()
	{
		return GetKeyLabel(KeyBindRotateTurnLeft, "Q");
	}

	public string GetRotateTurnRightKeyLabel()
	{
		return GetKeyLabel(KeyBindRotateTurnRight, "E");
	}

	public string GetSnapToGroundKeyLabel()
	{
		return GetKeyLabel(KeyBindSnapToGround, "G");
	}

	public string GetToggleHeightLockKeyLabel()
	{
		return GetKeyLabel(KeyBindToggleHeightLock, "H");
	}

	public string GetMoveUpKeyLabel()
	{
		return GetKeyLabel(KeyBindMoveUp, "I");
	}

	public string GetMoveDownKeyLabel()
	{
		return GetKeyLabel(KeyBindMoveDown, "8");
	}

	public string GetSicEmKeyLabel()
	{
		return GetKeyLabel(KeyBindSicEm, "N");
	}

	public string GetPetDogKeyLabel()
	{
		return GetKeyLabel(KeyBindPetDog, "F");
	}

	private InputKey GetKey(string toUse, InputKey defaultKey)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(toUse))
			{
				return defaultKey;
			}
			toUse = toUse.Trim();
			if (KeyAliases.TryGetValue(toUse, out string value))
			{
				toUse = value;
			}
			else if (toUse.Length == 1)
			{
				toUse = toUse.ToUpper();
			}
			return (InputKey)Enum.Parse(typeof(InputKey), toUse);
		}
		catch (Exception)
		{
			return defaultKey;
		}
	}

	private static string GetKeyLabel(string configuredValue, string fallbackKeyName)
	{
		string text = (string.IsNullOrWhiteSpace(configuredValue) ? fallbackKeyName : configuredValue.Trim());
		if (KeyAliases.TryGetValue(text, out string value))
		{
			text = value;
		}
		if (KeyDisplayNames.TryGetValue(text, out string value2))
		{
			return value2;
		}
		return text;
	}
}

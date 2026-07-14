using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace Homesteads.Models;

public class HomesteadMissionView : MissionView
{
	private sealed class PassiveGauntletLayer : GauntletLayer
	{
		public PassiveGauntletLayer(string name, int localOrder, bool shouldClear = false)
			: base(name, localOrder, shouldClear)
		{
		}

		public override bool HitTest()
		{
			return false;
		}
	}

	public static HomesteadMissionView? Instance;

	private HomesteadScene homesteadScene;

	private GauntletLayer layer;

	private GauntletMovieIdentifier movie;

	public HomesteadVM dataSource;

	private float forceHiddenCursorTimer;

	public HomesteadMissionView(Homestead homestead)
	{
		Instance = this;
		homesteadScene = homestead.GetHomesteadScene();
	}

	public static void TriggerSceneChanges()
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.BuildPointsLeft = Instance.homesteadScene.BuildPointsLeftToUse;
			Instance.dataSource.TotalProductivity = Instance.homesteadScene.TotalProductivity;
			Instance.dataSource.ExtraSpace = Instance.homesteadScene.TotalSpace;
			Instance.dataSource.TotalLeisure = Instance.homesteadScene.TotalLeisure;
			Instance.dataSource.UpdateAvailableMaterials(Instance.homesteadScene.Homestead);
		}
	}

	public static void TriggerPlanningSceneChanges(IEnumerable<HomesteadSceneSavedEntity> planningEntities)
	{
		if (Instance == null || Instance.dataSource == null)
		{
			return;
		}
		List<HomesteadSceneSavedEntity> list = planningEntities.ToList();
		int buildPointsLeft = list.Sum((HomesteadSceneSavedEntity e) => e.Placeable?.BuildPointsRequired ?? 0);
		int totalProductivity = list.Sum((HomesteadSceneSavedEntity e) => e.Placeable?.ProductivityIncrease ?? 0);
		int extraSpace = list.Sum((HomesteadSceneSavedEntity e) => e.Placeable?.SpaceIncrease ?? 0);
		int totalLeisure = list.Sum((HomesteadSceneSavedEntity e) => e.Placeable?.LeisureIncrease ?? 0);
		Instance.dataSource.BuildPointsLeft = buildPointsLeft;
		Instance.dataSource.TotalProductivity = totalProductivity;
		Instance.dataSource.ExtraSpace = extraSpace;
		Instance.dataSource.TotalLeisure = totalLeisure;
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (HomesteadSceneSavedEntity item in list)
		{
			if (item.Placeable?.ItemRequirements == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, int> itemRequirement in item.Placeable.ItemRequirements)
			{
				if (!dictionary.ContainsKey(itemRequirement.Key))
				{
					dictionary[itemRequirement.Key] = 0;
				}
				dictionary[itemRequirement.Key] += itemRequirement.Value;
			}
		}
		if (dictionary.Count == 0)
		{
			Instance.dataSource.AvailableMaterialsText = ((list.Count == 0) ? Utils.GetLocalizedString("{=homestead_planning_empty}No buildings placed yet") : Utils.GetLocalizedString("{=homestead_planning_free}No materials required"));
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			string arg = item2.Key;
			string[] array = item2.Key.Split(new char[1] { '|' });
			foreach (string text in array)
			{
				ItemObject itemObject = Campaign.Current?.ObjectManager?.GetObject<ItemObject>(text.Trim());
				if (itemObject != null)
				{
					arg = itemObject.Name.ToString();
					break;
				}
			}
			stringBuilder.AppendLine($"{arg}: {item2.Value}");
		}
		Instance.dataSource.AvailableMaterialsText = stringBuilder.ToString().TrimEnd(Array.Empty<char>());
	}

	public static void SetStatVisibility(bool visible)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.AreStatsVisible = visible;
		}
	}

	public static void SetPlaceableBoxVisibility(bool visible)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.CurrentPlaceableBoxVisible = visible;
		}
	}

	public static void SetTargetBoxVisibility(bool visible)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.TargetBoxVisible = visible;
		}
	}

	public static void SetTargetInfo(string targetName, string targetHint)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.TargetBoxVisible = true;
			Instance.dataSource.TargetName = targetName;
			Instance.dataSource.TargetHint = targetHint;
		}
	}

	public static void ChangeCurrentPlaceable(string displayName, string description)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.CurrentPlaceableName = displayName;
			Instance.dataSource.CurrentPlaceableDesc = description;
		}
	}

	public static void SetBuilderCategory(string categoryName)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.CurrentCategoryName = categoryName;
		}
	}

	public static void SetHeightLockState(bool locked, float lockedZ)
	{
		if (Instance != null && Instance.dataSource != null)
		{
			Instance.dataSource.HeightModeText = (locked ? Utils.GetLocalizedString("{=homestead_gui_height_locked}Height: Locked ({Z}m)", ("Z", lockedZ.ToString("F1"))) : Utils.GetLocalizedString("{=homestead_gui_height_follow}Height: Follow Terrain"));
		}
	}

	public override void OnMissionScreenInitialize()
	{
		dataSource = new HomesteadVM(homesteadScene);
		layer = new PassiveGauntletLayer("HomesteadHUD", 1);
		layer.IsFocusLayer = false;
		layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		movie = layer.LoadMovie("HomesteadEditorHUD", dataSource);
		((ScreenBase)(object)((MissionView)this).MissionScreen).AddLayer((ScreenLayer)layer);
		forceHiddenCursorTimer = 5f;
		ForceMissionInputMode();
	}

	public override void OnMissionScreenActivate()
	{
		forceHiddenCursorTimer = 5f;
		ForceMissionInputMode();
	}

	public override void OnMissionScreenDeactivate()
	{
		MouseManager.ShowCursor(show: true);
	}

	public override void OnMissionScreenTick(float dt)
	{
		if (forceHiddenCursorTimer > 0f)
		{
			forceHiddenCursorTimer -= dt;
		}
		ForceMissionInputMode();
	}

	public override void OnMissionScreenFinalize()
	{
		if (layer != null)
		{
			((ScreenBase)(object)((MissionView)this).MissionScreen).RemoveLayer((ScreenLayer)layer);
		}
		Instance = null;
	}

	private void ForceMissionInputMode()
	{
		if (((MissionView)this).MissionScreen == null || IsAnyBlockingDialogActive())
		{
			return;
		}
		bool flag = HomesteadFreeCameraView.Instance?.IsActive ?? false;
		if (forceHiddenCursorTimer > 0f)
		{
			if (!flag)
			{
				((MissionView)this).MissionScreen.SetDisplayDialog(false);
			}
			if (!flag)
			{
				MouseManager.ShowCursor(show: false);
			}
		}
		layer?.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
	}

	private bool IsAnyBlockingDialogActive()
	{
		Campaign current = Campaign.Current;
		if (current != null && current.ConversationManager?.IsConversationInProgress == true)
		{
			return true;
		}
		try
		{
			MissionScreen missionScreen = ((MissionView)this).MissionScreen;
			return missionScreen != null && ((ScreenBase)(object)missionScreen).Layers?.Any((ScreenLayer l) => l != layer && l.IsFocusLayer) == true;
		}
		catch
		{
			return false;
		}
	}
}

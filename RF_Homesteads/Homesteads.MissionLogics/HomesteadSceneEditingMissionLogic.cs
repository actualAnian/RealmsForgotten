using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using Homesteads.Views;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace Homesteads.MissionLogics;

public class HomesteadSceneEditingMissionLogic : MissionLogic
{
	private const float MaxPlacementDistance = 60f;

	private const float TargetSelectionRadius = 4f;

	private List<HomesteadScenePlaceable> allPlaceables = new List<HomesteadScenePlaceable>();

	private Homestead homestead;

	private HomesteadScene homesteadScene;

	private int editModeType;

	private GameEntity? gameEntityLookingAt;

	private Vec3 positionLookingAt;

	private int currentPlaceableIndex;

	private GameEntity? dummyEntity;

	private Vec3 buildingModeSavedUpDown = Vec3.Zero;

	private Mat3 buildingModeSavedRotation = Mat3.Identity;

	private HomesteadScenePlaceable? currentPlaceableOverride;

	private string currentCategoryString = "Misc";

	private List<HomesteadTemplate> availableTemplates = new List<HomesteadTemplate>();

	private int currentTemplateIndex;

	private List<GameEntity> templatePreviewEntities = new List<GameEntity>();

	private float templateRotationY;

	private float templateOffsetZ;

	private bool _heightLockEnabled;

	private float _lockedHeightZ;

	private bool isPlanningMode;

	private Dictionary<GameEntity, HomesteadSceneSavedEntity> planningLoadedEntities = new Dictionary<GameEntity, HomesteadSceneSavedEntity>();

	private bool _planningCameraActivated;

	private bool _anchorLoadOffered;

	private bool _navMarkerVisibilityDirty = true;

	private int _navMarkerVisibilityRetryFrames;

	private float _buildMenuPromptTimer;

	private const int NavMarkerVisibilityRetryFrameCount = 20;

	private List<HomesteadScenePlaceable> _allTiersCache;

	private List<HomesteadScenePlaceable> validPlaceablesInCurrentCategory => GetValidPlaceablesForCategory(currentCategoryString);

	private HomesteadScenePlaceable currentPlaceable
	{
		get
		{
			if (currentPlaceableOverride != null)
			{
				return currentPlaceableOverride;
			}
			return validPlaceablesInCurrentCategory[currentPlaceableIndex];
		}
	}

	private HomesteadTemplate CurrentTemplate
	{
		get
		{
			if (availableTemplates.Count <= 0)
			{
				return null;
			}
			return availableTemplates[currentTemplateIndex];
		}
	}

	public int MaxBuildableTierForPicker
	{
		get
		{
			if (!isPlanningMode)
			{
				return homestead.Tier;
			}
			return int.MaxValue;
		}
	}

	private List<HomesteadScenePlaceable> AllPlaceablesAllTiers => _allTiersCache ?? (_allTiersCache = HomesteadScenePlaceable.GetAllPlaceablesForPicker());

	public string CurrentCategoryStringForPicker => currentCategoryString;

	public void NotifyEntitiesLoaded()
	{
		_navMarkerVisibilityDirty = true;
	}

	public HomesteadSceneEditingMissionLogic(Homestead homestead, bool isPlanningMode = false)
	{
		this.homestead = homestead;
		this.isPlanningMode = isPlanningMode;
		homesteadScene = homestead.GetHomesteadScene();
		allPlaceables = (isPlanningMode ? HomesteadScenePlaceable.GetAllPlaceables() : HomesteadScenePlaceable.GetTierGroup(homestead.Tier));
	}

	public override void AfterStart()
	{
		if (isPlanningMode)
		{
			editModeType = 1;
			HomesteadMissionView.SetPlaceableBoxVisibility(visible: true);
			HomesteadMissionView.SetStatVisibility(visible: true);
			HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
			HomesteadMissionView.TriggerPlanningSceneChanges(planningLoadedEntities.Values);
			Utils.PrintLocalizedMessage("homestead_planning_mode_start", "Planning Mode: all buildings unlocked, no costs. Press {PLACE_KEY} to place, {LEFT_KEY}/{RIGHT_KEY} to cycle, {CATEGORY_KEY} to switch categories. Exit when done — you'll be asked to save as a template.", 200f, 200f, 255f, ("PLACE_KEY", HomesteadsReloaded.Settings?.GetPlaceKeyLabel() ?? "Q"), ("LEFT_KEY", HomesteadsReloaded.Settings?.GetCycleLeftKeyLabel() ?? "OpenBraces"), ("RIGHT_KEY", HomesteadsReloaded.Settings?.GetCycleRightKeyLabel() ?? "CloseBraces"), ("CATEGORY_KEY", HomesteadsReloaded.Settings?.GetSwitchBuilderModeCategoryKeyLabel() ?? "Apostrophe"));
		}
		else
		{
			Utils.PrintLocalizedMessage("homestead_mission_start_reminder", "Press {EDIT_MODE_KEY} to cycle through edit modes.", 0f, 201f, 0f, ("EDIT_MODE_KEY", HomesteadsReloaded.Settings?.GetEditModeKeyLabel() ?? "P"));
			HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
		}
	}

	public override void OnEndMissionInternal()
	{
		if (!isPlanningMode)
		{
			return;
		}
		List<HomesteadSceneSavedEntity> placedEntities = planningLoadedEntities.Values.ToList();
		if (placedEntities.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage("Planning mode exited — no buildings placed, no template created.", Color.FromUint(4294945360u)));
			return;
		}
		string homesteadName = homestead.Name?.ToString() ?? "Homestead";
		Utils.ShowTextInputMessage("Save Planning Template", $"You placed {placedEntities.Count} building(s). Enter a name to save as a template, or leave blank to discard:", delegate(string templateName)
		{
			if (!string.IsNullOrWhiteSpace(templateName))
			{
				string targetMap = ((Mission.Current != null) ? Mission.Current.SceneName : "");
				HomesteadTemplate homesteadTemplate = HomesteadTemplate.CreateFromEntities(templateName.Trim(), homesteadName, targetMap, placedEntities);
				if (homesteadTemplate != null)
				{
					HomesteadTemplateManager.SaveTemplate(homesteadTemplate);
					InformationManager.DisplayMessage(new InformationMessage("Template '" + templateName.Trim() + "' saved successfully!", Color.FromUint(4283498320u)));
				}
			}
			else
			{
				InformationManager.DisplayMessage(new InformationMessage("Planning session discarded — no template saved.", Color.FromUint(4294945360u)));
			}
		});
	}

	public override void OnMissionTick(float dt)
	{
		if (Agent.Main == null)
		{
			return;
		}
		if (isPlanningMode && !_planningCameraActivated && HomesteadFreeCameraView.Instance != null && HomesteadMissionView.Instance != null && HomesteadMissionView.Instance.dataSource != null)
		{
			HomesteadFreeCameraView.Instance.SetActive(active: true);
			HomesteadMissionView.SetPlaceableBoxVisibility(visible: true);
			HomesteadMissionView.SetStatVisibility(visible: true);
			HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
			HomesteadMissionView.TriggerPlanningSceneChanges(planningLoadedEntities.Values);
			if (HomesteadMissionView.Instance?.dataSource != null)
			{
				HomesteadMissionView.Instance.dataSource.IsBuildMenuPromptVisible = true;
				_buildMenuPromptTimer = 5f;
			}
			_planningCameraActivated = true;
		}
		if (_buildMenuPromptTimer > 0f)
		{
			_buildMenuPromptTimer -= dt;
			if (_buildMenuPromptTimer <= 0f && HomesteadMissionView.Instance?.dataSource != null)
			{
				HomesteadMissionView.Instance.dataSource.IsBuildMenuPromptVisible = false;
			}
		}
		if (isPlanningMode && _planningCameraActivated && !_anchorLoadOffered)
		{
			_anchorLoadOffered = true;
			TryOfferAnchorLoad();
		}
		if (editModeType == 1 && !HomesteadBuildingPickerView.IsOpen && OpenBuildMenuPressed())
		{
			if (HomesteadMissionView.Instance?.dataSource != null)
			{
				HomesteadMissionView.Instance.dataSource.IsBuildMenuPromptVisible = false;
			}
			HomesteadBuildingPickerView.Instance?.Open(this);
		}
		else if (!IsBlockingUiActive())
		{
			if (_navMarkerVisibilityDirty)
			{
				_navMarkerVisibilityDirty = false;
				_navMarkerVisibilityRetryFrames = 20;
			}
			if (_navMarkerVisibilityRetryFrames > 0)
			{
				ApplyNavMarkerEditVisibility();
				_navMarkerVisibilityRetryFrames--;
			}
			HandleLookingAtOnTick(dt);
			HandleInputOnTick(dt);
			BuildingModeDummyEntityTick(dt);
			HomesteadMissionView.SetHeightLockState(_heightLockEnabled, _lockedHeightZ);
		}
	}

	private void HandleLookingAtOnTick(float dt)
	{
		if (editModeType == 0)
		{
			return;
		}
		HomesteadFreeCameraView? instance = HomesteadFreeCameraView.Instance;
		if (instance != null && instance.IsFreezingRay)
		{
			return;
		}
		Vec3 rayBegin = Vec3.Invalid;
		Vec3 rayEnd = Vec3.Invalid;
		HomesteadFreeCameraView? instance2 = HomesteadFreeCameraView.Instance;
		if (instance2 == null || !instance2.TryGetMouseRay(out rayBegin, out rayEnd))
		{
			rayBegin = Agent.Main.GetEyeGlobalPosition();
			rayEnd = rayBegin + Agent.Main.LookDirection * 60f;
		}
		Vec3 vec = (rayEnd - rayBegin).NormalizedCopy();
		float collisionDistance = 0f;
		Mission.Current.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance, out positionLookingAt, out var collidedEntity);
		gameEntityLookingAt = (collidedEntity.IsValid ? GameEntity.CreateFromWeakEntity(collidedEntity) : null);
		if (collisionDistance > (rayEnd - rayBegin).Length)
		{
			positionLookingAt = Vec3.Invalid;
			gameEntityLookingAt = null;
		}
		if (positionLookingAt.IsValid && (editModeType == 1 || editModeType == 3 || editModeType == 4))
		{
			if (_heightLockEnabled)
			{
				float z = vec.Z;
				if (Math.Abs(z) > 0.001f)
				{
					float num = (_lockedHeightZ - rayBegin.Z) / z;
					float length = (rayEnd - rayBegin).Length;
					if (num > 0f && num <= length)
					{
						Vec3 vec2 = rayBegin + vec * num;
						positionLookingAt = new Vec3(vec2.X, vec2.Y, _lockedHeightZ);
					}
				}
			}
			else
			{
				float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(positionLookingAt);
				positionLookingAt = new Vec3(positionLookingAt.X, positionLookingAt.Y, groundHeightAtPosition);
			}
		}
		if (editModeType == 2 || (editModeType == 3 && currentPlaceableOverride == null))
		{
			gameEntityLookingAt = GetBestTargetedPlaceableEntity(rayBegin, vec, gameEntityLookingAt);
		}
		UpdateTargetHud();
	}

	private static bool Ctl(InputKey key)
	{
		if (key != InputKey.Invalid && Input.IsGamepadActive)
		{
			return Input.IsKeyPressed(key);
		}
		return false;
	}

	private static bool CtlDown(InputKey key)
	{
		if (key != InputKey.Invalid && Input.IsGamepadActive)
		{
			return Input.IsKeyDown(key);
		}
		return false;
	}

	private static bool YHeld()
	{
		return CtlDown(HomesteadsReloaded.Settings?.GetCtrlModifierKey() ?? InputKey.ControllerRUp);
	}

	private static bool CtlNoY(InputKey key)
	{
		if (!YHeld())
		{
			return Ctl(key);
		}
		return false;
	}

	private void HandleInputOnTick(float dt)
	{
		if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetEditModeKey()) || CtlNoY(HomesteadsReloaded.Settings.GetCtrlEditModeKey()))
		{
			SwitchEditMode();
		}
		else if (base.Mission.Mode != MissionMode.Conversation && Input.IsKeyPressed(HomesteadsReloaded.Settings.GetSetPlayerSpawnKey()))
		{
			Vec3 position;
			Mat3 playerSpawnRotation;
			if (editModeType == 0)
			{
				position = Agent.Main.Position;
				playerSpawnRotation = Agent.Main.Frame.rotation;
			}
			else
			{
				position = ((dummyEntity != null) ? dummyEntity.GlobalPosition : (positionLookingAt.IsValid ? positionLookingAt : Agent.Main.Position));
				HomesteadFreeCameraView? instance = HomesteadFreeCameraView.Instance;
				playerSpawnRotation = ((instance != null && instance.IsActive) ? HomesteadFreeCameraView.Instance.CameraHorizontalRotation : Agent.Main.Frame.rotation);
			}
			Vec3 playerSpawnPosition = new Vec3(position.X, position.Y, base.Mission.Scene.GetGroundHeightAtPosition(position));
			homesteadScene.PlayerSpawnRotation = playerSpawnRotation;
			homesteadScene.PlayerSpawnPosition = playerSpawnPosition;
			Utils.PrintLocalizedMessage("homestead_new_player_spawn_set", "New player spawn position set!", 0f, 201f, 0f);
		}
		else
		{
			if (editModeType == 0)
			{
				return;
			}
			float deltaMouseScroll = Input.DeltaMouseScroll;
			if (deltaMouseScroll != 0f)
			{
				float num = deltaMouseScroll * 0.003f;
				if (editModeType == 4)
				{
					if (_heightLockEnabled)
					{
						_lockedHeightZ += num;
					}
					else
					{
						templateOffsetZ += num;
					}
					UpdateTemplatePreview();
				}
				else if (dummyEntity != null)
				{
					if (_heightLockEnabled)
					{
						_lockedHeightZ += num;
					}
					else
					{
						buildingModeSavedUpDown += Vec3.Up * num;
					}
				}
			}
			if (Input.IsKeyDown(InputKey.RightMouseButton))
			{
				float num2 = HomesteadFreeCameraView.Instance?.SceneMouseMoveX ?? 0f;
				float num3 = HomesteadFreeCameraView.Instance?.SceneMouseMoveY ?? 0f;
				if (num2 != 0f || num3 != 0f)
				{
					if (editModeType == 4)
					{
						if (num2 != 0f)
						{
							templateRotationY += num2 * 0.005f * (180f / TaleWorlds.Library.MathF.PI);
							UpdateTemplatePreview();
						}
					}
					else if (dummyEntity != null && (editModeType == 1 || editModeType == 3))
					{
						if (num2 != 0f)
						{
							Vec3 v = HomesteadFreeCameraView.Instance?.CameraForwardHorizontal ?? new Vec3(0f, 1f);
							buildingModeSavedRotation.RotateAboutAnArbitraryVector(in v, num2 * 0.005f);
						}
						if (num3 != 0f)
						{
							Vec3 v2 = HomesteadFreeCameraView.Instance?.CameraRightHorizontal ?? new Vec3(1f);
							buildingModeSavedRotation.RotateAboutAnArbitraryVector(in v2, num3 * 0.005f);
						}
					}
				}
			}
			InputKey inputKey = HomesteadsReloaded.Settings?.GetPlaceKey() ?? InputKey.F;
			bool flag = false;
			flag = ((HomesteadFreeCameraView.Instance == null) ? Input.IsKeyPressed(inputKey) : (HomesteadFreeCameraView.Instance.LmbClickedThisFrame || (inputKey != InputKey.LeftMouseButton && Input.IsKeyPressed(inputKey))));
			if (flag || Ctl(HomesteadsReloaded.Settings?.GetCtrlPlaceKey() ?? InputKey.ControllerRDown))
			{
				if (editModeType == 1 && dummyEntity != null)
				{
					if (isPlanningMode)
					{
						PlacePlanningEntity(currentPlaceable, dummyEntity.GlobalPosition, buildingModeSavedRotation);
					}
					else
					{
						homesteadScene.AddPlaceableEntityToCurrentScene(currentPlaceable, dummyEntity.GlobalPosition, buildingModeSavedRotation);
					}
					RemoveDummyEntity();
				}
				else if (editModeType == 2 && gameEntityLookingAt != null)
				{
					if (isPlanningMode)
					{
						RemovePlanningEntity(gameEntityLookingAt);
					}
					else
					{
						homesteadScene.RemovePlaceableEntityFromCurrentScene(gameEntityLookingAt);
					}
				}
				else if (editModeType == 3)
				{
					if (currentPlaceableOverride != null && dummyEntity != null)
					{
						if (isPlanningMode)
						{
							PlacePlanningEntity(currentPlaceable, dummyEntity.GlobalPosition, buildingModeSavedRotation);
						}
						else
						{
							homesteadScene.AddPlaceableEntityToCurrentScene(currentPlaceable, dummyEntity.GlobalPosition, buildingModeSavedRotation, skipItemCheck: true);
						}
						currentPlaceableOverride = null;
						RemoveDummyEntity();
						if (IsEditOnlyMarker(currentPlaceable))
						{
							_navMarkerVisibilityDirty = true;
						}
					}
					else
					{
						if (currentPlaceableOverride != null || !(gameEntityLookingAt != null))
						{
							return;
						}
						GameEntity prefabParent;
						HomesteadScenePlaceable entityPlaceable = GetEntityPlaceable(gameEntityLookingAt, out prefabParent);
						if (entityPlaceable != null && !(prefabParent == null))
						{
							Mat3 mat = prefabParent.GetGlobalFrame().rotation;
							Vec3 s = mat.s;
							Vec3 f = mat.f;
							Vec3 u = mat.u;
							float length = s.Length;
							float length2 = f.Length;
							float length3 = u.Length;
							if (length > 0.001f && length2 > 0.001f && length3 > 0.001f)
							{
								mat = new Mat3(s * (1f / length), f * (1f / length2), u * (1f / length3));
							}
							buildingModeSavedRotation = mat;
							float z = prefabParent.GlobalPosition.z;
							if (_heightLockEnabled)
							{
								_lockedHeightZ = z;
							}
							else
							{
								buildingModeSavedUpDown = (positionLookingAt.IsValid ? new Vec3(0f, 0f, z - positionLookingAt.Z) : Vec3.Zero);
							}
							currentPlaceableOverride = entityPlaceable;
							if (isPlanningMode)
							{
								RemovePlanningEntity(prefabParent);
							}
							else
							{
								homesteadScene.RemovePlaceableEntityFromCurrentScene(prefabParent);
							}
						}
					}
				}
				else if (editModeType == 4 && CurrentTemplate != null)
				{
					TryPlaceTemplate();
				}
				return;
			}
			if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetToggleHeightLockKey()) || Ctl(HomesteadsReloaded.Settings.GetCtrlHeightLockSnapKey()))
			{
				ToggleHeightLock();
				if (Ctl(HomesteadsReloaded.Settings.GetCtrlHeightLockSnapKey()))
				{
					SnapPreviewToGround();
				}
				return;
			}
			if (editModeType == 4)
			{
				if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetCycleRightKey()) || CtlNoY(HomesteadsReloaded.Settings.GetCtrlCycleRightKey()))
				{
					CycleTemplate(1);
					return;
				}
				if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetCycleLeftKey()) || CtlNoY(HomesteadsReloaded.Settings.GetCtrlCycleLeftKey()))
				{
					CycleTemplate(-1);
					return;
				}
				if (Input.IsKeyDown(HomesteadsReloaded.Settings.GetRotateTurnLeftKey()))
				{
					templateRotationY -= dt * 90f;
					while (templateRotationY < -180f)
					{
						templateRotationY += 360f;
					}
					UpdateTemplatePreview();
					return;
				}
				if (Input.IsKeyDown(HomesteadsReloaded.Settings.GetRotateTurnRightKey()))
				{
					templateRotationY += dt * 90f;
					while (templateRotationY >= 180f)
					{
						templateRotationY -= 360f;
					}
					UpdateTemplatePreview();
					return;
				}
				if (YHeld() && CtlDown(HomesteadsReloaded.Settings.GetCtrlCycleLeftKey()))
				{
					templateRotationY -= dt * 90f;
					while (templateRotationY < -180f)
					{
						templateRotationY += 360f;
					}
					UpdateTemplatePreview();
					return;
				}
				if (YHeld() && CtlDown(HomesteadsReloaded.Settings.GetCtrlCycleRightKey()))
				{
					templateRotationY += dt * 90f;
					while (templateRotationY >= 180f)
					{
						templateRotationY -= 360f;
					}
					UpdateTemplatePreview();
					return;
				}
				if (IsMoveUpKeyDown())
				{
					if (_heightLockEnabled)
					{
						_lockedHeightZ += dt * 2f;
					}
					else
					{
						templateOffsetZ += dt * 2f;
					}
					UpdateTemplatePreview();
					return;
				}
				if (IsMoveDownKeyDown())
				{
					if (_heightLockEnabled)
					{
						_lockedHeightZ -= dt * 2f;
					}
					else
					{
						templateOffsetZ -= dt * 2f;
					}
					UpdateTemplatePreview();
					return;
				}
				if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetSnapToGroundKey()))
				{
					SnapPreviewToGround();
					return;
				}
			}
			if (dummyEntity == null)
			{
				return;
			}
			if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetSnapToGroundKey()))
			{
				SnapPreviewToGround();
				return;
			}
			if (IsResetRotationKeyPressed())
			{
				ResetDummyRotation();
				return;
			}
			if (Input.IsKeyDown(HomesteadsReloaded.Settings.GetRotateTurnLeftKey()))
			{
				RotateDummyEntity(dt, "z");
				return;
			}
			if (Input.IsKeyDown(HomesteadsReloaded.Settings.GetRotateTurnRightKey()))
			{
				RotateDummyEntity(dt, "z", isAdding: false);
				return;
			}
			if (YHeld())
			{
				if (CtlDown(HomesteadsReloaded.Settings.GetCtrlCycleLeftKey()))
				{
					RotateDummyEntity(dt, "z");
					return;
				}
				if (CtlDown(HomesteadsReloaded.Settings.GetCtrlCycleRightKey()))
				{
					RotateDummyEntity(dt, "z", isAdding: false);
					return;
				}
				if (CtlDown(HomesteadsReloaded.Settings.GetCtrlEditModeKey()))
				{
					RotateDummyEntity(dt, "x");
					return;
				}
				if (CtlDown(HomesteadsReloaded.Settings.GetCtrlCategoryKey()))
				{
					RotateDummyEntity(dt, "x", isAdding: false);
					return;
				}
			}
			if (IsMoveUpKeyDown())
			{
				if (_heightLockEnabled)
				{
					_lockedHeightZ += dt;
				}
				else
				{
					buildingModeSavedUpDown += Vec3.Up * dt;
				}
			}
			else if (IsMoveDownKeyDown())
			{
				if (_heightLockEnabled)
				{
					_lockedHeightZ -= dt;
				}
				else
				{
					buildingModeSavedUpDown -= Vec3.Up * dt;
				}
			}
			else if (editModeType == 1)
			{
				if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetSwitchBuilderModeCategoryKey()))
				{
					SwitchBuilderMenuCategory();
				}
				else if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetCycleRightKey()) || CtlNoY(HomesteadsReloaded.Settings.GetCtrlCycleRightKey()))
				{
					ChangeCurrentPlaceableIndex(1);
				}
				else if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetCycleLeftKey()) || CtlNoY(HomesteadsReloaded.Settings.GetCtrlCycleLeftKey()))
				{
					ChangeCurrentPlaceableIndex(-1);
				}
			}
		}
	}

	private void SwitchEditMode()
	{
		if (currentPlaceableOverride != null)
		{
			Utils.PrintLocalizedMessage("homestead_edit_mode_switch_when_building_picked_up", "You can't switch your edit mode until you place the currently picked up placeable.", 255f, 80f, 80f);
			return;
		}
		if (editModeType == 4)
		{
			CleanupTemplatePreview();
		}
		editModeType++;
		if (editModeType > 4)
		{
			editModeType = 0;
		}
		_navMarkerVisibilityDirty = true;
		HomesteadFreeCameraView.Instance?.SetActive(editModeType != 0);
		if (HomesteadMissionView.Instance?.dataSource != null)
		{
			HomesteadMissionView.Instance.dataSource.IsBuildMenuPromptVisible = editModeType == 1;
			if (editModeType == 1)
			{
				_buildMenuPromptTimer = 5f;
			}
		}
		string str = "";
		switch (editModeType)
		{
		case 0:
			str = Utils.GetLocalizedString("{=homestead_cancelled_edit_mode}You are no longer making any changes.");
			HomesteadMissionView.SetTargetBoxVisibility(visible: false);
			HomesteadMissionView.SetPlaceableBoxVisibility(visible: false);
			HomesteadMissionView.SetStatVisibility(visible: false);
			break;
		case 1:
			str = Utils.GetLocalizedString("{=homestead_entered_building_mode_dynamic}Building mode. {PLACE_KEY}: place. {LEFT_KEY}/{RIGHT_KEY}: cycle prefabs. {CATEGORY_KEY}: switch category. Scroll: object up/down. Hold RMB + mouse: rotate object. {ROTATE_TURN_LEFT_KEY}/{ROTATE_TURN_RIGHT_KEY}: rotate. {RESET_ROTATION_KEY}: reset orientation. {SNAP_KEY}: snap to ground. {HEIGHT_LOCK_KEY}: toggle height lock. WASD: pan camera flat. Hold Shift + WASD: fly in camera direction. Hold Shift + mouse: rotate camera. Space/Alt: camera up/down.", ("PLACE_KEY", HomesteadsReloaded.Settings?.GetPlaceKeyLabel() ?? "LMB"), ("LEFT_KEY", HomesteadsReloaded.Settings?.GetCycleLeftKeyLabel() ?? "OpenBraces"), ("RIGHT_KEY", HomesteadsReloaded.Settings?.GetCycleRightKeyLabel() ?? "CloseBraces"), ("CATEGORY_KEY", HomesteadsReloaded.Settings?.GetSwitchBuilderModeCategoryKeyLabel() ?? "Apostrophe"), ("ROTATE_TURN_LEFT_KEY", HomesteadsReloaded.Settings?.GetRotateTurnLeftKeyLabel() ?? "Q"), ("ROTATE_TURN_RIGHT_KEY", HomesteadsReloaded.Settings?.GetRotateTurnRightKeyLabel() ?? "E"), ("RESET_ROTATION_KEY", HomesteadsReloaded.Settings?.GetResetRotationKeyLabel() ?? "Ctrl"), ("SNAP_KEY", HomesteadsReloaded.Settings?.GetSnapToGroundKeyLabel() ?? "G"), ("HEIGHT_LOCK_KEY", HomesteadsReloaded.Settings?.GetToggleHeightLockKeyLabel() ?? "H"));
			HomesteadMissionView.SetPlaceableBoxVisibility(visible: true);
			HomesteadMissionView.SetStatVisibility(visible: true);
			HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
			break;
		case 2:
			str = Utils.GetLocalizedString("{=homestead_entered_delete_mode}You are now in delete mode. Press Q, by default, to delete the currently looked at entity.");
			HomesteadMissionView.SetPlaceableBoxVisibility(visible: false);
			HomesteadMissionView.SetTargetBoxVisibility(visible: true);
			break;
		case 3:
			str = Utils.GetLocalizedString("{=homestead_entered_edit_mode}You are now in edit mode. Press Q, by default, to pick up the currently looked at entity.");
			HomesteadMissionView.SetStatVisibility(visible: false);
			HomesteadMissionView.SetTargetBoxVisibility(visible: true);
			break;
		case 4:
			availableTemplates = HomesteadTemplateManager.GetAllTemplates();
			currentTemplateIndex = 0;
			if (availableTemplates.Count == 0)
			{
				str = Utils.GetLocalizedString("{=homestead_entered_template_mode_empty}You are now in template mode, but no templates are available. Save a homestead layout first.");
				editModeType = 0;
				HomesteadFreeCameraView.Instance?.SetActive(active: false);
				HomesteadMissionView.SetTargetBoxVisibility(visible: false);
				HomesteadMissionView.SetPlaceableBoxVisibility(visible: false);
				HomesteadMissionView.SetStatVisibility(visible: false);
			}
			else
			{
				str = Utils.GetLocalizedString("{=homestead_entered_template_mode_dynamic}Template mode. {PLACE_KEY}: place. {LEFT_KEY}/{RIGHT_KEY}: cycle. {ROTATE_TURN_LEFT_KEY}-{ROTATE_TURN_RIGHT_KEY}: rotate. Scroll wheel: adjust height. Hold RMB + mouse: rotate.", ("PLACE_KEY", HomesteadsReloaded.Settings?.GetPlaceKeyLabel() ?? "Q"), ("LEFT_KEY", HomesteadsReloaded.Settings?.GetCycleLeftKeyLabel() ?? "OpenBraces"), ("RIGHT_KEY", HomesteadsReloaded.Settings?.GetCycleRightKeyLabel() ?? "CloseBraces"), ("ROTATE_TURN_LEFT_KEY", HomesteadsReloaded.Settings?.GetRotateTurnLeftKeyLabel() ?? "D5"), ("ROTATE_TURN_RIGHT_KEY", HomesteadsReloaded.Settings?.GetRotateTurnRightKeyLabel() ?? "D6"));
				HomesteadMissionView.SetPlaceableBoxVisibility(visible: true);
				HomesteadMissionView.SetStatVisibility(visible: true);
				UpdateTemplatePreview();
			}
			break;
		}
		Utils.PrintDebugMessage(str, 201f, 0f, 0f);
	}

	private GameEntity? GetBestTargetedPlaceableEntity(Vec3 eyeGlobalPos, Vec3 lookDirection, GameEntity? raycastEntity)
	{
		if (raycastEntity != null && GetEntityPlaceable(raycastEntity, out GameEntity prefabParent) != null)
		{
			return prefabParent ?? raycastEntity;
		}
		GameEntity result = null;
		float num = float.MaxValue;
		float num2 = 16f;
		IEnumerable<GameEntity> source;
		if (!isPlanningMode)
		{
			source = homesteadScene.LoadedSavedEntities.Select<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>, GameEntity>((KeyValuePair<GameEntity, HomesteadSceneSavedEntity> kv) => kv.Key);
		}
		else
		{
			IEnumerable<GameEntity> keys = planningLoadedEntities.Keys;
			source = keys;
		}
		foreach (GameEntity item in source.ToList())
		{
			float num3 = Vec3.DotProduct(item.GlobalPosition - eyeGlobalPos, lookDirection);
			if (num3 < 0f || num3 > 60f)
			{
				continue;
			}
			Vec3 vec = eyeGlobalPos + lookDirection * num3;
			float lengthSquared = (item.GlobalPosition - vec).LengthSquared;
			if (!(lengthSquared > num2))
			{
				float num4 = lengthSquared + num3 * 0.01f;
				if (!(num4 >= num))
				{
					num = num4;
					result = item;
				}
			}
		}
		return result;
	}

	private void UpdateTargetHud()
	{
		if (editModeType != 2 && (editModeType != 3 || currentPlaceableOverride != null))
		{
			HomesteadMissionView.SetTargetBoxVisibility(visible: false);
			return;
		}
		GameEntity prefabParent;
		HomesteadScenePlaceable homesteadScenePlaceable = ((gameEntityLookingAt == null) ? null : GetEntityPlaceable(gameEntityLookingAt, out prefabParent));
		if (homesteadScenePlaceable == null)
		{
			HomesteadMissionView.SetTargetInfo(Utils.GetLocalizedString("{=homestead_target_none}No object selected"), Utils.GetLocalizedString("{=homestead_target_hint}Look at a built object and press the place key."));
			return;
		}
		string targetHint = ((editModeType == 2) ? Utils.GetLocalizedString("{=homestead_target_delete_hint}Press {PLACE_KEY} to delete this object.", ("PLACE_KEY", HomesteadsReloaded.Settings?.GetPlaceKeyLabel() ?? "Q")) : Utils.GetLocalizedString("{=homestead_target_edit_hint}Press {PLACE_KEY} to pick up this object.", ("PLACE_KEY", HomesteadsReloaded.Settings?.GetPlaceKeyLabel() ?? "Q")));
		HomesteadMissionView.SetTargetInfo(homesteadScenePlaceable.DisplayName, targetHint);
	}

	private void SwitchBuilderMenuCategory()
	{
		currentPlaceableIndex = 0;
		BuilderMenuCategory num = (BuilderMenuCategory)Enum.Parse(typeof(BuilderMenuCategory), currentCategoryString);
		int num2 = Enum.GetNames(typeof(BuilderMenuCategory)).Length;
		int num3 = (int)num;
		for (int i = 0; i < num2; i++)
		{
			num3++;
			if (num3 >= num2)
			{
				num3 = 0;
			}
			BuilderMenuCategory builderMenuCategory = (BuilderMenuCategory)num3;
			string categoryName = builderMenuCategory.ToString();
			if (GetValidPlaceablesForCategory(categoryName).Count != 0)
			{
				currentCategoryString = categoryName;
				_navMarkerVisibilityDirty = true;
				break;
			}
		}
		Utils.PrintLocalizedMessage("homestead_switched_building_mode_category", "BUILD CATEGORY SWITCHED TO: {NEW_CATEGORY_NAME}", 0f, 201f, 0f, ("NEW_CATEGORY_NAME", GetCategoryDisplayName(currentCategoryString)));
		HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
		RemoveDummyEntity();
	}

	private List<HomesteadScenePlaceable> GetValidPlaceablesForCategory(string categoryName)
	{
		return allPlaceables.Where((HomesteadScenePlaceable x) => x.BuilderMenuCategoryString == categoryName).ToList();
	}

	private static string GetCategoryDisplayName(string internalCategory)
	{
		if (!(internalCategory == "Light"))
		{
			return internalCategory;
		}
		return Utils.GetLocalizedString("{=homestead_category_lighting_insignia}Lighting and Insignia");
	}

	public List<(string CategoryKey, string DisplayName)> GetNonEmptyCategoriesForPicker()
	{
		List<(string, string)> list = new List<(string, string)>();
		foreach (BuilderMenuCategory value in Enum.GetValues(typeof(BuilderMenuCategory)))
		{
			string key = value.ToString();
			if (AllPlaceablesAllTiers.Any((HomesteadScenePlaceable p) => p.BuilderMenuCategoryString == key))
			{
				list.Add((key, GetCategoryDisplayName(key)));
			}
		}
		return list;
	}

	public List<HomesteadScenePlaceable> GetAllPlaceablesForPickerCategory(string categoryKey)
	{
		return (from p in AllPlaceablesAllTiers
			where p.BuilderMenuCategoryString == categoryKey
			orderby p.TierRequired, p.DisplayName
			select p).ToList();
	}

	public bool IsPlaceableAffordable(HomesteadScenePlaceable placeable)
	{
		if (isPlanningMode || placeable == null)
		{
			return true;
		}
		if (placeable.BuildPointsRequired <= homestead.GetHomesteadScene().BuildPointsLeftToUse)
		{
			return Utils.DoesItemRosterHaveItems(homestead.Stash, placeable.ItemRequirements);
		}
		return false;
	}

	public string GetUnaffordableReason(HomesteadScenePlaceable placeable)
	{
		if (isPlanningMode || placeable == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		int buildPointsLeftToUse = homestead.GetHomesteadScene().BuildPointsLeftToUse;
		if (placeable.BuildPointsRequired > buildPointsLeftToUse)
		{
			list.Add($"{placeable.BuildPointsRequired - buildPointsLeftToUse} more build points");
		}
		if (placeable.ItemRequirements != null && !Utils.DoesItemRosterHaveItems(homestead.Stash, placeable.ItemRequirements))
		{
			foreach (KeyValuePair<string, int> itemRequirement in placeable.ItemRequirements)
			{
				ItemObject itemFromID = Utils.GetItemFromID(itemRequirement.Key);
				if (itemFromID != null)
				{
					int num = homestead.Stash?.GetItemNumber(itemFromID) ?? 0;
					if (num < itemRequirement.Value)
					{
						list.Add($"{itemFromID.Name} x{itemRequirement.Value - num}");
					}
				}
			}
		}
		if (list.Count != 0)
		{
			return string.Join(", ", list);
		}
		return "";
	}

	public void SetActivePlaceableByIdentity(string categoryKey, string prefabName, string displayName, string npcAction)
	{
		currentCategoryString = categoryKey;
		List<HomesteadScenePlaceable> list = validPlaceablesInCurrentCategory;
		string na = npcAction ?? "";
		int num = list.FindIndex((HomesteadScenePlaceable p) => p.PrefabName == prefabName && p.DisplayName == displayName && (p.NpcAction ?? "") == na);
		if (num < 0)
		{
			num = list.FindIndex((HomesteadScenePlaceable p) => p.PrefabName == prefabName && p.DisplayName == displayName);
		}
		if (num < 0)
		{
			num = list.FindIndex((HomesteadScenePlaceable p) => p.PrefabName == prefabName);
		}
		if (num >= 0)
		{
			currentPlaceableIndex = num;
			currentPlaceableOverride = null;
			_navMarkerVisibilityDirty = true;
			RemoveDummyEntity();
			HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
		}
	}

	private void RotateDummyEntity(float dt, string typeOfRotation, bool isAdding = true)
	{
		if (!(dummyEntity == null))
		{
			float a = dt * (float)(isAdding ? 1 : (-1));
			switch (typeOfRotation)
			{
			case "x":
				buildingModeSavedRotation.RotateAboutSide(a);
				break;
			case "y":
				buildingModeSavedRotation.RotateAboutForward(a);
				break;
			case "z":
				buildingModeSavedRotation.RotateAboutUp(a);
				break;
			}
		}
	}

	private void SnapPreviewToGround()
	{
		if (!positionLookingAt.IsValid)
		{
			return;
		}
		float groundHeightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(positionLookingAt);
		if (_heightLockEnabled)
		{
			_lockedHeightZ = groundHeightAtPosition;
			if (editModeType == 4)
			{
				UpdateTemplatePreview();
			}
		}
		else if (editModeType == 4)
		{
			templateOffsetZ = groundHeightAtPosition - positionLookingAt.Z;
			UpdateTemplatePreview();
		}
		else if (dummyEntity != null)
		{
			buildingModeSavedUpDown = new Vec3(0f, 0f, groundHeightAtPosition - positionLookingAt.Z);
		}
	}

	private void ToggleHeightLock()
	{
		_heightLockEnabled = !_heightLockEnabled;
		if (_heightLockEnabled)
		{
			_lockedHeightZ = ((!positionLookingAt.IsValid) ? 0f : ((editModeType == 4) ? (positionLookingAt.Z + templateOffsetZ) : (positionLookingAt.Z + buildingModeSavedUpDown.Z)));
			Utils.PrintLocalizedMessage("homestead_height_lock_on", "Height locked at {Z}m. Scroll or use Move Up/Down to adjust. Press {KEY} to follow terrain again.", 200f, 220f, 100f, ("Z", _lockedHeightZ.ToString("F1")), ("KEY", HomesteadsReloaded.Settings?.GetToggleHeightLockKeyLabel() ?? "H"));
			return;
		}
		if (positionLookingAt.IsValid)
		{
			if (editModeType == 4)
			{
				templateOffsetZ = _lockedHeightZ - positionLookingAt.Z;
			}
			else
			{
				buildingModeSavedUpDown = new Vec3(0f, 0f, _lockedHeightZ - positionLookingAt.Z);
			}
		}
		Utils.PrintLocalizedMessage("homestead_height_lock_off", "Height lock off — preview follows terrain.", 200f, 220f, 100f);
	}

	private void ResetDummyRotation()
	{
		buildingModeSavedRotation = Mat3.Identity;
		buildingModeSavedUpDown = Vec3.Zero;
		if (!(dummyEntity == null))
		{
			Vec3 origin = (_heightLockEnabled ? new Vec3(positionLookingAt.X, positionLookingAt.Y, _lockedHeightZ) : (positionLookingAt + buildingModeSavedUpDown));
			MatrixFrame frame = MatrixFrame.Identity;
			frame.rotation = Utils.ApplyPrefabPlacementScale(currentPlaceable.PrefabName, Mat3.Identity);
			frame.origin = origin;
			dummyEntity.SetGlobalFrame(in frame);
		}
	}

	private static bool IsResetRotationKeyPressed()
	{
		if (!Input.IsKeyPressed(HomesteadsReloaded.Settings?.GetResetRotationKey() ?? InputKey.D7))
		{
			return Ctl(HomesteadsReloaded.Settings?.GetCtrlResetRotationKey() ?? InputKey.ControllerRThumb);
		}
		return true;
	}

	private static bool IsMoveUpKeyDown()
	{
		InputKey inputKey = HomesteadsReloaded.Settings?.GetMoveUpKey() ?? InputKey.I;
		if (inputKey == InputKey.D7 || !Input.IsKeyDown(inputKey))
		{
			return CtlDown(HomesteadsReloaded.Settings?.GetCtrlMoveUpKey() ?? InputKey.ControllerRTrigger);
		}
		return true;
	}

	private static bool IsMoveDownKeyDown()
	{
		if (!Input.IsKeyDown(HomesteadsReloaded.Settings.GetMoveDownKey()))
		{
			return CtlDown(HomesteadsReloaded.Settings?.GetCtrlMoveDownKey() ?? InputKey.ControllerLTrigger);
		}
		return true;
	}

	private void BuildingModeDummyEntityTick(float dt)
	{
		if (editModeType == 1 || editModeType == 3 || editModeType == 4)
		{
			if (!positionLookingAt.IsValid)
			{
				RemoveDummyEntity();
			}
			else
			{
				if (editModeType == 3 && currentPlaceableOverride == null)
				{
					return;
				}
				if (editModeType == 4)
				{
					UpdateTemplatePreview();
					return;
				}
				CreateBuildingModeDummyEntity();
				Vec3 origin = (_heightLockEnabled ? new Vec3(positionLookingAt.X, positionLookingAt.Y, _lockedHeightZ) : (positionLookingAt + buildingModeSavedUpDown));
				Vec3 s = buildingModeSavedRotation.s;
				Vec3 f = buildingModeSavedRotation.f;
				Vec3 u = buildingModeSavedRotation.u;
				float length = s.Length;
				float length2 = f.Length;
				float length3 = u.Length;
				if (length > 0.001f && length2 > 0.001f && length3 > 0.001f)
				{
					buildingModeSavedRotation = new Mat3(s * (1f / length), f * (1f / length2), u * (1f / length3));
				}
				MatrixFrame frame = MatrixFrame.Identity;
				frame.rotation = Utils.ApplyPrefabPlacementScale(currentPlaceable.PrefabName, buildingModeSavedRotation);
				frame.origin = origin;
				dummyEntity.SetGlobalFrame(in frame);
			}
		}
		else
		{
			RemoveDummyEntity();
			CleanupTemplatePreview();
		}
	}

	private void ChangeCurrentPlaceableIndex(int change)
	{
		int count = validPlaceablesInCurrentCategory.Count;
		currentPlaceableIndex = ((count > 0) ? (((currentPlaceableIndex + change) % count + count) % count) : 0);
		HomesteadMissionView.SetBuilderCategory(GetCategoryDisplayName(currentCategoryString));
		RemoveDummyEntity();
	}

	private void CreateBuildingModeDummyEntity()
	{
		if (dummyEntity != null)
		{
			return;
		}
		TraceLogger.Write("HomesteadSceneEditingMissionLogic", "Creating build preview dummy for '" + currentPlaceable.PrefabName + "' in category '" + currentCategoryString + "'");
		dummyEntity = Utils.CreateGameEntityWithPrefab(currentPlaceable.PrefabName, positionLookingAt, buildingModeSavedRotation, enablePhysics: false, makeStatic: false, applyPhysicsState: true, callScriptCallbacks: false);
		List<GameEntity> list = dummyEntity.GetEntityAndChildren().ToList();
		foreach (GameEntity item in list)
		{
			item.SetPhysicsState(isEnabled: false, setChildren: true);
		}
		SetBuildingModeDummyEntityColor(list);
		HomesteadMissionView.ChangeCurrentPlaceable(currentPlaceable.DisplayName, currentPlaceable.Description);
	}

	private void SetBuildingModeDummyEntityColor(List<GameEntity>? allEntitiesInDummy = null)
	{
		if (dummyEntity == null)
		{
			return;
		}
		if (allEntitiesInDummy == null)
		{
			allEntitiesInDummy = dummyEntity.GetEntityAndChildren().ToList();
		}
		foreach (GameEntity item in allEntitiesInDummy)
		{
			MetaMesh metaMesh = item.GetMetaMesh(0);
			if (metaMesh == null || !metaMesh.IsValid)
			{
				continue;
			}
			for (int i = 0; i < metaMesh.MeshCount; i++)
			{
				Mesh meshAtIndex = metaMesh.GetMeshAtIndex(i);
				if (!(meshAtIndex == null) && meshAtIndex.IsValid)
				{
					meshAtIndex.SetMaterial((isPlanningMode || currentPlaceableOverride != null) ? "plain_green" : ((currentPlaceable.BuildPointsRequired > homestead.GetHomesteadScene().BuildPointsLeftToUse) ? "plain_red" : (Utils.DoesItemRosterHaveItems(homestead.Stash, currentPlaceable.ItemRequirements) ? "plain_green" : "plain_red")));
				}
			}
		}
	}

	private void RemoveDummyEntity()
	{
		if (!(dummyEntity == null))
		{
			dummyEntity.RemoveAllChildren();
			dummyEntity.Remove(0);
			dummyEntity = null;
		}
	}

	private static bool IsConversationActive()
	{
		return Campaign.Current?.ConversationManager?.IsConversationInProgress == true;
	}

	private bool IsBlockingUiActive()
	{
		if (IsConversationActive())
		{
			return true;
		}
		if (editModeType == 0 && ScreenManager.GetMouseVisibility())
		{
			return true;
		}
		if (HomesteadBuildingPickerView.IsOpen)
		{
			return true;
		}
		return false;
	}

	private bool OpenBuildMenuPressed()
	{
		if (Input.IsKeyPressed(HomesteadsReloaded.Settings.GetOpenBuildMenuKey()))
		{
			return true;
		}
		return CtlNoY(HomesteadsReloaded.Settings.GetCtrlCategoryKey());
	}

	private void CycleTemplate(int direction)
	{
		if (availableTemplates.Count != 0)
		{
			templateRotationY = 0f;
			templateOffsetZ = 0f;
			currentTemplateIndex += direction;
			if (currentTemplateIndex < 0)
			{
				currentTemplateIndex = availableTemplates.Count - 1;
			}
			if (currentTemplateIndex >= availableTemplates.Count)
			{
				currentTemplateIndex = 0;
			}
			UpdateTemplatePreview();
			if (CurrentTemplate != null)
			{
				Utils.PrintLocalizedMessage("homestead_template_selected", "Selected template: {NAME} ({COUNT} buildings)", 200f, 200f, 255f, ("NAME", CurrentTemplate.Name), ("COUNT", CurrentTemplate.Entities.Count.ToString()));
			}
		}
	}

	private void UpdateTemplatePreview()
	{
		CleanupTemplatePreview();
		if (CurrentTemplate == null || editModeType != 4)
		{
			return;
		}
		float num = templateRotationY * (TaleWorlds.Library.MathF.PI / 180f);
		float num2 = (float)Math.Cos(num);
		float num3 = (float)Math.Sin(num);
		float num4 = (_heightLockEnabled ? _lockedHeightZ : (positionLookingAt.Z + templateOffsetZ));
		foreach (TemplateEntity entity in CurrentTemplate.Entities)
		{
			float num5 = entity.RelativePosX * num2 - entity.RelativePosY * num3;
			float num6 = entity.RelativePosX * num3 + entity.RelativePosY * num2;
			Vec3 vec = new Vec3(positionLookingAt.X + num5, positionLookingAt.Y + num6, num4 + entity.RelativePosZ);
			Mat3 mat = new Mat3(new Vec3(entity.RotSx, entity.RotSy, entity.RotSz), new Vec3(entity.RotFx, entity.RotFy, entity.RotFz), new Vec3(entity.RotUx, entity.RotUy, entity.RotUz));
			Mat3 rotation = new Mat3(new Vec3(mat.s.X * num2 - mat.s.Y * num3, mat.s.X * num3 + mat.s.Y * num2, mat.s.Z), new Vec3(mat.f.X * num2 - mat.f.Y * num3, mat.f.X * num3 + mat.f.Y * num2, mat.f.Z), new Vec3(mat.u.X * num2 - mat.u.Y * num3, mat.u.X * num3 + mat.u.Y * num2, mat.u.Z));
			GameEntity gameEntity = Utils.CreateGameEntityWithPrefab(entity.PrefabName, vec, rotation, enablePhysics: false, makeStatic: false, applyPhysicsState: true, callScriptCallbacks: false);
			if (!(gameEntity != null))
			{
				continue;
			}
			foreach (GameEntity item in gameEntity.GetEntityAndChildren().ToList())
			{
				if (item == gameEntity)
				{
					continue;
				}
				try
				{
					MetaMesh metaMesh = item.GetMetaMesh(0);
					if (metaMesh == null || !metaMesh.IsValid)
					{
						item.Remove(0);
					}
				}
				catch
				{
				}
			}
			List<GameEntity> list = gameEntity.GetEntityAndChildren().ToList();
			foreach (GameEntity item2 in list)
			{
				item2.SetPhysicsState(isEnabled: false, setChildren: true);
			}
			foreach (GameEntity item3 in list)
			{
				MetaMesh metaMesh2 = item3.GetMetaMesh(0);
				if (!(metaMesh2 != null) || !metaMesh2.IsValid)
				{
					continue;
				}
				for (int i = 0; i < metaMesh2.MeshCount; i++)
				{
					Mesh meshAtIndex = metaMesh2.GetMeshAtIndex(i);
					if (meshAtIndex != null && meshAtIndex.IsValid)
					{
						meshAtIndex.SetMaterial("plain_green");
					}
				}
			}
			bool flag = false;
			IEnumerable<GameEntity> enumerable;
			if (!isPlanningMode)
			{
				enumerable = (homesteadScene.LoadedSavedEntities ?? Enumerable.Empty<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>>()).Select<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>, GameEntity>((KeyValuePair<GameEntity, HomesteadSceneSavedEntity> kv) => kv.Key);
			}
			else
			{
				IEnumerable<GameEntity> keys = planningLoadedEntities.Keys;
				enumerable = keys;
			}
			foreach (GameEntity item4 in enumerable)
			{
				if (item4 != null && (item4.GlobalPosition - vec).Length < 2f)
				{
					flag = true;
					break;
				}
			}
			string material = (flag ? "plain_red" : "plain_green");
			foreach (GameEntity item5 in list)
			{
				MetaMesh metaMesh3 = item5.GetMetaMesh(0);
				if (!(metaMesh3 != null) || !metaMesh3.IsValid)
				{
					continue;
				}
				for (int num7 = 0; num7 < metaMesh3.MeshCount; num7++)
				{
					Mesh meshAtIndex2 = metaMesh3.GetMeshAtIndex(num7);
					if (meshAtIndex2 != null && meshAtIndex2.IsValid)
					{
						meshAtIndex2.SetMaterial(material);
					}
				}
			}
			templatePreviewEntities.Add(gameEntity);
		}
		if (CurrentTemplate == null)
		{
			return;
		}
		Dictionary<string, int> costSummary = CurrentTemplate.GetCostSummary();
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, int> item6 in costSummary)
		{
			list2.Add(item6.Key + " x" + item6.Value);
		}
		string text = string.Join(", ", list2);
		string description = "Build Points: " + CurrentTemplate.TotalBuildPoints + " | Items: " + text + " | Buildings: " + CurrentTemplate.Entities.Count;
		HomesteadMissionView.ChangeCurrentPlaceable(CurrentTemplate.Name, description);
	}

	private void CleanupTemplatePreview()
	{
		foreach (GameEntity templatePreviewEntity in templatePreviewEntities)
		{
			if (templatePreviewEntity != null)
			{
				templatePreviewEntity.RemoveAllChildren();
				templatePreviewEntity.Remove(0);
			}
		}
		templatePreviewEntities.Clear();
	}

	private void TryOfferAnchorLoad()
	{
		try
		{
			string scene = Mission.Current?.SceneName ?? "";
			if (string.IsNullOrEmpty(scene))
			{
				return;
			}
			List<HomesteadTemplate> list = (from homesteadTemplate in HomesteadTemplateManager.GetAllTemplates()
				where homesteadTemplate != null && homesteadTemplate.HasAnchor && homesteadTemplate.Entities != null && homesteadTemplate.Entities.Count > 0 && string.Equals(homesteadTemplate.TargetMap, scene, StringComparison.OrdinalIgnoreCase)
				select homesteadTemplate).ToList();
			if (list.Count == 0)
			{
				return;
			}
			if (list.Count == 1)
			{
				HomesteadTemplate t = list[0];
				InformationManager.ShowInquiry(new InquiryData("Load Saved Layout", "'" + t.Name + "' was built on this map. Load it at its original location to keep editing?", isAffirmativeOptionShown: true, isNegativeOptionShown: true, "Load it", "Start blank", delegate
				{
					PlaceTemplateAtAnchor(t);
				}, null));
				return;
			}
			List<InquiryElement> inquiryElements = list.Select((HomesteadTemplate homesteadTemplate) => new InquiryElement(homesteadTemplate, homesteadTemplate.Name, null)).ToList();
			MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Load Saved Layout", "These layouts were built on this map. Pick one to load at its original location, or cancel for a blank session.", inquiryElements, isExitShown: true, 0, 1, "Load", "Cancel", delegate(List<InquiryElement> selected)
			{
				if (selected != null && selected.Count > 0 && selected[0].Identifier is HomesteadTemplate template)
				{
					PlaceTemplateAtAnchor(template);
				}
			}, null));
		}
		catch (Exception arg)
		{
			TraceLogger.Write("HomesteadSceneEditingMissionLogic", $"TryOfferAnchorLoad failed: {arg}");
		}
	}

	private void PlaceTemplateAtAnchor(HomesteadTemplate template)
	{
		if (template?.Entities == null)
		{
			return;
		}
		Vec3 vec = new Vec3(template.AnchorX, template.AnchorY, template.AnchorZ);
		int num = 0;
		foreach (TemplateEntity entity in template.Entities)
		{
			Vec3 position = new Vec3(vec.X + entity.RelativePosX, vec.Y + entity.RelativePosY, vec.Z + entity.RelativePosZ);
			Mat3 rotation = new Mat3(new Vec3(entity.RotSx, entity.RotSy, entity.RotSz), new Vec3(entity.RotFx, entity.RotFy, entity.RotFz), new Vec3(entity.RotUx, entity.RotUy, entity.RotUz));
			HomesteadScenePlaceable homesteadScenePlaceable = HomesteadScenePlaceable.FindByPrefabName(entity.PrefabName);
			if (homesteadScenePlaceable != null)
			{
				PlacePlanningEntity(homesteadScenePlaceable, position, rotation);
				num++;
			}
		}
		Utils.PrintLocalizedMessage("homestead_template_loaded_at_anchor", "Loaded '{NAME}' at its original location ({COUNT} pieces). Edit and exit to re-save.", 80f, 255f, 80f, ("NAME", template.Name), ("COUNT", num.ToString()));
		TraceLogger.Write("HomesteadSceneEditingMissionLogic", $"Anchor-loaded template '{template.Name}': {num} entities at ({vec.X:0.#},{vec.Y:0.#},{vec.Z:0.#}).");
	}

	private void TryPlaceTemplate()
	{
		if (CurrentTemplate == null)
		{
			return;
		}
		HomesteadTemplate currentTemplate = CurrentTemplate;
		HomesteadScene homesteadScene = homestead.GetHomesteadScene();
		if (!isPlanningMode)
		{
			if (currentTemplate.TotalBuildPoints > homesteadScene.BuildPointsLeftToUse)
			{
				Utils.PrintLocalizedMessage("homestead_template_not_enough_build_points", "Not enough build points. Need {NEEDED}, have {AVAILABLE}.", 255f, 80f, 80f, ("NEEDED", currentTemplate.TotalBuildPoints.ToString()), ("AVAILABLE", homesteadScene.BuildPointsLeftToUse.ToString()));
				return;
			}
			foreach (KeyValuePair<string, int> item in currentTemplate.GetCostSummary())
			{
				Dictionary<string, int> itemsRequired = new Dictionary<string, int> { [item.Key] = item.Value };
				if (!Utils.DoesItemRosterHaveItems(homestead.Stash, itemsRequired))
				{
					Utils.PrintLocalizedMessage("homestead_template_not_enough_materials", "Not enough {MATERIAL}. Need {NEEDED}.", 255f, 80f, 80f, ("MATERIAL", item.Key), ("NEEDED", item.Value.ToString()));
					return;
				}
			}
		}
		int num = 0;
		float num2 = templateRotationY * (TaleWorlds.Library.MathF.PI / 180f);
		float num3 = (float)Math.Cos(num2);
		float num4 = (float)Math.Sin(num2);
		float num5 = (_heightLockEnabled ? _lockedHeightZ : (positionLookingAt.Z + templateOffsetZ));
		foreach (TemplateEntity entity in CurrentTemplate.Entities)
		{
			float num6 = entity.RelativePosX * num3 - entity.RelativePosY * num4;
			float num7 = entity.RelativePosX * num4 + entity.RelativePosY * num3;
			Vec3 position = new Vec3(positionLookingAt.X + num6, positionLookingAt.Y + num7, num5 + entity.RelativePosZ);
			Mat3 mat = new Mat3(new Vec3(entity.RotSx, entity.RotSy, entity.RotSz), new Vec3(entity.RotFx, entity.RotFy, entity.RotFz), new Vec3(entity.RotUx, entity.RotUy, entity.RotUz));
			Mat3 rotation = new Mat3(new Vec3(mat.s.X * num3 - mat.s.Y * num4, mat.s.X * num4 + mat.s.Y * num3, mat.s.Z), new Vec3(mat.f.X * num3 - mat.f.Y * num4, mat.f.X * num4 + mat.f.Y * num3, mat.f.Z), new Vec3(mat.u.X * num3 - mat.u.Y * num4, mat.u.X * num4 + mat.u.Y * num3, mat.u.Z));
			HomesteadScenePlaceable homesteadScenePlaceable = HomesteadScenePlaceable.FindByPrefabName(entity.PrefabName);
			if (homesteadScenePlaceable != null)
			{
				if (isPlanningMode)
				{
					PlacePlanningEntity(homesteadScenePlaceable, position, rotation);
				}
				else
				{
					homesteadScene.AddPlaceableEntityToCurrentScene(homesteadScenePlaceable, position, rotation);
				}
				num++;
			}
		}
		Utils.PrintLocalizedMessage("homestead_template_placed", "Template '{NAME}' placed successfully! {COUNT} buildings constructed.", 80f, 255f, 80f, ("NAME", currentTemplate.Name), ("COUNT", num.ToString()));
		CleanupTemplatePreview();
	}

	private HomesteadScenePlaceable? GetEntityPlaceable(GameEntity entity, out GameEntity? prefabParent)
	{
		if (!isPlanningMode)
		{
			return homesteadScene.GetHomesteadSceneEntityPlaceable(entity, out prefabParent);
		}
		return GetPlanningEntityPlaceable(entity, out prefabParent);
	}

	private void PlacePlanningEntity(HomesteadScenePlaceable placeable, Vec3 position, Mat3 rotation)
	{
		GameEntity gameEntity;
		try
		{
			gameEntity = Utils.CreateGameEntityWithPrefab(placeable.PrefabName, position, rotation, enablePhysics: true, makeStatic: true, applyPhysicsState: true, callScriptCallbacks: false);
		}
		catch (Exception arg)
		{
			Utils.PrintDebugMessage("FAILED TO PLACE PLANNING PREFAB " + placeable.PrefabName, 255f, 0f, 0f);
			TraceLogger.Write("HomesteadSceneEditingMissionLogic", $"PlacePlanningEntity failed for '{placeable.PrefabName}': {arg}");
			return;
		}
		HomesteadSceneSavedEntity value = new HomesteadSceneSavedEntity(placeable, gameEntity.GlobalPosition, rotation);
		planningLoadedEntities.Add(gameEntity, value);
		HomesteadMissionView.TriggerPlanningSceneChanges(planningLoadedEntities.Values);
	}

	private void RemovePlanningEntity(GameEntity entity)
	{
		if (GetPlanningEntityPlaceable(entity, out GameEntity prefabParent) != null && !(prefabParent == null))
		{
			planningLoadedEntities.Remove(prefabParent);
			HomesteadMissionView.TriggerPlanningSceneChanges(planningLoadedEntities.Values);
			prefabParent.RemoveAllChildren();
			prefabParent.Remove(0);
		}
	}

	private HomesteadScenePlaceable? GetPlanningEntityPlaceable(GameEntity entity, out GameEntity? prefabParent)
	{
		GameEntity gameEntity = entity;
		while (gameEntity != null)
		{
			if (planningLoadedEntities.ContainsKey(gameEntity))
			{
				prefabParent = gameEntity;
				return planningLoadedEntities[gameEntity].Placeable;
			}
			gameEntity = gameEntity.Parent;
		}
		prefabParent = null;
		return null;
	}

	private static bool IsRaceMarkerPrefab(string? prefabName)
	{
		if (!(prefabName == "homestead_race_start"))
		{
			return prefabName == "homestead_race_gate";
		}
		return true;
	}

	private static bool IsNavMarkerPrefab(string? prefabName)
	{
		if (!(prefabName == "homestead_nav_point"))
		{
			return IsRaceMarkerPrefab(prefabName);
		}
		return true;
	}

	private static bool IsEditOnlyMarker(HomesteadScenePlaceable? placeable)
	{
		if (placeable != null)
		{
			if (!IsNavMarkerPrefab(placeable.PrefabName))
			{
				return placeable.IsNpcActionFlag;
			}
			return true;
		}
		return false;
	}

	private void ApplyNavMarkerEditVisibility()
	{
		bool flag = editModeType != 0;
		IEnumerable<KeyValuePair<GameEntity, HomesteadSceneSavedEntity>> loadedSavedEntities = homesteadScene.LoadedSavedEntities;
		if (loadedSavedEntities != null)
		{
			foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> item in loadedSavedEntities)
			{
				HomesteadScenePlaceable homesteadScenePlaceable = item.Value?.Placeable;
				if (!IsEditOnlyMarker(homesteadScenePlaceable))
				{
					continue;
				}
				bool flag2 = flag && (isPlanningMode || !IsRaceMarkerPrefab(homesteadScenePlaceable.PrefabName));
				try
				{
					item.Key.SetVisibilityExcludeParents(flag2);
					if (flag2)
					{
						ApplyNavMarkerMaterial(item.Key, homesteadScenePlaceable);
					}
				}
				catch
				{
				}
			}
		}
		foreach (KeyValuePair<GameEntity, HomesteadSceneSavedEntity> planningLoadedEntity in planningLoadedEntities)
		{
			HomesteadScenePlaceable homesteadScenePlaceable2 = planningLoadedEntity.Value?.Placeable;
			if (!IsEditOnlyMarker(homesteadScenePlaceable2))
			{
				continue;
			}
			bool flag3 = flag && (isPlanningMode || !IsRaceMarkerPrefab(homesteadScenePlaceable2.PrefabName));
			try
			{
				planningLoadedEntity.Key.SetVisibilityExcludeParents(flag3);
				if (flag3)
				{
					ApplyNavMarkerMaterial(planningLoadedEntity.Key, homesteadScenePlaceable2);
				}
			}
			catch
			{
			}
		}
	}

	private static void ApplyNavMarkerMaterial(GameEntity entity, HomesteadScenePlaceable? placeable)
	{
		if (!IsEditOnlyMarker(placeable))
		{
			return;
		}
		string material;
		if (placeable.IsNpcActionFlag)
		{
			material = (string.IsNullOrEmpty(placeable.NpcColor) ? "plain_green" : placeable.NpcColor);
		}
		else
		{
			string prefabName = placeable.PrefabName;
			material = ((prefabName == "homestead_race_start") ? "plain_red" : ((prefabName == "homestead_race_gate") ? "plain_yellow" : "plain_green"));
		}
		try
		{
			foreach (GameEntity entityAndChild in entity.GetEntityAndChildren())
			{
				MetaMesh metaMesh = entityAndChild.GetMetaMesh(0);
				if (metaMesh == null || !metaMesh.IsValid)
				{
					continue;
				}
				for (int i = 0; i < metaMesh.MeshCount; i++)
				{
					Mesh meshAtIndex = metaMesh.GetMeshAtIndex(i);
					if (meshAtIndex != null && meshAtIndex.IsValid)
					{
						meshAtIndex.SetMaterial(material);
					}
				}
			}
		}
		catch
		{
		}
	}
}

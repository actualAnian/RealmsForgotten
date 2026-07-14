using System;
using Homesteads.Models;
using Homesteads.ViewModels;
using SandBox.View.Map;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Homesteads.Views;

public class HomesteadPlacementMapView : MapView
{
	private static readonly string[] MarkerMeshCandidates = new string[3] { "map_icons_sturgia_square_tower_l3", "map_icon_full_empire_village", "map_icon_castle_empire" };

	private const float RelocationSpeed = 1f;

	private GameEntity? _ghostEntity;

	private Homestead? _homestead;

	private Action<Vec2>? _onPlaced;

	private Action? _onCanceled;

	private bool _cleanedUp;

	private float _readoutTimer;

	private GauntletLayer? _gauntletLayer;

	private HomesteadPlacementVM? _viewModel;

	public static HomesteadPlacementMapView? Active { get; private set; }

	public void Initialize(Homestead homestead, Action<Vec2> onPlaced, Action onCanceled)
	{
		_homestead = homestead;
		_onPlaced = onPlaced;
		_onCanceled = onCanceled;
		_viewModel?.SetHomestead(homestead);
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		Active = this;
		_ghostEntity = null;
		string[] markerMeshCandidates = MarkerMeshCandidates;
		foreach (string text in markerMeshCandidates)
		{
			_ghostEntity = GameEntity.Instantiate(MapScreen.Instance.MapScene, text, callScriptCallbacks: false);
			if (_ghostEntity != null)
			{
				break;
			}
			MetaMesh copy = MetaMesh.GetCopy(text, showErrors: false);
			if (copy != null)
			{
				_ghostEntity = GameEntity.CreateEmpty(MapScreen.Instance.MapScene);
				_ghostEntity.AddMultiMesh(copy);
				break;
			}
		}
		if (_ghostEntity == null)
		{
			_ghostEntity = GameEntity.CreateEmpty(MapScreen.Instance.MapScene);
		}
		_ghostEntity.SetVisibilityExcludeParents(visible: true);
		_ghostEntity.SetAlpha(0.6f);
		InformationManager.DisplayMessage(new InformationMessage("Click on the map to relocate your homestead. Press ESC or right-click to cancel.", Colors.Yellow));
		_viewModel = new HomesteadPlacementVM(_homestead);
		_gauntletLayer = new GauntletLayer("HomesteadPlacementHUD", 100)
		{
			IsFocusLayer = false
		};
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		_gauntletLayer.LoadMovie("HomesteadPlacementHUD", _viewModel);
		((ScreenBase)(object)MapScreen.Instance).AddLayer((ScreenLayer)_gauntletLayer);
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		if (_cleanedUp)
		{
			return;
		}
		if (_ghostEntity != null && MapScreen.Instance != null)
		{
			Vec3 worldMouseNear = Vec3.Zero;
			Vec3 worldMouseFar = Vec3.Zero;
			MapScreen.Instance.SceneLayer.TranslateMouse(ref worldMouseNear, ref worldMouseFar);
			PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
			float num = default(float);
			Vec3 vec = default(Vec3);
			bool flag = default(bool);
			MapScreen.Instance.GetCursorIntersectionPoint(ref worldMouseNear, ref worldMouseFar, out num, out vec, ref nullFaceRecord, out flag, BodyFlags.CommonFocusRayCastExcludeFlags);
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = new Vec3(vec.x, vec.y, vec.z);
			_ghostEntity.SetFrame(ref frame);
			if (_viewModel != null)
			{
				_viewModel.UpdateTargetPosition(vec.AsVec2);
			}
		}
		if (Input.IsKeyReleased(InputKey.Escape) || Input.IsKeyReleased(InputKey.RightMouseButton))
		{
			CancelPlacement();
		}
	}

	protected override void OnMainPartyEncounter()
	{
		base.OnMainPartyEncounter();
		CancelPlacement();
	}

	protected override void OnMapConversationStart()
	{
		base.OnMapConversationStart();
		CancelPlacement();
	}

	public void PlaceAt(Vec2 target)
	{
		if (!_cleanedUp)
		{
			Action<Vec2>? onPlaced = _onPlaced;
			CleanUp();
			onPlaced?.Invoke(target);
		}
	}

	private void CancelPlacement()
	{
		if (!_cleanedUp)
		{
			Action? onCanceled = _onCanceled;
			InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_relocation_cancelled}Homestead relocation cancelled."), Colors.Yellow));
			CleanUp();
			onCanceled?.Invoke();
		}
	}

	private void CleanUp()
	{
		if (!_cleanedUp)
		{
			_cleanedUp = true;
			if (Active == this)
			{
				Active = null;
			}
			if (_ghostEntity != null)
			{
				_ghostEntity.Remove(1);
				_ghostEntity = null;
			}
			if (_gauntletLayer != null)
			{
				((ScreenBase)(object)MapScreen.Instance)?.RemoveLayer((ScreenLayer)_gauntletLayer);
				_gauntletLayer = null;
			}
			MapScreen instance = MapScreen.Instance;
			if (instance != null)
			{
				instance.RemoveMapView((MapView)(object)this);
			}
		}
	}
}

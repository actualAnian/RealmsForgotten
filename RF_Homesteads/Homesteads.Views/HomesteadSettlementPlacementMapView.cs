using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Homesteads.Views;

public class HomesteadSettlementPlacementMapView : MapView
{
	public enum Phase
	{
		Town,
		Gate,
		Port,
		Castle,
		CastleGate,
		Village,
		Done
	}

	public struct Placed
	{
		public Vec2 Position;

		public float Rotation;

		public int VariantIndex;

		public string ResourceId;

		public bool BoundToCastle;
	}

	public class Result
	{
		public Homestead Homestead;

		public Placed Town;

		public Placed Gate;

		public Placed? Port;

		public Placed Castle;

		public Placed CastleGate;

		public readonly List<Placed> Villages = new List<Placed>();
	}

	private static readonly string[] TownVariants = new string[1] { "hsr_map_town_empire" };

	private static readonly string[] GateVariants = new string[3] { "flagpole_b_ground", "flagpole_a", "bd_pole_3m" };

	private static readonly string[] PortVariants = new string[3] { "flagpole_b_ground", "flagpole_a", "bd_pole_3m" };

	private static readonly string[] CastleVariants = new string[1] { "hsr_map_castle_empire" };

	private static readonly string[] CastleGateVariants = new string[3] { "flagpole_b_ground", "flagpole_a", "bd_pole_3m" };

	private static readonly string[] VillageVariants = new string[1] { "hsr_map_village_empire" };

	private const float TownRadius = 6f;

	private const float PortRadius = 50f;

	private const float CastleMin = 20f;

	private const float CastleMax = 70f;

	private const float VillageRadiusBase = 10f;

	private const float VillageRadiusPerVillage = 4f;

	private const float RotateSpeed = 2f;

	private Homestead _homestead;

	private Vec2 _center;

	private int _villageCount;

	private Action<Result>? _onCommit;

	private Action? _onCancel;

	private readonly Result _result = new Result();

	private Phase _phase;

	private int _villageIndex;

	private const string FallbackMarker = "map_icons_sturgia_square_tower_l3";

	private static readonly string[] BoundaryFlagCandidates = new string[3] { "flagpole_b_ground", "flagpole_a", "bd_pole_3m" };

	private const string BoundaryFallbackMesh = "map_icons_empire_square_tower_l1";

	private const int BoundarySegments = 28;

	private const float BoundaryMarkerScale = 2.5f;

	private string _boundaryMarker;

	private GameEntity? _ghost;

	private Vec3 _ghostScale;

	private Mat3 _ghostNaturalRotation;

	private readonly List<GameEntity> _boundary = new List<GameEntity>();

	private readonly List<GameEntity> _placed = new List<GameEntity>();

	private float _yaw;

	private int _variantIndex;

	private Vec2 _lastMapPos;

	private bool _lastValid;

	private bool _gatePoorlyConnected;

	private bool _cleanedUp;

	private bool _awaitingResourcePick;

	private bool _villageBindToCastle;

	private const float SolidAlpha = 0.9f;

	private const float InvalidAlpha = 0.45f;

	private static readonly (string Id, string FallbackLabel)[] VillageResources = new(string, string)[17]
	{
		("wheat_farm", "{=homestead_villageres_wheat_farm}Grain Farm"),
		("cattle_farm", "{=homestead_villageres_cattle_farm}Cattle Range"),
		("sheep_farm", "{=homestead_villageres_sheep_farm}Sheep / Wool"),
		("swine_farm", "{=homestead_villageres_swine_farm}Swine Farm"),
		("europe_horse_ranch", "{=homestead_villageres_horse_ranch}Horse Ranch"),
		("fisherman", "{=homestead_villageres_fisherman}Fishing Village"),
		("vineyard", "{=homestead_villageres_vineyard}Vineyard"),
		("olive_trees", "{=homestead_villageres_olive_trees}Olive Grove"),
		("date_farm", "{=homestead_villageres_date_farm}Date Farm"),
		("flax_plant", "{=homestead_villageres_flax_plant}Flax Fields"),
		("silk_plant", "{=homestead_villageres_silk_plant}Silkworm Farm"),
		("lumberjack", "{=homestead_villageres_lumberjack}Woodland (timber)"),
		("trapper", "{=homestead_villageres_trapper}Trapper's Camp"),
		("clay_mine", "{=homestead_villageres_clay_mine}Clay Pit (pottery)"),
		("iron_mine", "{=homestead_villageres_iron_mine}Iron Mine"),
		("silver_mine", "{=homestead_villageres_silver_mine}Silver Mine"),
		("salt_mine", "{=homestead_villageres_salt_mine}Salt Mine")
	};

	public static HomesteadSettlementPlacementMapView? Active { get; private set; }

	private string[] CurrentVariants => _phase switch
	{
		Phase.Town => TownVariants, 
		Phase.Gate => GateVariants, 
		Phase.Port => PortVariants, 
		Phase.Castle => CastleVariants, 
		Phase.CastleGate => CastleGateVariants, 
		Phase.Village => VillageVariants, 
		_ => TownVariants, 
	};

	private string CurrentMesh => CurrentVariants[Math.Min(_variantIndex, CurrentVariants.Length - 1)];

	public void Initialize(Homestead homestead, int villageCount, Action<Result> onCommit, Action onCancel)
	{
		_homestead = homestead;
		_center = homestead?.MobileParty?.GetPosition2D ?? MobileParty.MainParty.GetPosition2D;
		_villageCount = Math.Max(0, villageCount);
		_onCommit = onCommit;
		_onCancel = onCancel;
		_result.Homestead = homestead;
		SpawnGhostForPhase();
		AnnouncePhase();
	}

	protected override void CreateLayout()
	{
		base.CreateLayout();
		Active = this;
	}

	private void SpawnGhostForPhase()
	{
		RemoveGhost();
		_yaw = 0f;
		_variantIndex = 0;
		_ghost = SpawnGhost(CurrentMesh);
		RebuildBoundary();
	}

	private void RebuildBoundary()
	{
		ClearBoundary();
		if (string.IsNullOrEmpty(_boundaryMarker))
		{
			_boundaryMarker = "map_icons_empire_square_tower_l1";
			string[] boundaryFlagCandidates = BoundaryFlagCandidates;
			foreach (string text in boundaryFlagCandidates)
			{
				if (GameEntity.PrefabExists(text))
				{
					_boundaryMarker = text;
					break;
				}
			}
			TraceLogger.Write("HomesteadSettlementPlacementMapView", "Boundary marker = '" + _boundaryMarker + "'.");
		}
		switch (_phase)
		{
		case Phase.Town:
			AddCircle(_center, 6f);
			break;
		case Phase.Port:
			AddCircle(_result.Town.Position, 50f);
			break;
		case Phase.Castle:
			AddCircle(_result.Town.Position, 20f);
			AddCircle(_result.Town.Position, 70f);
			break;
		case Phase.Village:
			AddCircle(_villageBindToCastle ? _result.Castle.Position : _result.Town.Position, CurrentVillageBindRadius());
			break;
		case Phase.Gate:
		case Phase.CastleGate:
			break;
		}
	}

	private void AddCircle(Vec2 center, float radius)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < 28; i++)
		{
			double num3 = (double)i * 2.0 * Math.PI / 28.0;
			Vec2 pos = new Vec2(center.x + radius * (float)Math.Cos(num3), center.y + radius * (float)Math.Sin(num3));
			GameEntity gameEntity = SpawnBoundaryPost(pos);
			if (gameEntity != null)
			{
				_boundary.Add(gameEntity);
				num++;
			}
			else
			{
				num2++;
			}
		}
		TraceLogger.Write("HomesteadSettlementPlacementMapView", $"AddCircle center=({center.x:0.#},{center.y:0.#}) r={radius:0.#}: created {num}, failed {num2}.");
	}

	private GameEntity SpawnBoundaryPost(Vec2 pos)
	{
		GameEntity gameEntity = TryCreateIcon(_boundaryMarker) ?? TryCreateIcon("map_icons_empire_square_tower_l1") ?? TryCreateIcon("map_icons_sturgia_square_tower_l3");
		if (gameEntity == null)
		{
			return null;
		}
		gameEntity.SetVisibilityExcludeParents(visible: true);
		gameEntity.SetAlpha(1f);
		float height = 0f;
		CampaignVec2 point = new CampaignVec2(pos, isOnLand: true);
		Campaign.Current.MapSceneWrapper.GetHeightAtPoint(in point, ref height);
		MatrixFrame frame = MatrixFrame.Identity;
		frame.rotation.f *= 2.5f;
		frame.rotation.s *= 2.5f;
		frame.rotation.u *= 2.5f;
		frame.origin = new Vec3(pos.x, pos.y, height);
		gameEntity.SetFrame(ref frame);
		return gameEntity;
	}

	private void ClearBoundary()
	{
		foreach (GameEntity item in _boundary)
		{
			try
			{
				item.Remove(1);
			}
			catch
			{
			}
		}
		_boundary.Clear();
	}

	private GameEntity SpawnGhost(string iconName)
	{
		GameEntity gameEntity = TryCreateIcon(iconName) ?? TryCreateIcon("map_icons_sturgia_square_tower_l3") ?? GameEntity.CreateEmpty(MapScreen.Instance.MapScene);
		gameEntity.SetVisibilityExcludeParents(visible: true);
		gameEntity.SetAlpha(0.9f);
		_ghostNaturalRotation = gameEntity.GetFrame().rotation;
		_ghostScale = _ghostNaturalRotation.GetScaleVector();
		return gameEntity;
	}

	private GameEntity TryCreateIcon(string name)
	{
		GameEntity gameEntity = GameEntity.Instantiate(MapScreen.Instance.MapScene, name, callScriptCallbacks: false, createPhysics: false);
		if (gameEntity != null)
		{
			gameEntity.AddBodyFlags(BodyFlags.CommonFlagsThatDoNotBlockRay | BodyFlags.OnlyCollideWithRaycast | BodyFlags.DroppedItem);
			return gameEntity;
		}
		MetaMesh copy = MetaMesh.GetCopy(name, showErrors: false);
		if (copy != null)
		{
			gameEntity = GameEntity.CreateEmpty(MapScreen.Instance.MapScene);
			gameEntity.AddMultiMesh(copy);
			gameEntity.AddBodyFlags(BodyFlags.CommonFlagsThatDoNotBlockRay | BodyFlags.OnlyCollideWithRaycast | BodyFlags.DroppedItem);
			return gameEntity;
		}
		return null;
	}

	private void RemoveGhost()
	{
		if (_ghost != null)
		{
			_ghost.Remove(1);
			_ghost = null;
		}
	}

	private void AnnouncePhase()
	{
		PrintControls();
	}

	private void PrintControls()
	{
		string text = _phase switch
		{
			Phase.Town => Utils.GetLocalizedString("{=homestead_place_msg_town}Place your TOWN near the homestead. Mouse = move, Q/E = rotate, Left-click = place, ESC = cancel."), 
			Phase.Gate => Utils.GetLocalizedString("{=homestead_place_msg_gate}Place the GATE near the town."), 
			Phase.Port => Utils.GetLocalizedString("{=homestead_place_msg_port}Place the PORT on the water. Right Click = Skip Port phase."), 
			Phase.Castle => Utils.GetLocalizedString("{=homestead_place_msg_castle}Place your CASTLE a short distance from the town."), 
			Phase.CastleGate => Utils.GetLocalizedString("{=homestead_place_msg_castlegate}Place the GATE near the castle."), 
			Phase.Village => Utils.GetLocalizedString(_villageBindToCastle ? "{=homestead_place_msg_village_castle}Place VILLAGE {INDEX} of {COUNT} close to its CASTLE." : "{=homestead_place_msg_village_town}Place VILLAGE {INDEX} of {COUNT} close to its TOWN.", ("INDEX", (_villageIndex + 1).ToString()), ("COUNT", _villageCount.ToString())), 
			_ => "", 
		};
		if (!string.IsNullOrEmpty(text))
		{
			InformationManager.DisplayMessage(new InformationMessage(text, Colors.Yellow));
		}
	}

	protected override void OnMapScreenUpdate(float dt)
	{
		base.OnMapScreenUpdate(dt);
		if (_cleanedUp || _awaitingResourcePick || _ghost == null || MapScreen.Instance == null)
		{
			return;
		}
		InputContext input = MapScreen.Instance.SceneLayer.Input;
		if (input.IsKeyDown(InputKey.Q))
		{
			_yaw -= 2f * dt;
		}
		if (input.IsKeyDown(InputKey.E))
		{
			_yaw += 2f * dt;
		}
		if (input.IsKeyPressed(InputKey.OpenBraces))
		{
			CycleVariant(-1);
		}
		if (input.IsKeyPressed(InputKey.CloseBraces))
		{
			CycleVariant(1);
		}
		Vec3 worldMouseNear = Vec3.Zero;
		Vec3 worldMouseFar = Vec3.Zero;
		MapScreen.Instance.SceneLayer.TranslateMouse(ref worldMouseNear, ref worldMouseFar);
		PathFaceRecord nullFaceRecord = PathFaceRecord.NullFaceRecord;
		float num = default(float);
		Vec3 vec = default(Vec3);
		bool flag = default(bool);
		MapScreen.Instance.GetCursorIntersectionPoint(ref worldMouseNear, ref worldMouseFar, out num, out vec, ref nullFaceRecord, out flag, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.Moveable);
		_lastMapPos = vec.AsVec2;
		bool flag2 = false;
		_gatePoorlyConnected = false;
		if (_phase == Phase.Port)
		{
			int num2 = Campaign.Current?.MapSceneWrapper?.GetFaceIndex(new CampaignVec2(vec.AsVec2, isOnLand: false)).FaceIndex ?? (-1);
			flag2 = !flag || num2 >= 0;
		}
		else
		{
			flag2 = flag && nullFaceRecord.IsValid();
			if (flag2 && (_phase == Phase.Gate || _phase == Phase.CastleGate) && !HomesteadSettlementBuilder.IsGateWellConnected(vec.AsVec2))
			{
				flag2 = false;
				_gatePoorlyConnected = true;
			}
		}
		_lastValid = flag2 && IsWithinConstraint(_lastMapPos);
		MatrixFrame frame = _ghost.GetFrame();
		frame.rotation = _ghostNaturalRotation;
		frame.rotation.RotateAboutUp(_yaw);
		frame.origin = new Vec3(vec.x, vec.y, vec.z);
		_ghost.SetFrame(ref frame);
		_ghost.SetAlpha(_lastValid ? 0.9f : 0.45f);
		if (input.IsKeyReleased(InputKey.Escape))
		{
			CancelPlacement();
		}
		if (input.IsKeyReleased(InputKey.RightMouseButton) && _phase == Phase.Port)
		{
			AdvanceTo(Phase.Castle);
		}
	}

	private void CycleVariant(int dir)
	{
		int num = CurrentVariants.Length;
		if (num > 1)
		{
			_variantIndex = ((_variantIndex + dir) % num + num) % num;
			float yaw = _yaw;
			RemoveGhost();
			_ghost = SpawnGhost(CurrentMesh);
			_yaw = yaw;
		}
	}

	private bool IsWithinConstraint(Vec2 pos)
	{
		switch (_phase)
		{
		case Phase.Town:
			return pos.Distance(_center) <= 6f;
		case Phase.Gate:
			return pos.Distance(_result.Town.Position) <= 6f;
		case Phase.Port:
			return pos.Distance(_result.Town.Position) <= 50f;
		case Phase.Castle:
		{
			float num = pos.Distance(_result.Town.Position);
			if (num >= 20f)
			{
				return num <= 70f;
			}
			return false;
		}
		case Phase.CastleGate:
			return pos.Distance(_result.Castle.Position) <= 6f;
		case Phase.Village:
		{
			Vec2 v = (_villageBindToCastle ? _result.Castle.Position : _result.Town.Position);
			return pos.Distance(v) <= CurrentVillageBindRadius();
		}
		default:
			return false;
		}
	}

	public void PlaceAt(Vec2 target)
	{
		if (_cleanedUp || _awaitingResourcePick)
		{
			return;
		}
		if (!_lastValid)
		{
			string information = ((!_gatePoorlyConnected || !IsWithinConstraint(target)) ? (IsWithinConstraint(target) ? Utils.GetLocalizedString("{=homestead_place_invalid_terrain}Invalid terrain or navmesh.") : GetFailureReason()) : Utils.GetLocalizedString("{=homestead_place_gate_unreachable}That gate can't reach the road network cleanly (steep/cliff ground) — parties would get stuck there. Pick flatter, more open ground for the gate."));
			InformationManager.DisplayMessage(new InformationMessage(information, Colors.Red));
			return;
		}
		Placed placed = new Placed
		{
			Position = target,
			Rotation = _yaw,
			VariantIndex = _variantIndex
		};
		AddPlacedMarker(placed.Position, placed.Rotation, CurrentMesh);
		switch (_phase)
		{
		case Phase.Town:
			_result.Town = placed;
			AdvanceTo(Phase.Gate);
			break;
		case Phase.Gate:
			_result.Gate = placed;
			PromptPortOrAdvance();
			break;
		case Phase.Port:
			_result.Port = placed;
			AdvanceTo(Phase.Castle);
			break;
		case Phase.Castle:
			_result.Castle = placed;
			AdvanceTo(Phase.CastleGate);
			break;
		case Phase.CastleGate:
			_result.CastleGate = placed;
			AdvanceTo((_villageCount > 0) ? Phase.Village : Phase.Done);
			break;
		case Phase.Village:
			placed.BoundToCastle = _villageBindToCastle;
			PromptResourceThenRecord(placed);
			break;
		}
	}

	private void AddPlacedMarker(Vec2 pos, float yaw, string mesh)
	{
		GameEntity gameEntity = TryCreateIcon(mesh) ?? TryCreateIcon("map_icons_sturgia_square_tower_l3");
		if (!(gameEntity == null))
		{
			gameEntity.SetVisibilityExcludeParents(visible: true);
			gameEntity.SetAlpha(1f);
			float height = 0f;
			CampaignVec2 point = new CampaignVec2(pos, isOnLand: true);
			Campaign.Current.MapSceneWrapper.GetHeightAtPoint(in point, ref height);
			MatrixFrame frame = gameEntity.GetFrame();
			frame.rotation.RotateAboutUp(yaw);
			frame.origin = new Vec3(pos.x, pos.y, height);
			gameEntity.SetFrame(ref frame);
			_placed.Add(gameEntity);
		}
	}

	private string GetFailureReason()
	{
		return _phase switch
		{
			Phase.Town => Utils.GetLocalizedString("{=homestead_place_fail_town}Too far from the homestead – place the town closer."), 
			Phase.Gate => Utils.GetLocalizedString("{=homestead_place_fail_gate}Place the gate close to the town."), 
			Phase.Port => Utils.GetLocalizedString("{=homestead_place_fail_port}Place the port within a reasonable distance."), 
			Phase.Castle => Utils.GetLocalizedString("{=homestead_place_fail_castle}Place the castle a short distance from the town (not too close, not too far)."), 
			Phase.CastleGate => Utils.GetLocalizedString("{=homestead_place_fail_castlegate}Place the gate close to the castle."), 
			Phase.Village => Utils.GetLocalizedString(_villageBindToCastle ? "{=homestead_place_fail_village_castle}Place the village close to its castle." : "{=homestead_place_fail_village_town}Place the village close to its town."), 
			_ => Utils.GetLocalizedString("{=homestead_place_fail_default}Invalid location."), 
		};
	}

	private void AdvanceTo(Phase next)
	{
		_phase = next;
		if (_phase == Phase.Done)
		{
			Commit();
			return;
		}
		if (_phase == Phase.Village)
		{
			_villageBindToCastle = VillageBoundToCastle(_villageIndex, _villageCount);
		}
		SpawnGhostForPhase();
		AnnouncePhase();
	}

	private void PromptPortOrAdvance()
	{
		if (!Utils.IsNavalDlcLoaded())
		{
			AdvanceTo(Phase.Castle);
			return;
		}
		_awaitingResourcePick = true;
		InformationManager.ShowInquiry(new InquiryData(Utils.GetLocalizedString("{=homestead_place_port_title}Port"), Utils.GetLocalizedString("{=homestead_place_port_prompt}Does this settlement have a port?"), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_no").ToString(), delegate
		{
			_awaitingResourcePick = false;
			AdvanceTo(Phase.Port);
		}, delegate
		{
			_awaitingResourcePick = false;
			AdvanceTo(Phase.Castle);
		}));
	}

	private static bool VillageBoundToCastle(int villageIndex, int totalVillages)
	{
		int num = Math.Min(3, totalVillages / 2);
		return villageIndex >= totalVillages - num;
	}

	private float CurrentVillageBindRadius()
	{
		int num = Math.Min(3, _villageCount / 2);
		int val = (_villageBindToCastle ? num : (_villageCount - num));
		return 10f + 4f * (float)Math.Max(1, val);
	}

	private static string ResolveResourceLabel(string villageTypeId, string fallbackLabel)
	{
		try
		{
			TextObject textObject = (MBObjectManager.Instance?.GetObjectTypeList<VillageType>().FirstOrDefault((VillageType v) => v?.StringId == villageTypeId))?.PrimaryProduction?.Name;
			if (textObject != null)
			{
				return textObject.ToString();
			}
		}
		catch
		{
		}
		return Utils.GetLocalizedString(fallbackLabel);
	}

	private void PromptResourceThenRecord(Placed placed)
	{
		_awaitingResourcePick = true;
		List<InquiryElement> list = new List<InquiryElement>();
		(string, string)[] villageResources = VillageResources;
		for (int i = 0; i < villageResources.Length; i++)
		{
			(string, string) tuple = villageResources[i];
			list.Add(new InquiryElement(tuple.Item1, ResolveResourceLabel(tuple.Item1, tuple.Item2), null));
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(Utils.GetLocalizedString("{=homestead_place_villageres_title}Village {INDEX} resource", ("INDEX", (_villageIndex + 1).ToString())), Utils.GetLocalizedString("{=homestead_place_villageres_desc}Choose what this village produces."), list, isExitShown: false, 1, 1, Utils.GetLocalizedString("{=homestead_place_villageres_confirm}Confirm"), null, delegate(List<InquiryElement> selected)
		{
			string text = ((selected != null && selected.Count > 0) ? (selected[0].Identifier as string) : VillageResources[0].Id);
			placed.ResourceId = text ?? VillageResources[0].Id;
			_result.Villages.Add(placed);
			_awaitingResourcePick = false;
			_villageIndex++;
			AdvanceTo((_villageIndex < _villageCount) ? Phase.Village : Phase.Done);
		}, null), pauseGameActiveState: true);
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

	private void Commit()
	{
		Action<Result>? onCommit = _onCommit;
		Result result = _result;
		CleanUp();
		onCommit?.Invoke(result);
	}

	private void CancelPlacement()
	{
		if (!_cleanedUp)
		{
			Action? onCancel = _onCancel;
			InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_place_cancelled}Settlement placement cancelled."), Colors.Yellow));
			CleanUp();
			onCancel?.Invoke();
		}
	}

	private void CleanUp()
	{
		if (_cleanedUp)
		{
			return;
		}
		_cleanedUp = true;
		if (Active == this)
		{
			Active = null;
		}
		RemoveGhost();
		ClearBoundary();
		foreach (GameEntity item in _placed)
		{
			try
			{
				item.Remove(1);
			}
			catch
			{
			}
		}
		_placed.Clear();
		MapScreen instance = MapScreen.Instance;
		if (instance != null)
		{
			instance.RemoveMapView((MapView)(object)this);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Homesteads.Models;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Homesteads.MissionLogics;

public class HomesteadRaceMissionLogic : MissionLogic
{
	private enum Phase
	{
		Loading,
		Countdown,
		Racing,
		Finished
	}

	private sealed class Racer
	{
		public Agent? Agent;

		public Hero? Hero;

		public float SpeedFactor = 2f;

		public int CheckpointIndex;

		public int GateIndex;

		public int Lap;

		public bool Finished;

		public Vec3 LastPos;

		public Vec3 HoldPos = Vec3.Invalid;

		public float MinWaypointDistSq = float.MaxValue;

		public float StuckTimer;

		public float SinceGate;

		public float OffTrackTimer;

		public float BoostActive;

		public float BoostWinded;

		public float BoostCooldown;

		public float HobbleTimer;

		public float LapStart;

		public float BestLap = float.MaxValue;

		public float FinishTime;

		public int FinishPlace;

		public uint Color;
	}

	public struct RaceProgress
	{
		public string Name;

		public float Fraction;

		public bool IsPlayer;

		public bool Finished;

		public uint Color;

		public int Place;

		public uint SpurColor;

		public string SpurText;

		public float Speed;
	}

	private const string StartTag = "homestead_race_start";

	private const string GateTag = "homestead_race_gate";

	private const string FlagTag = "homestead_race_flag";

	private const string FlagPrefabName = "flag_rope_cloth_02";

	private const string PropTag = "homestead_race_prop";

	private static readonly string[] GateFlagPrefabCandidates = new string[3] { "flagpole_b_ground", "flagpole_a", "bd_pole_3m" };

	private const float GateFlagScale = 1.8f;

	private const float DefaultGateRadius = 15f;

	private const float AiWaypointRadius = 5f;

	private const float GateTimeoutSec = 10f;

	private const float CornerSpeedFloor = 0.3f;

	private const float CornerSpeedStraight = 3f;

	private const float CornerCeilCurve = 3f;

	private const int CornerLookahead = 10;

	private const int RaceNpcXpBase = 1500;

	private const int RaceNpcXpWinBonus = 1000;

	private const float CountdownSec = 4f;

	private const float AiStartDelaySec = 1f;

	private readonly Homestead? _homestead;

	private readonly RaceTrack _track;

	private Phase _phase;

	private int _loadFrames;

	private bool _trackPlaced;

	private int _settleFrames;

	private GameEntity? _trackEntity;

	private Vec3 _startPos;

	private readonly List<Vec3> _checkpoints = new List<Vec3>();

	private List<GameEntity> _orderedGates = new List<GameEntity>();

	private readonly List<Vec3> _aiPath = new List<Vec3>();

	private readonly List<GameEntity> _gateMarkers = new List<GameEntity>();

	private int _checkpointIndex;

	private int _lap;

	private float _countdown;

	private float _goTextTimer;

	private float _aiReleaseTimer;

	private float _offTrackTimer;

	private float _playerSinceGate;

	private bool _showRecoverPrompt;

	private const float OffTrackRadius = 35f;

	private const float OffTrackSeconds = 3f;

	private const InputKey RecoverKey = InputKey.X;

	private float _playerLuck = 1f;

	private const float LuckMin = 0.9f;

	private const float LuckMax = 1.1f;

	private float _boostActive;

	private float _boostWinded;

	private float _boostCooldown;

	private const float BoostFactor = 2.8f;

	private const float BoostDuration = 6f;

	private const float WindedFactor = 0.75f;

	private const float WindedDuration = 3f;

	private const float BoostCooldownAfter = 24f;

	private const InputKey BoostKey = InputKey.Q;

	private float _dogCheatCooldown;

	private const float DogCheatCooldown = 5f;

	private const float DogCheatRange = 18f;

	private const float HobbleFactor = 0.4f;

	private const float HobbleDuration = 5f;

	private float _raceTime;

	private float _endDelay;

	private Agent? _player;

	private bool _playerFinished;

	private int _playerFinishPlace;

	private uint _playerColor;

	private Vec3 _playerHoldPos;

	private int _finishPlace;

	private float _playerLapStart;

	private float _playerBestLap = float.MaxValue;

	private float _playerFinishTime;

	private float _postFinishTimer;

	private bool _raceEnded;

	private bool _resultsRecorded;

	private const float MaxPostFinishWait = 45f;

	private readonly List<Hero> _rivalHeroes;

	private readonly List<Racer> _racers = new List<Racer>();

	private static ItemObject? _cachedRidingHorse;

	private static readonly uint[] RacerPalette = new uint[5] { 4282362357u, 4293016379u, 4283023950u, 4293050683u, 4290010082u };

	private const uint SpurReady = 4283023950u;

	private const uint SpurOn = 4282362357u;

	private const uint SpurWinded = 4293016379u;

	private const uint SpurCooldown = 4293042747u;

	private bool _flagsHidden;

	private const string GateColorPending = "plain_red";

	private const string GateColorPassed = "plain_green";

	private static ItemObject? _cachedBannerItem;

	private static ItemObject? _cachedHarness;

	private static readonly string[] WhinnyEvents = new string[4] { "event:/voice/combat/horse/01/neigh", "event:/voice/combat/horse/02/neigh", "event:/voice/combat/horse/03/neigh", "event:/voice/combat/horse/04/neigh" };

	private ItemObject? _boostedHorseItem;

	private int _origHorseSpeed = -1;

	private const float RaceHorseSpeedBoost = 1.7f;

	private float GateRadius
	{
		get
		{
			RaceTrack track = _track;
			if (track == null || !(track.GateRadius > 0f))
			{
				return 15f;
			}
			return _track.GateRadius;
		}
	}

	private int RaceLaps => _track?.Laps ?? 3;

	private static float RollLuck()
	{
		return MBRandom.RandomFloatRanged(0.9f, 1.1f);
	}

	private static uint ColorForRacer(int index)
	{
		return RacerPalette[index % RacerPalette.Length];
	}

	public HomesteadRaceMissionLogic(Homestead? homestead, RaceTrack track, List<Hero>? rivals = null)
	{
		_homestead = homestead;
		_track = track;
		_rivalHeroes = rivals ?? new List<Hero>();
		_flagsHidden = HomesteadBehavior.Instance?.RaceFlagsHidden ?? false;
	}

	private static float SpeedOf(Agent? a)
	{
		if (a == null || !a.IsActive())
		{
			return 0f;
		}
		return (a.MountAgent ?? a).MovementVelocity.Length;
	}

	private static uint SpurColorFor(float active, float winded, float cooldown)
	{
		if (!(active > 0f))
		{
			if (!(winded > 0f))
			{
				if (!(cooldown > 0f))
				{
					return 4283023950u;
				}
				return 4293042747u;
			}
			return 4293016379u;
		}
		return 4282362357u;
	}

	private static string SpurTextFor(float active, float winded, float cooldown)
	{
		if (!(active > 0f))
		{
			if (!(winded > 0f))
			{
				if (!(cooldown > 0f))
				{
					return "READY";
				}
				return TaleWorlds.Library.MathF.Ceiling(cooldown).ToString();
			}
			return "WINDED";
		}
		return "SPUR";
	}

	public List<RaceProgress> GetProgressSnapshot()
	{
		List<RaceProgress> list = new List<RaceProgress>();
		if (_phase == Phase.Loading)
		{
			return list;
		}
		int num = Math.Max(1, _checkpoints.Count);
		float num2 = Math.Max(1, RaceLaps);
		float fraction = (_playerFinished ? 1f : TaleWorlds.Library.MathF.Min(1f, ((float)_lap + (float)_checkpointIndex / (float)num) / num2));
		list.Add(new RaceProgress
		{
			Name = (Hero.MainHero?.FirstName?.ToString() ?? "You"),
			Fraction = fraction,
			IsPlayer = true,
			Finished = _playerFinished,
			Color = _playerColor,
			Place = (_playerFinished ? _playerFinishPlace : 0),
			SpurColor = SpurColorFor(_boostActive, _boostWinded, _boostCooldown),
			SpurText = SpurTextFor(_boostActive, _boostWinded, _boostCooldown),
			Speed = SpeedOf(_player)
		});
		foreach (Racer racer in _racers)
		{
			float fraction2 = (racer.Finished ? 1f : TaleWorlds.Library.MathF.Min(1f, ((float)racer.Lap + (float)(racer.GateIndex % num) / (float)num) / num2));
			list.Add(new RaceProgress
			{
				Name = (racer.Hero?.FirstName?.ToString() ?? "Rival"),
				Fraction = fraction2,
				IsPlayer = false,
				Finished = racer.Finished,
				Color = racer.Color,
				Place = (racer.Finished ? racer.FinishPlace : 0),
				SpurColor = SpurColorFor(racer.BoostActive, racer.BoostWinded, racer.BoostCooldown),
				SpurText = SpurTextFor(racer.BoostActive, racer.BoostWinded, racer.BoostCooldown),
				Speed = SpeedOf(racer.Agent)
			});
		}
		list.Sort(delegate(RaceProgress a, RaceProgress b)
		{
			bool flag = a.Place > 0;
			bool flag2 = b.Place > 0;
			if (flag && flag2)
			{
				return a.Place.CompareTo(b.Place);
			}
			return (flag != flag2) ? ((!flag) ? 1 : (-1)) : b.Fraction.CompareTo(a.Fraction);
		});
		return list;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		if (_raceEnded || _phase == Phase.Finished)
		{
			canLeave = true;
			RevertRaceHorse();
			return null;
		}
		if (_playerFinished)
		{
			canLeave = true;
			_raceEnded = true;
			foreach (Racer racer in _racers)
			{
				if (!racer.Finished)
				{
					AwardRidingXp(racer.Hero, 1500);
				}
			}
			ReportResults();
			RevertRaceHorse();
			return null;
		}
		canLeave = true;
		return new InquiryData(new TextObject("{=homestead_race_forfeit_title}Leave the Race?").ToString(), new TextObject("{=homestead_race_forfeit_text}Leaving now will forfeit the match.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, new TextObject("{=homestead_race_forfeit_yes}Forfeit").ToString(), new TextObject("{=homestead_race_forfeit_no}Keep Racing").ToString(), delegate
		{
			_raceEnded = true;
			Banner(new TextObject("{=homestead_race_forfeited}You forfeited the race."));
			_phase = Phase.Finished;
			_endDelay = 0.5f;
		}, null);
	}

	public override void OnMissionTick(float dt)
	{
		switch (_phase)
		{
		case Phase.Loading:
			TickLoading(dt);
			break;
		case Phase.Countdown:
			TickCountdown(dt);
			break;
		case Phase.Racing:
			TickRacing(dt);
			break;
		case Phase.Finished:
			TickFinished(dt);
			break;
		}
		CheckFlagToggle();
	}

	private void CheckFlagToggle()
	{
		if (_gateMarkers.Count == 0)
		{
			return;
		}
		MCMSettings instance = GlobalSettings<MCMSettings>.Instance;
		if (instance == null || !base.Mission.InputManager.IsKeyPressed(instance.GetRaceFlagToggleKey()))
		{
			return;
		}
		_flagsHidden = !_flagsHidden;
		if (HomesteadBehavior.Instance != null)
		{
			HomesteadBehavior.Instance.RaceFlagsHidden = _flagsHidden;
		}
		foreach (GameEntity gateMarker in _gateMarkers)
		{
			ApplyFlagVisibility(gateMarker);
		}
	}

	private void ApplyFlagVisibility(GameEntity? m)
	{
		if (m == null)
		{
			return;
		}
		foreach (GameEntity entityAndChild in m.GetEntityAndChildren())
		{
			entityAndChild.SetVisibilityExcludeParents(!_flagsHidden);
			if (!_flagsHidden)
			{
				entityAndChild.SetReadyToRender(ready: true);
			}
		}
	}

	private void TickLoading(float dt)
	{
		if (_loadFrames++ < 2)
		{
			return;
		}
		if (!_trackPlaced)
		{
			try
			{
				TraceLogger.Write("HomesteadRaceMissionLogic", $"TickLoading: instantiating track '{_track.PrefabName}' at ({_track.Anchor.x:0.#},{_track.Anchor.y:0.#},{_track.Anchor.z:0.#}).");
				_trackEntity = Utils.CreateGameEntityWithPrefab(_track.PrefabName, _track.Anchor, Mat3.Identity);
				_trackPlaced = true;
				_settleFrames = 0;
				TraceLogger.Write("HomesteadRaceMissionLogic", "TickLoading: track instantiated.");
				return;
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadRaceMissionLogic", "Failed to instantiate track '" + _track.PrefabName + "': " + ex.Message);
				AbortRace("The race track could not be loaded.");
				return;
			}
		}
		if (_settleFrames++ < 5)
		{
			return;
		}
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("homestead_race_start").ToList();
		List<GameEntity> list2 = base.Mission.Scene.FindEntitiesWithTag("homestead_race_gate").ToList();
		if (list.Count == 0 || list2.Count == 0)
		{
			if (_settleFrames >= 200)
			{
				AbortRace("The race track has no start/finish or gates.");
			}
			return;
		}
		_startPos = list[0].GlobalPosition;
		_orderedGates.Clear();
		for (int i = 0; i < 999; i++)
		{
			List<GameEntity> list3 = base.Mission.Scene.FindEntitiesWithTag($"gate_{i:D3}").ToList();
			if (list3.Count <= 0)
			{
				break;
			}
			_orderedGates.Add(list3[0]);
		}
		if (_orderedGates.Count > 0)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", $"TickLoading: found {_orderedGates.Count} explicitly ordered gates; building AI path.");
			_checkpoints.Clear();
			_checkpoints.AddRange(_orderedGates.Select((GameEntity g) => g.GlobalPosition));
			_checkpoints.Add(_startPos);
		}
		else
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", $"TickLoading: markers found ({list2.Count} gates); nearest-neighbour sorting.");
			OrderGatesIntoLap(_startPos, list2);
		}
		BuildAiPath();
		TraceLogger.Write("HomesteadRaceMissionLogic", "TickLoading: spawning gate flags.");
		SpawnGateFlags();
		TraceLogger.Write("HomesteadRaceMissionLogic", "TickLoading: spawning decor flags.");
		SpawnDecorFlags();
		TraceLogger.Write("HomesteadRaceMissionLogic", "TickLoading: spawning scenery props.");
		SpawnSceneryProps();
		BoostRaceHorse();
		TraceLogger.Write("HomesteadRaceMissionLogic", "TickLoading: spawning player.");
		SpawnPlayerAtStart();
		if (_player == null)
		{
			AbortRace("Could not spawn the rider.");
			return;
		}
		TraceLogger.Write("HomesteadRaceMissionLogic", "TickLoading: spawning rivals.");
		SpawnAiRidersAtStart();
		_lap = 0;
		_checkpointIndex = 0;
		_countdown = 4f;
		_phase = Phase.Countdown;
		TraceLogger.Write("HomesteadRaceMissionLogic", $"Race ready: {_checkpoints.Count - 1} gates, start at ({_startPos.x:0.#},{_startPos.y:0.#},{_startPos.z:0.#}).");
	}

	private void SpawnDecorFlags()
	{
		try
		{
			foreach (GameEntity item in base.Mission.Scene.FindEntitiesWithTag("homestead_race_flag").ToList())
			{
				try
				{
					MatrixFrame globalFrame = item.GetGlobalFrame();
					GameEntity.Instantiate(base.Mission.Scene, "flag_rope_cloth_02", globalFrame);
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadRaceMissionLogic", "Flag instantiate failed: " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "SpawnDecorFlags failed: " + ex2.Message);
		}
	}

	private void SetMarkerColor(GameEntity marker, string materialName)
	{
		if (marker == null)
		{
			return;
		}
		try
		{
			Material fromResource = Material.GetFromResource(materialName);
			if (fromResource == null)
			{
				TraceLogger.Write("HomesteadRaceMissionLogic", "SetMarkerColor: material '" + materialName + "' not found — leaving flag as-is.");
				return;
			}
			foreach (GameEntity entityAndChild in marker.GetEntityAndChildren())
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
						meshAtIndex.SetMaterial(fromResource);
					}
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "SetMarkerColor failed: " + ex.Message);
		}
	}

	private void RefreshGateMarkers()
	{
		foreach (GameEntity gateMarker in _gateMarkers)
		{
			ApplyFlagVisibility(gateMarker);
			SetMarkerColor(gateMarker, "plain_red");
		}
	}

	private void SpawnGateFlags()
	{
		try
		{
			_gateMarkers.Clear();
			string text = GateFlagPrefabCandidates.FirstOrDefault(GameEntity.PrefabExists);
			TraceLogger.Write("HomesteadRaceMissionLogic", "SpawnGateFlags: marker prefab = '" + (text ?? "NONE FOUND") + "'");
			if (text == null)
			{
				return;
			}
			int num = ((_checkpoints.Count > 1) ? (_checkpoints.Count - 1) : _checkpoints.Count);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				try
				{
					Vec3 position = _checkpoints[i];
					Mat3 identity = Mat3.Identity;
					identity.ApplyScaleLocal(1.8f);
					GameEntity gameEntity = Utils.CreateGameEntityWithPrefab(text, position, identity, enablePhysics: false);
					ApplyFlagVisibility(gameEntity);
					SetMarkerColor(gameEntity, "plain_red");
					_gateMarkers.Add(gameEntity);
					if (num2 == 0)
					{
						TraceLogger.Write("HomesteadRaceMissionLogic", $"  first gate marker at ({position.x:0.#},{position.y:0.#},{position.z:0.#}) visible={gameEntity.IsVisibleIncludeParents()}");
					}
					num2++;
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadRaceMissionLogic", $"Gate flag[{i}] failed: {ex.Message}");
				}
			}
			TraceLogger.Write("HomesteadRaceMissionLogic", $"SpawnGateFlags: placed {num2} gate flag(s).");
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "SpawnGateFlags failed: " + ex2.Message);
		}
	}

	private void SpawnSceneryProps()
	{
		try
		{
			List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("homestead_race_prop").ToList();
			TraceLogger.Write("HomesteadRaceMissionLogic", $"SpawnSceneryProps: {list.Count} prop point(s).");
			int num = 0;
			foreach (GameEntity item in list)
			{
				try
				{
					string name = item.Name;
					if (!string.IsNullOrEmpty(name))
					{
						MatrixFrame globalFrame = item.GetGlobalFrame();
						Utils.CreateGameEntityWithPrefab(name, globalFrame.origin, globalFrame.rotation);
						num++;
					}
				}
				catch (Exception ex)
				{
					TraceLogger.Write("HomesteadRaceMissionLogic", "Scenery prop instantiate failed: " + ex.Message);
				}
			}
			TraceLogger.Write("HomesteadRaceMissionLogic", $"SpawnSceneryProps: placed {num} prop(s).");
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "SpawnSceneryProps failed: " + ex2.Message);
		}
	}

	private void OrderGatesIntoLap(Vec3 start, List<GameEntity> gates)
	{
		_checkpoints.Clear();
		_orderedGates.Clear();
		List<GameEntity> list = new List<GameEntity>(gates);
		Vec3 vec = start;
		while (list.Count > 0)
		{
			int index = 0;
			float num = float.MaxValue;
			for (int i = 0; i < list.Count; i++)
			{
				float lengthSquared = (list[i].GlobalPosition - vec).AsVec2.LengthSquared;
				if (lengthSquared < num)
				{
					num = lengthSquared;
					index = i;
				}
			}
			vec = list[index].GlobalPosition;
			_checkpoints.Add(vec);
			_orderedGates.Add(list[index]);
			list.RemoveAt(index);
		}
		if (_track.ReverseDirection)
		{
			_checkpoints.Reverse();
			_orderedGates.Reverse();
		}
		_checkpoints.Add(start);
	}

	private void BuildAiPath()
	{
		_aiPath.Clear();
		List<Vec3> list = new List<Vec3> { _startPos };
		for (int i = 0; i < _checkpoints.Count - 1; i++)
		{
			list.Add(_checkpoints[i]);
		}
		int count = list.Count;
		if (count < 3)
		{
			_aiPath.AddRange(list);
			return;
		}
		for (int j = 0; j < count; j++)
		{
			Vec3 p = list[(j - 1 + count) % count];
			Vec3 p2 = list[j];
			Vec3 p3 = list[(j + 1) % count];
			Vec3 p4 = list[(j + 2) % count];
			for (int k = 0; k < 6; k++)
			{
				_aiPath.Add(CatmullRom(p, p2, p3, p4, (float)k / 6f));
			}
		}
	}

	private static Vec3 CatmullRom(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, float t)
	{
		float num = t * t;
		float num2 = num * t;
		return (p1 * 2f + (p2 - p0) * t + (p0 * 2f - p1 * 5f + p2 * 4f - p3) * num + (p3 - p0 + (p1 - p2) * 3f) * num2) * 0.5f;
	}

	private static void MakeRaceRiderUnharmable(Agent? agent)
	{
		if (agent == null)
		{
			return;
		}
		try
		{
			agent.SetMortalityState(Agent.MortalityState.Invulnerable);
			agent.MountAgent?.SetMortalityState(Agent.MortalityState.Invulnerable);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "MakeRaceRiderUnharmable failed: " + ex.Message);
		}
	}

	private void SpawnPlayerAtStart()
	{
		try
		{
			Vec3 vec = ((_checkpoints.Count > 0) ? (_checkpoints[0] - _startPos) : new Vec3(0f, 1f));
			vec.z = 0f;
			Vec2 direction = ((vec.LengthSquared > 0.01f) ? vec.AsVec2.Normalized() : new Vec2(0f, 1f));
			Vec3 position = _startPos - new Vec3(direction.x, direction.y) * _track.SpawnBackOffset;
			position.z = base.Mission.Scene.GetGroundHeightAtPosition(position) + 0.1f;
			_playerColor = ColorForRacer(0);
			_playerLuck = RollLuck();
			AgentBuildData agentBuildData = new AgentBuildData(CharacterObject.PlayerCharacter).Team(base.Mission.PlayerTeam).InitialPosition(in position).InitialDirection(in direction)
				.TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, CharacterObject.PlayerCharacter))
				.Equipment(BuildMountedEquipment(Hero.MainHero))
				.ClothingColor1(_playerColor)
				.ClothingColor2(_playerColor)
				.CivilianEquipment(civilianEquipment: false)
				.NoHorses(noHorses: false)
				.Controller(AgentControllerType.Player);
			_player = base.Mission.SpawnAgent(agentBuildData);
			base.Mission.MainAgent = _player;
			MakeRaceRiderUnharmable(_player);
			_playerHoldPos = position;
			SetMovementFrozen(frozen: true);
			WieldBanner(_player, Hero.MainHero, _playerColor);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "SpawnPlayerAtStart failed: " + ex.GetType().Name + ": " + ex.Message);
			_player = null;
		}
	}

	private void SpawnAiRidersAtStart()
	{
		List<Hero> list = _rivalHeroes.Where((Hero h) => h?.IsAlive ?? false).Take(2).ToList();
		if (list.Count == 0)
		{
			list = PickRivalHeroes(2);
		}
		if (list.Count == 0)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "No rival heroes available — racing solo.");
			return;
		}
		Vec3 vec = ((_checkpoints.Count > 0) ? (_checkpoints[0] - _startPos) : new Vec3(0f, 1f));
		vec.z = 0f;
		Vec2 direction = ((vec.LengthSquared > 0.01f) ? vec.AsVec2.Normalized() : new Vec2(0f, 1f));
		Vec2 vec2 = new Vec2(0f - direction.y, direction.x);
		for (int num = 0; num < list.Count; num++)
		{
			try
			{
				Hero hero = list[num];
				Vec3 vec3;
				if (_track.SpawnBackOffset > 0f)
				{
					float num2 = _track.SpawnBackOffset + 1.5f * (float)(num + 1);
					float num3 = ((num % 2 == 0) ? 0.6f : (-0.6f));
					vec3 = _startPos - new Vec3(direction.x, direction.y) * num2 + new Vec3(vec2.x * num3, vec2.y * num3);
				}
				else
				{
					float num4 = 1.5f * (float)(num / 2 + 1);
					float num5 = ((num % 2 == 0) ? num4 : (0f - num4));
					vec3 = _startPos + new Vec3(vec2.x * num5, vec2.y * num5);
				}
				vec3.z = _playerHoldPos.z;
				uint num6 = ColorForRacer(num + 1);
				AgentBuildData agentBuildData = new AgentBuildData(hero.CharacterObject).Team(base.Mission.PlayerTeam).InitialPosition(in _playerHoldPos).InitialDirection(in direction)
					.TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, hero.CharacterObject))
					.Equipment(BuildMountedEquipment(hero))
					.ClothingColor1(num6)
					.ClothingColor2(num6)
					.CivilianEquipment(civilianEquipment: false)
					.NoHorses(noHorses: false)
					.Controller(AgentControllerType.AI);
				Agent agent = base.Mission.SpawnAgent(agentBuildData);
				MakeRaceRiderUnharmable(agent);
				try
				{
					(agent.MountAgent ?? agent).TeleportToPosition(vec3);
				}
				catch
				{
				}
				float speedFactor = CapForRiding(hero) * RollLuck();
				_racers.Add(new Racer
				{
					Agent = agent,
					Hero = hero,
					SpeedFactor = speedFactor,
					LastPos = vec3,
					Color = num6,
					HoldPos = vec3
				});
				WieldBanner(agent, hero, num6);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadRaceMissionLogic", $"SpawnAiRider[{num}] failed: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	private static List<Hero> PickRivalHeroes(int count)
	{
		Clan playerClan = Clan.PlayerClan;
		if (playerClan?.Heroes == null)
		{
			return new List<Hero>();
		}
		return playerClan.Heroes.Where((Hero h) => h != null && h != Hero.MainHero && h.IsAlive && !h.IsChild && !h.IsPrisoner && !h.IsDisabled).Take(count).ToList();
	}

	private static float CapForRiding(Hero? hero)
	{
		int num = hero?.GetSkillValue(DefaultSkills.Riding) ?? 100;
		return TaleWorlds.Library.MathF.Clamp(0.6f + (float)num * 0.0008f, 0.6f, 0.8f);
	}

	private static Equipment BuildMountedEquipment(Hero hero)
	{
		Equipment equipment = StripWeapons(hero.BattleEquipment ?? hero.CharacterObject.Equipment);
		ItemObject itemObject = FindRidingHorse();
		if (itemObject != null)
		{
			equipment[EquipmentIndex.ArmorItemEndSlot] = new EquipmentElement(itemObject);
			ItemObject itemObject2 = FindBasicHarness();
			equipment[EquipmentIndex.HorseHarness] = ((itemObject2 != null) ? new EquipmentElement(itemObject2) : default(EquipmentElement));
		}
		return equipment;
	}

	private static ItemObject? FindBannerItem()
	{
		if (_cachedBannerItem != null)
		{
			return _cachedBannerItem;
		}
		_cachedBannerItem = (Game.Current?.ObjectManager.GetObjectTypeList<ItemObject>())?.FirstOrDefault((ItemObject it) => it != null && it.IsBannerItem && it.StringId != "campaign_banner_small");
		return _cachedBannerItem;
	}

	private void WieldBanner(Agent agent, Hero? hero, uint riderColor)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return;
			}
			ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("campaign_banner_small");
			if (itemObject != null)
			{
				Banner banner = BuildRiderBanner(hero, riderColor);
				if (banner != null)
				{
					MissionWeapon weapon = new MissionWeapon(itemObject, null, banner);
					agent.EquipWeaponToExtraSlotAndWield(ref weapon);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "WieldBanner failed: " + ex.Message);
		}
	}

	private static Banner? BuildRiderBanner(Hero? hero, uint riderColor)
	{
		try
		{
			Banner banner = hero?.ClanBanner ?? TaleWorlds.Core.Banner.CreateRandomBanner();
			if (banner == null)
			{
				return null;
			}
			uint color = NearestPaletteColor(riderColor);
			uint color2 = NearestPaletteColor(uint.MaxValue);
			return new Banner(banner, color, color2);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "BuildRiderBanner failed: " + ex.Message);
			return null;
		}
	}

	private static uint NearestPaletteColor(uint argb)
	{
		MBReadOnlyDictionary<int, BannerColor> mBReadOnlyDictionary = BannerManager.Instance?.ReadOnlyColorPalette;
		if (mBReadOnlyDictionary == null)
		{
			return argb;
		}
		Color color = Color.FromUint(argb);
		uint result = argb;
		float num = float.MaxValue;
		foreach (KeyValuePair<int, BannerColor> item in mBReadOnlyDictionary)
		{
			Color color2 = Color.FromUint(item.Value.Color);
			float num2 = color2.Red - color.Red;
			float num3 = color2.Green - color.Green;
			float num4 = color2.Blue - color.Blue;
			float num5 = num2 * num2 + num3 * num3 + num4 * num4;
			if (num5 < num)
			{
				num = num5;
				result = item.Value.Color;
			}
		}
		return result;
	}

	private static Equipment StripWeapons(Equipment src)
	{
		Equipment equipment = src.Clone();
		for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
		{
			equipment[equipmentIndex] = default(EquipmentElement);
		}
		return equipment;
	}

	private static ItemObject? FindBasicHarness()
	{
		if (_cachedHarness != null)
		{
			return _cachedHarness;
		}
		MBReadOnlyList<ItemObject> mBReadOnlyList = Game.Current?.ObjectManager.GetObjectTypeList<ItemObject>();
		if (mBReadOnlyList == null)
		{
			return null;
		}
		_cachedHarness = (from it in mBReadOnlyList
			where it != null && it.ItemType == ItemObject.ItemTypeEnum.HorseHarness && !it.StringId.Contains("_load_")
			orderby it.Value
			select it).FirstOrDefault();
		return _cachedHarness;
	}

	private static ItemObject? FindRidingHorse()
	{
		if (_cachedRidingHorse != null)
		{
			return _cachedRidingHorse;
		}
		MBReadOnlyList<ItemObject> mBReadOnlyList = Game.Current?.ObjectManager.GetObjectTypeList<ItemObject>();
		if (mBReadOnlyList == null)
		{
			return null;
		}
		_cachedRidingHorse = mBReadOnlyList.Where(Rideable).OrderByDescending(Speed).FirstOrDefault();
		if (_cachedRidingHorse == null)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "FindRidingHorse: no rideable horse found — riders keep their own mounts.");
		}
		else
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", $"FindRidingHorse: using '{_cachedRidingHorse.StringId}' (speed {Speed(_cachedRidingHorse)}).");
		}
		return _cachedRidingHorse;
		static bool Rideable(ItemObject it)
		{
			if (it != null && it.HasHorseComponent && it.HorseComponent.IsRideable && !it.StringId.Contains("_load_"))
			{
				return it.ItemCategory != DefaultItemCategories.PackAnimal;
			}
			return false;
		}
		static int Speed(ItemObject it)
		{
			try
			{
				return new EquipmentElement(it).GetModifiedMountSpeed(in EquipmentElement.Invalid);
			}
			catch
			{
				return 0;
			}
		}
	}

	private bool IsStraightAhead(Racer r)
	{
		int count = _aiPath.Count;
		if (count < 8)
		{
			return true;
		}
		int checkpointIndex = r.CheckpointIndex;
		for (int i = 0; i < 10; i++)
		{
			Vec3 vec = _aiPath[(checkpointIndex + i) % count];
			Vec3 vec2 = _aiPath[(checkpointIndex + i + 2) % count];
			Vec3 vec3 = _aiPath[(checkpointIndex + i + 4) % count];
			Vec2 asVec = (vec2 - vec).AsVec2;
			Vec2 asVec2 = (vec3 - vec2).AsVec2;
			if (!(asVec.LengthSquared < 0.001f) && !(asVec2.LengthSquared < 0.001f))
			{
				asVec.Normalize();
				asVec2.Normalize();
				if (asVec.x * asVec2.x + asVec.y * asVec2.y < 0.85f)
				{
					return false;
				}
			}
		}
		return true;
	}

	private float CornerSpeedCeiling(Racer r)
	{
		int count = _aiPath.Count;
		if (count < 8)
		{
			return 3f;
		}
		int checkpointIndex = r.CheckpointIndex;
		float num = 1f;
		for (int i = 0; i < 10; i++)
		{
			Vec3 vec = _aiPath[(checkpointIndex + i) % count];
			Vec3 vec2 = _aiPath[(checkpointIndex + i + 2) % count];
			Vec3 vec3 = _aiPath[(checkpointIndex + i + 4) % count];
			Vec2 asVec = (vec2 - vec).AsVec2;
			Vec2 asVec2 = (vec3 - vec2).AsVec2;
			if (!(asVec.LengthSquared < 0.001f) && !(asVec2.LengthSquared < 0.001f))
			{
				asVec.Normalize();
				asVec2.Normalize();
				float num2 = asVec.x * asVec2.x + asVec.y * asVec2.y;
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		float x = TaleWorlds.Library.MathF.Clamp((num - 0.4f) / 0.6f, 0f, 1f);
		return 0.3f + TaleWorlds.Library.MathF.Pow(x, 3f) * 2.7f;
	}

	private Vec3 LastGatePosition(int nextCumulativeGateIndex)
	{
		if (nextCumulativeGateIndex > 0 && _checkpoints.Count > 0)
		{
			return _checkpoints[(nextCumulativeGateIndex - 1) % _checkpoints.Count];
		}
		return _startPos;
	}

	private int NearestAiPathIndex(Vec3 pos)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = 0; i < _aiPath.Count; i++)
		{
			float lengthSquared = (_aiPath[i] - pos).LengthSquared;
			if (lengthSquared < num)
			{
				num = lengthSquared;
				result = i;
			}
		}
		return result;
	}

	private void RecoverAiToLastGate(Racer r)
	{
		if (r.Agent != null && r.Agent.IsActive())
		{
			Agent agent = r.Agent.MountAgent ?? r.Agent;
			Vec3 vec = LastGatePosition(r.GateIndex);
			try
			{
				agent.TeleportToPosition(vec);
			}
			catch
			{
			}
			r.CheckpointIndex = NearestAiPathIndex(vec);
			r.MinWaypointDistSq = float.MaxValue;
			r.SinceGate = 0f;
			r.StuckTimer = 0f;
			r.OffTrackTimer = 0f;
		}
	}

	private void TickAiRiders(float dt)
	{
		if (_aiPath.Count == 0)
		{
			return;
		}
		foreach (Racer racer in _racers)
		{
			if (racer.Agent == null || !racer.Agent.IsActive() || racer.Finished)
			{
				continue;
			}
			racer.SinceGate += dt;
			if (racer.SinceGate >= 10f)
			{
				RecoverAiToLastGate(racer);
				continue;
			}
			if (racer.BoostActive > 0f)
			{
				racer.BoostActive -= dt;
				if (racer.BoostActive <= 0f)
				{
					racer.BoostWinded = 3f;
				}
			}
			else if (racer.BoostWinded > 0f)
			{
				racer.BoostWinded -= dt;
				if (racer.BoostWinded <= 0f)
				{
					racer.BoostCooldown = 24f + MBRandom.RandomFloatRanged(0f, 5f);
				}
			}
			else if (racer.BoostCooldown > 0f)
			{
				racer.BoostCooldown -= dt;
			}
			else if (IsStraightAhead(racer) && MBRandom.RandomFloat < 0.012f)
			{
				racer.BoostActive = 6f;
			}
			float num = ((racer.BoostActive > 0f) ? 2.8f : ((racer.BoostWinded > 0f) ? 0.75f : 1f));
			if (racer.HobbleTimer > 0f)
			{
				racer.HobbleTimer -= dt;
				num *= 0.4f;
			}
			float num2 = racer.SpeedFactor * num;
			RaceTrack track = _track;
			float fraction = ((track != null && track.CarefulCorners) ? TaleWorlds.Library.MathF.Min(num2, CornerSpeedCeiling(racer)) : num2);
			ApplyMountSpeedCap(racer.Agent, fraction);
			bool flag = _track != null && _track.AiWaypointRadius > 0f;
			float num3 = (flag ? _track.AiWaypointRadius : 5f);
			int num4 = 0;
			while (num4++ < _aiPath.Count)
			{
				Vec3 vec = _aiPath[racer.CheckpointIndex % _aiPath.Count];
				Vec3 vec2 = _aiPath[(racer.CheckpointIndex + 1) % _aiPath.Count];
				if (flag)
				{
					Vec2 asVec = (racer.Agent.Position - vec).AsVec2;
					float lengthSquared = asVec.LengthSquared;
					if (lengthSquared < racer.MinWaypointDistSq)
					{
						racer.MinWaypointDistSq = lengthSquared;
					}
					float num5 = num3 * 2f * (num3 * 2f);
					bool num6 = lengthSquared <= num3 * num3;
					bool flag2 = racer.MinWaypointDistSq <= num5 && lengthSquared > racer.MinWaypointDistSq * 1.3f;
					Vec2 asVec2 = (vec2 - vec).AsVec2;
					float lengthSquared2 = asVec2.LengthSquared;
					bool flag3 = lengthSquared2 > 0.001f && asVec.x * asVec2.x + asVec.y * asVec2.y >= lengthSquared2;
					if (!(num6 || flag2 || flag3))
					{
						break;
					}
					racer.CheckpointIndex++;
					racer.MinWaypointDistSq = float.MaxValue;
				}
				else
				{
					float lengthSquared3 = (racer.Agent.Position - vec).AsVec2.LengthSquared;
					float lengthSquared4 = (racer.Agent.Position - vec2).AsVec2.LengthSquared;
					if (!(lengthSquared3 <= num3 * num3) && !(lengthSquared4 < lengthSquared3))
					{
						break;
					}
					racer.CheckpointIndex++;
				}
			}
			Vec3 vec3 = _aiPath[racer.CheckpointIndex % _aiPath.Count];
			WorldPosition position = new WorldPosition(base.Mission.Scene, vec3);
			racer.Agent.SetScriptedPosition(ref position, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.GoToPosition | Agent.AIScriptedFrameFlags.NeverSlowDown);
			if ((racer.Agent.Position - vec3).LengthSquared > 1225f)
			{
				racer.OffTrackTimer += dt;
				if (racer.OffTrackTimer >= 3f)
				{
					Agent agent = racer.Agent.MountAgent ?? racer.Agent;
					try
					{
						agent.TeleportToPosition(vec3);
					}
					catch
					{
					}
					racer.OffTrackTimer = 0f;
					racer.StuckTimer = 0f;
				}
			}
			else
			{
				racer.OffTrackTimer = 0f;
			}
			if ((racer.Agent.Position - racer.LastPos).LengthSquared < 0.25f)
			{
				racer.StuckTimer += dt;
				if (racer.StuckTimer > 4f)
				{
					racer.CheckpointIndex++;
					racer.StuckTimer = 0f;
					racer.MinWaypointDistSq = float.MaxValue;
				}
			}
			else
			{
				racer.StuckTimer = 0f;
			}
			racer.LastPos = racer.Agent.Position;
			if (_checkpoints.Count == 0)
			{
				continue;
			}
			Vec3 vec4 = _checkpoints[racer.GateIndex % _checkpoints.Count];
			if ((racer.Agent.Position - vec4).AsVec2.LengthSquared > GateRadius * GateRadius)
			{
				continue;
			}
			racer.SinceGate = 0f;
			racer.GateIndex++;
			if (racer.GateIndex % _checkpoints.Count == 0)
			{
				float num7 = _raceTime - racer.LapStart;
				if (num7 < racer.BestLap)
				{
					racer.BestLap = num7;
				}
				racer.LapStart = _raceTime;
				racer.Lap++;
				if (racer.Lap >= RaceLaps)
				{
					OnRacerFinished(racer);
				}
			}
		}
	}

	private void OnRacerFinished(Racer r)
	{
		r.Finished = true;
		r.FinishTime = _raceTime;
		_finishPlace++;
		r.FinishPlace = _finishPlace;
		AwardRidingXp(r.Hero, 1500 + ((_finishPlace == 1) ? 1000 : 0));
		string variable = r.Hero?.Name?.ToString() ?? "A rival";
		Banner(new TextObject("{=homestead_race_ai_finish}{RIDER} finishes in place {PLACE}!").SetTextVariable("RIDER", variable).SetTextVariable("PLACE", _finishPlace));
	}

	private void ReportResults()
	{
		if (_resultsRecorded)
		{
			return;
		}
		_resultsRecorded = true;
		List<(string name, string id, int bestLapCs, int totalCs, bool finished)> list = new List<(string name, string id, int bestLapCs, int totalCs, bool finished)>();
		if (Hero.MainHero != null)
		{
			list.Add((Hero.MainHero.Name.ToString(), Hero.MainHero.StringId, (_playerBestLap < float.MaxValue) ? ((int)(_playerBestLap * 100f)) : (-1), (int)(_playerFinishTime * 100f), true));
		}
		foreach (Racer racer in _racers)
		{
			if (racer.Hero != null)
			{
				list.Add((racer.Hero.Name.ToString(), racer.Hero.StringId, (racer.BestLap < float.MaxValue) ? ((int)(racer.BestLap * 100f)) : (-1), racer.Finished ? ((int)(racer.FinishTime * 100f)) : (-1), racer.Finished));
			}
		}
		try
		{
			HomesteadBehavior.Instance?.RecordRaceResults(_track.Id, list);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "RecordRaceResults failed: " + ex.Message);
		}
		try
		{
			List<(string, string, int, int, bool)> ordered = (from r in list
				orderby r.finished descending, (!r.finished) ? int.MaxValue : r.totalCs
				select r).Select<(string, string, int, int, bool), (string, string, int, int, bool)>(((string name, string id, int bestLapCs, int totalCs, bool finished) r, int i) => (name: r.name, id: r.id, placeIndex: i, totalCs: r.totalCs, finished: r.finished)).ToList();
			HomesteadBehavior.RaiseRaceFinished(_track.Id, ordered, _homestead);
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "RaiseRaceFinished failed: " + ex2.Message);
		}
		try
		{
			Hero hero = _homestead?.StableMasterHero;
			if (hero != null && hero.IsAlive)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, 1);
			}
		}
		catch (Exception ex3)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "Race relation gain failed: " + ex3.Message);
		}
		if (_homestead != null)
		{
			return;
		}
		foreach (Racer racer2 in _racers)
		{
			Hero hero2 = racer2.Hero;
			if (hero2 != null && hero2.IsAlive && hero2 != Hero.MainHero && hero2.Clan != Clan.PlayerClan)
			{
				try
				{
					ChangeRelationAction.ApplyPlayerRelation(hero2, 1);
				}
				catch (Exception ex4)
				{
					TraceLogger.Write("HomesteadRaceMissionLogic", "Arena race relation gain failed: " + ex4.Message);
				}
			}
		}
	}

	private static void AwardRidingXp(Hero? hero, int amount)
	{
		if (hero?.HeroDeveloper == null || amount <= 0)
		{
			return;
		}
		try
		{
			hero.HeroDeveloper.AddSkillXp(DefaultSkills.Riding, amount);
		}
		catch
		{
		}
	}

	private void TickCountdown(float dt)
	{
		if (dt > 0.1f)
		{
			dt = 0.1f;
		}
		SetMovementFrozen(frozen: true);
		_countdown -= dt;
		if (_countdown <= 0f)
		{
			SetMovementFrozen(frozen: false);
			_goTextTimer = 1.5f;
			_aiReleaseTimer = 1f;
			RefreshGateMarkers();
			_phase = Phase.Racing;
		}
	}

	public string GetCountdownText()
	{
		if (_phase == Phase.Countdown)
		{
			int num = TaleWorlds.Library.MathF.Ceiling(_countdown);
			if (num < 1 || num > 3)
			{
				return "";
			}
			return num.ToString();
		}
		if (_phase == Phase.Racing && _goTextTimer > 0f)
		{
			return new TextObject("{=homestead_race_go}Go!").ToString();
		}
		return "";
	}

	private void SetMovementFrozen(bool frozen)
	{
		HoldAgent(_player, frozen);
		foreach (Racer racer in _racers)
		{
			HoldAgent(racer.Agent, frozen);
		}
		if (!frozen)
		{
			return;
		}
		if (_player != null && _player.IsActive())
		{
			Agent agent = _player.MountAgent ?? _player;
			try
			{
				agent.TeleportToPosition(_playerHoldPos);
			}
			catch
			{
			}
		}
		foreach (Racer racer2 in _racers)
		{
			if (racer2.Agent != null && racer2.Agent.IsActive() && racer2.HoldPos.IsValid)
			{
				Agent agent2 = racer2.Agent.MountAgent ?? racer2.Agent;
				try
				{
					agent2.TeleportToPosition(racer2.HoldPos);
				}
				catch
				{
				}
			}
		}
	}

	private static void HoldAgent(Agent? agent, bool frozen)
	{
		if (agent != null && agent.IsActive())
		{
			float maximumSpeedLimit = (frozen ? 0.001f : (-1f));
			agent.SetMaximumSpeedLimit(maximumSpeedLimit, isMultiplier: false);
			agent.MountAgent?.SetMaximumSpeedLimit(maximumSpeedLimit, isMultiplier: false);
		}
	}

	private static void ApplyMountSpeedCap(Agent? rider, float fraction)
	{
		if (rider != null && rider.IsActive())
		{
			float maximumSpeedLimit = ((fraction >= 1f) ? (-1f) : fraction);
			bool isMultiplier = fraction < 1f;
			rider.SetMaximumSpeedLimit(maximumSpeedLimit, isMultiplier);
			Agent mountAgent = rider.MountAgent;
			if (mountAgent != null && mountAgent.IsActive())
			{
				mountAgent.SetMaximumSpeedLimit(maximumSpeedLimit, isMultiplier);
			}
		}
	}

	private void UpdateOffTrack(float dt)
	{
		if (_player == null || !_player.IsActive() || _aiPath.Count == 0)
		{
			_offTrackTimer = 0f;
			_showRecoverPrompt = false;
			return;
		}
		_playerSinceGate += dt;
		Vec3 position = _player.Position;
		float num = float.MaxValue;
		foreach (Vec3 item in _aiPath)
		{
			float lengthSquared = (position - item).LengthSquared;
			if (lengthSquared < num)
			{
				num = lengthSquared;
			}
		}
		if (num <= 1225f)
		{
			_offTrackTimer = 0f;
		}
		else
		{
			_offTrackTimer += dt;
		}
		bool flag = _playerSinceGate >= 10f;
		_showRecoverPrompt = _offTrackTimer >= 3f || flag;
		if (_showRecoverPrompt && base.Mission.InputManager.IsKeyPressed(InputKey.X))
		{
			RecoverToTrack();
		}
	}

	private void RecoverToTrack()
	{
		if (_player != null && _player.IsActive())
		{
			Agent agent = _player.MountAgent ?? _player;
			try
			{
				agent.TeleportToPosition(LastGatePosition(_checkpointIndex));
			}
			catch
			{
			}
			_offTrackTimer = 0f;
			_playerSinceGate = 0f;
			_showRecoverPrompt = false;
			Banner(new TextObject("{=homestead_race_recovered_gate}Back on the track at your last gate."));
		}
	}

	public string GetRecoverPrompt()
	{
		if (!_showRecoverPrompt)
		{
			return "";
		}
		return new TextObject("{=homestead_race_recover_prompt_gate}Stuck or off the track?  Press X to return to your last gate.").ToString();
	}

	private void UpdatePlayerBoost(float dt)
	{
		if (_boostActive > 0f)
		{
			_boostActive -= dt;
			if (_boostActive <= 0f)
			{
				_boostWinded = 3f;
			}
		}
		else if (_boostWinded > 0f)
		{
			_boostWinded -= dt;
			if (_boostWinded <= 0f)
			{
				_boostCooldown = 24f;
			}
		}
		else if (_boostCooldown > 0f)
		{
			_boostCooldown -= dt;
		}
		else if (base.Mission.InputManager.IsKeyPressed(InputKey.Q) || (Input.IsGamepadActive && base.Mission.InputManager.IsKeyPressed(InputKey.ControllerRThumb)))
		{
			_boostActive = 6f;
			Banner(new TextObject("{=homestead_race_spur}Spur!"));
			PlayHorseWhinny();
		}
	}

	private void PlayHorseWhinny()
	{
		if (_player == null || !_player.IsActive())
		{
			return;
		}
		Agent agent = _player.MountAgent ?? _player;
		int eventIdFromString = SoundEvent.GetEventIdFromString(WhinnyEvents[MBRandom.RandomInt(WhinnyEvents.Length)]);
		if (eventIdFromString == -1)
		{
			return;
		}
		try
		{
			Mission.Current.MakeSound(eventIdFromString, agent.Position, soundCanBePredicted: false, isReliable: true, agent.Index, -1);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "PlayHorseWhinny failed: " + ex.Message);
		}
	}

	private float PlayerBoostMult()
	{
		if (!(_boostActive > 0f))
		{
			if (!(_boostWinded > 0f))
			{
				return 1f;
			}
			return 0.75f;
		}
		return 2.8f;
	}

	public string GetBoostStatus()
	{
		if (_phase != Phase.Racing || _playerFinished || _player == null || !_player.IsActive())
		{
			return "";
		}
		if (_boostActive > 0f || _boostWinded > 0f || _boostCooldown > 0f)
		{
			return "";
		}
		if (_lap > 0 || _checkpointIndex > 1)
		{
			return "";
		}
		string text = GlobalSettings<MCMSettings>.Instance?.GetRaceFlagToggleKey().ToString() ?? "\\";
		if (text == "BackSlash")
		{
			text = "\\";
		}
		return new TextObject("{=homestead_race_spur_ready}Spur ready — press Q (Press {FLAG_KEY} to toggle flags)").SetTextVariable("FLAG_KEY", text).ToString();
	}

	private void CheckDogCheat(float dt)
	{
		if (_dogCheatCooldown > 0f)
		{
			_dogCheatCooldown -= dt;
		}
		if (!NativeConfig.CheatMode || _dogCheatCooldown > 0f || _player == null || !_player.IsActive())
		{
			return;
		}
		MCMSettings instance = GlobalSettings<MCMSettings>.Instance;
		HomesteadCompanionDogMissionLogic missionBehavior = base.Mission.GetMissionBehavior<HomesteadCompanionDogMissionLogic>();
		if (instance == null || missionBehavior?.CompanionDog == null || !base.Mission.InputManager.IsKeyPressed(instance.GetSicEmKey()))
		{
			return;
		}
		Racer racer = null;
		float num = 324f;
		Vec3 position = _player.Position;
		foreach (Racer racer2 in _racers)
		{
			if (racer2.Agent != null && racer2.Agent.IsActive() && !racer2.Finished && racer2.Agent.HasMount)
			{
				float lengthSquared = (racer2.Agent.Position - position).LengthSquared;
				if (lengthSquared < num)
				{
					num = lengthSquared;
					racer = racer2;
				}
			}
		}
		if (racer?.Agent != null)
		{
			_dogCheatCooldown = 5f;
			missionBehavior.ForceSicEm(racer.Agent, HobbleMount);
			InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_race_sic_em}Sic 'em!"), new Color(1f, 0.78f, 0.1f)));
		}
	}

	public void HobbleMount(Agent rider)
	{
		foreach (Racer racer in _racers)
		{
			if (racer.Agent == rider)
			{
				racer.HobbleTimer = 5f;
				InformationManager.DisplayMessage(new InformationMessage((racer.Hero?.FirstName?.ToString() ?? "A rival") + "'s horse is hobbled!", new Color(1f, 0.6f, 0.2f)));
				break;
			}
		}
	}

	private void TickRacing(float dt)
	{
		if ((_player == null || !_player.IsActive()) && !_playerFinished)
		{
			AbortRace("The rider left the track.");
			return;
		}
		_raceTime += dt;
		if (_goTextTimer > 0f)
		{
			_goTextTimer -= dt;
		}
		if (_aiReleaseTimer > 0f)
		{
			_aiReleaseTimer -= dt;
			if (_aiReleaseTimer > 0f)
			{
				foreach (Racer racer in _racers)
				{
					HoldAgent(racer.Agent, frozen: true);
				}
			}
			else
			{
				foreach (Racer racer2 in _racers)
				{
					HoldAgent(racer2.Agent, frozen: false);
				}
			}
		}
		if (_aiReleaseTimer <= 0f)
		{
			TickAiRiders(dt);
		}
		if (!_playerFinished && _player != null && _player.IsActive())
		{
			UpdatePlayerBoost(dt);
			CheckDogCheat(dt);
			ApplyMountSpeedCap(_player, CapForRiding(Hero.MainHero) * _playerLuck * PlayerBoostMult());
			TickPlayerProgress();
			UpdateOffTrack(dt);
		}
		else if (_playerFinished)
		{
			_postFinishTimer += dt;
			ApplyMountSpeedCap(_player, 1f);
		}
		CheckRaceEnd();
	}

	private void TickPlayerProgress()
	{
		if (_checkpointIndex >= _checkpoints.Count || (_player.Position - _checkpoints[_checkpointIndex]).AsVec2.LengthSquared > GateRadius * GateRadius)
		{
			return;
		}
		if (_checkpointIndex < _gateMarkers.Count)
		{
			SetMarkerColor(_gateMarkers[_checkpointIndex], "plain_green");
		}
		_playerSinceGate = 0f;
		_checkpointIndex++;
		if (_checkpointIndex < _checkpoints.Count)
		{
			return;
		}
		float num = _raceTime - _playerLapStart;
		if (num < _playerBestLap)
		{
			_playerBestLap = num;
		}
		_playerLapStart = _raceTime;
		_lap++;
		if (_lap >= RaceLaps)
		{
			OnPlayerFinished();
			return;
		}
		_checkpointIndex = 0;
		Banner(new TextObject("{=homestead_race_lap}Lap {DONE} of {TOTAL}!").SetTextVariable("DONE", _lap).SetTextVariable("TOTAL", RaceLaps));
		foreach (GameEntity gateMarker in _gateMarkers)
		{
			SetMarkerColor(gateMarker, "plain_red");
		}
	}

	private void OnPlayerFinished()
	{
		_playerFinished = true;
		_playerFinishTime = _raceTime;
		int num = _finishPlace + 1;
		_finishPlace++;
		_playerFinishPlace = num;
		AwardRidingXp(Hero.MainHero, 1500 + ((num == 1) ? 1000 : 0));
		try
		{
			if (_player != null && _player.IsActive())
			{
				_player.MakeVoice(SkinVoiceManager.VoiceType.Victory, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "Victory voice failed: " + ex.Message);
		}
		int num2 = (int)_raceTime;
		Banner(((num == 1) ? new TextObject("{=homestead_race_win}1st place! Time: {M}m {S}s") : new TextObject("{=homestead_race_place}You finish in place {PLACE}. Time: {M}m {S}s").SetTextVariable("PLACE", num)).SetTextVariable("M", num2 / 60).SetTextVariable("S", num2 % 60));
		TraceLogger.Write("HomesteadRaceMissionLogic", $"Player finished in {_raceTime:0.0}s (place {num}); waiting for rivals.");
	}

	private void CheckRaceEnd()
	{
		if (_raceEnded || !_playerFinished)
		{
			return;
		}
		bool num = _racers.All((Racer r) => r.Finished || r.Agent == null || !r.Agent.IsActive());
		float num2 = TaleWorlds.Library.MathF.Max(45f, _playerFinishTime);
		if (!num && _postFinishTimer < num2)
		{
			return;
		}
		_raceEnded = true;
		foreach (Racer racer in _racers)
		{
			if (!racer.Finished)
			{
				AwardRidingXp(racer.Hero, 1500);
			}
		}
		ReportResults();
		Banner(new TextObject("{=homestead_race_all_in}All racers are in. Good race!"));
		_phase = Phase.Finished;
		_endDelay = 5f;
	}

	private void TickFinished(float dt)
	{
		_endDelay -= dt;
		if (_endDelay <= 0f)
		{
			RevertRaceHorse();
			try
			{
				base.Mission.EndMission();
			}
			catch
			{
			}
			_phase = Phase.Loading;
		}
	}

	private void BoostRaceHorse()
	{
		try
		{
			ItemObject itemObject = FindRidingHorse();
			HorseComponent horseComponent = itemObject?.HorseComponent;
			if (horseComponent != null)
			{
				MethodInfo methodInfo = typeof(HorseComponent).GetProperty("Speed")?.GetSetMethod(nonPublic: true);
				if (!(methodInfo == null))
				{
					_boostedHorseItem = itemObject;
					_origHorseSpeed = horseComponent.Speed;
					RaceTrack track = _track;
					float num = ((track != null && track.SpeedScale > 0f) ? _track.SpeedScale : 1f);
					methodInfo.Invoke(horseComponent, new object[1] { (int)((float)_origHorseSpeed * 1.7f * num) });
					TraceLogger.Write("HomesteadRaceMissionLogic", $"BoostRaceHorse: '{itemObject.StringId}' Speed {_origHorseSpeed} -> {horseComponent.Speed}.");
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "BoostRaceHorse failed: " + ex.Message);
		}
	}

	private void RevertRaceHorse()
	{
		try
		{
			if (_boostedHorseItem != null && _origHorseSpeed >= 0)
			{
				HorseComponent horseComponent = _boostedHorseItem.HorseComponent;
				(typeof(HorseComponent).GetProperty("Speed")?.GetSetMethod(nonPublic: true))?.Invoke(horseComponent, new object[1] { _origHorseSpeed });
				TraceLogger.Write("HomesteadRaceMissionLogic", $"RevertRaceHorse: '{_boostedHorseItem.StringId}' Speed restored to {_origHorseSpeed}.");
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadRaceMissionLogic", "RevertRaceHorse failed: " + ex.Message);
		}
		finally
		{
			_boostedHorseItem = null;
			_origHorseSpeed = -1;
		}
	}

	private void AbortRace(string reason)
	{
		_phase = Phase.Finished;
		_endDelay = 3f;
		Banner(new TextObject(reason));
		TraceLogger.Write("HomesteadRaceMissionLogic", "Race aborted: " + reason);
	}

	private static void Banner(TextObject t)
	{
		MBInformationManager.AddQuickInformation(t);
	}
}

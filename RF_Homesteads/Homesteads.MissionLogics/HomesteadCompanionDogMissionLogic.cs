using System;
using System.Collections.Generic;
using Homesteads.Models;
using Homesteads.Patches;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadCompanionDogMissionLogic : MissionLogic
{
	private bool _hasSpawnedDog;

	private Agent? _companionDog;

	private Agent? _forcedTarget;

	private Action<Agent>? _forcedOnContact;

	private readonly Dictionary<Agent, ActionIndexCache> _dogAnims = new Dictionary<Agent, ActionIndexCache>();

	private readonly HashSet<Agent> _climbingDogs = new HashSet<Agent>();

	private float _biteCooldownTimer;

	private const float BiteCooldown = 1.5f;

	private const int CompanionDogBiteDamage = 25;

	private bool _sicEmMode;

	private bool _returningToPlayer;

	private const float ReturnHomeDist = 1.5f;

	private bool _pendingOrderBark;

	private float _petCooldownTimer;

	private const float PetRange = 3f;

	private const float PetFacingDot = 0.45f;

	private const float PetCooldown = 2f;

	private Agent? _petHintDog;

	private float _petHintCooldownTimer;

	private const float PetHintCooldown = 15f;

	private readonly List<Agent> _allCombatDogs = new List<Agent>();

	private bool _hasActiveEnemies;

	private float _enemyScanCooldown;

	private const float EnemyScanInterval = 0.5f;

	private const float FollowBehindDist = 2.5f;

	private const float FollowBehindDistClimb = 1f;

	private const float FollowStopDist = 0.6f;

	private const float FollowSpeed = 2.5f;

	private const float FollowSideAmp = 0.5f;

	private const float OscillationPeriod = 6f;

	private const float WanderTriggerSec = 5f;

	private const float WanderRadius = 4f;

	private const float WanderMinDist = 2f;

	private const float WanderPauseMin = 2f;

	private const float WanderPauseMax = 4.5f;

	private bool _playerIsActuallyMoving;

	private float _playerStationaryTimer;

	private bool _dogIsWandering;

	private Vec3 _wanderTarget;

	private bool _hasWanderTarget;

	private float _wanderPauseRemaining;

	private float _oscillationTimer;

	private Vec3 _lastFollowTarget;

	private bool _hasLastFollowTarget;

	private bool _dogSettled;

	private const float TrailSampleDist = 0.45f;

	private const float TrailReachDist = 0.55f;

	private const int TrailMaxCrumbs = 32;

	private readonly List<Vec3> _playerTrail = new List<Vec3>();

	private const float ClimbStuckTime = 0.9f;

	private const float ClimbStuckStepEps = 0.015f;

	private float _climbStuckTimer;

	private Vec3 _prevClimbDogPos;

	private readonly Dictionary<Agent, float> _combatStuckTimer = new Dictionary<Agent, float>();

	private readonly Dictionary<Agent, Vec3> _prevCombatDogPos = new Dictionary<Agent, Vec3>();

	private int _calmFollowState;

	private bool _tavernDogSettled;

	private Vec3 _tavernDogSettlePos = Vec3.Invalid;

	private bool _companionDownedHandled;

	private const float PaceAmp = 9.2f;

	private const float PacePeriod = 22f;

	private const float PaceSpeed = 0.3f;

	private float _paceTimer;

	private const float CombatScatterSpacing = 1.2f;

	private const float CombatScatterDepthA = 2f;

	private const float CombatScatterDepthB = 3.2f;

	private const float CombatScatterStopDist = 0.7f;

	private const float PostCombatHoldSpeed = 3.5f;

	public Agent? CompanionDog
	{
		get
		{
			if (_companionDog == null || !_companionDog.IsActive() || !(_companionDog.Health > 0f))
			{
				return null;
			}
			return _companionDog;
		}
	}

	private bool IsCalmFollowMission
	{
		get
		{
			if (_calmFollowState == 0)
			{
				_calmFollowState = ((Mission.Current?.GetMissionBehavior<HomesteadTavernMissionLogic>() != null) ? 1 : 2);
			}
			return _calmFollowState == 1;
		}
	}

	public void ForceSicEm(Agent target, Action<Agent>? onContact = null)
	{
		if (CompanionDog != null)
		{
			_forcedTarget = target;
			_forcedOnContact = onContact;
		}
	}

	private bool UpdateForcedTarget(float dt)
	{
		if (_forcedTarget == null)
		{
			return false;
		}
		Agent companionDog = CompanionDog;
		if (companionDog == null || !_forcedTarget.IsActive())
		{
			_forcedTarget = null;
			_forcedOnContact = null;
			return false;
		}
		Vec3 position = _forcedTarget.Position;
		float length = (companionDog.Position - position).Length;
		if (length <= 2.5f)
		{
			Agent forcedTarget = _forcedTarget;
			Action<Agent> forcedOnContact = _forcedOnContact;
			_forcedTarget = null;
			_forcedOnContact = null;
			try
			{
				companionDog.SetActionChannel(0, in DogAgentUtils.ActionJump, ignorePriority: true, (AnimFlags)0uL);
				DogAgentUtils.PlayBark(companionDog);
				forcedOnContact?.Invoke(forcedTarget);
			}
			catch (Exception ex)
			{
				TraceLogger.Write("HomesteadCompanionDogMissionLogic", "Forced sic-em contact failed: " + ex.Message);
			}
			return true;
		}
		float speed = 11.25f;
		bool allowAiPathing = length > 4f;
		bool flag = DogAgentUtils.MoveToTarget(companionDog, position, 1.6f, dt, speed, snapToGround: true, allowAiPathing);
		if (companionDog.Controller == AgentControllerType.AI)
		{
			_dogAnims.Remove(companionDog);
		}
		else if (flag)
		{
			SetDogAnim(companionDog, DogAgentUtils.AnimForSpeed(speed));
			companionDog.MovementInputVector = new Vec2(0f, 1f);
		}
		return true;
	}

	public override void OnEndMissionInternal()
	{
		base.OnEndMissionInternal();
		DogAgentUtils.ResetPathingState();
		_dogAnims.Clear();
		_climbingDogs.Clear();
		_playerTrail.Clear();
		_combatStuckTimer.Clear();
		_prevCombatDogPos.Clear();
	}

	private void SetDogAnim(Agent dog, ActionIndexCache desired)
	{
		if (!_dogAnims.TryGetValue(dog, out var value) || value.Index != desired.Index)
		{
			_dogAnims[dog] = desired;
			dog.SetActionChannel(0, in desired, ignorePriority: true, (AnimFlags)0uL);
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!_hasSpawnedDog)
		{
			Agent mainAgent = Mission.Current.MainAgent;
			if (mainAgent == null)
			{
				return;
			}
			if (HomesteadMissionStateOpenNewPatch.IsArenaMissionName(Mission.Current.Scene?.GetName()))
			{
				_hasSpawnedDog = true;
				return;
			}
			if (Mission.Current.GetMissionBehavior<HomesteadSparringMissionLogic>() != null)
			{
				_hasSpawnedDog = true;
				return;
			}
			if (Mission.Current.Mode == MissionMode.Battle)
			{
				HomesteadBattleSceneMissionLogic missionBehavior = Mission.Current.GetMissionBehavior<HomesteadBattleSceneMissionLogic>();
				if (missionBehavior != null && !missionBehavior.HasRepositionedPlayers)
				{
					return;
				}
			}
			SpawnCompanionDog(mainAgent);
			_hasSpawnedDog = true;
		}
		if (HomesteadMissionStateOpenNewPatch.IsArenaMissionName(Mission.Current.Scene?.GetName()) || UpdateForcedTarget(dt))
		{
			return;
		}
		if (IsCombatMission(dt))
		{
			UpdateDogCombatAI(dt);
			return;
		}
		TryPetDogInteraction(dt);
		if (_companionDog != null && _companionDog.IsActive())
		{
			if (IsCalmFollowMission)
			{
				UpdateDogTavernSettle(dt);
			}
			else
			{
				UpdateDogFollowAI(dt);
			}
		}
	}

	private void UpdateDogTavernSettle(float dt)
	{
		Agent companionDog = _companionDog;
		if (!_tavernDogSettlePos.IsValid)
		{
			_tavernDogSettlePos = companionDog.Position;
		}
		if (_tavernDogSettled)
		{
			HoldStill(companionDog);
			SetDogAnim(companionDog, DogAgentUtils.ActionIdle);
			return;
		}
		if ((companionDog.Position - _tavernDogSettlePos).AsVec2.Length <= 0.6f)
		{
			_tavernDogSettled = true;
			HoldStill(companionDog);
			SetDogAnim(companionDog, DogAgentUtils.ActionIdle);
			return;
		}
		bool flag = DogAgentUtils.MoveToTarget(companionDog, _tavernDogSettlePos, 0.6f, dt, 2.5f, snapToGround: false);
		if (companionDog.Controller == AgentControllerType.AI)
		{
			_dogAnims.Remove(companionDog);
		}
		else if (flag)
		{
			SetDogAnim(companionDog, DogAgentUtils.ActionWalk);
			companionDog.MovementInputVector = new Vec2(0f, 1f);
		}
		else
		{
			SetDogAnim(companionDog, DogAgentUtils.ActionIdle);
			companionDog.MovementInputVector = Vec2.Zero;
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent != null && affectedAgent == _companionDog)
		{
			HandleCompanionDogDowned();
		}
	}

	private void HandleCompanionDogDowned()
	{
		if (!_companionDownedHandled)
		{
			_companionDownedHandled = true;
			string text = HomesteadBehavior.Instance?.AdoptedDogName;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = new TextObject("{=hr_companion_dog_fallback_name}Your dog").ToString();
			}
			TextObject textObject = new TextObject("{=hr_companion_dog_downed}{DOG_NAME} has fallen in battle, but will recover and rejoin you afterward.");
			textObject.SetTextVariable("DOG_NAME", text);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(1f, 0.6f, 0.3f)));
			_sicEmMode = false;
			if (_companionDog != null)
			{
				_dogAnims.Remove(_companionDog);
				_climbingDogs.Remove(_companionDog);
				_allCombatDogs.Remove(_companionDog);
			}
			_companionDog = null;
		}
	}

	private bool IsCombatMission(float dt)
	{
		switch (Mission.Current.Mode)
		{
		case MissionMode.Battle:
			return true;
		case MissionMode.Conversation:
		case MissionMode.CutScene:
			return false;
		default:
		{
			_enemyScanCooldown -= dt;
			if (_enemyScanCooldown > 0f)
			{
				return _hasActiveEnemies;
			}
			_enemyScanCooldown = 0.5f;
			Team playerTeam = Mission.Current.PlayerTeam;
			if (playerTeam == null)
			{
				_hasActiveEnemies = false;
				return false;
			}
			try
			{
				foreach (Agent agent in Mission.Current.Agents)
				{
					if (agent != null && agent.IsActive() && agent.IsHuman && agent.Team != null && agent.Team.IsValid && agent.Team.IsEnemyOf(playerTeam))
					{
						_hasActiveEnemies = true;
						return true;
					}
				}
			}
			catch (Exception)
			{
			}
			_hasActiveEnemies = false;
			return false;
		}
		}
	}

	private void SpawnCompanionDog(Agent player)
	{
		HomesteadBehavior instance = HomesteadBehavior.Instance;
		if (instance == null || !instance.HasAdoptedDog)
		{
			return;
		}
		HomesteadSpawningMissionLogic.EnsureDogItemUsesCorrectMonster();
		HomesteadSpawningMissionLogic.EnsureCompanionDogVariantItemsRegistered();
		int adoptedDogMaterialIndex = HomesteadBehavior.Instance.AdoptedDogMaterialIndex;
		string text = $"homestead_companion_dog_{adoptedDogMaterialIndex}";
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>(text) ?? Game.Current.ObjectManager.GetObject<ItemObject>("dog");
		if (itemObject == null)
		{
			return;
		}
		ItemRosterElement rosterElement = new ItemRosterElement(itemObject);
		Vec3 position = player.Position;
		Vec3 vec = new Vec3(MBRandom.RandomFloat * 2f - 1f, MBRandom.RandomFloat * 2f - 1f);
		Vec3 initialPosition = position + vec;
		initialPosition.z = player.Position.z;
		Agent agent;
		try
		{
			agent = Mission.Current.SpawnMonster(rosterElement, default(ItemRosterElement), in initialPosition, in Vec2.Forward);
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadCompanionDogMissionLogic", "SpawnCompanionDog: Mission.SpawnMonster threw " + ex.GetType().Name + ": " + ex.Message + " — companion dog not spawned this mission.");
			return;
		}
		if (agent == null)
		{
			return;
		}
		agent.Controller = AgentControllerType.None;
		for (int i = 0; i < 3; i++)
		{
			agent.AgentVisuals?.GetSkeleton()?.TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), tickAnimsForChildren: true);
		}
		if (HomesteadBehavior.Instance != null && HomesteadBehavior.Instance.HasHoundmasterKnockdownUnlocked)
		{
			agent.BaseHealthLimit = 200f;
			agent.Health = 200f;
			if (agent.AgentDrivenProperties != null)
			{
				agent.AgentDrivenProperties.ArmorTorso = 30f;
				agent.AgentDrivenProperties.ArmorHead = 30f;
				agent.AgentDrivenProperties.ArmorLegs = 30f;
				agent.AgentDrivenProperties.ArmorArms = 30f;
			}
		}
		_companionDog = agent;
		_companionDownedHandled = false;
		TraceLogger.Write("HomesteadCompanionDogMissionLogic", $"SpawnCompanionDog: '{HomesteadBehavior.Instance.AdoptedDogName}' spawned (coat {text}) at {initialPosition}");
	}

	private void TryPetDogInteraction(float dt)
	{
		if (_petCooldownTimer > 0f)
		{
			_petCooldownTimer -= dt;
		}
		if (_petHintCooldownTimer > 0f)
		{
			_petHintCooldownTimer -= dt;
		}
		if (HomesteadFreeCameraView.Instance != null && HomesteadFreeCameraView.Instance.IsActive)
		{
			_petHintDog = null;
			return;
		}
		if (Mission.Current.Mode == MissionMode.Conversation)
		{
			_petHintDog = null;
			return;
		}
		MCMSettings instance = GlobalSettings<MCMSettings>.Instance;
		if (instance == null)
		{
			return;
		}
		Agent mainAgent = Mission.Current.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			_petHintDog = null;
			return;
		}
		Agent agent = FindNearbyFacedDog(mainAgent, 3f, 0.45f);
		if (agent == null)
		{
			_petHintDog = null;
		}
		else if (agent != _petHintDog)
		{
			_petHintDog = agent;
			if (_petHintCooldownTimer <= 0f)
			{
				_petHintCooldownTimer = 15f;
				TextObject textObject = new TextObject("{=hr_pet_dog_hint}Press {KEY} to pet the dog.");
				textObject.SetTextVariable("KEY", instance.GetPetDogKeyLabel());
				InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.6f, 0.85f, 1f)));
			}
		}
		if (agent != null && base.Mission.InputManager.IsKeyPressed(instance.GetPetDogKey()) && !(_petCooldownTimer > 0f))
		{
			_petCooldownTimer = 2f;
			DogAgentUtils.PlayBark(agent);
			string variable = ((agent != _companionDog) ? null : HomesteadBehavior.Instance?.AdoptedDogName) ?? "The dog";
			Color color = new Color(0.6f, 0.85f, 1f);
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=hr_pet_dog_player}You give the dog a scratch behind the ears. \"Good boy!\"").ToString(), color));
			TextObject textObject2 = new TextObject("{=hr_pet_dog_react}{DOG} barks happily and wags its tail!");
			textObject2.SetTextVariable("DOG", variable);
			InformationManager.DisplayMessage(new InformationMessage(textObject2.ToString(), color));
		}
	}

	private static Agent? FindNearbyFacedDog(Agent player, float range, float facingDot)
	{
		Vec3 lookDirection = player.LookDirection;
		Vec2 vec = new Vec2(lookDirection.x, lookDirection.y);
		if (vec.LengthSquared < 0.0001f)
		{
			return null;
		}
		vec = vec.Normalized();
		Vec3 position = player.Position;
		Agent result = null;
		float num = range;
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != null && agent.IsActive() && agent.Monster != null && !(agent.Monster.StringId != "dog"))
			{
				Vec3 vec2 = agent.Position - position;
				Vec2 vec3 = new Vec2(vec2.x, vec2.y);
				float length = vec3.Length;
				if (!(length > num) && (!(length > 0.01f) || !(Vec2.DotProduct(vec, vec3 * (1f / length)) < facingDot)))
				{
					result = agent;
					num = length;
				}
			}
		}
		return result;
	}

	private void UpdateDogFollowAI(float dt)
	{
		Agent mainAgent = Mission.Current.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			return;
		}
		Vec3 position = mainAgent.Position;
		bool flag = (_playerIsActuallyMoving = mainAgent.MovementInputVector.LengthSquared > 0.01f);
		if (flag)
		{
			_playerStationaryTimer = 0f;
			_dogIsWandering = false;
			_hasWanderTarget = false;
			_wanderPauseRemaining = 0f;
			_dogSettled = false;
			if (_playerTrail.Count == 0 || (_playerTrail[_playerTrail.Count - 1] - position).AsVec2.Length >= 0.45f)
			{
				_playerTrail.Add(position);
				if (_playerTrail.Count > 32)
				{
					_playerTrail.RemoveAt(0);
				}
			}
		}
		else
		{
			_playerStationaryTimer += dt;
		}
		_oscillationTimer += dt;
		bool flag2 = (flag ? UpdateFollowBehavior(mainAgent, dt) : UpdateLateralPace(mainAgent, dt));
		if (_companionDog.Controller == AgentControllerType.AI)
		{
			_dogAnims.Remove(_companionDog);
			_climbingDogs.Remove(_companionDog);
		}
		else if (flag2)
		{
			float num = (_dogIsWandering ? 0f : Math.Abs(_lastFollowTarget.z - _companionDog.Position.z));
			if (!IsCalmFollowMission && num > 0.25f)
			{
				if (_climbingDogs.Add(_companionDog))
				{
					SetDogAnim(_companionDog, DogAgentUtils.ActionJump);
				}
			}
			else
			{
				_climbingDogs.Remove(_companionDog);
				SetDogAnim(_companionDog, DogAgentUtils.ActionWalk);
			}
			_companionDog.MovementInputVector = new Vec2(0f, 1f);
		}
		else
		{
			if (_dogAnims.TryGetValue(_companionDog, out var value) && value.Index != DogAgentUtils.ActionIdle.Index)
			{
				_climbingDogs.Remove(_companionDog);
				SetDogAnim(_companionDog, DogAgentUtils.ActionIdle);
			}
			_companionDog.MovementInputVector = Vec2.Zero;
		}
	}

	private Vec3 ComputeFollowTarget(Agent player)
	{
		Vec3 f = player.Frame.rotation.f;
		Vec2 vec = new Vec2(f.x, f.y);
		if (vec.LengthSquared < 0.0001f)
		{
			Vec3 u = player.Frame.rotation.u;
			vec = new Vec2(u.x, u.y);
			if (f.z > 0f)
			{
				vec = new Vec2(0f - vec.x, 0f - vec.y);
			}
		}
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		vec = vec.Normalized();
		Vec3 vec2 = new Vec3(vec.x, vec.y);
		Vec3 vec3 = new Vec3(vec2.y, 0f - vec2.x);
		int num;
		float num2;
		if (!IsCalmFollowMission && _companionDog != null)
		{
			num = ((Math.Abs(player.Position.z - _companionDog.Position.z) > 0.25f) ? 1 : 0);
			if (num != 0)
			{
				num2 = 1f;
				goto IL_0127;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 2.5f;
		goto IL_0127;
		IL_0127:
		float num3 = num2;
		float num4 = ((num != 0) ? 0f : ((float)Math.Sin((double)_oscillationTimer * (Math.PI / 3.0)) * 0.5f));
		Vec3 result = player.Position - vec2 * num3 + vec3 * num4;
		result.z = player.Position.z;
		return result;
	}

	private bool UpdateLateralPace(Agent player, float dt)
	{
		Agent? companionDog = _companionDog;
		_paceTimer += dt;
		Vec3 f = player.Frame.rotation.f;
		Vec2 vec = new Vec2(f.x, f.y);
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		vec = vec.Normalized();
		Vec3 vec2 = new Vec3(vec.x, vec.y);
		Vec3 vec3 = new Vec3(vec2.y, 0f - vec2.x);
		float num = (float)Math.Sin((double)_paceTimer * (Math.PI / 11.0)) * 9.2f;
		Vec3 vec4 = player.Position - vec2 * 2.5f + vec3 * num;
		vec4.z = player.Position.z;
		_lastFollowTarget = vec4;
		bool result = DogAgentUtils.MoveToTarget(companionDog, vec4, 0.25f, dt, 0.3f, snapToGround: true, allowAiPathing: false);
		float num2 = (float)Math.Cos((double)_paceTimer * (Math.PI / 11.0));
		Vec3 targetDirection = vec3 * ((num2 >= 0f) ? 1f : (-1f));
		companionDog.SetTargetPositionAndDirection((companionDog.Position + targetDirection).AsVec2, in targetDirection);
		return result;
	}

	private bool UpdateFollowBehavior(Agent player, float dt)
	{
		Agent companionDog = _companionDog;
		if (_playerIsActuallyMoving || !_hasLastFollowTarget)
		{
			_lastFollowTarget = ComputeFollowTarget(player);
			_hasLastFollowTarget = true;
		}
		bool flag = !IsCalmFollowMission && Math.Abs(player.Position.z - companionDog.Position.z) > 0.25f;
		Vec3 vec = _lastFollowTarget;
		bool flag2 = false;
		if (flag)
		{
			while (_playerTrail.Count > 0 && (_playerTrail[0] - companionDog.Position).AsVec2.Length <= 0.55f)
			{
				_playerTrail.RemoveAt(0);
			}
			if (_playerTrail.Count > 0)
			{
				vec = _playerTrail[0];
				flag2 = true;
			}
			else
			{
				vec = player.Position;
				flag2 = true;
			}
		}
		if (_dogSettled)
		{
			HoldStill(companionDog);
			return false;
		}
		float length = (companionDog.Position - vec).AsVec2.Length;
		bool flag3 = Math.Abs(companionDog.Position.z - vec.z) > 0.1f;
		if (!_playerIsActuallyMoving && !flag && length <= 0.90000004f)
		{
			_dogSettled = true;
			_playerTrail.Clear();
			HoldStill(companionDog);
			return false;
		}
		ActionIndexCache value;
		float num = ((_dogAnims.TryGetValue(companionDog, out value) && value.Index != DogAgentUtils.ActionIdle.Index) ? 0.6f : 1.6f);
		float stopDistance = ((flag2 || flag3) ? 0f : num);
		bool result = DogAgentUtils.MoveToTarget(companionDog, vec, stopDistance, dt, 2.5f, snapToGround: false);
		if (flag)
		{
			if ((companionDog.Position - _prevClimbDogPos).Length > 0.015f)
			{
				_climbStuckTimer = 0f;
			}
			else
			{
				_climbStuckTimer += dt;
			}
			if (_climbStuckTimer >= 0.9f)
			{
				companionDog.TeleportToPosition(_lastFollowTarget);
				SetDogAnim(companionDog, DogAgentUtils.ActionJump);
				_playerTrail.Clear();
				_climbStuckTimer = 0f;
				result = true;
			}
			_prevClimbDogPos = companionDog.Position;
		}
		else
		{
			_climbStuckTimer = 0f;
			_prevClimbDogPos = companionDog.Position;
		}
		return result;
	}

	private static void HoldStill(Agent dog)
	{
		if (dog.Controller != AgentControllerType.None)
		{
			dog.DisableScriptedMovement();
			dog.Controller = AgentControllerType.None;
		}
		dog.MovementInputVector = Vec2.Zero;
	}

	private bool UpdateWanderBehavior(Vec3 playerPos, float dt)
	{
		if (_wanderPauseRemaining > 0f)
		{
			_wanderPauseRemaining -= dt;
			return false;
		}
		if (!_hasWanderTarget)
		{
			float num = MBRandom.RandomFloat * (TaleWorlds.Library.MathF.PI * 2f);
			float num2 = 2f + MBRandom.RandomFloat * 2f;
			_wanderTarget = new Vec3(playerPos.x + (float)Math.Cos(num) * num2, playerPos.y + (float)Math.Sin(num) * num2, playerPos.z);
			_hasWanderTarget = true;
		}
		ActionIndexCache value;
		float stopDistance = ((_dogAnims.TryGetValue(_companionDog, out value) && value.Index != DogAgentUtils.ActionIdle.Index) ? 0.5f : 0.8f);
		bool num3 = DogAgentUtils.MoveToTarget(_companionDog, _wanderTarget, stopDistance, dt, 2.5f, snapToGround: false);
		if (!num3)
		{
			_hasWanderTarget = false;
			_wanderPauseRemaining = 2f + MBRandom.RandomFloat * 2.5f;
		}
		return num3;
	}

	private Vec3 ComputeCombatScatterTarget(Agent player, int dogIndex, int totalDogs)
	{
		Vec3 f = player.Frame.rotation.f;
		Vec2 vec = new Vec2(f.x, f.y);
		if (vec.LengthSquared < 0.0001f)
		{
			Vec3 u = player.Frame.rotation.u;
			vec = new Vec2(u.x, u.y);
			if (f.z > 0f)
			{
				vec = new Vec2(0f - vec.x, 0f - vec.y);
			}
		}
		if (vec.LengthSquared < 0.0001f)
		{
			vec = new Vec2(0f, 1f);
		}
		vec = vec.Normalized();
		Vec3 vec2 = new Vec3(vec.x, vec.y);
		Vec3 vec3 = new Vec3(vec2.y, 0f - vec2.x);
		float num = ((totalDogs > 1) ? (((float)dogIndex - (float)(totalDogs - 1) * 0.5f) * 1.2f) : 0f);
		float num2 = ((dogIndex % 2 == 0) ? 2f : 3.2f);
		Vec3 result = player.Position - vec2 * num2 + vec3 * num;
		result.z = player.Position.z;
		return result;
	}

	private void RefreshCombatDogs()
	{
		for (int num = _allCombatDogs.Count - 1; num >= 0; num--)
		{
			Agent agent = _allCombatDogs[num];
			if (agent == null || !agent.IsActive() || agent.Health <= 0f)
			{
				bool num2 = agent != null && agent == _companionDog;
				_dogAnims.Remove(agent);
				_climbingDogs.Remove(agent);
				if (agent != null)
				{
					_combatStuckTimer.Remove(agent);
					_prevCombatDogPos.Remove(agent);
				}
				_allCombatDogs.RemoveAt(num);
				if (num2)
				{
					HandleCompanionDogDowned();
				}
			}
		}
		if (_companionDog != null && _companionDog.IsActive() && _companionDog.Health > 0f && !_allCombatDogs.Contains(_companionDog))
		{
			_allCombatDogs.Add(_companionDog);
		}
		HomesteadDogCombatLogic homesteadDogCombatLogic = Mission.Current?.GetMissionBehavior<HomesteadDogCombatLogic>();
		if (homesteadDogCombatLogic == null)
		{
			return;
		}
		foreach (Agent combatDog in homesteadDogCombatLogic.CombatDogs)
		{
			if (combatDog != null && combatDog.IsActive() && combatDog.Health > 0f && !_allCombatDogs.Contains(combatDog))
			{
				_allCombatDogs.Add(combatDog);
			}
		}
	}

	private void UpdateDogCombatAI(float dt)
	{
		Agent mainAgent = Mission.Current.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			return;
		}
		RefreshCombatDogs();
		if (_allCombatDogs.Count == 0)
		{
			_sicEmMode = false;
			_returningToPlayer = false;
			return;
		}
		bool flag = Mission.Current.MissionResult != null && Mission.Current.MissionResult.BattleResolved;
		if (flag && _sicEmMode)
		{
			_sicEmMode = false;
			_returningToPlayer = true;
			_pendingOrderBark = true;
		}
		MCMSettings instance = GlobalSettings<MCMSettings>.Instance;
		if (!flag && instance != null && (base.Mission.InputManager.IsKeyPressed(instance.GetSicEmKey()) || (Input.IsGamepadActive && base.Mission.InputManager.IsKeyPressed(InputKey.ControllerRThumb))))
		{
			_sicEmMode = !_sicEmMode;
			_returningToPlayer = !_sicEmMode;
			string text = ((_companionDog == null || !_companionDog.IsActive() || !(_companionDog.Health > 0f)) ? "" : (HomesteadBehavior.Instance?.AdoptedDogName ?? ""));
			string text2 = (string.IsNullOrWhiteSpace(text) ? "Dogs" : text);
			string information = (_sicEmMode ? (text2 + ": Sic 'em!") : (text2 + ": Stand down."));
			Color color = (_sicEmMode ? new Color(1f, 0.78f, 0.1f) : new Color(0.75f, 0.75f, 0.75f));
			InformationManager.DisplayMessage(new InformationMessage(information, color));
			_pendingOrderBark = true;
		}
		if (_pendingOrderBark)
		{
			_pendingOrderBark = false;
			DogAgentUtils.PlayBark(_allCombatDogs[0]);
		}
		_biteCooldownTimer -= dt;
		bool flag2 = _biteCooldownTimer <= 0f;
		float aggroRange = (_sicEmMode ? 1000f : 6f);
		int count = _allCombatDogs.Count;
		bool flag3 = false;
		bool flag4 = true;
		for (int i = 0; i < count; i++)
		{
			Agent agent = _allCombatDogs[i];
			float nearestDistSq = float.MaxValue;
			Agent agent2 = ((_returningToPlayer || flag) ? null : DogAgentUtils.FindNearestEnemy(agent, aggroRange, out nearestDistSq));
			if (agent2 != null)
			{
				flag3 = true;
			}
			if (flag2 && agent2 != null && nearestDistSq <= 4f)
			{
				int damage = ((agent == _companionDog) ? 25 : 10);
				DogAgentUtils.BiteEnemy(agent, agent2, damage);
			}
			Vec3 vec;
			float num;
			if (agent2 != null)
			{
				vec = agent2.Position;
				num = 1.6f;
			}
			else
			{
				vec = ComputeCombatScatterTarget(mainAgent, i, count);
				num = 0.7f;
			}
			if (_returningToPlayer && (agent.Position - vec).AsVec2.Length > 1.5f)
			{
				flag4 = false;
			}
			float speed = ((agent2 == null) ? ((Mission.Current.MissionResult != null && Mission.Current.MissionResult.BattleResolved) ? 2.5f : 3.5f) : (_sicEmMode ? 11.25f : 9f));
			bool allowAiPathing = _sicEmMode && agent2 != null && nearestDistSq > 16f;
			ActionIndexCache value;
			float stopDistance = ((_dogAnims.TryGetValue(agent, out value) && value.Index != DogAgentUtils.ActionIdle.Index) ? num : (num + 0.3f));
			bool flag5 = DogAgentUtils.MoveToTarget(agent, vec, stopDistance, dt, speed, snapToGround: true, allowAiPathing);
			if (agent.Controller != AgentControllerType.AI)
			{
				if (Math.Abs(vec.z - agent.Position.z) > 0.25f)
				{
					Vec3 value2;
					Vec3 vec2 = (_prevCombatDogPos.TryGetValue(agent, out value2) ? value2 : agent.Position);
					float length = (agent.Position - vec2).Length;
					float num2 = (_combatStuckTimer.TryGetValue(agent, out var value3) ? value3 : 0f);
					num2 = ((length > 0.015f) ? 0f : (num2 + dt));
					if (num2 >= 0.9f)
					{
						Vec3 vec3 = agent.Position - vec;
						vec3.z = 0f;
						if (vec3.Length > 0.01f)
						{
							vec3.Normalize();
						}
						else
						{
							vec3 = new Vec3(0f, 1f);
						}
						Vec3 position = vec + vec3 * 1.2f;
						agent.TeleportToPosition(position);
						SetDogAnim(agent, DogAgentUtils.ActionJump);
						num2 = 0f;
						flag5 = true;
					}
					_combatStuckTimer[agent] = num2;
				}
				else
				{
					_combatStuckTimer[agent] = 0f;
				}
				_prevCombatDogPos[agent] = agent.Position;
			}
			else
			{
				_combatStuckTimer[agent] = 0f;
				_prevCombatDogPos[agent] = agent.Position;
			}
			ActionIndexCache value4;
			if (agent.Controller == AgentControllerType.AI)
			{
				_dogAnims.Remove(agent);
			}
			else if (flag5)
			{
				SetDogAnim(agent, DogAgentUtils.AnimForSpeed(speed));
				agent.MovementInputVector = new Vec2(0f, 1f);
			}
			else if (_dogAnims.TryGetValue(agent, out value4) && value4.Index != DogAgentUtils.ActionIdle.Index)
			{
				SetDogAnim(agent, DogAgentUtils.ActionIdle);
				agent.MovementInputVector = Vec2.Zero;
			}
		}
		_oscillationTimer += dt;
		if (_returningToPlayer && flag4)
		{
			_returningToPlayer = false;
		}
		if (_sicEmMode && !flag3)
		{
			_sicEmMode = false;
		}
		if (flag2)
		{
			_biteCooldownTimer = 1.5f;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Homesteads.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadTrainingFieldMissionLogic : MissionBehavior
{
	private readonly Homestead? _homestead;

	private Team? _trainingTeam1;

	private Team? _trainingTeam2;

	private Agent? _fighter1;

	private Agent? _fighter2;

	private Agent? _fighter3;

	private Agent? _fighter4;

	private GameEntity? _spawnPoint1;

	private GameEntity? _spawnPoint2;

	private GameEntity? _spawnPoint3;

	private GameEntity? _spawnPoint4;

	private float _resetTimer = -1f;

	private const float ResetDelay = 5f;

	private bool _fightersSpawned;

	private readonly TrainingMatchOverlay _overlay = new TrainingMatchOverlay();

	private bool _featuredMatch;

	private bool _pendingFeaturedMatch;

	private bool _pendingPlayerBout;

	private float _overlayLinger = -1f;

	private bool _boutActive;

	private Agent? _boutChallenger;

	private Team? _sparTeamPlayer;

	private Team? _sparTeamChallenger;

	private Team? _playerOriginalTeam;

	private float _playerStartHealth;

	private float _boutTimer;

	private const float BoutMaxDuration = 240f;

	private const float OverlayLingerTime = 5f;

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public bool CanHostMatch
	{
		get
		{
			if (_homestead != null)
			{
				return _homestead.HasTrainingFieldBuilding;
			}
			return true;
		}
	}

	public HomesteadTrainingFieldMissionLogic(Homestead? homestead = null)
	{
		_homestead = homestead;
	}

	public void RequestFeaturedMatch()
	{
		_pendingFeaturedMatch = true;
	}

	public void RequestPlayerBout()
	{
		_pendingPlayerBout = true;
	}

	public override void AfterStart()
	{
	}

	private void SpawnFighters()
	{
		if (!(_spawnPoint1 == null) && !(_spawnPoint2 == null) && !(_spawnPoint3 == null) && !(_spawnPoint4 == null) && _trainingTeam1 != null && _trainingTeam2 != null)
		{
			_fighter1 = SpawnFighter(_spawnPoint1, _trainingTeam1);
			_fighter3 = SpawnFighter(_spawnPoint3, _trainingTeam1);
			_fighter2 = SpawnFighter(_spawnPoint2, _trainingTeam2);
			_fighter4 = SpawnFighter(_spawnPoint4, _trainingTeam2);
		}
	}

	private Agent SpawnFighter(GameEntity spawnEntity, Team team)
	{
		return SpawnFighterCore(spawnEntity.GlobalPosition, spawnEntity.GetGlobalFrame().rotation.f.AsVec2, team, 200f, 1.5f);
	}

	private Agent SpawnFighterCore(Vec3 position, Vec2 direction, Team team, float health, float? speedLimit)
	{
		AgentBuildData agentBuildData = new AgentBuildData(Game.Current.ObjectManager.GetObject<CharacterObject>("imperial_recruit")).Team(team).InitialPosition(in position).InitialDirection(in direction)
			.NoHorses(noHorses: true)
			.FixedEquipment(fixedEquipment: true)
			.ClothingColor1(team.Color)
			.ClothingColor2(team.Color2)
			.Controller(AgentControllerType.AI);
		ItemObject itemObject = Game.Current.ObjectManager.GetObject<ItemObject>("empire_sword_1_t2_blunt");
		ItemObject itemObject2 = Game.Current.ObjectManager.GetObject<ItemObject>("oval_shield");
		Equipment equipment = new Equipment();
		if (itemObject != null)
		{
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(itemObject));
		}
		if (itemObject2 != null)
		{
			equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(itemObject2));
		}
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("ragged_robes")));
		equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(Game.Current.ObjectManager.GetObject<ItemObject>("ragged_boots")));
		agentBuildData.Equipment(equipment);
		Agent agent = base.Mission.SpawnAgent(agentBuildData);
		agent.TryToWieldWeaponInSlot(EquipmentIndex.WeaponItemBeginSlot, Agent.WeaponWieldActionType.Instant, isWieldedOnSpawn: false);
		agent.SetWatchState(Agent.WatchState.Alarmed);
		if (speedLimit.HasValue)
		{
			agent.SetMaximumSpeedLimit(speedLimit.Value, isMultiplier: false);
		}
		agent.HealthLimit = health;
		agent.Health = health;
		return agent;
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent != _fighter1 && affectedAgent != _fighter2 && affectedAgent != _fighter3 && affectedAgent != _fighter4)
		{
			return;
		}
		bool flag = (_fighter1 == null || !_fighter1.IsActive()) && (_fighter3 == null || !_fighter3.IsActive());
		bool flag2 = (_fighter2 == null || !_fighter2.IsActive()) && (_fighter4 == null || !_fighter4.IsActive());
		if (!(flag || flag2) || !(_resetTimer < 0f))
		{
			return;
		}
		_resetTimer = 5f;
		Color color = new Color(0.2f, 0.6f, 1f);
		string text = ((flag && flag2) ? Utils.GetLocalizedString("{=homestead_spar_draw}Training Match: Draw!") : ((!flag) ? Utils.GetLocalizedString("{=homestead_spar_red_wins}Training Match: Red Team Wins!") : Utils.GetLocalizedString("{=homestead_spar_blue_wins}Training Match: Blue Team Wins!")));
		InformationManager.DisplayMessage(new InformationMessage(Utils.GetLocalizedString("{=homestead_spar_resetting}{RESULT} Resetting match...", ("RESULT", text)), color));
		if (_featuredMatch)
		{
			_featuredMatch = false;
			if (_overlay.IsShown)
			{
				_overlay.SetText(text);
				_overlayLinger = 5f;
			}
		}
		Agent[] array = new Agent[4] { _fighter1, _fighter2, _fighter3, _fighter4 };
		foreach (Agent agent in array)
		{
			if (agent != null && agent.IsActive())
			{
				agent.SetActionChannel(1, ActionIndexCache.Create("act_cheer_1"), ignorePriority: false, (AnimFlags)0uL);
			}
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (base.Mission.Mode != MissionMode.Conversation)
		{
			if (_pendingFeaturedMatch)
			{
				_pendingFeaturedMatch = false;
				StartFeaturedMatch();
			}
			if (_pendingPlayerBout)
			{
				_pendingPlayerBout = false;
				StartPlayerBout();
			}
		}
		UpdatePlayerBout(dt);
		UpdateOverlay(dt);
		bool flag = _homestead == null || _homestead.HasTrainingFieldBuilding;
		if (!_fightersSpawned && _resetTimer < 0f && flag)
		{
			List<GameEntity> list = (from e in base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_npc")
				where e.Name.StartsWith("training_spawn_") || e.Name.StartsWith("sp_training_spawn_")
				select e).ToList();
			if (list.Count >= 4)
			{
				_spawnPoint1 = list.FirstOrDefault((GameEntity e) => e.Name == "sp_training_spawn_1" || e.Name == "training_spawn_1");
				_spawnPoint2 = list.FirstOrDefault((GameEntity e) => e.Name == "sp_training_spawn_2" || e.Name == "training_spawn_2");
				_spawnPoint3 = list.FirstOrDefault((GameEntity e) => e.Name == "sp_training_spawn_3" || e.Name == "training_spawn_3");
				_spawnPoint4 = list.FirstOrDefault((GameEntity e) => e.Name == "sp_training_spawn_4" || e.Name == "training_spawn_4");
				if (_spawnPoint1 != null && _spawnPoint2 != null && _spawnPoint3 != null && _spawnPoint4 != null)
				{
					if (_trainingTeam1 == null)
					{
						_trainingTeam1 = base.Mission.Teams.Add(BattleSideEnum.Defender, 4294901760u, 4294901760u);
					}
					if (_trainingTeam2 == null)
					{
						_trainingTeam2 = base.Mission.Teams.Add(BattleSideEnum.Attacker, 4278190335u, 4278190335u);
					}
					_trainingTeam1.SetIsEnemyOf(_trainingTeam2, isEnemyOf: true);
					_trainingTeam2.SetIsEnemyOf(_trainingTeam1, isEnemyOf: true);
					if (base.Mission.PlayerTeam != null)
					{
						_trainingTeam1.SetIsEnemyOf(base.Mission.PlayerTeam, isEnemyOf: false);
						_trainingTeam2.SetIsEnemyOf(base.Mission.PlayerTeam, isEnemyOf: false);
						base.Mission.PlayerTeam.SetIsEnemyOf(_trainingTeam1, isEnemyOf: false);
						base.Mission.PlayerTeam.SetIsEnemyOf(_trainingTeam2, isEnemyOf: false);
					}
					SpawnFighters();
					_fightersSpawned = true;
				}
			}
		}
		else if (_fightersSpawned)
		{
			if ((from e in base.Mission.Scene.FindEntitiesWithTag("spawnpoint_homestead_npc")
				where e.Name.StartsWith("training_spawn_") || e.Name.StartsWith("sp_training_spawn_")
				select e).ToList().Count < 4)
			{
				_fightersSpawned = false;
				_fighter1 = null;
				_fighter2 = null;
				_fighter3 = null;
				_fighter4 = null;
				_spawnPoint1 = null;
				_spawnPoint2 = null;
				_spawnPoint3 = null;
				_spawnPoint4 = null;
			}
			else if (_spawnPoint1 != null && _spawnPoint2 != null && _spawnPoint3 != null && _spawnPoint4 != null)
			{
				Vec3 vec = (_spawnPoint1.GlobalPosition + _spawnPoint2.GlobalPosition + _spawnPoint3.GlobalPosition + _spawnPoint4.GlobalPosition) * 0.25f;
				if (_fighter1 != null && _fighter1.IsActive() && (_fighter1.Position - vec).LengthSquared > 56.25f)
				{
					_fighter1.TeleportToPosition(_spawnPoint1.GlobalPosition);
				}
				if (_fighter2 != null && _fighter2.IsActive() && (_fighter2.Position - vec).LengthSquared > 56.25f)
				{
					_fighter2.TeleportToPosition(_spawnPoint2.GlobalPosition);
				}
				if (_fighter3 != null && _fighter3.IsActive() && (_fighter3.Position - vec).LengthSquared > 56.25f)
				{
					_fighter3.TeleportToPosition(_spawnPoint3.GlobalPosition);
				}
				if (_fighter4 != null && _fighter4.IsActive() && (_fighter4.Position - vec).LengthSquared > 56.25f)
				{
					_fighter4.TeleportToPosition(_spawnPoint4.GlobalPosition);
				}
			}
		}
		if (!(_resetTimer > 0f))
		{
			return;
		}
		_resetTimer -= dt;
		if (_resetTimer <= 0f)
		{
			_resetTimer = -1f;
			Agent fighter = _fighter1;
			Agent fighter2 = _fighter2;
			Agent fighter3 = _fighter3;
			Agent fighter4 = _fighter4;
			_fighter1 = null;
			_fighter2 = null;
			_fighter3 = null;
			_fighter4 = null;
			if (fighter != null && fighter.IsActive())
			{
				fighter.FadeOut(hideInstantly: false, hideMount: true);
			}
			if (fighter2 != null && fighter2.IsActive())
			{
				fighter2.FadeOut(hideInstantly: false, hideMount: true);
			}
			if (fighter3 != null && fighter3.IsActive())
			{
				fighter3.FadeOut(hideInstantly: false, hideMount: true);
			}
			if (fighter4 != null && fighter4.IsActive())
			{
				fighter4.FadeOut(hideInstantly: false, hideMount: true);
			}
			SpawnFighters();
		}
	}

	public bool IsSparringAgent(Agent agent)
	{
		if (agent != _fighter1 && agent != _fighter2 && agent != _fighter3 && agent != _fighter4)
		{
			return agent == _boutChallenger;
		}
		return true;
	}

	private void StartFeaturedMatch()
	{
		if (CanHostMatch)
		{
			Agent fighter = _fighter1;
			Agent fighter2 = _fighter2;
			Agent fighter3 = _fighter3;
			Agent fighter4 = _fighter4;
			_fighter1 = null;
			_fighter2 = null;
			_fighter3 = null;
			_fighter4 = null;
			if (fighter != null && fighter.IsActive())
			{
				fighter.FadeOut(hideInstantly: false, hideMount: true);
			}
			if (fighter2 != null && fighter2.IsActive())
			{
				fighter2.FadeOut(hideInstantly: false, hideMount: true);
			}
			if (fighter3 != null && fighter3.IsActive())
			{
				fighter3.FadeOut(hideInstantly: false, hideMount: true);
			}
			if (fighter4 != null && fighter4.IsActive())
			{
				fighter4.FadeOut(hideInstantly: false, hideMount: true);
			}
			_fightersSpawned = false;
			_resetTimer = -1f;
			_featuredMatch = true;
			_overlayLinger = -1f;
			_overlay.Show("Training Match — forming up...");
		}
	}

	private string MockMatchStatusText()
	{
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		float num4 = 0f;
		if (_fighter1 != null && _fighter1.IsActive())
		{
			num++;
			num3 += _fighter1.Health;
		}
		if (_fighter3 != null && _fighter3.IsActive())
		{
			num++;
			num3 += _fighter3.Health;
		}
		if (_fighter2 != null && _fighter2.IsActive())
		{
			num2++;
			num4 += _fighter2.Health;
		}
		if (_fighter4 != null && _fighter4.IsActive())
		{
			num2++;
			num4 += _fighter4.Health;
		}
		return $"Training Match — Red {num}/2 ({(int)num3} HP)  vs  Blue {num2}/2 ({(int)num4} HP)";
	}

	private void StartPlayerBout()
	{
		if (_boutActive || !CanHostMatch)
		{
			return;
		}
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			return;
		}
		try
		{
			if (_sparTeamPlayer == null)
			{
				_sparTeamPlayer = base.Mission.Teams.Add(BattleSideEnum.Defender, 4278255360u, 4278255360u);
				_sparTeamChallenger = base.Mission.Teams.Add(BattleSideEnum.Attacker, 4294945280u, 4294945280u);
				_sparTeamPlayer.SetIsEnemyOf(_sparTeamChallenger, isEnemyOf: true);
				_sparTeamChallenger.SetIsEnemyOf(_sparTeamPlayer, isEnemyOf: true);
				if (base.Mission.PlayerTeam != null)
				{
					_sparTeamPlayer.SetIsEnemyOf(base.Mission.PlayerTeam, isEnemyOf: false);
					_sparTeamChallenger.SetIsEnemyOf(base.Mission.PlayerTeam, isEnemyOf: false);
					base.Mission.PlayerTeam.SetIsEnemyOf(_sparTeamPlayer, isEnemyOf: false);
					base.Mission.PlayerTeam.SetIsEnemyOf(_sparTeamChallenger, isEnemyOf: false);
				}
			}
			_playerOriginalTeam = mainAgent.Team;
			mainAgent.SetTeam(_sparTeamPlayer, sync: true);
			Vec3 vec;
			if (_spawnPoint1 != null)
			{
				vec = _spawnPoint1.GlobalPosition;
			}
			else
			{
				Vec3 vec2 = mainAgent.LookDirection;
				vec2.z = 0f;
				if (vec2.LengthSquared < 0.01f)
				{
					vec2 = new Vec3(0f, 1f);
				}
				else
				{
					vec2.Normalize();
				}
				vec = mainAgent.Position + vec2 * 4f;
			}
			Vec3 vec3 = mainAgent.Position - vec;
			vec3.z = 0f;
			Vec2 direction = ((vec3.LengthSquared > 0.01f) ? vec3.AsVec2.Normalized() : Vec2.Forward);
			_boutChallenger = SpawnFighterCore(vec, direction, _sparTeamChallenger, 100f, null);
			_playerStartHealth = mainAgent.Health;
			_boutTimer = 0f;
			_boutActive = true;
			_overlayLinger = -1f;
			_overlay.Show("Sparring Bout — begin!");
			InformationManager.DisplayMessage(new InformationMessage("Sparring bout started! Defeat the challenger — the bout ends if you fall below 30% health.", new Color(1f, 0.78f, 0.1f)));
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTrainingFieldMissionLogic", "StartPlayerBout failed: " + ex.GetType().Name + ": " + ex.Message);
			EndPlayerBout(playerWon: false, draw: true, silent: true);
		}
	}

	private void UpdatePlayerBout(float dt)
	{
		if (!_boutActive)
		{
			return;
		}
		Agent mainAgent = base.Mission.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			EndPlayerBout(playerWon: false, draw: true, silent: true);
			return;
		}
		_boutTimer += dt;
		if (_boutChallenger == null || !_boutChallenger.IsActive() || _boutChallenger.Health <= 0f)
		{
			EndPlayerBout(playerWon: true);
			return;
		}
		float num = Math.Max(_playerStartHealth * 0.3f, 20f);
		if (mainAgent.Health <= num)
		{
			EndPlayerBout(playerWon: false);
		}
		else if (_boutTimer >= 240f)
		{
			EndPlayerBout(playerWon: false, draw: true);
		}
	}

	private void EndPlayerBout(bool playerWon, bool draw = false, bool silent = false)
	{
		_boutActive = false;
		Agent mainAgent = base.Mission.MainAgent;
		try
		{
			if (mainAgent != null && mainAgent.IsActive())
			{
				if (_playerOriginalTeam != null)
				{
					mainAgent.SetTeam(_playerOriginalTeam, sync: true);
				}
				mainAgent.Health = Math.Max(mainAgent.Health, _playerStartHealth);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadTrainingFieldMissionLogic", "EndPlayerBout (restore player) failed: " + ex.Message);
		}
		_playerOriginalTeam = null;
		Agent boutChallenger = _boutChallenger;
		_boutChallenger = null;
		try
		{
			if (boutChallenger != null && boutChallenger.IsActive())
			{
				boutChallenger.FadeOut(hideInstantly: false, hideMount: true);
			}
		}
		catch
		{
		}
		if (silent)
		{
			if (_overlay.IsShown && _overlayLinger < 0f)
			{
				_overlay.Remove();
			}
			return;
		}
		string text = (draw ? "Sparring Bout: called off — a draw." : (playerWon ? "Sparring Bout: you win!" : "Sparring Bout: the challenger takes it."));
		InformationManager.DisplayMessage(new InformationMessage(text, new Color(0.2f, 0.6f, 1f)));
		if (_overlay.IsShown)
		{
			_overlay.SetText(text);
			_overlayLinger = 5f;
		}
		if (!playerWon)
		{
			return;
		}
		try
		{
			Hero hero = HomesteadBehavior.Instance?.CurrentHomestead?.ArmsMasterHero;
			if (hero != null && hero.IsAlive)
			{
				ChangeRelationAction.ApplyPlayerRelation(hero, 1);
			}
		}
		catch (Exception ex2)
		{
			TraceLogger.Write("HomesteadTrainingFieldMissionLogic", "EndPlayerBout (relation) failed: " + ex2.Message);
		}
	}

	private void UpdateOverlay(float dt)
	{
		if (_overlayLinger > 0f)
		{
			_overlayLinger -= dt;
			if (_overlayLinger <= 0f)
			{
				_overlay.Remove();
				_overlayLinger = -1f;
			}
		}
		else if (_boutActive)
		{
			Agent mainAgent = base.Mission.MainAgent;
			if (mainAgent != null && mainAgent.IsActive() && _boutChallenger != null && _boutChallenger.IsActive())
			{
				int num = (int)(mainAgent.Health / Math.Max(1f, mainAgent.HealthLimit) * 100f);
				int num2 = (int)(_boutChallenger.Health / Math.Max(1f, _boutChallenger.HealthLimit) * 100f);
				_overlay.SetText($"Sparring Bout — You {num}%  vs  Challenger {num2}%");
			}
		}
		else if (_featuredMatch && _fightersSpawned)
		{
			_overlay.SetText(MockMatchStatusText());
		}
	}

	public override void OnEndMissionInternal()
	{
		base.OnEndMissionInternal();
		if (_boutActive)
		{
			EndPlayerBout(playerWon: false, draw: true, silent: true);
		}
		_overlay.Remove();
	}
}

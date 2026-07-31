using System;
using System.Collections.Generic;
using SOTOR.AbilitySystem.Crosshairs;
using SOTOR.AbilitySystem.StatusEffects;
using SOTOR.Extensions;
using SOTOR.Extensions.ExtendedInfoSystem;
using SOTOR.GameManagers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SOTOR.AbilitySystem;

public class AbilityManagerMissionLogic : MissionLogic
{
	private AbilityModeState _currentState;

	private AbilityComponent _abilityComponent;

	private AbilityHUDMissionView _abilityView;

	private GameKey _quickCastMenuKey;

	private bool _hasInitializedForMainAgent;

	private bool _loggedGatingBlock;

	private bool _battleResultReached;

	private int _timeRequestID = 1338;

	private const float PostCastSuppressDuration = 0.3f;

	private float _postCastSuppressUntil;

	private bool _shouldSheathWeapon;

	private bool _shouldWieldWeapon;

	private EquipmentIndex _mainHand = EquipmentIndex.None;

	private EquipmentIndex _offHand = EquipmentIndex.None;

	private bool _shouldPlayIdleCastStanceAnim;

	private ActionIndexCache? _idleCastAnimation;

	private bool _loggedAnimResolve;

	private const string CastStanceParticleName = "psys_spellcasting_stance";

	private ParticleSystem[] _castStancePsys;

	private GameEntity[] _castStanceEntities;

	private Agent _castStanceAgent;

	private static bool _missionPatchesApplied;

	private bool _tickCrashLogged;

	public AbilityModeState CurrentState => _currentState;

	public bool ShouldSuppressCombatActions
	{
		get
		{
			if (_currentState != AbilityModeState.QuickMenuSelection && _currentState != AbilityModeState.Targeting && _currentState != AbilityModeState.Casting)
			{
				if (base.Mission != null)
				{
					return base.Mission.CurrentTime < _postCastSuppressUntil;
				}
				return false;
			}
			return true;
		}
	}

	private ActionIndexCache IdleCastAnimation
	{
		get
		{
			if (!_idleCastAnimation.HasValue)
			{
				_idleCastAnimation = ActionIndexCache.Create("act_spellcasting_idle");
			}
			return _idleCastAnimation.Value;
		}
	}

	public override void OnRemoveBehavior()
	{
		RemoveMissionOnlyPatches();
		// [RF-B] o pool e indexado por Agent e vale so dentro da batalha; sem isto
		// ele seguraria referencias de agentes mortos entre missoes.
		SOTOR.RFIntegration.TroopWindsPool.Reset();
		SOTOR.RFIntegration.ArcaneFocusCasterSource.ClearCache();
		base.OnRemoveBehavior();
	}

	public override void OnAgentDeleted(Agent affectedAgent)
	{
		SOTOR.RFIntegration.TroopWindsPool.Forget(affectedAgent);
		base.OnAgentDeleted(affectedAgent);
	}

	public override void OnMissionResultReady(MissionResult missionResult)
	{
		base.OnMissionResultReady(missionResult);
		if (missionResult == null || (!missionResult.PlayerDefeated && !missionResult.PlayerVictory))
		{
			return;
		}
		_battleResultReached = true;
		if (_currentState != AbilityModeState.Off)
		{
			DisableAbilityMode();
		}
		AgentReadOnlyList agentReadOnlyList = base.Mission?.AllAgents;
		if (agentReadOnlyList != null)
		{
			foreach (Agent item in agentReadOnlyList)
			{
				item?.GetComponent<StatusEffectComponent>()?.Dispose();
			}
		}
		SotorSpellDamageLog.FlushExpired(base.Mission);
		SotorSpellDamageLog.Reset();
		SotorLog.Info($"OnMissionResultReady: magic stopped (victory={missionResult.PlayerVictory} defeat={missionResult.PlayerDefeated}); status effects disposed.");
	}

	private static void ApplyMissionOnlyPatches()
	{
		if (_missionPatchesApplied)
		{
			return;
		}
		try
		{
			SubModule.HarmonyInstance?.PatchCategory(typeof(SubModule).Assembly, "SotorMissionOnlyPatches");
			_missionPatchesApplied = true;
			SotorLog.Info("Mission-only Harmony patches applied (combat-actions lockout live).");
		}
		catch (Exception ex)
		{
			SotorLog.Error("Failed to apply mission-only patches: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void RemoveMissionOnlyPatches()
	{
		if (!_missionPatchesApplied)
		{
			return;
		}
		try
		{
			SubModule.HarmonyInstance?.UnpatchCategory(typeof(SubModule).Assembly, "SotorMissionOnlyPatches");
			SotorLog.Info("Mission-only Harmony patches removed (off the campaign map / doll).");
		}
		catch (Exception ex)
		{
			SotorLog.Error("Failed to remove mission-only patches: " + ex.GetType().Name + ": " + ex.Message);
		}
		finally
		{
			_missionPatchesApplied = false;
		}
	}

	public override void EarlyStart()
	{
		base.EarlyStart();
		ApplyMissionOnlyPatches();
		_abilityView = base.Mission.GetMissionBehavior<AbilityHUDMissionView>();
		_quickCastMenuKey = null;
		try
		{
			GameKeyContext gameKeyContext = null;
			foreach (GameKeyContext allCategory in HotKeyManager.GetAllCategories())
			{
				if (allCategory is SotorGameKeyContext)
				{
					gameKeyContext = allCategory;
					break;
				}
			}
			_quickCastMenuKey = gameKeyContext?.GetGameKey(111);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbilityManager EarlyStart: hotkey category lookup failed (" + ex.GetType().Name + "); using Q fallback.");
		}
		SotorLog.Info(string.Format("AbilityManager EarlyStart. castingMission={0} quickCastKey={1}", IsCastingMission(), (_quickCastMenuKey != null) ? "ok" : "missing (Q fallback)"));
		EnsureMainAgentAbilityComponent();
	}

	private void Safe(string where, Action body, bool throttle = false)
	{
		try
		{
			body();
		}
		catch (Exception ex)
		{
			if (!throttle || !_tickCrashLogged)
			{
				_tickCrashLogged = true;
				SotorLog.Error("EXCEPTION in " + where + ": " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			}
		}
	}

	public override void OnPreMissionTick(float dt)
	{
		// [RF-B] regeneracao do pool das tropas conjuradoras. FORA do bloco de
		// Agent.Main de proposito: tropa regenera com ou sem jogador em campo.
		SOTOR.RFIntegration.TroopWindsPool.TickRegeneration();
		Safe("OnPreMissionTick", delegate
		{
			if (Agent.Main != null)
			{
				EnsureMainAgentAbilityComponent();
				_abilityComponent = Agent.Main.GetComponent<AbilityComponent>();
				if (!_hasInitializedForMainAgent)
				{
					ApplyBattleStartWindsPerks();
				}
				_hasInitializedForMainAgent = true;
			}
			if (_hasInitializedForMainAgent)
			{
				if (_shouldSheathWeapon || _shouldWieldWeapon)
				{
					UpdateWieldedItems();
				}
				if (IsAbilityModeAvailableForMainAgent())
				{
					_loggedGatingBlock = false;
					HandleInput();
					HandleAnimations();
				}
				else if (Agent.Main != null && !_loggedGatingBlock)
				{
					LogAbilityGatingOnce();
				}
			}
		}, throttle: true);
	}

	private void LogAbilityGatingOnce()
	{
		_loggedGatingBlock = true;
		Agent main = Agent.Main;
		SotorLog.Debug($"Ability input gated. active={main.IsActive()} mouse={ScreenManager.GetMouseVisibility()} " + $"castingMission={IsCastingMission()} photo={base.Mission.IsInPhotoMode} orders={base.Mission.IsOrderMenuOpen} " + $"mode={(int)base.Mission.Mode} component={_abilityComponent != null} " + string.Format("current={0} abilityUser={1}", _abilityComponent?.CurrentAbility?.StringID ?? "none", main.IsAbilityUser()));
	}

	/// <summary>
	/// [RF-B] Segunda tentativa de anexar o AbilityComponent, agora que o agente
	/// esta COMPLETO.
	///
	/// <c>OnAgentCreated</c> roda ANTES do equipamento ser montado: ali
	/// <c>agent.Equipment</c> ainda esta vazio, entao a checagem "esta tropa carrega
	/// um foco arcano?" respondia sempre NAO e nenhuma tropa virava conjuradora.
	/// Media no log: 1 unico AbilityComponent criado numa batalha inteira, o do
	/// heroi — que passa por outro caminho e nao depende do equipamento.
	///
	/// <c>OnAgentBuild</c> e o callback que o jogo dispara depois de vestir e armar
	/// o agente, e e onde a pergunta tem resposta.
	/// </summary>
	public override void OnAgentBuild(Agent agent, Banner banner)
	{
		Safe("OnAgentBuild", delegate
		{
			if (ShouldAttachAbilityComponent(agent) && agent.GetComponent<AbilityComponent>() == null)
			{
				agent.AddComponent(new AbilityComponent(agent));
			}
		});
		base.OnAgentBuild(agent, banner);
	}

	public override void OnAgentCreated(Agent agent)
	{
		Safe("OnAgentCreated", delegate
		{
			if (ShouldAttachAbilityComponent(agent) && agent.GetComponent<AbilityComponent>() == null)
			{
				agent.AddComponent(new AbilityComponent(agent));
			}
		});
	}

	private void EnsureMainAgentAbilityComponent()
	{
		Agent main = Agent.Main;
		if (main == null || !AbilityMissionModeHelper.IsBattleAbilityContext(base.Mission))
		{
			return;
		}
		Hero hero = main.GetHero();
		if (hero != null)
		{
			hero.AddAttribute("AbilityUser");
			hero.AddAttribute("SpellCaster");
			HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
			if (extendedInfo != null)
			{
				foreach (string item in extendedInfo.AcquiredLores ?? new List<string>())
				{
					foreach (AbilityTemplate item2 in AbilityFactory.GetTemplatesByLore(item))
					{
						if (extendedInfo.HasSpell(item2.StringID) && !hero.HasAbility(item2.StringID))
						{
							hero.AddAbility(item2.StringID);
						}
					}
				}
			}
		}
		if (ShouldAttachAbilityComponent(main) && main.GetComponent<AbilityComponent>() == null)
		{
			main.AddComponent(new AbilityComponent(main));
			SotorLog.Info("Attached AbilityComponent to main agent (fallback).");
		}
	}

	private bool ShouldAttachAbilityComponent(Agent agent)
	{
		if (!AbilityMissionModeHelper.IsBattleAbilityContext(base.Mission))
		{
			return false;
		}
		if (agent.IsAbilityUser())
		{
			return agent.GetSelectedAbilities().Count > 0;
		}
		Hero hero = agent.GetHero();
		if (agent.IsMainAgent && hero != null)
		{
			HeroExtendedInfo extendedInfo = hero.GetExtendedInfo();
			if (extendedInfo == null)
			{
				return false;
			}
			return extendedInfo.SelectedAbilities.Count > 0;
		}
		return false;
	}

	private bool IsQuickCastMenuKeyPressed()
	{
		if (_quickCastMenuKey != null && (Input.IsKeyPressed(_quickCastMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCastMenuKey.ControllerKey.InputKey)))
		{
			return true;
		}
		return Input.IsKeyPressed(InputKey.Q);
	}

	private bool IsQuickCastMenuKeyDown()
	{
		if (_quickCastMenuKey != null && (Input.IsKeyDown(_quickCastMenuKey.KeyboardKey.InputKey) || Input.IsKeyDown(_quickCastMenuKey.ControllerKey.InputKey)))
		{
			return true;
		}
		return Input.IsKeyDown(InputKey.Q);
	}

	private void HandleInput()
	{
		if (Input.IsKeyDown(InputKey.LeftAlt))
		{
			return;
		}
		if ((_currentState == AbilityModeState.QuickMenuSelection || _currentState == AbilityModeState.Targeting) && Input.IsKeyPressed(InputKey.RightMouseButton))
		{
			DisableAbilityMode();
			return;
		}
		switch (_currentState)
		{
		case AbilityModeState.Off:
			if (IsQuickCastMenuKeyPressed())
			{
				EnableQuickSelectionMenuMode();
			}
			break;
		case AbilityModeState.QuickMenuSelection:
			if (!IsQuickCastMenuKeyDown())
			{
				Ability ability = _abilityComponent?.CurrentAbility;
				TextObject disabledReason;
				if (ability == null)
				{
					DisableAbilityMode();
				}
				else if (ability.IsDisabled(Agent.Main, out disabledReason))
				{
					SotorLog.Info("Q release: '" + ability.StringID + "' disabled: " + (disabledReason?.ToString() ?? "unknown"));
					DisableAbilityMode();
				}
				else if (ability.IsThrownWeapon)
				{
					TryQuickCastCurrentAbility();
					DisableAbilityMode(suppressWeaponRestore: true);
				}
				else if (ability.RequiresTargeting)
				{
					EnableTargetingMode();
				}
				else
				{
					TryQuickCastCurrentAbility();
					DisableAbilityMode();
				}
			}
			break;
		case AbilityModeState.Targeting:
		{
			AbilityCrosshair abilityCrosshair = _abilityComponent?.CurrentAbility?.Crosshair;
			abilityCrosshair?.Tick();
			if (Input.IsKeyPressed(InputKey.LeftMouseButton) && IsCrosshairReadyToFire(abilityCrosshair))
			{
				TryQuickCastCurrentAbility();
				DisableAbilityMode();
			}
			break;
		}
		}
	}

	private void EnableTargetingMode()
	{
		CacheWieldedItemsForRestore();
		// [RF-B] no RF a magia E o cajado: com foco na mao nao se embainha nada.
		_shouldSheathWeapon = !SOTOR.RFIntegration.RFCastAnimation.ShouldKeepFocusInHand(Agent.Main);
		_shouldPlayIdleCastStanceAnim = true;
		_loggedAnimResolve = false;
		try
		{
			SetUpCastStanceParticles();
			EnableCastStanceParticles(enable: true);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("Cast-stance particles failed: " + ex.Message);
		}
		_currentState = AbilityModeState.Targeting;
		AbilityHUDMissionView abilityView = _abilityView;
		if (abilityView != null)
		{
			MissionScreen missionScreen = ((MissionView)abilityView).MissionScreen;
			if (missionScreen != null)
			{
				missionScreen.UnregisterRadialMenuObject((object)_abilityView);
			}
		}
		AbilityHUDMissionView abilityView2 = _abilityView;
		MissionScreen val = ((abilityView2 != null) ? ((MissionView)abilityView2).MissionScreen : null);
		Ability ability = _abilityComponent?.CurrentAbility;
		if (ability != null && Agent.Main != null && val != null)
		{
			AbilityCrosshair abilityCrosshair = AbilityFactory.InitializeCrosshair(ability.Template, base.Mission, val, Agent.Main);
			ability.SetCrosshair(abilityCrosshair);
			abilityCrosshair?.Show();
		}
		SlowDownTime(enable: true);
		SotorLog.Info("Targeting mode ON for '" + ability?.StringID + "' (crosshair=" + (ability?.Crosshair?.GetType().Name ?? "none") + "; left-click fires, right-click cancels).");
	}

	private static bool IsCrosshairReadyToFire(AbilityCrosshair crosshair)
	{
		if (crosshair == null || !crosshair.IsVisible)
		{
			return false;
		}
		if (crosshair is SingleTargetCrosshair singleTargetCrosshair)
		{
			return singleTargetCrosshair.IsTargetLocked;
		}
		return true;
	}

	private void EnableQuickSelectionMenuMode()
	{
		SotorThrownJavelinMissionLogic.Instance?.CancelReadiedJavelin("new cast (cast key)");
		_currentState = AbilityModeState.QuickMenuSelection;
		AbilityHUDMissionView abilityView = _abilityView;
		MissionScreen obj = ((abilityView != null) ? ((MissionView)abilityView).MissionScreen : null);
		if (obj != null)
		{
			obj.RegisterRadialMenuObject<AbilityHUDMissionView>(_abilityView);
		}
		_abilityView?.OnQuickMenuOpened();
		SlowDownTime(enable: true);
		SotorLog.Info("Radial menu opened (Q).");
	}

	private void TryQuickCastCurrentAbility()
	{
		Safe("TryQuickCastCurrentAbility", delegate
		{
			Agent main = Agent.Main;
			if (main == null || _abilityComponent == null)
			{
				SotorLog.Debug($"Q release: cannot cast (mainAgent={main != null} component={_abilityComponent != null}).");
			}
			else
			{
				Ability currentAbility = _abilityComponent.CurrentAbility;
				if (currentAbility == null)
				{
					SotorLog.Debug("Q release: no current ability to cast.");
				}
				else
				{
					_abilityComponent.LastCastWasQuickCast = true;
					SotorTarget preferredTarget = null;
					Agent agent = (currentAbility.Crosshair as SingleTargetCrosshair)?.CachedTarget;
					if (agent != null)
					{
						preferredTarget = new SotorTarget
						{
							Agent = agent
						};
					}
					SotorLog.Debug("Q release: attempting cast '" + currentAbility.StringID + "' (prefab='" + currentAbility.Template?.ParticleEffectPrefab + "', lockedTarget='" + (agent?.Name ?? "none") + "').");
					if (!currentAbility.TryCast(main, preferredTarget, out var failureReason))
					{
						SotorLog.Info("Q quick-cast '" + currentAbility.StringID + "' failed: " + (failureReason?.ToString() ?? "unknown"));
					}
					else
					{
						if (base.Mission != null && !currentAbility.IsThrownWeapon)
						{
							_postCastSuppressUntil = base.Mission.CurrentTime + 0.3f;
						}
						if (!currentAbility.IsThrownWeapon)
						{
							// [RF-B] lancamento com o foco na mao = gesto de pedra.
							PlayCastReleaseAnimation(main, SOTOR.RFIntegration.RFCastAnimation.GetReleaseActionName(main, currentAbility.Template?.AnimationActionName));
						}
						SotorLog.Info("Q quick-cast '" + currentAbility.StringID + "' succeeded.");
					}
				}
			}
		});
	}

	private void DisableAbilityMode(bool suppressWeaponRestore = false)
	{
		Agent main = Agent.Main;
		EquipmentIndex equipmentIndex = main?.GetPrimaryWieldedItemIndex() ?? EquipmentIndex.None;
		EquipmentIndex equipmentIndex2 = main?.GetOffhandWieldedItemIndex() ?? EquipmentIndex.None;
		bool flag = _mainHand != EquipmentIndex.None && equipmentIndex != _mainHand;
		bool flag2 = _offHand != EquipmentIndex.None && equipmentIndex2 != _offHand;
		_shouldWieldWeapon = !suppressWeaponRestore && (flag || flag2);
		if (suppressWeaponRestore)
		{
			_mainHand = EquipmentIndex.None;
			_offHand = EquipmentIndex.None;
		}
		_shouldSheathWeapon = false;
		_shouldPlayIdleCastStanceAnim = false;
		try
		{
			EnableCastStanceParticles(enable: false);
			RemoveCastStanceParticles();
		}
		catch (Exception ex)
		{
			SotorLog.Warn("Cast-stance particle teardown failed: " + ex.Message);
		}
		_currentState = AbilityModeState.Off;
		if (_abilityComponent != null)
		{
			_abilityComponent.LastCastWasQuickCast = false;
		}
		SlowDownTime(enable: false);
		AbilityHUDMissionView abilityView = _abilityView;
		if (abilityView != null)
		{
			MissionScreen missionScreen = ((MissionView)abilityView).MissionScreen;
			if (missionScreen != null)
			{
				missionScreen.UnregisterRadialMenuObject((object)_abilityView);
			}
		}
		AbilityCrosshair abilityCrosshair = _abilityComponent?.CurrentAbility?.Crosshair;
		if (abilityCrosshair != null)
		{
			abilityCrosshair.Hide();
			abilityCrosshair.Dispose();
			_abilityComponent.CurrentAbility.SetCrosshair(null);
		}
		SotorLog.Info("Radial menu closed.");
	}

	private void HandleAnimations()
	{
		if (_currentState == AbilityModeState.Off || Agent.Main == null || _currentState != AbilityModeState.Targeting || !_shouldPlayIdleCastStanceAnim)
		{
			return;
		}
		ActionIndexCache currentAction = Agent.Main.GetCurrentAction(1);
		if (!_idleCastAnimation.HasValue || currentAction != _idleCastAnimation.Value)
		{
			// [RF-B] com foco na mao a postura de mira e a de arremesso de pedra.
			ActionIndexCache actionIndexCache = SOTOR.RFIntegration.RFCastAnimation.GetIdleStanceAction(Agent.Main, IdleCastAnimation);
			if (!_loggedAnimResolve)
			{
				_loggedAnimResolve = true;
				int index = actionIndexCache.Index;
				int index2 = ActionIndexCache.act_none.Index;
				SotorLog.Info(string.Format("CastAnim: act_spellcasting_idle index={0} (act_none={1}; {2}). ", index, index2, (index == index2) ? "NOT REGISTERED — merge failed" : "resolved OK") + $"channel1Before={currentAction.Index} mainHandWielded={Agent.Main.GetPrimaryWieldedItemIndex()} shouldSheath={_shouldSheathWeapon}");
			}
			Agent.Main.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL);
		}
	}

	private void PlayCastReleaseAnimation(Agent caster, string actionName)
	{
		if (caster == null || string.IsNullOrWhiteSpace(actionName) || actionName.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			ActionIndexCache actionIndexCache = ActionIndexCache.Create(actionName);
			if (actionIndexCache.Index == ActionIndexCache.act_none.Index)
			{
				SotorLog.Debug("CastRelease: action '" + actionName + "' unresolved (act_none); skipping.");
				return;
			}
			caster.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)0uL);
			SotorLog.Info($"CastRelease: played '{actionName}' (index={actionIndexCache.Index}) on '{caster.Name}'.");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("PlayCastReleaseAnimation('" + actionName + "') failed: " + ex.Message);
		}
	}

	private void SetUpCastStanceParticles()
	{
		RemoveCastStanceParticles();
		Agent main = Agent.Main;
		if (main != null && !(main.AgentVisuals == null) && !(base.Mission?.Scene == null))
		{
			Monster monster = Game.Current?.DefaultMonster;
			if (monster != null)
			{
				_castStanceAgent = main;
				_castStancePsys = new ParticleSystem[2];
				_castStanceEntities = new GameEntity[2];
				_castStancePsys[0] = ApplyParticleToAgentBone(main, "psys_spellcasting_stance", monster.MainHandItemBoneIndex, out _castStanceEntities[0]);
				_castStancePsys[1] = ApplyParticleToAgentBone(main, "psys_spellcasting_stance", monster.OffHandItemBoneIndex, out _castStanceEntities[1]);
				EnableCastStanceParticles(enable: false);
			}
		}
	}

	private static ParticleSystem ApplyParticleToAgentBone(Agent agent, string particleId, sbyte boneIndex, out GameEntity childEntity)
	{
		childEntity = null;
		if (string.IsNullOrWhiteSpace(particleId) || agent.AgentVisuals == null)
		{
			return null;
		}
		Scene scene = Mission.Current?.Scene;
		if (scene == null)
		{
			return null;
		}
		Skeleton skeleton = agent.AgentVisuals.GetSkeleton();
		if (skeleton == null || !skeleton.IsValid || boneIndex < 0 || boneIndex >= skeleton.GetBoneCount())
		{
			return null;
		}
		childEntity = GameEntity.CreateEmpty(scene);
		MatrixFrame boneLocalFrame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, 0f, -1f));
		ParticleSystem particleSystem = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, childEntity, ref boneLocalFrame);
		if (particleSystem == null)
		{
			childEntity.Remove(0);
			childEntity = null;
			return null;
		}
		agent.AgentVisuals.AddChildEntity(childEntity);
		skeleton.AddComponentToBone(boneIndex, particleSystem);
		return particleSystem;
	}

	private void EnableCastStanceParticles(bool enable)
	{
		if (_castStancePsys != null)
		{
			ParticleSystem[] castStancePsys = _castStancePsys;
			for (int i = 0; i < castStancePsys.Length; i++)
			{
				castStancePsys[i]?.SetEnable(enable);
			}
		}
	}

	private void RemoveCastStanceParticles()
	{
		if (_castStancePsys == null)
		{
			return;
		}
		for (int i = 0; i < _castStancePsys.Length; i++)
		{
			GameEntity gameEntity = ((_castStanceEntities != null && i < _castStanceEntities.Length) ? _castStanceEntities[i] : null);
			if (gameEntity != null)
			{
				gameEntity.RemoveAllParticleSystems();
				if (_castStanceAgent != null && _castStanceAgent.AgentVisuals != null)
				{
					_castStanceAgent.AgentVisuals.RemoveChildEntity(gameEntity, 0);
				}
				else
				{
					gameEntity.Remove(0);
				}
			}
		}
		_castStancePsys = null;
		_castStanceEntities = null;
		_castStanceAgent = null;
	}

	private void CacheWieldedItemsForRestore()
	{
		if (!_shouldWieldWeapon && Agent.Main != null)
		{
			_mainHand = Agent.Main.GetPrimaryWieldedItemIndex();
			_offHand = Agent.Main.GetOffhandWieldedItemIndex();
			// [RF-B] o cast EMBAINHA a arma; sem registrar o foco que saiu da mao o
			// gate se auto-bloquearia (mao vazia = sem foco = feitico de mira impossivel).
			SOTOR.RFIntegration.ArcaneFocusGate.NoteFocusSuspendedForWieldedItem(Agent.Main, _mainHand, _offHand);
		}
	}

	private void UpdateWieldedItems()
	{
		Agent main = Agent.Main;
		if (main == null || !main.IsActive())
		{
			_shouldSheathWeapon = false;
			_shouldWieldWeapon = false;
			_mainHand = EquipmentIndex.None;
			_offHand = EquipmentIndex.None;
			return;
		}
		if (_currentState == AbilityModeState.Targeting && _shouldSheathWeapon)
		{
			if (main.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
			{
				main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
				return;
			}
			if (main.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
			{
				main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
				return;
			}
			_shouldSheathWeapon = false;
		}
		if (_currentState == AbilityModeState.Off && _shouldWieldWeapon && base.Mission.CurrentTime >= _postCastSuppressUntil)
		{
			EquipmentIndex primaryWieldedItemIndex = main.GetPrimaryWieldedItemIndex();
			EquipmentIndex offhandWieldedItemIndex = main.GetOffhandWieldedItemIndex();
			bool flag = _mainHand == EquipmentIndex.None || primaryWieldedItemIndex == _mainHand;
			bool flag2 = _offHand == EquipmentIndex.None || offhandWieldedItemIndex == _offHand;
			if (flag && flag2)
			{
				_shouldWieldWeapon = false;
				// [RF-B] arma de volta a mao: o gate volta a ler o item empunhado.
				SOTOR.RFIntegration.ArcaneFocusGate.ClearSuspendedFocus();
			}
			else if (_mainHand != EquipmentIndex.None && !flag)
			{
				main.TryToWieldWeaponInSlot(_mainHand, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
			}
			else if (_offHand != EquipmentIndex.None && !flag2)
			{
				main.TryToWieldWeaponInSlot(_offHand, Agent.WeaponWieldActionType.WithAnimation, isWieldedOnSpawn: false);
			}
		}
	}

	private void ApplyBattleStartWindsPerks()
	{
		try
		{
			Hero hero = Agent.Main?.GetHero();
			if (hero == null)
			{
				return;
			}
			if (HeroExtendedInfo.TestingMaxWindsOverride >= 0f)
			{
				hero.SetWindsOfMagic(HeroExtendedInfo.TestingMaxWindsOverride);
				SotorLog.Info($"TESTING: filled Winds of Magic to {HeroExtendedInfo.TestingMaxWindsOverride} at battle start.");
			}
			if (SotorPerks.Improvision != null && hero.GetPerkValue(SotorPerks.Improvision) && hero.GetWindsOfMagic() < 25f)
			{
				hero.SetWindsOfMagic(25f);
				SotorLog.Info("Improvision: floored Winds of Magic to 25 at battle start.");
			}
			if (SotorPerks.Catalyst == null || !hero.GetPerkValue(SotorPerks.Catalyst))
			{
				return;
			}
			int num = 0;
			Equipment battleEquipment = hero.BattleEquipment;
			if (battleEquipment != null)
			{
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ArmorItemEndSlot; equipmentIndex++)
				{
					EquipmentElement equipmentElement = battleEquipment[equipmentIndex];
					if (equipmentElement.Item != null && equipmentElement.ItemModifier != null && equipmentElement.ItemModifier.ItemQuality == ItemQuality.Legendary)
					{
						num++;
					}
				}
			}
			if (num > 0)
			{
				hero.AddWindsOfMagic((float)num * 5f, allowOverMax: true);
				SotorLog.Info($"Catalyst: +{num * 5} Winds at battle start ({num} legendary item(s)).");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("ApplyBattleStartWindsPerks failed: " + ex.Message);
		}
	}

	private void SlowDownTime(bool enable)
	{
		if (base.Mission != null)
		{
			float requestedTime;
			bool requestedTimeSpeed = base.Mission.GetRequestedTimeSpeed(_timeRequestID, out requestedTime);
			if (requestedTimeSpeed && !enable)
			{
				base.Mission.RemoveTimeSpeedRequest(_timeRequestID);
			}
			else if (!requestedTimeSpeed && enable)
			{
				Mission.TimeSpeedRequest request = new Mission.TimeSpeedRequest(0.3f, _timeRequestID);
				_timeRequestID = request.RequestID;
				base.Mission.AddTimeSpeedRequest(request);
			}
		}
	}

	public bool IsCastingMission()
	{
		if (!base.Mission.IsFriendlyMission && base.Mission.CombatType != Mission.MissionCombatType.ArenaCombat)
		{
			return base.Mission.CombatType != Mission.MissionCombatType.NoCombat;
		}
		return false;
	}

	private bool IsAbilityModeAvailableForMainAgent()
	{
		if (!_battleResultReached && Agent.Main != null && Agent.Main.IsActive() && !ScreenManager.GetMouseVisibility() && IsCastingMission() && !base.Mission.IsInPhotoMode && !base.Mission.IsOrderMenuOpen && AbilityMissionModeHelper.IsAbilityHudMissionMode(base.Mission) && _abilityComponent != null)
		{
			return _abilityComponent.CurrentAbility != null;
		}
		return false;
	}
}

using System;
using SOTOR.AbilitySystem.Crosshairs;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public abstract class Ability
{
	private int _coolDownLeft;

	private float _cooldownEndTime;

	public string StringID { get; }

	public AbilityTemplate Template { get; }

	public AbilityCrosshair Crosshair { get; private set; }

	public bool RequiresTargeting => Template.AbilityTargetType != AbilityTargetType.Self;

	public virtual bool IsThrownWeapon => false;

	protected Ability(AbilityTemplate template)
	{
		StringID = template.StringID;
		Template = template;
	}

	public void SetCrosshair(AbilityCrosshair crosshair)
	{
		Crosshair = crosshair;
	}

	/// <summary>
	/// [RF-B] A MIRA DA IA.
	///
	/// <see cref="Crosshair" /> so e criado para <c>Agent.Main</c> (ver
	/// AbilityManagerMissionLogic: InitializeCrosshair recebe Agent.Main). Para
	/// qualquer conjurador de IA ele e null PARA SEMPRE — e em GetSpawnFrame todos
	/// os ramos que posicionam o efeito no ponto mirado dependem dele.
	///
	/// Consequencia medida em log: Blast, Wind, Vortex, Bombardment e Hex nasciam
	/// no LookFrame do PROPRIO mago. Um Ice Purge (raio 5, offset 4) lancado contra
	/// uma formacao a 15m detonava em cima do conjurador e registrava
	/// "considered=0 applied=0"; um Wind Blast pegava 17 agentes, metade deles
	/// aliados, e um Bolt of Aqshy chegou a MATAR um Mage Initiate Elite amigo.
	///
	/// A IA ja escolhe uma posicao de alvo (Target.GetPositionPrioritizeCalculated).
	/// Ela so nunca chegava ate aqui. Este campo e essa ponte: o comportamento de
	/// conjuracao a define imediatamente antes do lancamento, e GetSpawnFrame passa
	/// a trata-la como a IA tratando o que a mira e para o jogador.
	/// </summary>
	private Vec3 _aiAimPosition = Vec3.Invalid;

	public void SetAiAimPosition(Vec3 position)
	{
		_aiAimPosition = position;
	}

	/// <summary>Ha ponto de mira da IA utilizavel? (jogador tem mira propria)</summary>
	private bool TryGetAimPosition(out Vec3 position)
	{
		position = _aiAimPosition;
		return position != Vec3.Invalid;
	}

	public virtual bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
	{
		disabledReason = new TextObject("{=sotor_ability_enabled}Enabled");
		if (IsOnCooldown())
		{
			disabledReason = new TextObject("{=sotor_ability_on_cooldown}On cooldown");
			return true;
		}
		// [RF-B] identidade do RF: sem foco arcano empunhado, nao se conjura.
		if (SOTOR.RFIntegration.ArcaneFocusGate.IsBlocked(casterAgent, Template, out TextObject rfReason))
		{
			disabledReason = rfReason;
			return true;
		}
		return false;
	}

	public bool CanCast(Agent casterAgent, out TextObject disabledReason)
	{
		return !IsDisabled(casterAgent, out disabledReason);
	}

	public bool IsOnCooldown()
	{
		if (Mission.Current == null)
		{
			return false;
		}
		float num = _cooldownEndTime - Mission.Current.CurrentTime;
		if (num <= 0f)
		{
			_coolDownLeft = 0;
			return false;
		}
		_coolDownLeft = (int)Math.Ceiling(num);
		return true;
	}

	public int GetCoolDownLeft()
	{
		IsOnCooldown();
		return _coolDownLeft;
	}

	public void SetCoolDown(int cooldownTime)
	{
		if (Mission.Current != null)
		{
			_coolDownLeft = cooldownTime;
			_cooldownEndTime = Mission.Current.CurrentTime + (float)_coolDownLeft + 0.8f;
		}
	}

	public bool TryCast(Agent casterAgent, out TextObject failureReason)
	{
		return TryCast(casterAgent, null, out failureReason);
	}

	public virtual bool TryCast(Agent casterAgent, SotorTarget preferredTarget, out TextObject failureReason)
	{
		failureReason = null;
		if (casterAgent == null || Mission.Current == null || Mission.Current.Scene == null)
		{
			failureReason = new TextObject("{=sotor_cast_no_context}No mission context.");
			SotorLog.Warn("TryCast " + StringID + ": no caster/mission/scene.");
			return false;
		}
		if (IsDisabled(casterAgent, out failureReason))
		{
			SotorLog.Info(string.Format("TryCast {0}: blocked — {1} (cooldown {2}s).", StringID, failureReason?.ToString() ?? "disabled", GetCoolDownLeft()));
			return false;
		}
		try
		{
			Scene scene = Mission.Current.Scene;
			MatrixFrame frame = GetSpawnFrame(casterAgent);
			GameEntity entity = GameEntity.CreateEmpty(scene, isModifiableFromEditor: false, createPhysics: true, callScriptCallbacks: false);
			entity.SetGlobalFrame(in frame);
			if (Template.TriggerType == TriggerType.OnCollision)
			{
				AddPhysics(entity);
			}
			MatrixFrame frame2 = casterAgent.Frame;
			Vec3 eyeGlobalPosition = casterAgent.GetEyeGlobalPosition();
			Vec3 vec = frame2.TransformToLocal(frame.origin - frame2.origin);
			Vec3 f = frame.rotation.f;
			float num = (float)Math.Sqrt(f.x * f.x + f.y * f.y);
			float num2 = (float)(Math.Atan2(f.z, num) * 180.0 / Math.PI);
			SotorLog.Debug($"Spawn {StringID} ({Template.AbilityEffectType}): casterPos={frame2.origin} eye={eyeGlobalPosition} " + $"spawnOrigin={frame.origin} localOffset(r,f,u)={vec} " + $"lookForward={f} lookElevation={num2:0.0}deg " + $"spawnZ-casterZ={frame.origin.z - frame2.origin.z:0.00} spawnZ-eyeZ={frame.origin.z - eyeGlobalPosition.z:0.00}");
			string scriptTypeName = GetScriptTypeName();
			entity.CreateAndAddScriptComponent(scriptTypeName, callScriptCallbacks: false);
			AbilityScript firstScriptOfType = entity.GetFirstScriptOfType<AbilityScript>();
			if (firstScriptOfType == null)
			{
				failureReason = new TextObject("{=sotor_cast_no_script}Ability script not attached.");
				SotorLog.Warn("TryCast " + StringID + ": '" + scriptTypeName + "' not attached (type not registered?).");
				return false;
			}
			string particleEffectPrefab = Template.ParticleEffectPrefab;
			GameEntity gameEntity = null;
			if (!string.IsNullOrEmpty(particleEffectPrefab) && particleEffectPrefab != "none")
			{
				gameEntity = GameEntity.Instantiate(scene, particleEffectPrefab, callScriptCallbacks: true, createPhysics: true, string.Empty);
				if (gameEntity == null)
				{
					SotorLog.Warn("TryCast " + StringID + ": Instantiate('" + particleEffectPrefab + "') returned null; continuing without VFX child.");
				}
				else
				{
					entity.AddChild(gameEntity);
				}
			}
			firstScriptOfType.Initialize(this, ref entity);
			firstScriptOfType.SetCasterAgent(casterAgent);
			Agent agent = ResolveExplicitTarget(casterAgent, preferredTarget);
			if (agent != null)
			{
				MBList<Agent> explicitTargetAgents = new MBList<Agent> { agent };
				firstScriptOfType.SetExplicitTargetAgents(explicitTargetAgents);
			}
			if (Template.SeekerParameters != null)
			{
				SotorTarget sotorTarget = ((preferredTarget != null && preferredTarget.IsValid) ? preferredTarget : FindNearestEnemyTarget(casterAgent));
				if (sotorTarget != null)
				{
					firstScriptOfType.SetTargetSeeking(sotorTarget, Template.SeekerParameters);
					SotorLog.Info("TryCast " + StringID + ": homing at '" + sotorTarget.Agent?.Name + "' (source=" + ((preferredTarget != null) ? "crosshair" : "auto") + ").");
				}
			}
			entity.CallScriptCallbacks(registerScriptComponents: true);
			SotorLog.Info($"TryCast {StringID}: spawned via {scriptTypeName} | effect={Template.AbilityEffectType} " + $"prefab='{particleEffectPrefab}' origin={frame.origin}");
		}
		catch (Exception ex)
		{
			failureReason = new TextObject("{=sotor_cast_spawn_failed}Cast spawn failed.");
			SotorLog.Error("TryCast " + StringID + ": spawn EXCEPTION: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace);
			return false;
		}
		// [RF-B] segundo eixo da afinidade: cajado afim rearma mais rapido.
		SetCoolDown(SOTOR.RFIntegration.ArcaneFocusAffinity.ApplyToCooldown(Template.CoolDown, casterAgent, Template.BelongsToLoreID));
		OnCastSucceeded(casterAgent);
		SotorLog.Info($"TryCast {StringID}: cast OK; cooldown started ({Template.CoolDown}s).");
		return true;
	}

	protected virtual void OnCastSucceeded(Agent casterAgent)
	{
	}

	private void AddPhysics(GameEntity entity)
	{
		try
		{
			using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
			{
				entity.AddSphereAsBody(Vec3.Zero, Template.Radius, BodyFlags.Moveable | BodyFlags.DoNotCollideWithRaycast);
				if (Template.UseGravity)
				{
					entity.AddPhysics(1f, entity.CenterOfMass, entity.GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("missile"), isStatic: false, -1);
				}
			}
			SotorLog.Info($"AddPhysics {StringID}: sphere body radius={Template.Radius} (gravity={Template.UseGravity}).");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AddPhysics " + StringID + " failed: " + ex.Message + "; relying on raycast/proximity collision.");
		}
	}

	private string GetScriptTypeName()
	{
		switch (Template.AbilityEffectType)
		{
		case AbilityEffectType.Missile:
		case AbilityEffectType.SeekerMissile:
			return "MissileScript";
		case AbilityEffectType.Heal:
			return "HealScript";
		case AbilityEffectType.Augment:
		case AbilityEffectType.TacticalReposition:
			return "AugmentScript";
		case AbilityEffectType.Wind:
			return "WindScript";
		case AbilityEffectType.Vortex:
			return "VortexScript";
		case AbilityEffectType.Blast:
			return "BlastScript";
		case AbilityEffectType.Bombardment:
			return "BombardmentScript";
		case AbilityEffectType.Hex:
			return "AugmentScript";
		case AbilityEffectType.MindControl:
			return "MindControlScript";
		default:
			SotorLog.Debug($"TryCast {StringID}: no dedicated script for {Template.AbilityEffectType}; using AugmentScript.");
			return "AugmentScript";
		}
	}

	private MatrixFrame GetSpawnFrame(Agent casterAgent)
	{
		MatrixFrame result = casterAgent.LookFrame;
		switch (Template.AbilityEffectType)
		{
		case AbilityEffectType.Missile:
		case AbilityEffectType.SeekerMissile:
		{
			result.origin = casterAgent.GetEyeGlobalPosition();
			if (casterAgent.IsPlayerControlled && Crosshair != null && Crosshair.TryGetCameraAimDirection(out var direction))
			{
				Mat3 rotation = Mat3.CreateMat3WithForward(in direction);
				result.rotation = rotation;
				SotorLog.Debug($"GetSpawnFrame {StringID}: camera-ray aim dir={direction} (was LookFrame).");
			}
			else if (TryGetAimPosition(out var missileAim))
			{
				// [RF-B] PONTARIA DA IA.
				//
				// Um projetil e apontado pela ROTACAO, nao pela posicao — por isso o
				// conserto de mira dos feiticos de area nao alcancou este ramo. Sem
				// isto a rotacao continua sendo casterAgent.LookFrame: o feitico sai
				// para onde a CABECA do mago por acaso esta virada, que nao tem
				// relacao com a formacao que a IA escolheu.
				//
				// Medido em log: 37 impactos em terreno contra 11 em agente (77% no
				// chao), com agentDist=MaxValue — o projetil nao passava nem PERTO de
				// alguem.
				//
				// O +0.75 e altura de peito. UpdateTarget mira em Agent.Position, que
				// e o PE; disparar para la de uma origem na altura dos olhos aponta o
				// feitico para o chao. HaveLineOfSightToTarget ja somava esse mesmo
				// 0.75 antes de tracar o raio de visada — ou seja, a IA aprovava uma
				// linha e disparava outra. Agora as duas coincidem.
				missileAim.z += 0.75f;
				Vec3 aimDirection = missileAim - result.origin;
				if (aimDirection.LengthSquared > 0.01f)
				{
					Vec3 normalized = aimDirection.NormalizedCopy();
					result.rotation = Mat3.CreateMat3WithForward(in normalized);
				}
			}
			return result;
		}
		case AbilityEffectType.Wind:
			if (Crosshair != null)
			{
				result = Crosshair.Frame;
			}
			else if (TryGetAimPosition(out var windAim))
			{
				// [RF-B] Cone: nasce no conjurador, mas APONTADO para o alvo. Sem isto
				// o cone saia na direcao em que o corpo do agente por acaso estivesse.
				Vec3 windDir = windAim - result.origin;
				windDir.z = 0f;
				if (windDir.LengthSquared > 0.01f)
				{
					result.rotation = Mat3.CreateMat3WithForward(in windDir);
				}
			}
			return result;
		case AbilityEffectType.Blast:
			if (Crosshair != null)
			{
				result = Crosshair.Frame;
				result.origin.z += 1f;
			}
			else if (TryGetAimPosition(out var blastAim))
			{
				result.origin = blastAim;
				result.origin.z += 1f;
			}
			return result;
		case AbilityEffectType.Vortex:
			if (Crosshair != null)
			{
				MatrixFrame frame = casterAgent.Frame;
				result = new MatrixFrame(in frame.rotation, Crosshair.Position);
			}
			else if (TryGetAimPosition(out var vortexAim))
			{
				MatrixFrame casterFrame = casterAgent.Frame;
				result = new MatrixFrame(in casterFrame.rotation, vortexAim);
			}
			return result;
		case AbilityEffectType.Bombardment:
			if (Crosshair != null)
			{
				result = new MatrixFrame(Mat3.Identity, Crosshair.Position);
				result.origin.z += Template.Offset;
			}
			else if (TryGetAimPosition(out var bombardmentAim))
			{
				result = new MatrixFrame(Mat3.Identity, bombardmentAim);
				result.origin.z += Template.Offset;
			}
			return result;
		case AbilityEffectType.Hex:
			if (Crosshair != null)
			{
				result = new MatrixFrame(Mat3.Identity, Crosshair.Position);
				if (StringID == "CurseOfMidnightWind")
				{
					result.origin.z -= 1.5f;
				}
			}
			else if (TryGetAimPosition(out var hexAim))
			{
				result = new MatrixFrame(Mat3.Identity, hexAim);
				if (StringID == "CurseOfMidnightWind")
				{
					result.origin.z -= 1.5f;
				}
			}
			else
			{
				result.origin = casterAgent.GetChestGlobalPosition();
			}
			return result;
		case AbilityEffectType.MindControl:
			if (Crosshair != null)
			{
				result = new MatrixFrame(Mat3.Identity, Crosshair.Position);
			}
			else if (TryGetAimPosition(out var mindControlAim))
			{
				result = new MatrixFrame(Mat3.Identity, mindControlAim);
			}
			else
			{
				result.origin = casterAgent.GetChestGlobalPosition();
			}
			return result;
		default:
			if (Crosshair != null)
			{
				result = new MatrixFrame(Mat3.Identity, Crosshair.Position);
			}
			else if (TryGetAimPosition(out var defaultAim))
			{
				result = new MatrixFrame(Mat3.Identity, defaultAim);
			}
			else
			{
				result.origin = casterAgent.GetChestGlobalPosition();
			}
			return result;
		}
	}

	private Agent ResolveExplicitTarget(Agent casterAgent, SotorTarget preferredTarget)
	{
		if (Template.AbilityTargetType == AbilityTargetType.Self)
		{
			return casterAgent;
		}
		if ((Template.AbilityTargetType == AbilityTargetType.SingleAlly || Template.AbilityTargetType == AbilityTargetType.SingleEnemy) && preferredTarget != null && preferredTarget.IsValid)
		{
			return preferredTarget.Agent;
		}
		return null;
	}

	private SotorTarget FindNearestEnemyTarget(Agent casterAgent)
	{
		Mission current = Mission.Current;
		if (current == null || casterAgent?.Team == null)
		{
			return null;
		}
		float radius = ((Template.MaxDistance > 0f) ? Template.MaxDistance : 100f);
		Vec3 eyeGlobalPosition = casterAgent.GetEyeGlobalPosition();
		Vec3 lookDirection = casterAgent.LookDirection;
		lookDirection.z = 0f;
		lookDirection = lookDirection.NormalizedCopy();
		MBList<Agent> agents = new MBList<Agent>();
		agents = current.GetNearbyEnemyAgents(eyeGlobalPosition.AsVec2, radius, casterAgent.Team, agents);
		Agent agent = null;
		float num = float.MaxValue;
		foreach (Agent item in agents)
		{
			if (item == null || !item.IsActive() || item.Health <= 0f || !item.IsEnemyOf(casterAgent))
			{
				continue;
			}
			Vec3 vec = item.CollisionCapsuleCenter - eyeGlobalPosition;
			Vec3 vec2 = vec;
			vec2.z = 0f;
			if (!(Vec3.DotProduct(lookDirection, vec2.NormalizedCopy()) <= 0f))
			{
				float length = vec.Length;
				if (length < num)
				{
					num = length;
					agent = item;
				}
			}
		}
		if (agent == null)
		{
			return null;
		}
		return new SotorTarget
		{
			Agent = agent
		};
	}

	public void TickCastingState()
	{
	}
}

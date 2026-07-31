using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.AbilitySystem;

public abstract class AbilityScript : ScriptComponentBehavior
{
	private Ability _ability;

	private Agent _casterAgent;

	private float _abilityLife = -1f;

	private float _timeSinceLastTick;

	private bool _lifeTimeExpired;

	private bool _hasTickedOnce;

	private bool _hasTriggered;

	private bool _hasCollided;

	private bool _canCollide;

	private readonly float _minArmingTimeForCollision = 0.1f;

	private Vec3 _previousFrameOrigin = Vec3.Zero;

	private GameEntity _entity;

	private SeekerController _controller;

	private MBList<Agent> _targetAgents;

	private SoundEvent _sound;

	private int _soundIndex = -1;

	private bool _soundStarted;

	public Agent CasterAgent => _casterAgent;

	public Ability Ability => _ability;

	public bool IsFading { get; private set; }

	public bool HasTickedOnce => _hasTickedOnce;

	protected bool CanCollide => _canCollide;

	public Vec3 CurrentGlobalPosition => base.GameEntity.GetGlobalFrame().origin;

	public Vec3 LastFrameGlobalPosition => _previousFrameOrigin;

	public void SetCasterAgent(Agent agent)
	{
		_casterAgent = agent;
	}

	public void SetTargetSeeking(SotorTarget target, SeekerParameters parameters)
	{
		_controller = new SeekerController(target, parameters);
		SotorLog.Debug("AbilityScript: seeking target '" + target?.Agent?.Name + "'.");
	}

	public void SetExplicitTargetAgents(MBList<Agent> agents)
	{
		_targetAgents = agents;
	}

	public virtual void Initialize(Ability ability, ref GameEntity entity)
	{
		_ability = ability;
		_entity = entity;
		SotorLog.Debug("AbilityScript.Initialize for '" + ability?.StringID + "'.");
		InitializeSound();
	}

	private void InitializeSound()
	{
		string text = _ability?.Template?.SoundEffectToPlay?.Trim();
		if (string.IsNullOrEmpty(text) || text.Equals("none", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		try
		{
			_soundIndex = SoundEvent.GetEventIdFromString(text);
			if (_soundIndex < 0)
			{
				SotorLog.Debug("AbilityScript sound '" + text + "': not registered (eventId<0) for '" + _ability.StringID + "'.");
			}
			else
			{
				_sound = SoundEvent.CreateEvent(_soundIndex, base.Scene);
				if (_sound == null || !_sound.IsValid)
				{
					_sound = null;
					_soundIndex = -1;
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbilityScript.InitializeSound('" + text + "') failed: " + ex.Message);
			_sound = null;
			_soundIndex = -1;
		}
	}

	private void UpdateSound(Vec3 position)
	{
		if (_sound == null)
		{
			return;
		}
		if (!_sound.IsValid)
		{
			if (_soundIndex < 0)
			{
				_sound = null;
				return;
			}
			_sound = SoundEvent.CreateEvent(_soundIndex, base.Scene);
			if (_sound == null || !_sound.IsValid)
			{
				_sound = null;
				_soundIndex = -1;
				return;
			}
			_soundStarted = false;
		}
		_sound.SetPosition(position);
		if (!_sound.IsPlaying())
		{
			if (!_soundStarted)
			{
				_sound.Play();
				_soundStarted = true;
			}
			else if (_ability != null && _ability.Template.ShouldSoundLoopOverDuration)
			{
				_sound.Play();
			}
			else
			{
				_sound.Release();
				_sound = null;
			}
		}
	}

	private void StopOrReleaseSpellSound()
	{
		if (_sound == null)
		{
			return;
		}
		try
		{
			if (_sound.IsValid)
			{
				if (_sound.IsPlaying())
				{
					_sound.Stop();
				}
				else
				{
					_sound.Release();
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbilityScript.StopOrReleaseSpellSound failed: " + ex.Message);
		}
		_sound = null;
	}

	protected override void OnInit()
	{
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick;
	}

	protected virtual void OnBeforeTick(float dt)
	{
	}

	protected virtual void OnAfterTick(float dt)
	{
	}

	protected virtual bool ShouldMove()
	{
		if (_ability == null)
		{
			return false;
		}
		AbilityEffectType abilityEffectType = _ability.Template.AbilityEffectType;
		if (abilityEffectType != AbilityEffectType.Missile && abilityEffectType != AbilityEffectType.SeekerMissile && abilityEffectType != AbilityEffectType.Vortex)
		{
			return abilityEffectType == AbilityEffectType.Wind;
		}
		return true;
	}

	protected virtual MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
	{
		return oldFrame.Advance(_ability.Template.BaseMovementSpeed * dt);
	}

	protected override void OnTick(float dt)
	{
		if (_ability == null || !base.GameEntity.IsValid)
		{
			return;
		}
		if (Mission.Current == null || Mission.Current.MissionEnded)
		{
			Stop();
			return;
		}
		OnBeforeTick(dt);
		_timeSinceLastTick += dt;
		UpdateLifeTime(dt);
		if (IsFading)
		{
			return;
		}
		MatrixFrame matrixFrame = base.GameEntity.GetGlobalFrame();
		if (_controller != null)
		{
			matrixFrame = _controller.CalculateRotatedFrame(matrixFrame, dt);
		}
		AbilityTemplate template = _ability.Template;
		if (template.TriggerType == TriggerType.OnCollision && !template.Piercing && CollidedWithAgent())
		{
			HandleCollision(matrixFrame.origin, matrixFrame.origin.NormalizedCopy());
			return;
		}
		if (template.TriggerType == TriggerType.EveryTick && !_hasTriggered)
		{
			TriggerEffects(matrixFrame.origin, matrixFrame.origin.NormalizedCopy());
		}
		else if (template.TriggerType == TriggerType.EveryTick && _timeSinceLastTick > template.TickInterval)
		{
			_timeSinceLastTick = 0f;
			TriggerEffects(matrixFrame.origin, matrixFrame.origin.NormalizedCopy());
		}
		else if (template.TriggerType == TriggerType.TickOnce && _abilityLife > template.TickInterval && !_hasTriggered)
		{
			TriggerEffects(matrixFrame.origin, matrixFrame.origin.NormalizedCopy());
		}
		_hasTickedOnce = true;
		_previousFrameOrigin = matrixFrame.origin;
		if (ShouldMove())
		{
			MatrixFrame frame = GetNextGlobalFrame(matrixFrame, dt);
			base.GameEntity.SetGlobalFrame(in frame);
			try
			{
				using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
				{
					PhysicsShape bodyShape = base.GameEntity.GetBodyShape();
					if (bodyShape != null)
					{
						bodyShape.ManualInvalidate();
					}
				}
			}
			catch
			{
			}
		}
		UpdateSound(base.GameEntity.GetGlobalFrame().origin);
		OnAfterTick(dt);
		if (_lifeTimeExpired)
		{
			Stop();
		}
	}

	protected virtual bool CollidedWithAgent()
	{
		if (!_canCollide || _ability == null)
		{
			return false;
		}
		float num = _ability.Template.Radius + 1f;
		Vec3 origin = base.GameEntity.GetGlobalFrame().origin;
		foreach (Agent nearbyAgent in Mission.Current.GetNearbyAgents(origin.AsVec2, num, new MBList<Agent>()))
		{
			if (nearbyAgent != null && nearbyAgent != _casterAgent && Math.Abs(origin.Z - nearbyAgent.Position.Z) < num)
			{
				return true;
			}
		}
		return false;
	}

	protected override bool MovesEntity()
	{
		return true;
	}

	protected virtual void OnPhysicsCollision(ref PhysicsContact contact, WeakGameEntity entity0, WeakGameEntity entity1, bool isFirstShape)
	{
		if (_ability == null || _ability.Template.TriggerType != TriggerType.OnCollision || !_canCollide)
		{
			return;
		}
		PhysicsContactInfo contact2 = contact.ContactPair0.Contact0;
		if (_ability.Template.Piercing && this is MissileScript missileScript)
		{
			Agent agent = null;
			try
			{
				Vec3 sourcePoint = contact2.Position - contact2.Normal * 0.5f;
				Vec3 targetPoint = contact2.Position + contact2.Normal * 0.5f;
				int excludedAgentIndex = ((_casterAgent != null && _casterAgent.Health > 0f) ? _casterAgent.Index : (-1));
				using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
				{
					agent = Mission.Current.RayCastForClosestAgent(sourcePoint, targetPoint, excludedAgentIndex, 1f, out var _);
				}
			}
			catch
			{
			}
			if (agent != null)
			{
				SotorLog.Info($"OnPhysicsCollision (pierce) '{_ability.StringID}' hit agent '{agent.Name}' at {contact2.Position}.");
				missileScript.TryPierceAgent(agent, contact2.Position, contact2.Normal);
			}
			else
			{
				SotorLog.Info($"OnPhysicsCollision (pierce) '{_ability.StringID}' hit WORLD at {contact2.Position}; stopping.");
				Stop();
			}
		}
		else
		{
			SotorLog.Info($"OnPhysicsCollision '{_ability.StringID}' at {contact2.Position}.");
			HandleCollision(contact2.Position, contact2.Normal);
		}
	}

	protected virtual void HandleCollision(Vec3 position, Vec3 normal)
	{
		if (_hasTickedOnce && !_hasCollided && position.IsValid && position.IsNonZero)
		{
			TriggerEffects(position, normal);
			_hasCollided = true;
			Stop();
		}
	}

	protected bool TryGetWaterCrossing(Vec3 from, Vec3 to, out Vec3 crossPoint, out float crossDist)
	{
		crossPoint = default(Vec3);
		crossDist = float.MaxValue;
		float waterLevelAtPosition;
		using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
		{
			waterLevelAtPosition = Mission.Current.Scene.GetWaterLevelAtPosition(from.AsVec2, useWaterRenderer: true, checkWaterBodyEntities: true);
		}
		if (!(from.z > waterLevelAtPosition) || !(to.z <= waterLevelAtPosition))
		{
			return false;
		}
		float num = from.z - to.z;
		if (num <= 0.0001f)
		{
			return false;
		}
		float num2 = (from.z - waterLevelAtPosition) / num;
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		if (num2 > 1f)
		{
			num2 = 1f;
		}
		crossPoint = from + (to - from) * num2;
		crossPoint.z = waterLevelAtPosition;
		crossDist = (crossPoint - from).Length;
		return true;
	}

	protected void TriggerEffectsOnAgent(Agent agent, Vec3 position, Vec3 normal, float damageMultiplier = 1f)
	{
		if (agent == null)
		{
			return;
		}
		MBList<Agent> targets = new MBList<Agent> { agent };
		foreach (TriggeredEffect item in GetEffectsToTrigger())
		{
			item?.Trigger(position, normal, _casterAgent, targets, damageMultiplier);
		}
	}

	private void TriggerEffects(Vec3 position, Vec3 normal)
	{
		MBList<Agent> targets = ((_targetAgents != null && _targetAgents.Count > 0) ? _targetAgents : null);
		foreach (TriggeredEffect item in GetEffectsToTrigger())
		{
			item?.Trigger(position, normal, _casterAgent, targets);
		}
		_hasTriggered = true;
	}

	protected virtual List<TriggeredEffect> GetEffectsToTrigger()
	{
		List<TriggeredEffect> list = new List<TriggeredEffect>();
		if (_ability == null)
		{
			return list;
		}
		TriggeredEffectTemplate template = TriggeredEffectManager.GetTemplate(_ability.Template.TriggeredEffectID);
		if (template != null)
		{
			AbilityTargetType abilityTargetType = _ability.Template.AbilityTargetType;
			list.Add(new TriggeredEffect(template)
			{
				OwnerIsSingleTarget = (abilityTargetType == AbilityTargetType.SingleEnemy || abilityTargetType == AbilityTargetType.SingleAlly || abilityTargetType == AbilityTargetType.Self),
				OwnerSpellName = _ability.Template.Name,
				OwnerShipTag = _ability.Template.ShipTag,
				OwnerSpellTier = _ability.Template.SpellTier
			});
		}
		else if (!string.IsNullOrEmpty(_ability.Template.TriggeredEffectID))
		{
			SotorLog.Warn("AbilityScript: TriggeredEffectID '" + _ability.Template.TriggeredEffectID + "' not found.");
		}
		return list;
	}

	private void UpdateLifeTime(float dt)
	{
		if (_abilityLife < 0f)
		{
			_abilityLife = 0f;
		}
		else
		{
			_abilityLife += dt;
		}
		if (_ability != null && _abilityLife > _ability.Template.Duration && !IsFading)
		{
			_lifeTimeExpired = true;
		}
		float num = ((_ability != null && _ability.Template.Piercing) ? 0.001f : _minArmingTimeForCollision);
		if (_abilityLife > num)
		{
			_canCollide = true;
		}
	}

	public void Stop()
	{
		if (IsFading)
		{
			return;
		}
		IsFading = true;
		if (!(_entity != null))
		{
			return;
		}
		try
		{
			_entity.FadeOut(0.05f, isRemovingFromScene: true);
		}
		catch (Exception ex)
		{
			SotorLog.Warn("AbilityScript.Stop: FadeOut failed (" + ex.GetType().Name + "); entity may already be removed.");
		}
	}

	protected override void OnRemoved(int removeReason)
	{
		StopOrReleaseSpellSound();
		_ability = null;
		_entity = null;
		_casterAgent = null;
	}
}

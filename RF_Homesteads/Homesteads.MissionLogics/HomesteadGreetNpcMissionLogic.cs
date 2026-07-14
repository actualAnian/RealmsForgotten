using System;
using Homesteads.Models;
using SandBox.Conversation.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Homesteads.MissionLogics;

public class HomesteadGreetNpcMissionLogic : MissionLogic
{
	private const float TeleportDist = 1.5f;

	private const float RotationSettleSec = 0.1f;

	private const float CamBehind = 2.2f;

	private const float CamSide = 2.4f;

	private const float CamHeight = 3.4f;

	private const float CamLookHeight = 1f;

	private const float CamLookBiasToNpc = 0.6f;

	private readonly Hero? _target;

	private float _findTimeout = 12f;

	private float _settleTimer;

	private bool _locating = true;

	private bool _npcSeenLastFrame;

	private bool _settling;

	private bool _done;

	private Agent? _pendingPlayer;

	private Agent? _pendingNpc;

	public HomesteadGreetNpcMissionLogic(Hero? target)
	{
		_target = target;
	}

	public override void OnEndMissionInternal()
	{
		base.OnEndMissionInternal();
		SafeRestorePlayerController(_pendingPlayer);
		HomesteadFreeCameraView.Instance?.ClearConversationCamera();
	}

	public override void OnMissionTick(float dt)
	{
		if (_done || _target == null)
		{
			return;
		}
		if (_locating)
		{
			Agent mainAgent = Mission.Current.MainAgent;
			if (mainAgent == null || !mainAgent.IsActive())
			{
				return;
			}
			Agent agent = FindAgentForHero(_target);
			if (agent == null)
			{
				_npcSeenLastFrame = false;
				_findTimeout -= dt;
				if (_findTimeout <= 0f)
				{
					_done = true;
					TraceLogger.Write("HomesteadGreetNpcMissionLogic", $"'{_target.Name}' agent never spawned — falling back to plain walk-around.");
				}
				return;
			}
			if (!_npcSeenLastFrame)
			{
				_npcSeenLastFrame = true;
				return;
			}
			TeleportAndOrient(mainAgent, agent);
			_pendingPlayer = mainAgent;
			_pendingNpc = agent;
			_locating = false;
			_settling = true;
			_settleTimer = 0.1f;
		}
		if (_settling)
		{
			_settleTimer -= dt;
			if (!(_settleTimer > 0f))
			{
				_settling = false;
				_done = true;
				BeginConversation(_pendingPlayer, _pendingNpc);
			}
		}
	}

	private static Agent? FindAgentForHero(Hero hero)
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != null && agent.IsHuman && agent.IsActive())
			{
				CharacterObject characterObject = (agent.Character as CharacterObject) ?? (agent.Origin?.Troop as CharacterObject);
				if (characterObject != null && characterObject.IsHero && characterObject.HeroObject == hero)
				{
					return agent;
				}
			}
		}
		return null;
	}

	private void TeleportAndOrient(Agent player, Agent npc)
	{
		try
		{
			if ((player.Position - npc.Position).AsVec2.LengthSquared > 9f)
			{
				Vec3 vec = npc.Frame.rotation.f;
				vec.z = 0f;
				if (vec.LengthSquared < 0.01f)
				{
					vec = new Vec3(0f, 1f);
				}
				else
				{
					vec.Normalize();
				}
				Vec3 position = npc.Position + vec * 1.5f;
				position.z = npc.Position.z;
				player.TeleportToPosition(position);
				TraceLogger.Write("HomesteadGreetNpcMissionLogic", "Spawn placement missing — teleported player in front of NPC (mounted facing may be off).");
			}
			Vec3 position2 = player.Position;
			Vec3 position3 = npc.Position;
			Vec3 vec2 = position3 - position2;
			vec2.z = 0f;
			if (vec2.LengthSquared < 0.01f)
			{
				vec2 = npc.Frame.rotation.f;
				vec2.z = 0f;
			}
			vec2 = ((vec2.LengthSquared > 0.01f) ? vec2.NormalizedCopy() : new Vec3(0f, 1f));
			Vec3 vec3 = new Vec3(vec2.y, 0f - vec2.x);
			Vec3 camPos = position2 - vec2 * 2.2f + vec3 * 2.4f;
			camPos.z = position3.z + 3.4f;
			Vec3 lookTarget = position2 + (position3 - position2) * 0.6f;
			lookTarget.z = position3.z + 1f;
			HomesteadFreeCameraView.Instance?.SetConversationCamera(camPos, lookTarget);
			TraceLogger.Write("HomesteadGreetNpcMissionLogic", "Phase 1: over-the-shoulder conversation camera set; spawn placement owns body/horse facing.");
			npc.SetLookAgent(player);
			npc.SetLookToPointOfInterest(player.GetEyeGlobalPosition());
			player.SetLookAgent(npc);
			player.SetLookToPointOfInterest(npc.GetEyeGlobalPosition());
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadGreetNpcMissionLogic", "TeleportAndOrient threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private void BeginConversation(Agent player, Agent npc)
	{
		try
		{
			MissionConversationLogic val = Mission.Current?.GetMissionBehavior<MissionConversationLogic>();
			if (val == null)
			{
				TraceLogger.Write("HomesteadGreetNpcMissionLogic", "BeginConversation: MissionConversationLogic not found.");
				SafeRestorePlayerController(player);
				return;
			}
			val.StartConversation(npc, true, false);
			TraceLogger.Write("HomesteadGreetNpcMissionLogic", "Phase 2: conversation started with '" + npc.Name + "'.");
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				HomesteadFreeCameraView.Instance?.BeginConversationBlendOut();
				SafeRestorePlayerController(player);
				if (npc.IsActive())
				{
					npc.SetLookAgent(null);
					npc.SetLookToPointOfInterest(Vec3.Invalid);
				}
			};
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadGreetNpcMissionLogic", "BeginConversation threw " + ex.GetType().Name + ": " + ex.Message);
			SafeRestorePlayerController(player);
		}
	}

	private static void SafeRestorePlayerController(Agent? player)
	{
		try
		{
			if (player != null && player.IsActive())
			{
				player.SetLookAgent(null);
				player.SetLookToPointOfInterest(Vec3.Invalid);
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadGreetNpcMissionLogic", "SafeRestorePlayerController threw " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}

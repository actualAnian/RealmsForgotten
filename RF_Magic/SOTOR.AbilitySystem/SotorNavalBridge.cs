using System;
using System.Collections.Generic;
using System.Reflection;
using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.NavalPhysics;
using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;
using TaleWorlds.MountAndBlade.Objects.Usables;

namespace SOTOR.AbilitySystem;

internal static class SotorNavalBridge
{
	public const float ImpactHullMult = 50f;

	public const float FlamingFireMult = 40f;

	public const float FlamingHullMult = 5f;

	public const float EnergyHullMult = 8f;

	public const float EnergyFireMult = 25f;

	public const float ImpactSailMult = 25f;

	public const float FlamingSailMult = 22f;

	public const float EnergySailMult = 12f;

	public const int EscapeNone = 0;

	public const int EscapeSwimEnemy = 1;

	public const int EscapeSwimFriendly = 2;

	public const int EscapeRunEnemy = 3;

	public const int EscapeRunFriendly = 4;

	private const float MaxSwimDistance = 90f;

	private const float MaxSwimDistanceSq = 8100f;

	private const float FriendlyBiasMult = 0.6f;

	private static ActionIndexCache? _escapeJumpAnim;

	private static bool _escapeJumpResolved;

	private const float FootprintEdgeMargin = 1.15f;

	private const float DeckEdgeAgentDistSq = 9f;

	private static bool _fireReflectResolved;

	private static MethodInfo _getFirstScriptRecursiveClosed;

	private static MethodInfo _registerBlow;

	private static MethodInfo _startFire;

	private static PropertyInfo _scbGameEntityProp;

	public static float SailMult(string tag)
	{
		return tag switch
		{
			"impact" => 25f, 
			"flaming" => 22f, 
			"energy" => 12f, 
			_ => 0f, 
		};
	}

	public static float GetTierWeight(int tier)
	{
		return tier switch
		{
			1 => 0.6f, 
			2 => 0.8f, 
			4 => 1.2f, 
			_ => 1f, 
		};
	}

	public static bool IsNavalMission(Mission mission)
	{
		return mission?.IsNavalBattle ?? false;
	}

	public static void CollectAgentsOnAblazeDecks(Mission mission, List<int> outAgentIndices)
	{
		if (outAgentIndices == null)
		{
			return;
		}
		try
		{
			if (!IsNavalMission(mission) || mission.GetMissionBehavior<NavalShipsLogic>() == null)
			{
				return;
			}
			AgentReadOnlyList agents = mission.Agents;
			for (int i = 0; i < agents.Count; i++)
			{
				Agent agent = agents[i];
				if (agent != null && agent.IsActive() && !(agent.Health < 1f))
				{
					AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
					MissionShip val = ((component != null) ? component.SteppedShip : null);
					if (val != null && !val.IsSinking && val.FireHitPoints <= 0f)
					{
						outAgentIndices.Add(agent.Index);
					}
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.CollectAgentsOnAblazeDecks failed (" + ex.GetType().Name + "): " + ex.Message);
		}
	}

	public static bool TryGetEscapeTarget(Agent agent, out Vec3 deckPoint, out int targetShipIndex, out int quality, out bool needsSwim, out Vec3 waterEntryPoint)
	{
		deckPoint = Vec3.Zero;
		targetShipIndex = -1;
		quality = 0;
		needsSwim = false;
		waterEntryPoint = Vec3.Zero;
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return false;
			}
			AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
			MissionShip val = ((component != null) ? component.SteppedShip : null);
			Team team = agent.Team;
			if (val != null && !IsShipDoomed(val))
			{
				return false;
			}
			if (val != null && !ShipTeamMatches(val, team))
			{
				return false;
			}
			MissionShip val2 = null;
			int num = 0;
			if (val != null)
			{
				MBReadOnlyList<MissionShip> shipsConnectedWithBridges = val.GetShipsConnectedWithBridges();
				if (shipsConnectedWithBridges != null)
				{
					for (int i = 0; i < shipsConnectedWithBridges.Count; i++)
					{
						MissionShip val3 = shipsConnectedWithBridges[i];
						if (val3 != null && !IsShipDoomed(val3))
						{
							int num2 = (ShipTeamMatches(val3, team) ? 4 : 3);
							if (num2 > num)
							{
								num = num2;
								val2 = val3;
							}
						}
					}
				}
			}
			if (num < 3)
			{
				NavalShipsLogic obj = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
				MBReadOnlyList<MissionShip> mBReadOnlyList = ((obj != null) ? obj.AllShips : null);
				Vec3 position = agent.Position;
				float num3 = 8100f;
				if (mBReadOnlyList != null)
				{
					for (int j = 0; j < mBReadOnlyList.Count; j++)
					{
						MissionShip val4 = mBReadOnlyList[j];
						if (val4 == null || val4 == val || IsShipDoomed(val4) || !ShipHasUsableNet(val4))
						{
							continue;
						}
						float num4 = val4.GlobalFrame.origin.DistanceSquared(position);
						if (!(num4 > 8100f))
						{
							bool flag = ShipTeamMatches(val4, team);
							float num5 = (flag ? (num4 * 0.36f) : num4);
							if (num5 < num3)
							{
								num3 = num5;
								num = ((!flag) ? 1 : 2);
								val2 = val4;
								needsSwim = true;
							}
						}
					}
				}
			}
			if (val2 == null)
			{
				return false;
			}
			WorldPosition worldPosition = default(WorldPosition);
			val2.GetWorldPositionOnDeck(out worldPosition); // [RF-A] 1.4.7: parametro virou out
			Vec3 navMeshVec = worldPosition.GetNavMeshVec3();
			if (!navMeshVec.IsValid || !navMeshVec.IsNonZero)
			{
				return false;
			}
			deckPoint = navMeshVec;
			targetShipIndex = val2.Index;
			quality = num;
			if (num >= 3)
			{
				needsSwim = false;
			}
			else if (needsSwim && !TryGetWaterEntryPoint(agent, val, navMeshVec, out waterEntryPoint))
			{
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.TryGetEscapeTarget failed (" + ex.GetType().Name + "): " + ex.Message);
			return false;
		}
	}

	public static bool TryBoardViaClimbingNet(Agent agent, int targetShipIndex)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return false;
			}
			NavalShipsLogic val = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
			MissionShip val2 = default(MissionShip);
			if (val == null || !val.GetShipWithShipIndex(targetShipIndex, out val2) || val2 == null) // [RF-A] 1.4.7: parametro virou out
			{
				return false;
			}
			MBReadOnlyList<ClimbingMachine> climbingMachines = val2.ClimbingMachines;
			if (climbingMachines == null || climbingMachines.Count == 0)
			{
				return false;
			}
			Vec3 position = agent.Position;
			ClimbingMachine climbingMachine = null;
			StandingPoint standingPoint = null;
			bool flag = false;
			float num = float.MaxValue;
			for (int i = 0; i < climbingMachines.Count; i++)
			{
				ClimbingMachine climbingMachine2 = climbingMachines[i];
				StandingPoint standingPoint2 = climbingMachine2?.PilotStandingPoint;
				if (standingPoint2 != null)
				{
					float num2 = standingPoint2.GameEntity.GlobalPosition.DistanceSquared(position);
					bool flag2 = (!standingPoint2.HasUser || standingPoint2.UserAgent == agent) && standingPoint2.IsUsableByAgent(agent);
					if (standingPoint == null || (flag2 && !flag) || (flag2 == flag && num2 < num))
					{
						climbingMachine = climbingMachine2;
						standingPoint = standingPoint2;
						flag = flag2;
						num = num2;
					}
				}
			}
			if (climbingMachine == null || standingPoint == null)
			{
				return false;
			}
			Vec3 globalPosition = standingPoint.GameEntity.GlobalPosition;
			if (flag && globalPosition.DistanceSquared(position) < 6.25f)
			{
				if (!agent.IsUsingGameObject)
				{
					agent.UseGameObject(standingPoint);
				}
			}
			else
			{
				agent.SetTargetPosition(globalPosition.AsVec2);
			}
			return true;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.TryBoardViaClimbingNet failed (" + ex.GetType().Name + "): " + ex.Message);
			return false;
		}
	}

	public static bool MakeAgentJumpOverboard(Agent agent, Vec3 towardPoint)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				return false;
			}
			AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
			if (component == null)
			{
				return false;
			}
			Vec3 vec = towardPoint - agent.Position;
			vec.z = 0f;
			if (vec.LengthSquared < 0.01f)
			{
				vec = agent.LookDirection;
				vec.z = 0f;
			}
			vec = vec.NormalizedCopy();
			if (agent.IsUsingGameObject)
			{
				agent.StopUsingGameObject();
			}
			component.SetupAgentToAbandonShip(); // [RF-A] NavalDLC: renomeado, mesma semantica
			if (!_escapeJumpResolved)
			{
				_escapeJumpResolved = true;
				try
				{
					_escapeJumpAnim = ActionIndexCache.Create("act_escape_jump");
				}
				catch
				{
					_escapeJumpAnim = null;
				}
			}
			if (_escapeJumpAnim.HasValue && _escapeJumpAnim.Value.Index != ActionIndexCache.act_none.Index)
			{
				agent.SetActionChannel(0, _escapeJumpAnim.Value, ignorePriority: false, (AnimFlags)0uL);
			}
			agent.SetTargetPositionAndDirection((agent.Position + vec * 10f).AsVec2, in vec);
			agent.ClearTargetFrame();
			return true;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.MakeAgentJumpOverboard failed (" + ex.GetType().Name + "): " + ex.Message);
			return false;
		}
	}

	private static bool TryGetWaterEntryPoint(Agent agent, MissionShip myShip, Vec3 targetDeck, out Vec3 water)
	{
		water = Vec3.Zero;
		try
		{
			Vec3 position = agent.Position;
			Vec2 vec = targetDeck.AsVec2 - position.AsVec2;
			if (vec.Length < 0.01f)
			{
				return false;
			}
			vec = vec.Normalized();
			Scene scene = Mission.Current?.Scene;
			if (scene == null)
			{
				return false;
			}
			for (float num = 6f; num <= 22f; num += 4f)
			{
				Vec2 position2 = position.AsVec2 + vec * num;
				float waterLevelAtPosition = scene.GetWaterLevelAtPosition(position2, useWaterRenderer: true, checkWaterBodyEntities: true);
				float groundHeightAtPosition = scene.GetGroundHeightAtPosition(new Vec3(position2.x, position2.y, waterLevelAtPosition));
				if (waterLevelAtPosition >= groundHeightAtPosition - 0.25f)
				{
					water = new Vec3(position2.x, position2.y, waterLevelAtPosition);
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	public static int GetShipClimbingNetCount(int shipIndex)
	{
		try
		{
			NavalShipsLogic val = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
			MissionShip val2 = default(MissionShip);
			if (val == null || !val.GetShipWithShipIndex(shipIndex, out val2) || val2 == null) // [RF-A] 1.4.7: parametro virou out
			{
				return -1;
			}
			return val2.ClimbingMachines?.Count ?? 0;
		}
		catch
		{
			return -1;
		}
	}

	public static bool IsShipIndexDoomed(int shipIndex)
	{
		try
		{
			NavalShipsLogic val = Mission.Current?.GetMissionBehavior<NavalShipsLogic>();
			MissionShip val2 = default(MissionShip);
			if (val == null || !val.GetShipWithShipIndex(shipIndex, out val2) || val2 == null) // [RF-A] 1.4.7: parametro virou out
			{
				return true;
			}
			return IsShipDoomed(val2);
		}
		catch
		{
			return true;
		}
	}

	private static bool ShipHasUsableNet(MissionShip ship)
	{
		MBReadOnlyList<ClimbingMachine> mBReadOnlyList = ((ship != null) ? ship.ClimbingMachines : null);
		if (mBReadOnlyList != null)
		{
			return mBReadOnlyList.Count > 0;
		}
		return false;
	}

	private static bool IsShipDoomed(MissionShip ship)
	{
		if (ship != null)
		{
			if (!ship.IsSinking)
			{
				return ship.FireHitPoints <= 0f;
			}
			return true;
		}
		return false;
	}

	private static bool ShipTeamMatches(MissionShip ship, Team team)
	{
		if (ship == null || team == null)
		{
			return false;
		}
		Team team2 = ship.Team;
		if (team2 != null)
		{
			return team2 == team;
		}
		return false;
	}

	public static bool IsAgentShipDoomed(Agent agent)
	{
		try
		{
			AgentNavalComponent obj = agent?.GetComponent<AgentNavalComponent>();
			MissionShip ship = ((obj != null) ? obj.SteppedShip : null);
			if (!IsShipDoomed(ship))
			{
				return false;
			}
			return ShipTeamMatches(ship, agent.Team);
		}
		catch
		{
			return false;
		}
	}

	public static bool IsAgentOnSafeShip(Agent agent)
	{
		try
		{
			AgentNavalComponent obj = agent?.GetComponent<AgentNavalComponent>();
			MissionShip val = ((obj != null) ? obj.SteppedShip : null);
			return val != null && !IsShipDoomed(val);
		}
		catch
		{
			return false;
		}
	}

	public static void ApplyShipDamage(Mission mission, Agent caster, Vec3 position, string shipTag, float scaledBase, int nearbyAgentHintIndex, DamageType logElement, string spellName)
	{
		try
		{
			if (!IsNavalMission(mission) || string.IsNullOrEmpty(shipTag))
			{
				return;
			}
			NavalShipsLogic missionBehavior = mission.GetMissionBehavior<NavalShipsLogic>();
			if (missionBehavior == null)
			{
				return;
			}
			MissionShip val = ResolveShip(mission, missionBehavior, position, caster, nearbyAgentHintIndex);
			if (val == null || val.IsSinking)
			{
				return;
			}
			bool flag = val.FireHitPoints <= 0f;
			float num = 0f;
			float num2 = 0f;
			switch (shipTag.ToLowerInvariant())
			{
			default:
				return;
			case "impact":
				num = 50f;
				break;
			case "flaming":
				num = 5f;
				num2 = 40f;
				break;
			case "energy":
				num = 8f;
				num2 = 25f;
				break;
			}
			int num4;
			if (num > 0f)
			{
				float num3 = scaledBase * num;
				if (num3 > 0f)
				{
					val.DealCollisionDamage((MissionShip)null, false, position, num3);
					if (!(val.HitPoints <= 0f))
					{
						num4 = (val.IsSinking ? 1 : 0);
						if (num4 == 0)
						{
							goto IL_010c;
						}
					}
					else
					{
						num4 = 1;
					}
					if (!val.IsSinking)
					{
						val.SetSinkingState((NavalPhysics.SinkingState)1); // [RF-A] SinkingState e enum aninhado em NavalPhysics
					}
					goto IL_010c;
				}
			}
			goto IL_0131;
			IL_0131:
			if (num2 > 0f)
			{
				float num5 = scaledBase * num2;
				if (num5 > 0f)
				{
					val.DealFireDamage(num5);
					bool flag2 = val.FireHitPoints <= 0f;
					if (flag2 && !flag)
					{
						SotorSpellDamageLog.BookShipEvent(caster, logElement, spellName, "sets the ship ablaze!");
					}
					else if (!flag2)
					{
						SotorSpellDamageLog.BookShipEvent(caster, logElement, spellName, "scorches the ship");
					}
					TryDriveVisualFire(val, position, num5 > 40f, flag2);
				}
			}
			float num6 = scaledBase * SailMult(shipTag);
			if (num6 > 0f)
			{
				TryDamageNearestSail(val, caster, position, num6);
			}
			return;
			IL_010c:
			if (num4 != 0)
			{
				SotorSpellDamageLog.BookShipEvent(caster, logElement, spellName, "sends the ship to the depths!");
				return;
			}
			SotorSpellDamageLog.BookShipEvent(caster, logElement, spellName, "smashes into the hull");
			goto IL_0131;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.ApplyShipDamage failed (" + ex.GetType().Name + "): " + ex.Message);
		}
	}

	private static MissionShip ResolveShip(Mission mission, NavalShipsLogic shipsLogic, Vec3 position, Agent caster, int nearbyAgentHintIndex)
	{
		Vec2 asVec = position.AsVec2;
		MissionShip val = null;
		float num = float.MaxValue;
		MBReadOnlyList<MissionShip> allShips = shipsLogic.AllShips;
		if (allShips != null)
		{
			for (int i = 0; i < allShips.Count; i++)
			{
				MissionShip val2 = allShips[i];
				if (val2 != null && !val2.IsSinking && PointOverShip(val2, asVec))
				{
					float num2 = val2.GlobalFrame.origin.AsVec2.DistanceSquared(asVec);
					if (num2 < num)
					{
						num = num2;
						val = val2;
					}
				}
			}
		}
		if (val != null)
		{
			return val;
		}
		if (nearbyAgentHintIndex >= 0)
		{
			Agent agent = mission.FindAgentWithIndex(nearbyAgentHintIndex);
			if (agent != null && agent.IsActive() && agent.Position.AsVec2.DistanceSquared(asVec) <= 9f)
			{
				MissionShip val3 = ShipUnderAgent(agent);
				if (val3 != null && !val3.IsSinking)
				{
					return val3;
				}
			}
		}
		return null;
	}

	private static bool PointOverShip(MissionShip ship, Vec2 xy)
	{
		try
		{
			MatrixFrame globalFrame = ship.GlobalFrame;
			Vec2[] array = ship.CalculateBoundingXYGlobalPlaneFromLocal(ref globalFrame);
			if (array == null || array.Length < 4)
			{
				return false;
			}
			if (MBMath.CheckPointInsidePolygon(in array[0], in array[1], in array[2], in array[3], in xy))
			{
				return true;
			}
			Vec2 vec = (array[0] + array[1] + array[2] + array[3]) * 0.25f;
			if (MBMath.CheckPointInsidePolygon(vec + (array[0] - vec) * 1.15f, vec + (array[1] - vec) * 1.15f, vec + (array[2] - vec) * 1.15f, vec + (array[3] - vec) * 1.15f, in xy))
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static MissionShip ShipUnderAgent(Agent agent)
	{
		if (agent == null)
		{
			return null;
		}
		AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
		if (component == null)
		{
			return null;
		}
		return component.SteppedShip ?? component.FormationShip;
	}

	private static void TryDamageNearestSail(MissionShip ship, Agent caster, Vec3 position, float sailDmg)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		try
		{
			if ((int)ship.ShipSailState == 2)
			{
				return;
			}
			MBReadOnlyList<MissionSail> sails = ship.Sails;
			if (sails == null || sails.Count == 0)
			{
				ship.DealDamageToSails(caster, sailDmg, sailDmg, (MissionSail)null); // [RF-A] NavalDLC: ganhou parametro inflictedDamage
				return;
			}
			MissionSail val = null;
			float num = float.MaxValue;
			MBReadOnlyList<GameEntity> sailMeshEntities = ship.SailMeshEntities;
			if (sailMeshEntities != null && sailMeshEntities.Count == sails.Count)
			{
				for (int i = 0; i < sailMeshEntities.Count; i++)
				{
					GameEntity gameEntity = sailMeshEntities[i];
					if (!(gameEntity == null))
					{
						float num2 = gameEntity.GetGlobalFrame().origin.DistanceSquared(position);
						if (num2 < num)
						{
							num = num2;
							val = sails[i];
						}
					}
				}
			}
			if (val == null)
			{
				val = sails[0];
			}
			ship.DealDamageToSails(caster, sailDmg, sailDmg, val); // [RF-A] NavalDLC: ganhou parametro inflictedDamage
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.TryDamageNearestSail failed (" + ex.GetType().Name + "): " + ex.Message);
		}
	}

	private static void ResolveFireReflection()
	{
		_fireReflectResolved = true;
		try
		{
			Assembly assembly = typeof(MissionShip).Assembly;
			Type type = assembly.GetType("ShipBurningSystem", throwOnError: false) ?? Array.Find(assembly.GetTypes(), (Type t) => t.Name == "ShipBurningSystem");
			if (!(type == null))
			{
				_registerBlow = type.GetMethod("RegisterBlow", new Type[1] { typeof(Vec3) });
				_startFire = type.GetMethod("StartFire", Type.EmptyTypes);
				MethodInfo methodInfo = Array.Find(typeof(WeakGameEntity).GetMethods(), (MethodInfo m) => m.Name == "GetFirstScriptOfTypeRecursive" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
				if (methodInfo != null)
				{
					_getFirstScriptRecursiveClosed = methodInfo.MakeGenericMethod(type);
				}
				_scbGameEntityProp = typeof(ScriptComponentBehavior).GetProperty("GameEntity");
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge: visual-fire reflection resolve failed (" + ex.GetType().Name + "): " + ex.Message + ". Flames will be cosmetic-only via the game's own spiral.");
		}
	}

	private static void TryDriveVisualFire(MissionShip ship, Vec3 position, bool bigHit, bool nowAblaze)
	{
		try
		{
			if (!_fireReflectResolved)
			{
				ResolveFireReflection();
			}
			if (_getFirstScriptRecursiveClosed == null || _scbGameEntityProp == null)
			{
				return;
			}
			object value = _scbGameEntityProp.GetValue(ship);
			if (value == null)
			{
				return;
			}
			object obj = _getFirstScriptRecursiveClosed.Invoke(value, null);
			if (obj != null)
			{
				if (nowAblaze && _startFire != null)
				{
					_startFire.Invoke(obj, null);
				}
				else if (bigHit && _registerBlow != null)
				{
					_registerBlow.Invoke(obj, new object[1] { position });
				}
			}
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.TryDriveVisualFire failed (" + ex.GetType().Name + "): " + ex.Message);
		}
	}

	public static int GetSummonedAgentNavalBinding(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return 0;
			}
			AgentNavalComponent component = agent.GetComponent<AgentNavalComponent>();
			if (component == null)
			{
				return 0;
			}
			return (component.SteppedShip != null || component.FormationShip != null) ? 1 : 2;
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.GetSummonedAgentNavalBinding failed (" + ex.GetType().Name + "): " + ex.Message);
			return 0;
		}
	}

	public static MethodInfo ResolveNavalOnCombatHitMethod()
	{
		try
		{
			Assembly assembly = typeof(MissionShip).Assembly;
			Type type = assembly.GetType("NavalDLC.CharacterDevelopment.NavalSkillLevellingManager", throwOnError: false) ?? Array.Find(assembly.GetTypes(), (Type t) => t.Name == "NavalSkillLevellingManager");
			if (type == null)
			{
				return null;
			}
			return Array.Find(type.GetMethods(BindingFlags.Instance | BindingFlags.Public), (MethodInfo m) => m.Name == "OnCombatHit");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("SotorNavalBridge.ResolveNavalOnCombatHitMethod failed (" + ex.GetType().Name + "): " + ex.Message);
			return null;
		}
	}
}

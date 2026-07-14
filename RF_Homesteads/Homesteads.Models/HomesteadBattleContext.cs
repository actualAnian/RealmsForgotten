using System;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Homesteads.Models;

internal static class HomesteadBattleContext
{
	private static MapEvent? pendingMapEvent;

	private static Homestead? pendingHomestead;

	private static bool appliedToCurrentMission;

	public static bool BypassMeetingSkip;

	public static bool HasPendingHomestead => pendingHomestead != null;

	public static bool SuppressEncounterReinit { get; set; }

	public static void TryPrepare(MapEvent mapEvent, PartyBase attacker, PartyBase defender)
	{
		object obj = ((defender?.MobileParty == null) ? null : Homestead.GetFor(defender.MobileParty));
		Homestead homestead = ((attacker?.MobileParty == null) ? null : Homestead.GetFor(attacker.MobileParty));
		if (obj == null)
		{
			obj = homestead;
		}
		Homestead homestead2 = (Homestead)obj;
		if (homestead2 != null)
		{
			pendingMapEvent = mapEvent;
			pendingHomestead = homestead2;
			appliedToCurrentMission = false;
			TraceLogger.Write("HomesteadBattleContext", string.Format("Prepared homestead battle context for '{0}' scene='{1}' attacker='{2}' defender='{3}'", homestead2.Name, homestead2.GetHomesteadScene().SceneName, attacker?.MobileParty?.StringId ?? "null", defender?.MobileParty?.StringId ?? "null"));
		}
	}

	public static bool TryApplyTo(ref MissionInitializerRecord record, string source)
	{
		if (!IsBattleSceneRecord(record))
		{
			TraceLogger.Write("HomesteadBattleContext", "Skipped homestead battle context in " + source + " because mission record scene '" + (record.SceneName ?? "null") + "' does not look like a battle scene.");
			return false;
		}
		EnsurePreparedFromActivePlayerEncounter(source);
		if (pendingHomestead == null || pendingMapEvent == null)
		{
			return false;
		}
		if (!IsLikelyPlayerHomesteadBattle())
		{
			TraceLogger.Write("HomesteadBattleContext", string.Format("Skipped homestead battle scene in {0} because player map event did not match pending homestead event. pending='{1}' originalScene='{2}'", source, pendingHomestead.Name, record.SceneName ?? "null"));
			return false;
		}
		string sceneName = pendingHomestead.GetHomesteadScene().SceneName;
		string text = record.SceneName ?? "";
		TraceLogger.Write("HomesteadBattleContext", string.Format("Battle mission record before redirect in {0}: eventType={1} eventTerrain={2} originalScene='{3}' sceneLevels='{4}' needsRandomTerrain={5} sceneHasMapPatch={6} patchCoordinates={7} patchEncounterDir={8} terrainType={9} decalAtlasGroup={10}.", source, pendingMapEvent.EventType, pendingMapEvent.EventTerrainType, text, record.SceneLevels ?? "null", record.NeedsRandomTerrain, record.SceneHasMapPatch, Format(record.PatchCoordinates), Format(record.PatchEncounterDir), record.TerrainType, record.DecalAtlasGroup));
		record.SceneName = sceneName;
		record.SceneLevels = "";
		record.NeedsRandomTerrain = false;
		record.DecalAtlasGroup = 2;
		if (pendingHomestead.MobileParty != null)
		{
			Vec2 getPosition2D = pendingHomestead.MobileParty.GetPosition2D;
			record.PatchCoordinates = getPosition2D;
		}
		appliedToCurrentMission = true;
		TraceLogger.Write("HomesteadBattleContext", $"Redirected player battle mission in {source} from scene '{text}' to homestead scene '{sceneName}' for '{pendingHomestead.Name}'. Updated record: needsRandomTerrain={record.NeedsRandomTerrain} sceneHasMapPatch={record.SceneHasMapPatch} patchCoordinates={Format(record.PatchCoordinates)} patchEncounterDir={Format(record.PatchEncounterDir)} terrainType={record.TerrainType} decalAtlasGroup={record.DecalAtlasGroup}.");
		return true;
	}

	public static bool TryApplyTo(ref string scene, string source)
	{
		EnsurePreparedFromActivePlayerEncounter(source);
		if (pendingHomestead == null || pendingMapEvent == null)
		{
			return false;
		}
		if (!IsLikelyPlayerHomesteadBattle())
		{
			TraceLogger.Write("HomesteadBattleContext", string.Format("Skipped homestead battle scene in {0} because player map event did not match pending homestead event. pending='{1}' originalScene='{2}'", source, pendingHomestead.Name, scene ?? "null"));
			return false;
		}
		string text = scene ?? "";
		scene = pendingHomestead.GetHomesteadScene().SceneName;
		appliedToCurrentMission = true;
		TraceLogger.Write("HomesteadBattleContext", $"Redirected player battle mission in {source} from scene '{text}' to homestead scene '{scene}' for '{pendingHomestead.Name}'. eventType={pendingMapEvent.EventType} eventTerrain={pendingMapEvent.EventTerrainType}.");
		return true;
	}

	public static Homestead? TryGetHomesteadFromActivePlayerEncounter(string source)
	{
		EnsurePreparedFromActivePlayerEncounter(source);
		return pendingHomestead;
	}

	public static Homestead? TryGetHomesteadForMissionAttach()
	{
		if (!appliedToCurrentMission)
		{
			return null;
		}
		return pendingHomestead;
	}

	public static void MarkMissionAttached(string source)
	{
		Homestead homestead = pendingHomestead;
		pendingHomestead = null;
		appliedToCurrentMission = false;
		if (homestead != null)
		{
			TraceLogger.Write("HomesteadBattleContext", $"Consumed homestead battle context after attaching mission logic via {source}: '{homestead.Name}'.");
		}
	}

	public static void ClearIfMatches(MapEvent mapEvent, string reason)
	{
		if (pendingMapEvent != null && (pendingMapEvent == mapEvent || pendingMapEvent == mapEvent))
		{
			Clear(reason);
		}
	}

	public static void Clear(string reason)
	{
		if (pendingHomestead != null || pendingMapEvent != null)
		{
			TraceLogger.Write("HomesteadBattleContext", "Cleared homestead battle context during " + reason + "; homestead='" + (pendingHomestead?.Name.ToString() ?? "null") + "'.");
		}
		pendingMapEvent = null;
		pendingHomestead = null;
		appliedToCurrentMission = false;
	}

	private static bool IsLikelyPlayerHomesteadBattle()
	{
		if (pendingMapEvent == null)
		{
			return false;
		}
		MapEvent playerMapEvent = MapEvent.PlayerMapEvent;
		if (playerMapEvent == pendingMapEvent || playerMapEvent == pendingMapEvent)
		{
			return true;
		}
		if (pendingMapEvent.IsPlayerMapEvent)
		{
			return true;
		}
		return PartyBase.MainParty?.MapEventSide?.MapEvent == pendingMapEvent;
	}

	private static void EnsurePreparedFromActivePlayerEncounter(string source)
	{
		if (pendingHomestead != null && pendingMapEvent != null)
		{
			return;
		}
		MapEvent mapEvent = MapEvent.PlayerMapEvent ?? TryGetPlayerEncounteredBattle(source) ?? PartyBase.MainParty?.MapEventSide?.MapEvent;
		Homestead homestead = TryFindHomesteadInMapEvent(mapEvent);
		if (homestead == null && mapEvent != null)
		{
			Homestead homestead2 = HomesteadBehavior.Instance?.CurrentHomestead;
			MobileParty mobileParty = homestead2?.MobileParty;
			if (mobileParty != null && MobileParty.MainParty != null && mobileParty.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) < 5f)
			{
				homestead = homestead2;
			}
		}
		if (homestead != null)
		{
			pendingMapEvent = mapEvent;
			pendingHomestead = homestead;
			appliedToCurrentMission = false;
			TraceLogger.Write("HomesteadBattleContext", $"Recovered homestead battle context from active player encounter in {source} for '{homestead.Name}' scene='{homestead.GetHomesteadScene().SceneName}'.");
		}
	}

	private static MapEvent? TryGetPlayerEncounteredBattle(string source)
	{
		try
		{
			// Missions without a player encounter (hunts, arena, custom scenes)
			// reach this via the MissionState.OpenNew prefix. The vanilla
			// EncounteredBattle getter dereferences PlayerEncounter.Current and
			// throws NRE when there is none — guard instead of catching it.
			if (PlayerEncounter.Current == null)
			{
				return null;
			}
			return PlayerEncounter.EncounteredBattle;
		}
		catch (Exception ex)
		{
			TraceLogger.Write("HomesteadBattleContext", "Could not read PlayerEncounter.EncounteredBattle in " + source + ": " + ex.GetType().Name + ": " + ex.Message);
			return null;
		}
	}

	private static bool IsBattleSceneRecord(MissionInitializerRecord record)
	{
		string text = record.SceneName ?? "";
		if (text.Length == 0)
		{
			return record.NeedsRandomTerrain;
		}
		string text2 = text.ToLowerInvariant();
		if (!record.NeedsRandomTerrain && !text2.Contains("battle") && !text2.Contains("combat") && !text2.Contains("caravan"))
		{
			return text2.Contains("homestead");
		}
		return true;
	}

	private static Homestead? TryFindHomesteadInMapEvent(MapEvent? mapEvent)
	{
		if (mapEvent == null)
		{
			return null;
		}
		return TryFindHomesteadOnSide(mapEvent, BattleSideEnum.Attacker) ?? TryFindHomesteadOnSide(mapEvent, BattleSideEnum.Defender);
	}

	private static Homestead? TryFindHomesteadOnSide(MapEvent mapEvent, BattleSideEnum side)
	{
		foreach (MapEventParty item in mapEvent.PartiesOnSide(side))
		{
			MobileParty mobileParty = item?.Party?.MobileParty;
			if (mobileParty != null)
			{
				Homestead homestead = Homestead.GetFor(mobileParty);
				if (homestead != null)
				{
					return homestead;
				}
			}
		}
		return null;
	}

	private static string Format(Vec2 value)
	{
		return $"({value.x:0.####}, {value.y:0.####})";
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Homesteads.MissionLogics;
using Homesteads.Models;
using Homesteads.Views;
using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using SandBox.View;
using SandBox.View.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Homesteads;

public static class CustomMissions
{
	public const string PlanningSceneName = "mp_skirmish_spawn_test";

	private static Assembly? _aiInfluenceAssembly;

	private static bool _aiInfluenceAssemblySearched;

	private static MissionInitializerRecord CreateHomesteadVisitRecord(Homestead homestead, string sceneName)
	{
		MissionInitializerRecord result = SandBoxMissions.CreateSandBoxMissionInitializerRecord(sceneName, "", false, DecalAtlasGroup.Town);
		Vec2 patchCoordinates = homestead.MobileParty?.GetPosition2D ?? Vec2.Zero;
		if (patchCoordinates.x != 0f || patchCoordinates.y != 0f)
		{
			result.SceneHasMapPatch = true;
			result.PatchCoordinates = patchCoordinates;
			TraceLogger.Write("CustomMissions", $"Applied terrain patch to homestead visit record for '{homestead.Name}': sceneHasMapPatch=true patchCoordinates=({patchCoordinates.x:0.####}, {patchCoordinates.y:0.####}) scene='{sceneName}'.");
		}
		else
		{
			TraceLogger.Write("CustomMissions", $"Could not read homestead anchor for '{homestead.Name}' (MobileParty null or at origin) — terrain patch NOT applied to visit record.");
		}
		return result;
	}

	private static MissionInitializerRecord CreatePlanningRecord(string sceneName)
	{
		return SandBoxMissions.CreateSandBoxMissionInitializerRecord(sceneName, "", false, DecalAtlasGroup.Town);
	}

	public static Mission StartHomesteadSparringMission(Homestead homestead, int teamSize, TroopRoster? playerRoster = null, TroopRoster? enemyRoster = null)
	{
		string scene = SkirmishMaps.Default.Scene;
		TraceLogger.Write("CustomMissions", $"StartHomesteadSparringMission: scene='{scene}' teamSize={teamSize} homestead='{homestead.Name}'.");
		return MissionState.OpenNew(scene, SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town), delegate(Mission mission)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Expected O, but got Unknown
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				(MissionBehavior)new CampaignMissionComponent(),
				new BasicLeaveMissionLogic(),
				(MissionBehavior)new MissionSingleplayerViewHandler(),
				(MissionBehavior)new MissionAgentLookHandler(),
				(MissionBehavior)new HeroSkillHandler(),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic(),
				new MissionBoundaryPlacer()
			};
			list.AddRange(new MissionBehavior[12]
			{
				new AgentVictoryLogic(),
				new BannerBearerLogic(),
				(MissionBehavior)new MissionMainAgentController(),
				new EquipmentControllerLeaveLogic(),
				(MissionBehavior)(object)ViewCreator.CreateMissionLeaveView(),
				(MissionBehavior)(object)ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				(MissionBehavior)(object)ViewCreator.CreateMissionMainAgentCheerBarkControllerView((Mission)null),
				(MissionBehavior)(object)ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
				(MissionBehavior)(object)ViewCreator.CreateOptionsUIHandler(),
				(MissionBehavior)(object)ViewCreator.CreatePhotoModeView(),
				(MissionBehavior)(object)new HomesteadSparringOverlayView(),
				new HomesteadSparringMissionLogic(homestead, teamSize, playerRoster, enemyRoster)
			});
			return list.ToArray();
		});
	}

	public static Mission StartHomesteadSparringMission(Settlement settlement, Hero armsMaster, int teamSize, TroopRoster? playerRoster = null, TroopRoster? enemyRoster = null)
	{
		string scene = SkirmishMaps.Default.Scene;
		TraceLogger.Write("CustomMissions", $"StartHomesteadSparringMission (settlement): scene='{scene}' teamSize={teamSize} settlement='{settlement.Name}'.");
		return MissionState.OpenNew(scene, SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town), delegate(Mission mission)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Expected O, but got Unknown
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				(MissionBehavior)new CampaignMissionComponent(),
				new BasicLeaveMissionLogic(),
				(MissionBehavior)new MissionSingleplayerViewHandler(),
				(MissionBehavior)new MissionAgentLookHandler(),
				(MissionBehavior)new HeroSkillHandler(),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic(),
				new MissionBoundaryPlacer()
			};
			list.AddRange(new MissionBehavior[12]
			{
				new AgentVictoryLogic(),
				new BannerBearerLogic(),
				(MissionBehavior)new MissionMainAgentController(),
				new EquipmentControllerLeaveLogic(),
				(MissionBehavior)(object)ViewCreator.CreateMissionLeaveView(),
				(MissionBehavior)(object)ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				(MissionBehavior)(object)ViewCreator.CreateMissionMainAgentCheerBarkControllerView((Mission)null),
				(MissionBehavior)(object)ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
				(MissionBehavior)(object)ViewCreator.CreateOptionsUIHandler(),
				(MissionBehavior)(object)ViewCreator.CreatePhotoModeView(),
				(MissionBehavior)(object)new HomesteadSparringOverlayView(),
				new HomesteadSparringMissionLogic(settlement, armsMaster, teamSize, playerRoster, enemyRoster)
			});
			return list.ToArray();
		});
	}

	public static Mission StartHorseRaceMission(Homestead? homestead, RaceTrack track, List<Hero>? rivals = null)
	{
		string scene = track.Scene;
		TraceLogger.Write("CustomMissions", string.Format("StartHorseRaceMission: track='{0}' scene='{1}' homestead='{2}' rivals={3}.", track.Id, scene, homestead?.Name?.ToString() ?? "(console)", rivals?.Count ?? 0));
		return MissionState.OpenNew(scene, SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.Town), delegate(Mission mission)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Expected O, but got Unknown
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				(MissionBehavior)new CampaignMissionComponent(),
				(MissionBehavior)new MissionBasicTeamLogic(),
				(MissionBehavior)new MissionSingleplayerViewHandler(),
				(MissionBehavior)new MissionAgentLookHandler(),
				(MissionBehavior)new HeroSkillHandler(),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic(),
				new MissionBoundaryPlacer()
			};
			list.AddRange(new MissionBehavior[8]
			{
				(MissionBehavior)new MissionMainAgentController(),
				new EquipmentControllerLeaveLogic(),
				(MissionBehavior)(object)ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				(MissionBehavior)(object)ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
				(MissionBehavior)(object)ViewCreator.CreateOptionsUIHandler(),
				(MissionBehavior)(object)ViewCreator.CreatePhotoModeView(),
				(MissionBehavior)(object)new HomesteadRaceProgressView(),
				new HomesteadRaceMissionLogic(homestead, track, rivals)
			});
			return list.ToArray();
		});
	}

	public static Mission? StartHomesteadTavernMission(Homestead homestead)
	{
		string text = ResolveTavernSceneName(homestead);
		if (string.IsNullOrEmpty(text))
		{
			TraceLogger.Write("CustomMissions", $"StartHomesteadTavernMission: could not resolve a tavern scene for '{homestead.Name}' — aborting.");
			InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=hr_tavern_no_scene}There is no nearby tavern to model this one after.").ToString()));
			return null;
		}
		TraceLogger.Write("CustomMissions", $"StartHomesteadTavernMission: scene='{text}' homestead='{homestead.Name}'.");
		return MissionState.OpenNew("HomesteadTavern", SandBoxMissions.CreateSandBoxMissionInitializerRecord(text, "", false, DecalAtlasGroup.Town), delegate(Mission mission)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected O, but got Unknown
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Expected O, but got Unknown
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Expected O, but got Unknown
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				(MissionBehavior)new CampaignMissionComponent(),
				(MissionBehavior)new MissionBasicTeamLogic(),
				new BasicLeaveMissionLogic(),
				(MissionBehavior)new MissionSingleplayerViewHandler(),
				(MissionBehavior)new MissionAgentLookHandler(),
				(MissionBehavior)new HeroSkillHandler(),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic()
			};
			AddThirdPartyMissionBehaviors(list);
			list.AddRange(new MissionBehavior[13]
			{
				(MissionBehavior)new MissionMainAgentController(),
				new EquipmentControllerLeaveLogic(),
				new HomesteadConversationMissionLogic(),
				(MissionBehavior)new MissionConversationLogic(),
				(MissionBehavior)(object)ViewCreator.CreateMissionLeaveView(),
				(MissionBehavior)(object)ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				(MissionBehavior)(object)SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				(MissionBehavior)(object)ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
				(MissionBehavior)(object)ViewCreator.CreateOptionsUIHandler(),
				(MissionBehavior)(object)ViewCreator.CreatePhotoModeView(),
				(MissionBehavior)(object)SandBoxViewCreator.CreateMissionConversationView(mission),
				new HomesteadTavernMissionLogic(homestead),
				new AIInfluenceGroupChatBridge()
			});
			return list.ToArray();
		});
	}

	private static string ResolveTavernSceneName(Homestead homestead)
	{
		try
		{
			CultureObject cultureObject = Hero.MainHero?.Culture;
			string preferredTavernCultureId = homestead.PreferredTavernCultureId;
			Vec2 hsPos = homestead.MobileParty?.GetPosition2D ?? Vec2.Zero;
			Settlement settlement = ((!string.IsNullOrEmpty(preferredTavernCultureId)) ? Pick(preferredTavernCultureId) : null) ?? Pick(cultureObject?.StringId) ?? Pick(null);
			if (settlement == null)
			{
				TraceLogger.Write("CustomMissions", "ResolveTavernSceneName: no town with a LocationComplex found.");
				return "";
			}
			Location locationWithId = settlement.LocationComplex.GetLocationWithId("tavern");
			if (locationWithId == null)
			{
				TraceLogger.Write("CustomMissions", $"ResolveTavernSceneName: town '{settlement.Name}' has no 'tavern' location.");
				return "";
			}
			int num = settlement.Town?.GetWallLevel() ?? 1;
			string sceneName = locationWithId.GetSceneName(num);
			TraceLogger.Write("CustomMissions", $"ResolveTavernSceneName: borrowing tavern '{sceneName}' from '{settlement.Name}' " + $"(culture='{settlement.Culture?.StringId}' wallLevel={num}, sameCulture={settlement.Culture == cultureObject}).");
			return sceneName ?? "";
			Settlement? Pick(string? cultureIdFilter)
			{
				return (from s in Settlement.All
					where s.IsTown && s.LocationComplex != null && (cultureIdFilter == null || s.Culture?.StringId == cultureIdFilter)
					orderby (s.GetPosition2D - hsPos).LengthSquared
					select s).FirstOrDefault();
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("CustomMissions", "ResolveTavernSceneName failed: " + ex.GetType().Name + ": " + ex.Message);
			return "";
		}
	}

	public static Mission StartHomesteadMission(Homestead homestead)
	{
		string sceneName = homestead.GetHomesteadScene().SceneName;
		Hero talkTarget = HomesteadBehavior.Instance?.PendingTalkHero;
		if (HomesteadBehavior.Instance != null)
		{
			HomesteadBehavior.Instance.PendingTalkHero = null;
		}
		return MissionState.OpenNew(sceneName, CreateHomesteadVisitRecord(homestead, sceneName), delegate(Mission mission)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected O, but got Unknown
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Expected O, but got Unknown
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Expected O, but got Unknown
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				(MissionBehavior)new CampaignMissionComponent(),
				(MissionBehavior)new MissionBasicTeamLogic(),
				new BasicLeaveMissionLogic(),
				(MissionBehavior)new MissionSingleplayerViewHandler(),
				(MissionBehavior)new MissionAgentLookHandler(),
				(MissionBehavior)new HeroSkillHandler(),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic(),
				new MissionBoundaryPlacer()
			};
			AddThirdPartyMissionBehaviors(list);
			list.AddRange(new MissionBehavior[19]
			{
				(MissionBehavior)new MissionMainAgentController(),
				new EquipmentControllerLeaveLogic(),
				new HomesteadConversationMissionLogic(),
				(MissionBehavior)new MissionConversationLogic(),
				(MissionBehavior)(object)ViewCreator.CreateMissionLeaveView(),
				(MissionBehavior)(object)ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				(MissionBehavior)(object)SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				(MissionBehavior)(object)ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
				(MissionBehavior)(object)ViewCreator.CreateOptionsUIHandler(),
				(MissionBehavior)(object)ViewCreator.CreatePhotoModeView(),
				(MissionBehavior)(object)SandBoxViewCreator.CreateMissionConversationView(mission),
				new HomesteadSpawningMissionLogic(homestead, isPlanningMode: false, talkTarget),
				new HomesteadSceneEditingMissionLogic(homestead),
				new HomesteadTrainingFieldMissionLogic(homestead),
				(MissionBehavior)(object)new HomesteadMissionView(homestead),
				(MissionBehavior)(object)new HomesteadFreeCameraView(),
				(MissionBehavior)(object)new HomesteadBuildingPickerView(),
				new AIInfluenceGroupChatBridge(),
				new HomesteadGreetNpcMissionLogic(talkTarget)
			});
			return list.ToArray();
		});
	}

	private static bool PlanningSceneExists()
	{
		try
		{
			string path = System.IO.Path.Combine(BasePath.Name, "Modules");
			if (!Directory.Exists(path))
			{
				return false;
			}
			string[] directories = Directory.GetDirectories(path);
			for (int i = 0; i < directories.Length; i++)
			{
				if (Directory.Exists(System.IO.Path.Combine(directories[i], "SceneObj", "mp_skirmish_spawn_test")))
				{
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

	public static Mission StartHomesteadPlanningMission(Homestead homestead, string targetSceneName = null)
	{
		// The old planning canvas was Native's multiplayer test scene
		// "mp_skirmish_spawn_test". On current game builds (1.4.x + WarSails)
		// loading that MP scene as a singleplayer sandbox mission hard-crashes
		// the engine during mission init (CTD right after team creation, no
		// managed exception — see rgl_log). Plan on the homestead's own scene
		// instead: it is the exact scene normal visits already load fine, and
		// the editor logics work the same there.
		//
		// The record must carry the terrain map patch (like the visit record
		// does): homestead scenes are battle-terrain scenes generated from the
		// campaign map, and opening one without patch data sends terrain
		// generation into infinite recursion (StackOverflow mid-transition).
		string text = (!string.IsNullOrEmpty(targetSceneName)) ? targetSceneName : homestead.GetHomesteadScene().SceneName;
		MissionInitializerRecord rec = CreateHomesteadVisitRecord(homestead, text);
		return MissionState.OpenNew(text, rec, delegate(Mission mission)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected O, but got Unknown
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Expected O, but got Unknown
			List<MissionBehavior> list = new List<MissionBehavior>
			{
				new MissionOptionsComponent(),
				(MissionBehavior)new CampaignMissionComponent(),
				(MissionBehavior)new MissionBasicTeamLogic(),
				new BasicLeaveMissionLogic(),
				(MissionBehavior)new MissionSingleplayerViewHandler(),
				(MissionBehavior)new MissionAgentLookHandler(),
				(MissionBehavior)new HeroSkillHandler(),
				new MissionFacialAnimationHandler(),
				new AgentHumanAILogic(),
				new MissionBoundaryPlacer()
			};
			AddThirdPartyMissionBehaviors(list);
			list.AddRange(new MissionBehavior[14]
			{
				(MissionBehavior)new MissionMainAgentController(),
				new EquipmentControllerLeaveLogic(),
				(MissionBehavior)(object)ViewCreator.CreateMissionLeaveView(),
				(MissionBehavior)(object)ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				(MissionBehavior)(object)SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				(MissionBehavior)(object)ViewCreator.CreateMissionSingleplayerEscapeMenu(false),
				(MissionBehavior)(object)ViewCreator.CreateOptionsUIHandler(),
				(MissionBehavior)(object)ViewCreator.CreatePhotoModeView(),
				new HomesteadSpawningMissionLogic(homestead, isPlanningMode: true),
				new HomesteadSceneEditingMissionLogic(homestead, isPlanningMode: true),
				new HomesteadTrainingFieldMissionLogic(homestead),
				(MissionBehavior)(object)new HomesteadMissionView(homestead),
				(MissionBehavior)(object)new HomesteadFreeCameraView(),
				(MissionBehavior)(object)new HomesteadBuildingPickerView()
			});
			return list.ToArray();
		});
	}

	private static void AddThirdPartyMissionBehaviors(List<MissionBehavior> behaviors)
	{
		TryAddBehavior(behaviors, "AIInfluence.Behaviors.GroupConversation.GroupConversationMissionBehavior");
		TryAddBehavior(behaviors, "AIInfluence.Behaviors.GroupConversation.GroupConversationPopupView");
		TryAddBehavior(behaviors, "AIInfluence.Behaviors.GroupConversation.AmbientNpcConversationSystem");
		TryAddBehavior(behaviors, "AIInfluence.Behaviors.GroupConversation.MissionNpcNavigationService");
	}

	private static Assembly? GetAIInfluenceAssembly()
	{
		if (_aiInfluenceAssemblySearched)
		{
			return _aiInfluenceAssembly;
		}
		_aiInfluenceAssemblySearched = true;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (string.Equals(assembly.GetName().Name, "AIInfluence", StringComparison.Ordinal))
			{
				_aiInfluenceAssembly = assembly;
				break;
			}
		}
		return _aiInfluenceAssembly;
	}

	private static void TryAddBehavior(List<MissionBehavior> behaviors, string fullTypeName)
	{
		try
		{
			Assembly aIInfluenceAssembly = GetAIInfluenceAssembly();
			if (aIInfluenceAssembly == null)
			{
				return;
			}
			Type behaviorType = aIInfluenceAssembly.GetType(fullTypeName);
			if (behaviorType == null)
			{
				TraceLogger.Write("CustomMissions", "AI Influence assembly found but type '" + fullTypeName + "' was not.");
			}
			else if (!behaviors.Any((MissionBehavior b) => b.GetType() == behaviorType))
			{
				MissionBehavior missionBehavior = (MissionBehavior)Activator.CreateInstance(behaviorType);
				if (missionBehavior != null)
				{
					behaviors.Add(missionBehavior);
				}
			}
		}
		catch (Exception ex)
		{
			TraceLogger.Write("CustomMissions", "Could not add optional behavior '" + fullTypeName + "': " + ex.GetType().Name + ": " + ex.Message);
		}
	}
}

using System;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SOTOR.CampaignBehaviors;

public class SotorGraveyardFightMissionController : MissionLogic
{
	public static int DefenderSpawnCap = 5;

	// [RF-A] API do jogo mudou depois do build do SOTOR: MissionAgentSpawnLogic
	// virou a interface IMissionAgentSpawnLogic + DefaultBattleMissionAgentSpawnLogic.
	// SetSpawnHorses/InitWithSinglePhase so existem na classe concreta, e e ela que
	// SandBoxMissions.CreateCampaignMissionAgentSpawnLogic instancia na campanha.
	private DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;

	private MapEvent _mapEvent;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		SotorGraveyardDeploymentPatch.SuppressOrderOfBattleDeployment = true;
		SotorGraveyardMountPatch.SuppressPlayerMount = true;
		_missionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>(); // [RF-A]
		_mapEvent = MapEvent.PlayerMapEvent;
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		SotorGraveyardDeploymentPatch.SuppressOrderOfBattleDeployment = false;
		SotorGraveyardMountPatch.SuppressPlayerMount = false;
	}

	public override void AfterStart()
	{
		int num = MathF.Min(_mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender), DefenderSpawnCap);
		int numberOfInvolvedMen = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
		base.Mission.DoesMissionRequireCivilianEquipment = false;
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
		_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);
		MissionSpawnSettings missionSpawnSettings = MissionSpawnSettings.CreateDefaultSpawnSettings();
		_missionAgentSpawnLogic.InitWithSinglePhase(num, numberOfInvolvedMen, num, numberOfInvolvedMen, true, true, ref missionSpawnSettings);
		try
		{
			base.Mission.Scene.SetAtmosphereWithName("sotor_graveyard_night");
		}
		catch (Exception ex)
		{
			SotorLog.Warn("[SOTOR] graveyard SetAtmosphereWithName failed: " + ex.Message);
		}
	}
}

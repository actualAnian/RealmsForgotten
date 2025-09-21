using RealmsForgotten.AiMade.Encounters.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using RealmsForgotten.AiMade.Encounters.Interfaces;
using RealmsForgotten.AiMade.Encounters.Scenario;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.MountAndBlade;
using SandBox.Missions.MissionLogics;
using SandBox.View.Missions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.CampaignSystem.Map;

namespace RealmsForgotten.AiMade.Encounters.Behaviors
{
    public class EncounterSystemBehavior : CampaignBehaviorBase
    {
        private ScenarioDispatcher scenarioDispatcher;
        private bool isDuelRunning = false;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
        }

        public void OnSessionLaunched(CampaignGameStarter gameStarter)
        {
            scenarioDispatcher = new ScenarioDispatcher();
            AddDialogs(gameStarter);
        }

        public void StartDuel(CharacterObject opponent, bool onHorse)
        {
            isDuelRunning = true;
            string scene = GetDuelScene();

            MissionState.OpenNew("DuelMission", CreateDuelMissionInitializerRecord(scene), (mission) => new MissionBehavior[]
            {
                new DuelMissionController(opponent, onHorse),
                new MissionCampaignView(),
                new CampaignMissionComponent(),
                new MissionOptionsComponent(),
                // CORRECTED: Get Ironman mode from CampaignOptions
                ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                new MissionBoundaryWallView(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView()
            });
        }

        private string GetDuelScene()
        {
            var forbiddenScenes = new List<string> { "battle_terrain_b", "battle_terrain_g", "battle_terrain_v", "battle_terrain_s", "battle_terrain_a" };
            string scene;

            if (PlayerEncounter.Current != null && PlayerEncounter.InsideSettlement && Settlement.CurrentSettlement.IsTown)
            {
                scene = Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("arena").GetSceneName(Settlement.CurrentSettlement.Town.GetWallLevel());
            }
            else
            {
                // CORRECTED: Get MapPatchData first, then pass it to the method
                IMapScene mapSceneWrapper = Campaign.Current.MapSceneWrapper;
                MapPatchData mapPatchAtPosition = mapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position);
                scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(mapPatchAtPosition, MobileParty.MainParty.IsCurrentlyAtSea);
            }

            if (forbiddenScenes.Contains(scene))
            {
                return "battle_terrain_o";
            }

            return scene;
        }

        public static MissionInitializerRecord CreateDuelMissionInitializerRecord(string sceneName)
        {
            var initializerRecord = new MissionInitializerRecord(sceneName);
            initializerRecord.DamageToPlayerMultiplier = Campaign.Current.Models.DifficultyModel.GetDamageToPlayerMultiplier();
            initializerRecord.DamageToFriendsMultiplier = Campaign.Current.Models.DifficultyModel.GetPlayerTroopsReceivedDamageMultiplier();
            initializerRecord.PlayingInCampaignMode = Campaign.Current.GameMode == CampaignGameMode.Campaign;
            initializerRecord.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
            initializerRecord.SceneLevels = "";
            initializerRecord.DoNotUseLoadingScreen = false;
            return initializerRecord;
        }

        public void EndDuel(bool playerWon)
        {
            isDuelRunning = false;

            if (Settlement.CurrentSettlement != null)
            {
                string menuId = Settlement.CurrentSettlement.IsTown ? "town" : "castle";
                GameMenu.SwitchToMenu(menuId);
            }
            else if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(true);
            }
            else
            {
                GameMenu.ExitToLast();
            }
        }

        private void OnGameMenuOpened(MenuCallbackArgs args)
        {
            // Your logic here
        }

        private void AddDialogs(CampaignGameStarter gameStarter)
        {
            // Your logic here
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("isDuelRunning", ref isDuelRunning);
        }
    }
}
// Decompiled with JetBrains decompiler
// Type: SandBox.Missions.MissionLogics.Towns.PrisonBreakMissionController
// Assembly: SandBox, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: D4DFB4CB-584D-4D15-B025-C60D4AF99C09
// Assembly location: A:\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\bin\Win64_Shipping_Client\SandBox.dll

using SandBox.CampaignBehaviors;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects.AnimationPoints;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using Quest;
using HarmonyLib;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.AgentOrigins;

namespace SandBox.Missions.MissionLogics.Towns
{
    public class RFPrisonBreakMissionController : MissionLogic
    {
        private const int PrisonerSwitchToAlarmedDistance = 3;
        private List<Agent> _guardAgents;
        private List<Agent> _agentsToRemove;
        private List<AreaMarker> _areaMarkers;
        private bool _isPrisonerFollowing;
        public static Mission OpenPrisonBreakMission(string scene, Location location)
        {
            Mission mission2 = MissionState.OpenNew("PrisonBreak", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "prison_break", true, DecalAtlasGroup.Town), delegate (Mission mission)
            {
                List<MissionBehavior> list = new List<MissionBehavior>();
                list.Add(new MissionOptionsComponent());
                list.Add(new CampaignMissionComponent());
                list.Add(new MissionBasicTeamLogic());
                list.Add(new BasicLeaveMissionLogic());
                list.Add(new LeaveMissionLogic());
                list.Add(new SandBoxMissionHandler());
                list.Add(new MissionAgentLookHandler());
                list.Add(new MissionAgentHandler(location, null, "sp_prison_break"));
                list.Add(new HeroSkillHandler());
                list.Add(new MissionFightHandler());
                list.Add(new BattleAgentLogic());
                list.Add(new AgentHumanAILogic());
                list.Add(new MissionCrimeHandler());
                list.Add(new MissionFacialAnimationHandler());
                list.Add(new LocationItemSpawnHandler());
                list.Add(new RFPrisonBreakMissionController());
                list.Add(new VisualTrackerMissionBehavior());
                list.Add(new EquipmentControllerLeaveLogic());
                list.Add(new BattleSurgeonLogic());
                return list.ToArray();
            }, true, true);
            mission2.ForceNoFriendlyFire = true;
            return mission2;
        }
        public override void OnCreated()
        {
            base.OnCreated();

            this.Mission.DoesMissionRequireCivilianEquipment = true;
        }

        public override void OnBehaviorInitialize() => this.Mission.IsAgentInteractionAllowed_AdditionalCondition += new Func<bool>(this.IsAgentInteractionAllowed_AdditionalCondition);

        public override void AfterStart()
        {
            this.Mission.SetMissionMode(MissionMode.Stealth, true);
            this.Mission.IsInventoryAccessible = false;
            this.Mission.IsQuestScreenAccessible = true;
            this._areaMarkers = this.Mission.ActiveMissionObjects.FindAllWithType<AreaMarker>().OrderBy<AreaMarker, int>((Func<AreaMarker, int>)(area => area.AreaIndex)).ToList<AreaMarker>();
            MissionAgentHandler missionBehavior = this.Mission.GetMissionBehavior<MissionAgentHandler>();
            foreach (UsableMachine townPassageProp in missionBehavior.TownPassageProps)
                townPassageProp.Deactivate();
            missionBehavior.SpawnPlayer(this.Mission.DoesMissionRequireCivilianEquipment, true);
            missionBehavior.SpawnLocationCharacters();
            this.ArrangeGuardCount();
            for (int index = 0; index < this._guardAgents.Count; ++index)
            {
                Agent guardAgent = this._guardAgents[index];
                guardAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag = this._areaMarkers[index % this._areaMarkers.Count].Tag;
                missionBehavior.SimulateAgent(guardAgent);
            }
            this.SetTeams();
            if (Agent.Main?.Health < 100)
                Agent.Main.Health = 100;
        }

        public override void OnMissionTick(float dt)
        {
            SandBoxHelpers.MissionHelper.FadeOutAgents((IEnumerable<Agent>)this._agentsToRemove, true, true);
            this._agentsToRemove.Clear();


        }

        public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
        {
            if (this._guardAgents == null || !(usedObject is AnimationPoint) || !this._guardAgents.Contains(userAgent))
                return;
            userAgent.StopUsingGameObject();
        }


        public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
        {
            this.UpdateDoorPermission();
        }

        private void ArrangeGuardCount()
        {
            int num1 = 2 + Settlement.CurrentSettlement.Town.GetWallLevel();
            float security = Settlement.CurrentSettlement.Town.Security;
            if ((double)security < 40.0)
                --num1;
            else if ((double)security > 70.0)
                ++num1;
            this._guardAgents = this.Mission.Agents.Where<Agent>((Func<Agent, bool>)(x => x.Character is CharacterObject character && character.IsSoldier)).ToList<Agent>();
            this._agentsToRemove = new List<Agent>();
            int count1 = this._guardAgents.Count;
            if (count1 > num1)
            {
                int num2 = count1 - num1;
                for (int index = 0; index < count1 && num2 > 0; ++index)
                {
                    Agent guardAgent = this._guardAgents[index];
                    if (!guardAgent.Character.IsHero)
                    {
                        this._agentsToRemove.Add(guardAgent);
                        --num2;
                    }
                }
            }
            else if (count1 < num1)
            {
                List<LocationCharacter> list = LocationComplex.Current.GetListOfCharactersInLocation("prison").Where<LocationCharacter>((Func<LocationCharacter, bool>)(x => !x.Character.IsHero && x.Character.IsSoldier)).ToList<LocationCharacter>();
                if (list.IsEmpty<LocationCharacter>())
                {
                    AgentData agentData = GuardsCampaignBehavior.PrepareGuardAgentDataFromGarrison(PlayerEncounter.LocationEncounter.Settlement.Culture.Guard, true);
                    LocationCharacter locationCharacter = new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddStandGuardBehaviors), "sp_guard", true, LocationCharacter.CharacterRelations.Neutral, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_guard"), false);
                    list.Add(locationCharacter);
                }
                int count2 = list.Count;
                Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
                int num3 = num1 - count1;
                for (int index = 0; index < num3; ++index)
                {
                    LocationCharacter locationCharacter = list[index % count2];
                    LocationComplex.Current.ChangeLocation(new LocationCharacter(new AgentData((IAgentOriginBase)new SimpleAgentOrigin((BasicCharacterObject)locationCharacter.Character, banner: locationCharacter.AgentData.AgentOrigin.Banner)).Equipment(locationCharacter.AgentData.AgentOverridenEquipment).Monster(locationCharacter.AgentData.AgentMonster).NoHorses(true), locationCharacter.AddBehaviors, this._areaMarkers[index % this._areaMarkers.Count].Tag, true, LocationCharacter.CharacterRelations.Enemy, locationCharacter.ActionSetCode, locationCharacter.UseCivilianEquipment), (Location)null, locationWithId);
                }
            }
            this._guardAgents = this.Mission.Agents.Where<Agent>((Func<Agent, bool>)(x => x.Character is CharacterObject && x.Character.IsSoldier && !this._agentsToRemove.Contains(x))).ToList<Agent>();
        }

        public override void OnAgentRemoved(
          Agent affectedAgent,
          Agent affectorAgent,
          AgentState agentState,
          KillingBlow blow)
        {
            if (this._guardAgents.Contains(affectedAgent))
            {
                this._guardAgents.Remove(affectedAgent);
                this.UpdateDoorPermission();
            }
        }


        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            canLeave = Agent.Main == null || !Agent.Main.IsActive() || this._guardAgents.IsEmpty<Agent>() || this._guardAgents.All<Agent>((Func<Agent, bool>)(x => !x.IsActive()));
            if (!canLeave)
                MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat"));
            return (InquiryData)null;
        }

        private void SetTeams()
        {
            this.Mission.PlayerTeam.SetIsEnemyOf(this.Mission.PlayerEnemyTeam, true);
            foreach (Agent guardAgent in this._guardAgents)
            {
                guardAgent.SetTeam(this.Mission.PlayerEnemyTeam, true);
                guardAgent.SetAgentFlags((guardAgent.GetAgentFlags() | AgentFlag.CanGetAlarmed) & ~AgentFlag.CanRetreat);
            }
        }

        protected override void OnEndMission()
        {
            PlayerEncounter.LeaveSettlement();
            Campaign.Current.GameMenuManager.NextLocation = null;
            Campaign.Current.GameMenuManager.PreviousLocation = null;
            PlayerEncounter.EnterSettlement();

        }


        public override bool MissionEnded(ref MissionResult missionResult)
        {
            return false;
        }

        private void UpdateDoorPermission()
        {
            bool flag = this._guardAgents.IsEmpty<Agent>() || this._guardAgents.All<Agent>((Func<Agent, bool>)(x => x.CurrentWatchState != Agent.WatchState.Alarmed));
            foreach (UsableMachine townPassageProp in this.Mission.GetMissionBehavior<MissionAgentHandler>().TownPassageProps)
            {
                if (flag)
                    townPassageProp.Activate();
                else
                    townPassageProp.Deactivate();
                
            }
        }
        private bool IsAgentInteractionAllowed_AdditionalCondition() => true;
    }
}

using System;
using System.Runtime.Remoting.Messaging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using RealmsForgotten;
using RealmsForgotten.HuntableHerds.Extensions;
namespace RFCustomSettlements
{
    internal class CustomSettlementMissionLogic : MissionBehavior
    {
        private bool isRandomScene;

        public CustomSettlementMissionLogic(bool isRandomScene)
        {
            this.isRandomScene = isRandomScene;
        }

        public override MissionBehaviorType BehaviorType 
		{
			get
			{
				return MissionBehaviorType.Other;
			}
		}

        public override void AfterStart()
        {
            SpawnPlayer();
        }

        private Agent SpawnPlayer()
        {
            MatrixFrame matrixFrame = MatrixFrame.Identity;
            GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("spawnpoint_player");
            CharacterObject playerCharacter = CharacterObject.PlayerCharacter;

            Vec3 playerSpawnFallback = Mission.Current.Scene.FindEntityWithName("sp_player").GlobalPosition;
            Mission.Current.GetTrueRandomPositionAroundPoint(playerSpawnFallback, 20, 500, false);

            AgentBuildData agentBuildData = new AgentBuildData(playerCharacter).Team(base.Mission.PlayerTeam).InitialPosition(gameEntity.GetGlobalFrame().origin);

            Vec2 vec = matrixFrame.rotation.f.AsVec2;
            vec = vec.Normalized();

            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(false).NoHorses(false).NoWeapons(false).ClothingColor1(base.Mission.PlayerTeam.Color).ClothingColor2(base.Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default(UniqueTroopDescriptor), false)).MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed())).Controller(Agent.ControllerType.Player);


            Agent agent = base.Mission.SpawnAgent(agentBuildData2);

            for (int i = 0; i < 3; i++)
            {
                Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), true);
            }

            return agent;
        }
    }
}
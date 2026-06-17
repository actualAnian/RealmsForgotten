using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class WerewolfAmbushMissionBehavior : MissionLogic
    {
        private readonly WerewolfQuest _quest;

        public WerewolfAmbushMissionBehavior(WerewolfQuest quest)
        {
            _quest = quest;
        }

        public override void AfterStart()
        {
            InformationManager.DisplayMessage(new InformationMessage("[WerewolfQuest] Missão de duelo iniciada."));

            // Spawna o player sozinho
            AgentBuildData playerBuildData = new AgentBuildData(Hero.MainHero.CharacterObject)
                .Team(Mission.Current.DefenderTeam)
                .Controller(AgentControllerType.Player);

            Agent player = Mission.Current.SpawnAgent(playerBuildData);
            if (player != null)
            {
                InformationManager.DisplayMessage(new InformationMessage("[WerewolfQuest] Player spawnado: " + player.Name));
            }

            // Spawna o werewolf
            var werewolfChar = CharacterObject.All.FirstOrDefault(c => c.StringId == "werewolf");
            if (werewolfChar != null)
            {
                var werewolfOrigin = new SimpleAgentOrigin(werewolfChar, -1, null);

                var werewolf = Mission.Current.SpawnTroop(
                    werewolfOrigin,
                    false,  // isPlayerSide
                    false,  // hasFormation
                    false,  // spawnWithHorse
                    false,  // isReinforcement
                    0, 0,
                    false, false,
                    null, null, null, null,
                    FormationClass.Infantry,
                    false
                );
            }
        }
        public override void OnMissionResultReady(MissionResult missionResult)
        {
            if (missionResult.PlayerVictory)
            {
                InformationManager.DisplayMessage(new InformationMessage("[WerewolfQuest] Player venceu o duelo."));
                _quest.OnWerewolfDefeated();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("[WerewolfQuest] Player perdeu o duelo."));
                _quest.OnWerewolfFailed();
            }
        }
    }
}
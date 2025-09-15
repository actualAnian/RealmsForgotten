using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Quest.FourthUpdate
{
    public class WingedWitchFinalMissionLogic : MissionLogic
    {
        private bool _isPlayerDead = false;
        private float _playerDeadTimer = 0f;
        private bool _questNotified = false;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Se player morreu → aplica morte real após delay
            if (_isPlayerDead)
            {
                _playerDeadTimer += dt;
                if (_playerDeadTimer > 4f)
                {
                    _isPlayerDead = false;
                    KillCharacterAction.ApplyByWounds(Hero.MainHero, true);
                }
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            // Caso 1: player morto
            if (affectedAgent.IsHero && affectedAgent.Character.StringId == Hero.MainHero.CharacterObject.StringId)
            {
                _isPlayerDead = true;
            }

            // Caso 2: Witch final derrotada
            if (!_questNotified &&
                affectedAgent?.Character?.StringId == "winged_evil_witch" &&
                agentState == AgentState.Killed)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "DEBUG: Winged Witch defeated inside mission!", Colors.Green));

                NinthQuest.OnWitchDefeatedInNinthQuest(); // marca flag
                _questNotified = true; // garante que não dispara duas vezes
            }

            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }
    }
}

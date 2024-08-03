// Decompiled with JetBrains decompiler
// Type: LordOfDuels.DuelsBehavior
// Assembly: LordOfDuels, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: C7DCADFD-A11D-4FC1-8BC0-F13A182DA900
// Assembly location: C:\Users\gupol\Downloads\lordofduels-7021-0-0-1-0-1721043990\LordOfDuels\bin\Win64_Shipping_Client\LordOfDuels.dll

using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


#nullable enable
namespace RealmsForgotten.AiMade
{
    internal class DuelsBehavior : MissionBehavior
    {
        private Agent? _mainAgent;
        private Agent? _aiAgent;
        private bool onceMission = true;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (this.onceMission && Mission.Current?.IsFieldBattle == true)
            {
                DuelsBehavior.DisplayMessage("Hold down Left Alt and press L to make the opponent army lord target you.", 0);
                this.onceMission = false;
            }

            if (Mission.Current == null)
            {
                DuelsBehavior.DisplayMessage("Mission.Current is null", 1);
                return;
            }

            if (!Input.IsKeyDown(InputKey.LeftAlt) || !Input.IsKeyReleased(InputKey.L) || !Mission.Current.IsFieldBattle || Mission.Current.MainAgent?.State != AgentState.Active)
                return;

            this.SetupAgents();

            if (this._aiAgent == null)
            {
                DuelsBehavior.DisplayMessage("_aiAgent is null after SetupAgents", 1);
                return;
            }

            if (this._mainAgent == null)
            {
                DuelsBehavior.DisplayMessage("_mainAgent is null after SetupAgents", 1);
                return;
            }

            DuelsBehavior.SetOpponent(this._aiAgent, this._mainAgent);
            DuelsBehavior.DisplayMessage(this._aiAgent.Name + " is heading towards you", 0);
        }

        private void SetupAgents()
        {
            this._mainAgent = Mission.Current?.MainAgent;
            if (this._mainAgent == null)
            {
                DuelsBehavior.DisplayMessage("_mainAgent is null in SetupAgents", 1);
                return;
            }

            foreach (Agent allAgent in Mission.Current.AllAgents)
            {
                if (allAgent.IsHero && allAgent.IsAIControlled && !allAgent.Team.IsPlayerAlly && allAgent.State == AgentState.Active)
                {
                    this._aiAgent = allAgent;
                    break;
                }
            }

            if (this._aiAgent == null)
            {
                DuelsBehavior.DisplayMessage("No suitable _aiAgent found in SetupAgents", 1);
            }
        }

        public static void SetOpponent(Agent aiAgent, Agent targetAgent)
        {
            if (aiAgent == null)
            {
                DuelsBehavior.DisplayMessage("aiAgent is null in SetOpponent", 1);
                return;
            }

            if (targetAgent == null)
            {
                DuelsBehavior.DisplayMessage("targetAgent is null in SetOpponent", 1);
                return;
            }

            aiAgent.SetAutomaticTargetSelection(false);
            aiAgent.SetDetachableFromFormation(true);
            aiAgent.Formation = null;
            aiAgent.SetTargetAgent(targetAgent);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (affectedAgent == this._aiAgent && affectorAgent == this._mainAgent)
            {
                DuelsBehavior.Cheer(Mission.Current, this._mainAgent.Team);

                if (affectedAgent.Character is CharacterObject character && character.IsHero)
                {
                    Hero heroObject = character.HeroObject;
                    if (heroObject != null)
                    {
                        KillCharacterAction.ApplyByBattle(heroObject, Hero.MainHero, true);
                    }
                    else
                    {
                        DuelsBehavior.DisplayMessage("heroObject is null in OnAgentRemoved", 1);
                    }
                }
            }
            else if (affectedAgent == this._mainAgent && affectorAgent == this._aiAgent && affectorAgent.Character is CharacterObject character && character.IsHero)
            {
                int num = new Random().Next(100);
                Hero heroObject = character.HeroObject;

                if (heroObject != null)
                {
                    if (num < 50)
                        KillCharacterAction.ApplyByBattle(Hero.MainHero, heroObject, true);
                    else
                        InformationManager.DisplayMessage(new InformationMessage("You lost the duel, but managed to survive", Colors.Magenta));

                    DuelsBehavior.ResetOpponent(this._aiAgent);
                }
                else
                {
                    DuelsBehavior.DisplayMessage("heroObject is null in OnAgentRemoved after duel", 1);
                }
            }
        }

        public static void DisplayMessage(string message, int colorNum)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, colorNum == 0 ? Colors.Magenta : Colors.Red));
        }

        public static void Cheer(Mission mission, Team team)
        {
            if (Agent.Main == null)
            {
                DuelsBehavior.DisplayMessage("Agent.Main is null in Cheer", 1);
                return;
            }

            foreach (Agent activeAgent in team.ActiveAgents)
                activeAgent.SetWantsToYell();
        }

        public static void ResetOpponent(Agent aiAgent)
        {
            if (aiAgent == null)
            {
                DuelsBehavior.DisplayMessage("aiAgent is null in ResetOpponent", 1);
                return;
            }

            aiAgent.SetAutomaticTargetSelection(true);
            aiAgent.SetDetachableFromFormation(false);
        }
    }
}

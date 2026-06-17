using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
namespace RFCustomSettlements
{

    public class TestMissionLogic : MissionLogic
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            var properties = agent.AgentDrivenProperties;
            int a = 5;
            properties.AIBlockOnDecideAbility = 0;
            properties.AIAttackOnDecideChance = 1;
            properties.AiKick = 0.5f;
            properties.AiDecideOnAttackingContinue = 1;
            properties.AiAttackingShieldDefenseChance = 0f;
            agent.UpdateAgentProperties();

        }
        long ticksLootersDefended = 0;
        long ticksElvesDefended = 0;
        public override void OnMissionTick(float dt)
        {
            //var lolol = Mission.Current.Agents.First(a => a.Character.StringId.Contains("witch"));
            //float num = _bbBase.Agent.HealthLimit * _healthPercentageThreshold / 100f;
            //return _bbBase.Agent.Health < num;
            base.OnMissionTick(dt);
            //Agent.Main.Health = 1000;
            if (Agent.Main == null)
                return;
            var playerTeam = Agent.Main.Team;
            //if (Input.IsKeyPressed(InputKey.J))

            //foreach (var agent in Mission.Current.AllAgents)
            //{
            //    if (agent.IsFadingOut()) continue;
            //    var action = agent.GetCurrentActionType(1);
            //    if (action == Agent.ActionCodeType.DefendAllBegin
            //        || action == Agent.ActionCodeType.DefendFist
            //        || action == Agent.ActionCodeType.DefendForward1h
            //        || action == Agent.ActionCodeType.DefendForward2h
            //        || action == Agent.ActionCodeType.DefendLeft2h
            //        || action == Agent.ActionCodeType.DefendRight2h
            //        || action == Agent.ActionCodeType.DefendUp2h
            //        || action == Agent.ActionCodeType.BlockedMelee)
            //    {
            //        agent.EventControlFlags = Agent.EventControlFlag.Jump;
            //        agent.SetIsAIPaused(false);
            //        //agent.ResetGuard();
            //        if (agent.GetCurrentActionStage(1) == Agent.ActionStage.Defend)
            //            InformationManager.DisplayMessage(new($"priority: {agent.GetCurrentActionPriority(1)}"));
            //        if (agent.Team == playerTeam)
            //            ticksElvesDefended += 1;
            //        else ticksLootersDefended += 1;
            //        InformationManager.DisplayMessage(new($"Elves: {ticksElvesDefended} Looters: {ticksLootersDefended}"));
            //    }
            //}
                    //var act = "act_release_overswing_2h";
                    //ActionIndexCache actionIndexCache = ActionIndexCache.Create(act);
                    //agent.SetActionChannel(1, actionIndexCache);
                //InformationManager.DisplayMessage(new($"priority: {Agent.Main.GetCurrentActionPriority(1)}"));

            if (Input.IsKeyPressed(InputKey.H))
            {
                    //agent.AddComponent(new BerserkerAgentComponent(agent));
                    //agent.SetHasOnAiInputSetCallback(true);
                    //int a = 5;
                    //agent.AgentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, 1f);
                    //float[] values = AccessTools.Field(typeof(AgentDrivenProperties), "_statValues")
                    //.GetValue(agent.AgentDrivenProperties) as float[];

                    //Array.Clear(values, 0, 51);
                    ////agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanDefend);
                    //agent.AgentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = 0;
                    //agent.AgentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = 0;
                    //agent.AgentDrivenProperties.AIDecideOnAttackChance = 1;
                    //agent.AgentDrivenProperties.AIAttackOnDecideChance = 1;
                    //agent.AgentDrivenProperties.AIBlockOnDecideAbility = float.MinValue;
                    //agent.AgentDrivenProperties.AiDecideOnAttackingContinue = float.MaxValue;
                    //agent.AgentDrivenProperties.AIBlockOnDecideAbility = float.MinValue;
                    //agent.AgentDrivenProperties.AIParryOnAttackAbility = float.MinValue;
                    //agent.AgentDrivenProperties.AiAttackingShieldDefenseChance = float.MinValue;
                    //agent.AgentDrivenProperties.AIParryOnAttackingContinueAbility = float.MinValue;
                    //agent.AgentDrivenProperties.AIParryOnAttackAbility = float.MinValue;
                    //agent.AgentDrivenProperties.AiDefendWithShieldDecisionChanceValue = float.MinValue;
                    //agent.AgentDrivenProperties.AiParryDecisionChangeValue = float.MinValue;
                    //agent.AgentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = float.MinValue;
                    //}
                Agent.Main.Health = 1000;

                //InformationManager.DisplayMessage(new InformationMessage("TestMissionLogic: H key pressed"));
            }
        }
    }
}

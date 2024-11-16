//using MCM.Abstractions.Base.Global;
//using SandBox.GameComponents;
//using TaleWorlds.Core;
//using TaleWorlds.MountAndBlade;

//namespace Marching
//{
//  public class MarchingAgentStatCalculateModel : SandboxAgentStatCalculateModel
//  {
//    private readonly AgentStatCalculateModel _previousModel;

//    public MarchingAgentStatCalculateModel(AgentStatCalculateModel previousModel) => this._previousModel = previousModel;

//    public virtual void InitializeAgentStats(
//      Agent agent,
//      Equipment spawnEquipment,
//      AgentDrivenProperties agentDrivenProperties,
//      AgentBuildData agentBuildData)
//    {
//      this._previousModel.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
//      MarchingAgentStatCalculateModel.DoMarching(agent);
//    }

//    public virtual void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
//    {
//      this._previousModel.UpdateAgentStats(agent, agentDrivenProperties);
//      MarchingAgentStatCalculateModel.DoMarching(agent);
//    }

//    public static bool IsMarching(Agent agent)
//    {
//      if (agent.IsMount)
//      {
//        if (!MarchMissionBehavior.MarchingFormations.Contains(agent.RiderAgent?.Formation))
//          return false;
//      }
//      else if (!MarchMissionBehavior.MarchingFormations.Contains(agent.Formation))
//        return false;
//      return true;
//    }

//    public static void DoMarching(Agent agent)
//    {
//      if (!MarchingAgentStatCalculateModel.IsMarching(agent))
//        return;
//      float marchingSpeed = GlobalSettings<MarchGlobalConfig>.Instance.MarchingSpeed;
//      if (!agent.IsMount)
//      {
//        agent.SetAgentDrivenPropertyValueFromConsole((DrivenProperty) 75, marchingSpeed);
//        agent.SetAgentDrivenPropertyValueFromConsole((DrivenProperty) 76, marchingSpeed);
//        agent.UpdateCustomDrivenProperties();
//      }
//      else
//      {
//        agent.SetAgentDrivenPropertyValueFromConsole((DrivenProperty) 80, marchingSpeed + 2.25f);
//        agent.UpdateCustomDrivenProperties();
//      }
//    }
//  }
//}

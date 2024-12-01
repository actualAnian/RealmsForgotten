//using TaleWorlds.Core;
//using TaleWorlds.MountAndBlade;

//namespace Marching
//{
//  public class CustomMarchingAgentStatCalculateModel : CustomBattleAgentStatCalculateModel
//  {
//    private readonly AgentStatCalculateModel _previousModel;

//    public CustomMarchingAgentStatCalculateModel(AgentStatCalculateModel previousModel) => this._previousModel = previousModel;

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
//  }
//}

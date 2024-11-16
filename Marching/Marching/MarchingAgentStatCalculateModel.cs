// Decompiled with JetBrains decompiler
// Type: Marching.MarchingAgentStatCalculateModel
// Assembly: Marching, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: FAB07C52-9EF1-4E87-B983-D3A51612112E
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Marching\bin\Win64_Shipping_Client\Marching.dll

using MCM.Abstractions.Base.Global;
using SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;


#nullable enable
namespace Marching
{
  public class MarchingAgentStatCalculateModel : SandboxAgentStatCalculateModel
  {
    private readonly AgentStatCalculateModel _previousModel;

    public MarchingAgentStatCalculateModel(AgentStatCalculateModel previousModel) => this._previousModel = previousModel;

    public virtual void InitializeAgentStats(
      Agent agent,
      Equipment spawnEquipment,
      AgentDrivenProperties agentDrivenProperties,
      AgentBuildData agentBuildData)
    {
      this._previousModel.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
      MarchingAgentStatCalculateModel.DoMarching(agent);
    }

    public virtual void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
    {
      this._previousModel.UpdateAgentStats(agent, agentDrivenProperties);
      MarchingAgentStatCalculateModel.DoMarching(agent);
    }

    public static bool IsMarching(Agent agent)
    {
      if (agent.IsMount)
      {
        if (!MarchMissionBehavior.MarchingFormations.Contains(agent.RiderAgent?.Formation))
          return false;
      }
      else if (!MarchMissionBehavior.MarchingFormations.Contains(agent.Formation))
        return false;
      return true;
    }

    public static void DoMarching(Agent agent)
    {
      if (!MarchingAgentStatCalculateModel.IsMarching(agent))
        return;
      float marchingSpeed = GlobalSettings<MarchGlobalConfig>.Instance.MarchingSpeed;
      if (!agent.IsMount)
      {
        agent.SetAgentDrivenPropertyValueFromConsole((DrivenProperty) 75, marchingSpeed);
        agent.SetAgentDrivenPropertyValueFromConsole((DrivenProperty) 76, marchingSpeed);
        agent.UpdateCustomDrivenProperties();
      }
      else
      {
        agent.SetAgentDrivenPropertyValueFromConsole((DrivenProperty) 80, marchingSpeed + 2.25f);
        agent.UpdateCustomDrivenProperties();
      }
    }
  }
}

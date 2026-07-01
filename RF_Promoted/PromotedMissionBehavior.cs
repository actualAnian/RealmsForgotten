using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;

namespace RF_Promoted;

public sealed class PromotedMissionBehavior : MissionLogic
{
    private PromotedCampaignBehavior? _campaignBehavior;

    public override void AfterStart()
    {
        base.AfterStart();
        _campaignBehavior = Campaign.Current?.GetCampaignBehavior<PromotedCampaignBehavior>();
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
    {
        base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

        if (_campaignBehavior == null
            || affectedAgent == null
            || affectorAgent == null
            || affectedAgent.IsMount
            || affectorAgent.IsMount
            || (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
            || !affectedAgent.IsEnemyOf(affectorAgent)
            || affectorAgent.Character == null
            || affectorAgent.Team == null
            || Mission.Current?.PlayerTeam == null
            || affectorAgent.Team != Mission.Current.PlayerTeam)
        {
            return;
        }

        if (affectorAgent.Origin is PartyAgentOrigin partyAgentOrigin
            && partyAgentOrigin.Party != PartyBase.MainParty)
        {
            return;
        }

        _campaignBehavior.RegisterBattleKill(affectorAgent.Character.StringId);
    }
}

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_Enlistment;

public sealed class RFEnlistmentMissionBehavior : MissionLogic
{
    private const float SampleInterval = 2f;
    private const float CohesionDistance = 25f;
    private const float CommanderDistance = 40f;
    private const float EngagementDistance = 35f;

    private RFEnlistmentCampaignBehavior? _campaignBehavior;
    private float _missionStartTime;
    private float _nextSampleTime;
    private float? _mainAgentDownTime;
    private int _killCount;
    private int _sampleCount;
    private int _cohesionSamples;
    private int _commanderSamples;
    private int _engagementSamples;
    private float _enemyDistanceTotal;
    private int _enemyDistanceSamples;

    public override void AfterStart()
    {
        base.AfterStart();

        _campaignBehavior = Campaign.Current?.GetCampaignBehavior<RFEnlistmentCampaignBehavior>();
        _missionStartTime = Mission.Current?.CurrentTime ?? 0f;
        _nextSampleTime = _missionStartTime;
        PushBattleMeritSnapshot();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        Mission? mission = Mission.Current;
        if (mission == null || mission.CurrentTime < _nextSampleTime)
        {
            return;
        }

        _nextSampleTime = mission.CurrentTime + SampleInterval;
        SampleMainAgentState(mission);
        PushBattleMeritSnapshot();
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
    {
        base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

        if (affectedAgent != null
            && affectedAgent.IsMainAgent
            && _mainAgentDownTime == null
            && (agentState == AgentState.Killed || agentState == AgentState.Unconscious))
        {
            _mainAgentDownTime = Mission.Current?.CurrentTime ?? _missionStartTime;
            PushBattleMeritSnapshot();
        }

        if (affectedAgent == null
            || affectorAgent == null
            || affectedAgent.IsMount
            || (agentState != AgentState.Killed && agentState != AgentState.Unconscious)
            || !affectedAgent.IsEnemyOf(affectorAgent))
        {
            return;
        }

        if (!affectorAgent.IsMainAgent)
        {
            return;
        }

        _killCount++;
        _campaignBehavior?.RegisterBattleKill();
        PushBattleMeritSnapshot();
    }

    protected override void OnEndMission()
    {
        PushBattleMeritSnapshot();
        base.OnEndMission();
    }

    private void SampleMainAgentState(Mission mission)
    {
        Agent? mainAgent = Agent.Main;
        if (mainAgent == null || !mainAgent.IsHuman || mainAgent.Team == null)
        {
            return;
        }

        _sampleCount++;

        Agent? formationCaptain = mainAgent.Formation?.Captain;
        if (formationCaptain != null
            && formationCaptain != mainAgent
            && formationCaptain.IsActive()
            && mainAgent.Position.Distance(formationCaptain.Position) <= CohesionDistance)
        {
            _cohesionSamples++;
        }

        Agent? nearestCommander = FindNearestAlliedHero(mainAgent, mission);
        if (nearestCommander != null
            && mainAgent.Position.Distance(nearestCommander.Position) <= CommanderDistance)
        {
            _commanderSamples++;
        }

        float nearestEnemyDistance = FindNearestEnemyDistance(mainAgent, mission);
        if (nearestEnemyDistance >= 0f)
        {
            _enemyDistanceTotal += nearestEnemyDistance;
            _enemyDistanceSamples++;

            if (nearestEnemyDistance <= EngagementDistance)
            {
                _engagementSamples++;
            }
        }
    }

    private void PushBattleMeritSnapshot()
    {
        _campaignBehavior ??= Campaign.Current?.GetCampaignBehavior<RFEnlistmentCampaignBehavior>();
        _campaignBehavior?.UpdateBattleMeritReport(BuildReport());
    }

    private RFEnlistmentBattleMeritReport BuildReport()
    {
        float currentTime = Mission.Current?.CurrentTime ?? _missionStartTime;
        float missionDuration = MathF.Max(1f, currentTime - _missionStartTime);
        float aliveUntil = _mainAgentDownTime ?? currentTime;
        float survivalRatio = MBMath.ClampFloat((aliveUntil - _missionStartTime) / missionDuration, 0f, 1f);

        return new RFEnlistmentBattleMeritReport
        {
            Valid = _sampleCount > 0 || _killCount > 0,
            Kills = _killCount,
            SurvivalRatio = survivalRatio,
            CohesionRatio = _sampleCount > 0 ? (float)_cohesionSamples / _sampleCount : 0f,
            CommanderRatio = _sampleCount > 0 ? (float)_commanderSamples / _sampleCount : 0f,
            EngagementRatio = _sampleCount > 0 ? (float)_engagementSamples / _sampleCount : 0f,
            AverageEnemyDistance = _enemyDistanceSamples > 0 ? _enemyDistanceTotal / _enemyDistanceSamples : -1f,
            FellInBattle = _mainAgentDownTime.HasValue
        };
    }

    private static Agent? FindNearestAlliedHero(Agent mainAgent, Mission mission)
    {
        Agent? nearest = null;
        float bestDistance = float.MaxValue;

        foreach (Agent agent in mission.Agents)
        {
            if (agent == null
                || agent == mainAgent
                || !agent.IsHuman
                || !agent.IsActive()
                || agent.Team == null
                || mainAgent.Team == null
                || agent.Team != mainAgent.Team
                || !agent.IsHero)
            {
                continue;
            }

            float distance = mainAgent.Position.Distance(agent.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = agent;
            }
        }

        return nearest;
    }

    private static float FindNearestEnemyDistance(Agent mainAgent, Mission mission)
    {
        float bestDistance = float.MaxValue;

        foreach (Agent agent in mission.Agents)
        {
            if (agent == null
                || !agent.IsHuman
                || !agent.IsActive()
                || agent.Team == null
                || mainAgent.Team == null
                || !agent.Team.IsEnemyOf(mainAgent.Team))
            {
                continue;
            }

            float distance = mainAgent.Position.Distance(agent.Position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        return bestDistance == float.MaxValue ? -1f : bestDistance;
    }
}

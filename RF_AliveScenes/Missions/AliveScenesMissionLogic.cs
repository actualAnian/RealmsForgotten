using System;
using System.Collections.Generic;
using RF_AliveScenes.Config;
using RF_AliveScenes.Runtime;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AliveScenes.Missions;

/// <summary>
/// Roda o relogio da fala ambiente e escolhe quem fala. A view escuta os eventos
/// <see cref="AgentSpoke"/> / <see cref="AgentStoppedSpeaking"/> para desenhar o balao.
///
/// Diferenca para o mod original: a escolha do agente e um sorteio "swap-remove" sobre
/// uma lista propria — la o codigo sorteava de uma lista que podia estar vazia e
/// estourava dentro do OnMissionTick.
/// </summary>
public sealed class AliveScenesMissionLogic : MissionBehavior
{
    private const float CasualScanCooldown = 7f;
    private const float BattleScanCooldown = 22f;
    private const float NavalScanCooldown = 42f;
    private const float CasualScanRadius = 10f;
    private const float BattleScanRadius = 25f;
    private const float AgentRetryDelay = 5f;

    public event Action<Agent, string, bool> AgentSpoke;
    public event Action<Agent> AgentStoppedSpeaking;

    private readonly BattleSetup _battle;
    private readonly Dictionary<Agent, float> _retryAfter = new();
    private readonly MBList<Agent> _scanBuffer = new();
    private readonly List<Agent> _candidates = new();

    private SpeechDirector _director;
    private float _clock;
    private float _lastScanAt;

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    public AliveScenesMissionLogic(BattleSetup battle)
    {
        _battle = battle;
    }

    public override void AfterStart()
    {
        base.AfterStart();
        _director = new SpeechDirector(Mission, _battle);
        _director.AgentSpoke += OnDirectorSpeak;
        _director.AgentStoppedSpeaking += OnDirectorStop;
    }

    protected override void OnEndMission()
    {
        if (_director != null)
        {
            _director.AgentSpoke -= OnDirectorSpeak;
            _director.AgentStoppedSpeaking -= OnDirectorStop;
            _director = null;
        }
        base.OnEndMission();
    }

    private void OnDirectorSpeak(Agent agent, string text, bool isEnemy) => AgentSpoke?.Invoke(agent, text, isEnemy);

    private void OnDirectorStop(Agent agent) => AgentStoppedSpeaking?.Invoke(agent);

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (_director == null)
        {
            return;
        }

        _clock += dt;
        _director.Tick(dt);

        AliveScenesSettings settings = AliveScenesSettings.Instance;
        if (_battle != null)
        {
            if (!settings.BattleChatEnabled || !Mission.IsDeploymentFinished)
            {
                return;
            }
        }
        else if (!settings.CasualChatEnabled)
        {
            return;
        }

        if (!_director.HasFreeSlot() || Mission.MainAgent == null)
        {
            return;
        }

        float scanCooldown = _battle == null
            ? CasualScanCooldown
            : (_battle.AtSea ? NavalScanCooldown : BattleScanCooldown);

        if (_lastScanAt + scanCooldown > _clock)
        {
            return;
        }
        _lastScanAt = _clock;

        TrySpeak(settings);
    }

    private void TrySpeak(AliveScenesSettings settings)
    {
        CollectCandidates();
        if (_candidates.Count == 0)
        {
            return;
        }

        float chance = (_battle != null ? settings.BattleChancePerPerson : settings.CasualChancePerPerson) / 100f;

        while (_candidates.Count > 0)
        {
            int index = MBRandom.RandomInt(_candidates.Count);
            Agent agent = _candidates[index];
            _candidates[index] = _candidates[_candidates.Count - 1];
            _candidates.RemoveAt(_candidates.Count - 1);

            if (agent == null)
            {
                continue;
            }

            if (_retryAfter.TryGetValue(agent, out float retryAt))
            {
                if (retryAt > _clock)
                {
                    continue;
                }
                _retryAfter.Remove(agent);
            }

            if (MBRandom.RandomFloat > chance)
            {
                _retryAfter[agent] = _clock + AgentRetryDelay;
                continue;
            }

            if (!_director.MayAgentSpeak(agent))
            {
                continue;
            }

            string text = _battle != null ? _director.TryBattleLine(agent) : _director.TryCasualLine(agent);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            bool isEnemy = Mission.MainAgent != null && agent.IsEnemyOf(Mission.MainAgent);
            AgentSpoke?.Invoke(agent, text, isEnemy);
            return;
        }
    }

    private void CollectCandidates()
    {
        _candidates.Clear();
        _scanBuffer.Clear();

        Agent main = Mission.MainAgent;
        float radius = _battle != null ? BattleScanRadius : CasualScanRadius;
        Mission.GetNearbyAgents(main.Position.AsVec2, radius, _scanBuffer);

        float mainZ = main.Position.z;
        foreach (Agent agent in _scanBuffer)
        {
            if (agent == null || agent == main || !agent.IsHuman || !agent.IsActive())
            {
                continue;
            }
            // mesmo andar/convés que o jogador
            if (Math.Abs(agent.Position.z - mainZ) >= 1f)
            {
                continue;
            }
            _candidates.Add(agent);
        }
    }
}

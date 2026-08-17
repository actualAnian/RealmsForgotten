using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_Ambush.Missions;

/// <summary>
/// A BATALHA de emboscada quando o JOGADOR salta sobre a presa.
///
/// Implementacao propria (o TotalAmbush e mod fechado do Nexus — reusamos so o
/// CONCEITO: coluna de marcha + deteccao por cones + spring; ver
/// ESTUDO_TOTALAMBUSH.md).
///
/// A ideia que faz tudo funcionar sem brigar com a IA vanilla: agentes com
/// MOVIMENTO SCRIPTADO ignoram a IA de combate. O inimigo entra em coluna de
/// marcha scriptada atravessando o campo; enquanto ninguem da coluna PERCEBE um
/// soldado nosso (cone frontal estreito + raio lateral curto), eles nao lutam.
/// O spring — nosso ataque ou a deteccao — devolve os agentes a IA normal, com
/// choque de moral por serem pegos desprevenidos.
/// </summary>
public sealed class PlayerAmbushMissionBehavior : MissionLogic
{
    private bool _active;
    private bool _sprung;
    private bool _columnStarted;
    private float _detectionTimer;

    /// <summary>Flags originais por agente, para restaurar no spring.</summary>
    private readonly Dictionary<Agent, AgentFlag> _savedFlags = new Dictionary<Agent, AgentFlag>();

    private Vec2 _marchDirection = Vec2.Forward;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        _active = true;
    }

    public override void OnDeploymentFinished()
    {
        base.OnDeploymentFinished();
        if (!_active || Mission.PlayerEnemyTeam == null)
        {
            return;
        }
        try
        {
            StartMarchColumn();
            _columnStarted = true;
            MBInformationManager.AddQuickInformation(
                new TaleWorlds.Localization.TextObject(
                    "The enemy marches on, unaware. Strike when ready — or wait for a better moment."),
                0, null, null, "");
        }
        catch (Exception)
        {
            // Qualquer falha aqui nao pode inutilizar a batalha: vira batalha normal.
            SpringAmbush("The ambush dissolves into a normal engagement.");
        }
    }

    // ------------------------------------------------------------------
    //  coluna de marcha
    // ------------------------------------------------------------------

    /// <summary>
    /// Poe cada agente inimigo num slot de coluna e manda todos, por movimento
    /// scriptado, a um destino do outro lado do campo. Scriptado = a IA de
    /// combate deles dorme; o limite de velocidade mantem o bloco coeso.
    /// A flag CanAttack sai para o arqueiro da coluna nao abrir fogo sozinho
    /// num alvo avistado pela IA de baixo nivel.
    /// </summary>
    private void StartMarchColumn()
    {
        List<Agent> marchers = CollectEnemyAgents();
        if (marchers.Count == 0)
        {
            SpringAmbush("The ambush dissolves into a normal engagement.");
            return;
        }

        // Direcao: do centro dos inimigos para ALEM do centro do mapa — eles
        // cruzam o campo, passando pela area onde o jogador se posicionou.
        Vec2 enemyCenter = AveragePosition(marchers);
        Vec2 mapCenter = Mission.GetClosestBoundaryPosition(enemyCenter);
        Vec3 sceneMid;
        Mission.Scene.GetBoundingBox(out Vec3 min, out Vec3 max);
        sceneMid = (min + max) * 0.5f;
        Vec2 center2 = sceneMid.AsVec2;
        _marchDirection = (center2 - enemyCenter);
        if (_marchDirection.LengthSquared < 1f)
        {
            _marchDirection = Vec2.Forward;
        }
        _marchDirection.Normalize();

        // Destino: espelho da posicao inicial atraves do centro (cruzar o campo).
        Vec2 destination = enemyCenter + _marchDirection * (2f * enemyCenter.Distance(center2) + 40f);

        int columns = AmbushConfig.MarchColumns;
        for (int i = 0; i < marchers.Count; i++)
        {
            Agent agent = marchers[i];
            int col = i % columns;
            int row = i / columns;
            float lateral = (col - (columns - 1) * 0.5f) * AmbushConfig.MarchLateralSpacing;
            float back = row * AmbushConfig.MarchRowSpacing;

            Vec2 side = _marchDirection.LeftVec();
            Vec2 slot = destination + side * lateral - _marchDirection * back;

            WorldPosition target = new WorldPosition(Mission.Scene, new Vec3(slot.x, slot.y, 0f));
            agent.SetScriptedPosition(ref target, addHumanLikeDelay: false, Agent.AIScriptedFrameFlags.DoNotRun);
            agent.SetMaximumSpeedLimit(AmbushConfig.MarchSpeedMultiplier, isMultiplier: true);

            if (!_savedFlags.ContainsKey(agent))
            {
                _savedFlags[agent] = agent.GetAgentFlags();
            }
            agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanAttack);
        }
    }

    private List<Agent> CollectEnemyAgents()
    {
        var list = new List<Agent>();
        foreach (Agent agent in Mission.Agents)
        {
            if (agent.IsActive() && agent.IsHuman && agent.Team == Mission.PlayerEnemyTeam)
            {
                list.Add(agent);
            }
        }
        return list;
    }

    private static Vec2 AveragePosition(List<Agent> agents)
    {
        Vec2 sum = Vec2.Zero;
        foreach (Agent a in agents)
        {
            sum += a.Position.AsVec2;
        }
        return agents.Count > 0 ? sum * (1f / agents.Count) : Vec2.Zero;
    }

    // ------------------------------------------------------------------
    //  deteccao: a coluna percebe a emboscada?
    // ------------------------------------------------------------------

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        if (!_active || _sprung || !_columnStarted)
        {
            return;
        }
        _detectionTimer -= dt;
        if (_detectionTimer > 0f)
        {
            return;
        }
        _detectionTimer = AmbushConfig.DetectionInterval;

        if (CheckDetection())
        {
            SpringAmbush("You've been seen! The enemy scrambles into fighting order!");
        }
    }

    /// <summary>
    /// Quem marcha desatento so ve: (a) o que esta DENTRO do cone frontal
    /// estreito ate MarchForwardVision, ou (b) o que praticamente esbarra
    /// (MarchSideAwareness em qualquer direcao). Cavalaria de batedor veria
    /// mais — refinamento para depois do primeiro teste.
    /// </summary>
    private bool CheckDetection()
    {
        Team? playerTeam = Mission.PlayerTeam;
        Team? enemyTeam = Mission.PlayerEnemyTeam;
        if (playerTeam == null || enemyTeam == null)
        {
            return false;
        }

        float sideSq = AmbushConfig.MarchSideAwareness * AmbushConfig.MarchSideAwareness;
        float frontSq = AmbushConfig.MarchForwardVision * AmbushConfig.MarchForwardVision;

        foreach (Agent enemy in Mission.Agents)
        {
            if (!enemy.IsActive() || !enemy.IsHuman || enemy.Team != enemyTeam)
            {
                continue;
            }
            Vec2 enemyPos = enemy.Position.AsVec2;
            foreach (Agent ours in Mission.Agents)
            {
                if (!ours.IsActive() || !ours.IsHuman || ours.Team != playerTeam)
                {
                    continue;
                }
                Vec2 delta = ours.Position.AsVec2 - enemyPos;
                float distSq = delta.LengthSquared;
                if (distSq <= sideSq)
                {
                    return true;
                }
                if (distSq <= frontSq)
                {
                    float invDist = 1f / (float)Math.Sqrt(Math.Max(0.0001f, distSq));
                    float facing = _marchDirection.x * delta.x * invDist + _marchDirection.y * delta.y * invDist;
                    if (facing >= AmbushConfig.MarchVisionConeCos)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // ------------------------------------------------------------------
    //  o jogador ataca = a emboscada salta
    // ------------------------------------------------------------------

    public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon,
        in Blow blow, in AttackCollisionData attackCollisionData)
    {
        base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
        if (_active && !_sprung && affectorAgent?.Team == Mission.PlayerTeam)
        {
            SpringAmbush("Ambush! Your troops fall upon the column!");
        }
    }

    public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex, Vec3 position,
        Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
    {
        base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);
        if (_active && !_sprung && shooterAgent?.Team == Mission.PlayerTeam)
        {
            SpringAmbush("Ambush! Arrows rain on the column!");
        }
    }

    // ------------------------------------------------------------------
    //  spring
    // ------------------------------------------------------------------

    private void SpringAmbush(string message)
    {
        if (_sprung)
        {
            return;
        }
        _sprung = true;

        foreach (Agent agent in Mission.Agents)
        {
            if (!agent.IsActive() || !agent.IsHuman || agent.Team != Mission.PlayerEnemyTeam)
            {
                continue;
            }
            try
            {
                agent.DisableScriptedMovement();
                agent.SetMaximumSpeedLimit(-1f, isMultiplier: false);
                if (_savedFlags.TryGetValue(agent, out AgentFlag flags))
                {
                    agent.SetAgentFlags(flags);
                }
                // O choque: pegos em marcha, moral despenca. E o que faz emboscar
                // valer a pena alem da posicao.
                agent.SetMorale(Math.Max(5f, agent.GetMorale() - AmbushConfig.SpringMoraleShock));
            }
            catch (Exception)
            {
            }
        }

        MBInformationManager.AddQuickInformation(new TaleWorlds.Localization.TextObject(message), 0, null, null, "");
    }

    protected override void OnEndMission()
    {
        _active = false;
        _savedFlags.Clear();
        base.OnEndMission();
    }
}

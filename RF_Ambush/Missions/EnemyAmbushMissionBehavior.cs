using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_Ambush.Missions;

/// <summary>
/// A BATALHA quando a IA embosca o JOGADOR (fase 2).
///
/// Espelho simplificado da PlayerAmbushMissionBehavior — com uma diferenca
/// deliberada: NAO scriptamos os agentes do jogador (scriptar tropa que o
/// jogador comanda seria tirar o controle dele, e scriptar o proprio jogador e
/// inaceitavel). Em vez disso:
///
///   - as tropas do jogador sao TELEPORTADAS para uma coluna de marcha no
///     momento em que o deployment termina (pegos em marcha, nao em linha);
///   - o inimigo ja comeca posicionado no flanco, perto;
///   - as tropas do jogador levam o choque de moral;
///   - o inimigo recebe ordem de carga apos um instante.
///
/// O jogador mantem controle total desde o segundo zero — a emboscada custa
/// posicao e moral, nao agencia.
/// </summary>
public sealed class EnemyAmbushMissionBehavior : MissionLogic
{
    private bool _active;
    private bool _executed;
    private float _chargeDelay = 3f;
    private bool _chargeOrdered;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        _active = true;
    }

    public override void OnDeploymentFinished()
    {
        base.OnDeploymentFinished();
        if (!_active || _executed || Mission.PlayerTeam == null || Mission.PlayerEnemyTeam == null)
        {
            return;
        }
        _executed = true;
        try
        {
            ArrangePlayerColumn();
            MBInformationManager.AddQuickInformation(
                new TaleWorlds.Localization.TextObject(
                    "Ambush! The enemy was lying in wait — your column is caught on the march!"),
                0, null, null, "");
        }
        catch (Exception)
        {
            // Falha aqui = batalha normal, nunca batalha quebrada.
        }
    }

    /// <summary>
    /// Recoloca as tropas do jogador (nao o heroi) em coluna de marcha no ponto
    /// onde estao, sobrescrevendo o deployment — e o preco de ser emboscado.
    /// </summary>
    private void ArrangePlayerColumn()
    {
        var troops = new List<Agent>();
        Agent? player = Agent.Main;
        foreach (Agent agent in Mission.Agents)
        {
            if (agent.IsActive() && agent.IsHuman && agent.Team == Mission.PlayerTeam && agent != player)
            {
                troops.Add(agent);
            }
        }
        if (troops.Count == 0)
        {
            return;
        }

        // Coluna ancorada no jogador (ou no centro das tropas), apontando para
        // longe do inimigo — estavamos "indo embora" quando fomos pegos.
        Vec2 anchor = player?.Position.AsVec2 ?? AveragePos(troops);
        Vec2 enemyCenter = EnemyAverage();
        Vec2 dir = anchor - enemyCenter;
        if (dir.LengthSquared < 1f)
        {
            dir = Vec2.Forward;
        }
        dir.Normalize();

        int columns = AmbushConfig.MarchColumns;
        for (int i = 0; i < troops.Count; i++)
        {
            Agent agent = troops[i];
            int col = i % columns;
            int row = i / columns;
            float lateral = (col - (columns - 1) * 0.5f) * AmbushConfig.MarchLateralSpacing;
            float back = (row + 2) * AmbushConfig.MarchRowSpacing;
            Vec2 side = dir.LeftVec();
            Vec2 slot = anchor + side * lateral - dir * back;

            float z = agent.Position.z;
            agent.TeleportToPosition(new Vec3(slot.x, slot.y, z));
            agent.SetMorale(Math.Max(5f, agent.GetMorale() - AmbushConfig.SpringMoraleShock));
        }
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        if (!_active || !_executed || _chargeOrdered)
        {
            return;
        }
        _chargeDelay -= dt;
        if (_chargeDelay > 0f)
        {
            return;
        }
        _chargeOrdered = true;
        try
        {
            foreach (Formation formation in Mission.PlayerEnemyTeam.FormationsIncludingSpecialAndEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    private Vec2 EnemyAverage()
    {
        Vec2 sum = Vec2.Zero;
        int n = 0;
        foreach (Agent agent in Mission.Agents)
        {
            if (agent.IsActive() && agent.IsHuman && agent.Team == Mission.PlayerEnemyTeam)
            {
                sum += agent.Position.AsVec2;
                n++;
            }
        }
        return n > 0 ? sum * (1f / n) : Vec2.Zero;
    }

    private static Vec2 AveragePos(List<Agent> agents)
    {
        Vec2 sum = Vec2.Zero;
        foreach (Agent a in agents)
        {
            sum += a.Position.AsVec2;
        }
        return agents.Count > 0 ? sum * (1f / agents.Count) : Vec2.Zero;
    }

    protected override void OnEndMission()
    {
        _active = false;
        base.OnEndMission();
    }
}

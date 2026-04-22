using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.StaticAmbushes;

public class AmbushMissionBehavior : MissionLogic
{
    private readonly List<AmbushZone> _zones = new();
    private readonly List<AmbushReserve> _reserves = new();

    public IEnumerable<AmbushZone> Zones => _zones;
    public IEnumerable<AmbushReserve> Reserves => _reserves;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        CollectZones();
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        EvaluateAutoRevealTriggers();
    }

    public void RegisterReserve(AmbushReserve reserve)
    {
        if (reserve != null && reserve.Zone != null && !_reserves.Contains(reserve))
        {
            _reserves.Add(reserve);
        }
    }

    public bool TryTriggerReserve(string reserveId)
    {
        AmbushReserve reserve = _reserves.FirstOrDefault(x => x.ReserveId == reserveId);
        if (reserve == null || !reserve.CanRevealNow(Mission.CurrentTime))
        {
            return false;
        }

        RevealReserve(reserve);
        return true;
    }

    private void CollectZones()
    {
        _zones.Clear();
        _zones.AddRange(Mission.ActiveMissionObjects.FindAllWithType<AmbushZone>());
    }

    private void EvaluateAutoRevealTriggers()
    {
        foreach (AmbushReserve reserve in _reserves)
        {
            if (!reserve.CanRevealNow(Mission.CurrentTime))
            {
                continue;
            }

            switch (reserve.TriggerMode)
            {
                case AmbushTriggerMode.EnemyEntersZone:
                    if (HasEnemyInsideZone(reserve))
                    {
                        RevealReserve(reserve);
                    }
                    break;
                case AmbushTriggerMode.EnemyNearZone:
                    if (reserve.Zone.IsEnemyClose(GetEnemyAgents(reserve.Side)))
                    {
                        RevealReserve(reserve);
                    }
                    break;
                case AmbushTriggerMode.TimedAutoReveal:
                    RevealReserve(reserve);
                    break;
            }
        }
    }

    private bool HasEnemyInsideZone(AmbushReserve reserve)
    {
        foreach (Agent enemy in GetEnemyAgents(reserve.Side))
        {
            if (enemy?.IsActive() == true && reserve.Zone.ContainsWorldPosition(enemy.Position))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<Agent> GetEnemyAgents(BattleSideEnum side)
    {
        return Mission.Agents.Where(agent => agent?.Team != null && agent.Team.Side != side);
    }

    private void RevealReserve(AmbushReserve reserve)
    {
        // v1 scaffold:
        // the reserve state is now integrated into the mission, but troop-to-agent spawn
        // conversion still depends on scene setup and a later troop supplier pass.
        reserve.RevealState = AmbushRevealState.Triggered;
    }
}

using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.StaticAmbushes;

public sealed class AmbushReserve
{
    public string ReserveId { get; }
    public BattleSideEnum Side { get; }
    public AmbushZone Zone { get; }
    public AmbushTriggerMode TriggerMode { get; set; }
    public AmbushRevealState RevealState { get; set; }
    public bool IsPlayerControlled { get; set; }
    public int FormationIndex { get; set; }
    public float EarliestRevealTime { get; set; }
    public List<IAgentOriginBase> ReservedTroops { get; }

    public AmbushReserve(string reserveId, BattleSideEnum side, AmbushZone zone)
    {
        ReserveId = reserveId;
        Side = side;
        Zone = zone;
        TriggerMode = zone.TriggerMode;
        RevealState = AmbushRevealState.Ready;
        ReservedTroops = new List<IAgentOriginBase>();
        FormationIndex = -1;
    }

    public bool CanRevealNow(float missionTime)
    {
        if (RevealState != AmbushRevealState.Ready)
        {
            return false;
        }

        return missionTime >= EarliestRevealTime && ReservedTroops.Count > 0;
    }
}

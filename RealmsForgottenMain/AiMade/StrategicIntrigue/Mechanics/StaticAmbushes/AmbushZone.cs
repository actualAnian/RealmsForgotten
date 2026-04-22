using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.StaticAmbushes;

public class AmbushZone : MissionObject
{
    private float _width = 20f;

    private float _depth = 20f;

    private float _height = 8f;

    private AmbushZoneShape _shape = AmbushZoneShape.Box;

    private AmbushAllowedSide _allowedSide = AmbushAllowedSide.Defender;

    private AmbushConcealmentType _concealmentType = AmbushConcealmentType.Forest;

    private AmbushTriggerMode _triggerMode = AmbushTriggerMode.Manual;

    private float _revealDistance = 18f;

    private int _maxAssignedTroops = 60;

    private bool _debugDraw;

    public float Width => _width;
    public float Depth => _depth;
    public float Height => _height;
    public AmbushZoneShape Shape => _shape;
    public AmbushAllowedSide AllowedSide => _allowedSide;
    public AmbushConcealmentType ConcealmentType => _concealmentType;
    public AmbushTriggerMode TriggerMode => _triggerMode;
    public float RevealDistance => _revealDistance;
    public int MaxAssignedTroops => _maxAssignedTroops;
    public bool DebugDraw => _debugDraw;

    public bool AllowsSide(BattleSideEnum side)
    {
        return _allowedSide switch
        {
            AmbushAllowedSide.Attacker => side == BattleSideEnum.Attacker,
            AmbushAllowedSide.Defender => side == BattleSideEnum.Defender,
            AmbushAllowedSide.Both => side == BattleSideEnum.Attacker || side == BattleSideEnum.Defender,
            _ => false
        };
    }

    public IEnumerable<WeakGameEntity> GetSpawnPoints()
    {
        return GameEntity.GetChildren().Where(child => child.Name != null && child.Name.StartsWith("spawn_point_"));
    }

    public IEnumerable<WeakGameEntity> GetFallbackPoints()
    {
        return GameEntity.GetChildren().Where(child => child.Name != null && child.Name.StartsWith("fallback_point_"));
    }

    public bool ContainsWorldPosition(Vec3 worldPosition)
    {
        Vec3 center = GameEntity.GlobalPosition;
        Vec3 delta = worldPosition - center;

        if (_shape == AmbushZoneShape.Circle)
        {
            float radius = _width * 0.5f;
            return delta.AsVec2.LengthSquared <= radius * radius && TaleWorlds.Library.MathF.Abs(delta.z) <= _height;
        }

        return TaleWorlds.Library.MathF.Abs(delta.x) <= _width * 0.5f
            && TaleWorlds.Library.MathF.Abs(delta.y) <= _depth * 0.5f
            && TaleWorlds.Library.MathF.Abs(delta.z) <= _height;
    }

    public bool IsEnemyClose(IEnumerable<Agent> enemies)
    {
        Vec3 center = GameEntity.GlobalPosition;
        float revealDistanceSquared = _revealDistance * _revealDistance;

        foreach (Agent enemy in enemies)
        {
            if (enemy?.IsActive() == true)
            {
                Vec3 delta = enemy.Position - center;
                if (delta.AsVec2.LengthSquared <= revealDistanceSquared)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

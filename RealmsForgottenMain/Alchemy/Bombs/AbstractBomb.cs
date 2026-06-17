using RealmsForgotten.Alchemy;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.Alchemy.Bombs
{
    public abstract class AbstractBomb : IMissionPlane
    {
        public Vec3 Center { get; private set; }
        readonly RFBoundingBox _bounds;

        internal abstract float BaseDuration { get; }
        internal abstract float BaseRadius { get; }
        public HashSet<Agent> InsideAgents { get; } = new();
        public HashSet<Missile> InsideProjectiles { get; } = new();
        public AbstractBomb(Vec3 center, Agent caster)
        {
            float radius = BaseRadius;
            Center = center;
            _bounds = new(center.X - radius, center.X + radius, center.Y - radius, center.Y + radius);
            Radius = radius;
        }
        public RFBoundingBox Bounds { get => _bounds; }
        public bool Contains(Vec3 position) { return Center.DistanceSquared(position) < Radius * Radius; }
        public float Radius { get; internal set; }
        public float Duration { get; set; } = 5;
        public virtual string? ParticleId { get; set; } = null;
        public virtual void OnAgentDiedInside(Agent agent) { }
        public virtual void OnAgentEntered(Agent agent) { }
        public virtual void OnLeft(Agent agent) { }
        public virtual void OnProjectileEntered(Mission.Missile missile) { }
        public virtual void OnProjectileLeft(Mission.Missile missile) { }
        public virtual void OnInteraction(IMissionPlane anotherPlane) { }
    }
}

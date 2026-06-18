using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Alchemy
{
    public record RFBoundingBox
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
        public RFBoundingBox(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }
    }

    public interface IMissionPlane
    {
        public RFBoundingBox Bounds { get; }
        public bool Contains(Vec3 position);
        float Duration { get; set; }
        string? ParticleId { get; set; }
        public string Description { get; }
        public abstract Color TextColor { get; }
        public Vec3 TextPositionInMission { get; }
        void OnAgentEntered(Agent agent);
        void OnLeft(Agent agent);
        void OnProjectileEntered(Mission.Missile missile);
        void OnProjectileLeft(Mission.Missile missile);
        void OnAgentDiedInside(Agent agent);
        void OnInteraction(IMissionPlane anotherPlane);
        void OnTick(float dt);
    }
}

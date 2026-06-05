using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.Alchemy
{
    public interface IAlchemicalBomb
    {
        float Size { get; set; }
        float Duration { get; set; }
        string ParticleId { get; set; }
        void OnEntered(Agent agent);
        void OnLeft(Agent agent);
        void OnProjectileEntered(Mission.Missile missile);
        void OnProjectileLeft(Mission.Missile missile);
    }
}

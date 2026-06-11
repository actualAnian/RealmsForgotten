using TaleWorlds.MountAndBlade;
using RealmsForgotten.RFEffects.MissionEffects;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    internal class FireBomb : IAlchemicalBomb
    {
        public float Size { get; set; } = 6f;
        public float Duration { get; set; } = 10f;
        public string ParticleId { get; set; } = "alchemical_mist2";

        public float DurationSecondsAfterLeave => 5f;
        public float DamagePerSecond => 5f;

        public void OnAgentDiedInside(Agent agent)
        {
        }

        public void OnEntered(Agent agent)
        {
            var burn = new BurnEffect(DamagePerSecond);
            MissionEffectsBehavior.Instance.ApplyEffect(burn, agent, 0f, null, DurationSecondsAfterLeave);
        }

        public void OnLeft(Agent agent)
        {
        }

        public void OnProjectileEntered(Mission.Missile missile)
        {
        }

        public void OnProjectileLeft(Mission.Missile missile)
        {
        }
    }
}
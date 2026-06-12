using RealmsForgotten.RFEffects.Alchemy.OnHitEffects;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    internal class HeatBomb : AbstractBomb
    {
        public HeatBomb(Vec3 center, Agent caster) : base(center, caster) { }
        internal override float BaseDuration => 10f;
        internal override float BaseRadius => 6f;
        public override string? ParticleId { get; set; } = "alchemical_mist2";
        public float DurationSecondsAfterLeave => 5f;
        public float DamagePerSecond => 5f;

        public override void OnEntered(Agent agent)
        {
            agent.AgentDrivenProperties.SwingSpeedMultiplier *= 1.2f;
            agent.AgentDrivenProperties.ReloadSpeed *= 1.2f;
            //var burn = new BurnEffect(DamagePerSecond);
            //MissionEffectsBehavior.Instance.ApplyEffect(burn, agent, 0f, null, DurationSecondsAfterLeave);
        }
        public override void OnLeft(Agent agent)
        {
            agent.AgentDrivenProperties.SwingSpeedMultiplier *= 5/6;
            agent.AgentDrivenProperties.ReloadSpeed *= 5/6f;
        }
        public override void OnProjectileEntered(Missile missile)
        {
            var particleId = "fire_ground";
            if (ParticleSystemManager.GetRuntimeIdByName(particleId) == -1)
                InformationManager.DisplayMessage(new InformationMessage("Error, Particle with id: " + particleId + "not found", new Color(1, 0, 0)));

            MatrixFrame localFrame = new(Mat3.Identity, new(0, 0, 0));
            GameEntity childEntity = GameEntity.CreateEmpty(Current.Scene);
            ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, childEntity, ref localFrame);
            missile.Entity.AddChild(childEntity);
            AlchemyMissionLogic.Instance?.AddMissileEffect(missile, new ReduceMoraleEffect(1));
        }
    }
}
using RealmsForgotten.Alchemy.OnHitEffects;
using RealmsForgotten.MissionEffects;
using RealmsForgotten.MissionEffects.EffectDurationTypes;
using RealmsForgotten.MissionEffects.MissionEffectTypes;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.Alchemy.Bombs
{
    public class HeatBomb : AbstractBomb 
    {
        private readonly OnProjectilePassedTextProvider _projectilePassedText = new();
        public HeatBomb(Vec3 center, Agent caster) : base(center, caster) { }
        internal override float BaseDuration => 10f;
        internal override float BaseRadius => 6f;
        public override string? ParticleId { get; set; } = "alchemical_mist2";
        public float DurationSecondsAfterLeave => 5f;
        public float DamagePerSecond => 5f;

        TextObject _baseDescription = new("{rf_alchemy_heat_base} +20% swing speed, reload for {DURATION} seconds");
        TextObject _projectilePassedDescription = new("{rf_alchemy_heat_pro} +1 morale damage on projectile hit");
        public override string Description
        {
            get
            {
                GameTexts.SetVariable("DUTARION", Duration);
                return TextHelper.GetBaseDecriptionAndOnProjectilePassed(_baseDescription, _projectilePassedDescription, _projectilePassedText.ShowText);
            }
        } 
        public override void OnAgentEntered(Agent agent)
        {
            var heat = new HeatEffect(agent, new TimeBasedDuration(BaseDuration));
            MissionEffectsBehavior.Instance?.ApplyEffect(heat);

            var burn = new BurnEffect(agent, new TimeBasedDuration(DurationSecondsAfterLeave), DamagePerSecond);
            MissionEffectsBehavior.Instance?.ApplyEffect(burn);
        }
        public override void OnLeft(Agent agent)
        {
            MissionEffectsBehavior.Instance?.RemoveEffect(agent, typeof(HeatEffect));
        }
        public override void OnProjectileEntered(Missile missile)
        {
            _projectilePassedText.OnProjectilePassed();
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
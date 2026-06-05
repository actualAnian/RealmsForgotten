using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static TaleWorlds.MountAndBlade.Mission;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    public class SmokeBomb : IAlchemicalBomb
    {
        public float Size { get; set; } = 6f;
        public float Duration { get; set; } = 10f;
        public string ParticleId { get; set; } = "alchemical_mist2";

        public void OnEntered(Agent agent)
        {
            //agent.AgentDrivenProperties.WeaponInaccuracy += 1000;
            //agent.UpdateAgentProperties();
            agent.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire);
        }

        public void OnLeft(Agent agent)
        {
            agent.SetFiringOrder(FiringOrder.RangedWeaponUsageOrderEnum.FireAtWill);
        }

        public void OnProjectileEntered(Mission.Missile missile)
        {
            var particleId = "fire_ground";
            if (ParticleSystemManager.GetRuntimeIdByName(particleId) == -1)
                InformationManager.DisplayMessage(new InformationMessage("Error, Particle with id: " + particleId + "not found", new Color(1, 0, 0)));

            MatrixFrame localFrame = new(Mat3.Identity, new(0, 0, 0));
            GameEntity childEntity = GameEntity.CreateEmpty(Current.Scene);
            ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, childEntity, ref localFrame);
            missile.Entity.AddChild(childEntity);
        }

        public void OnProjectileLeft(Mission.Missile missile)
        {
        }
    }
}

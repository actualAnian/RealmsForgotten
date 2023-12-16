using RealmsForgotten.CustomSkills;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    //Thanks to TOW for providing this class
    public class TOWParticleSystem
    {
        public static ParticleSystem ApplyParticleToAgent(Agent agent, string particleId, out GameEntity childEntities, ParticleIntensity intensity = ParticleIntensity.Low, bool rootOnly = false)
        {
            childEntities = null;
            ParticleSystem particle = null;
            if (intensity != ParticleIntensity.Undefined)
            {
                int[] boneIndexes;
                if (rootOnly)
                {
                    boneIndexes = new int[] { 1 };
                }
                else
                {
                    boneIndexes = new int[] { 0, 1, 2, 3, 5, 6, 7, 9, 12, 13, 15, 17, 22, 24 };
                }
                for (byte i = 0; i < boneIndexes.Length / (int)intensity; i++)
                {
                    GameEntity childEntity;
                    particle = ApplyParticleToAgentBone(agent, particleId, (sbyte)boneIndexes[i], out childEntity);
                    if (particle == null)
                        return particle;

                    childEntities = childEntity;
                }
            }

            return particle;
        }

        public static ParticleSystem ApplyParticleToAgentBone(Agent agent, string particleId, sbyte boneIndex, out GameEntity childEntity, float elevationOffset = 0)
        {
            Skeleton skeleton = agent.AgentVisuals.GetSkeleton();
            Scene scene = Mission.Current.Scene;
            childEntity = GameEntity.CreateEmpty(scene);
            MatrixFrame localFrame = new MatrixFrame(Mat3.Identity, default(Vec3));
            localFrame.Elevate(elevationOffset);
            if (ParticleSystemManager.GetRuntimeIdByName(particleId) == -1)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Particle '{particleId}' doens't exist."));
                return null;
            }

            ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, childEntity, ref localFrame);
            if (particle != null)
            {
                agent.AgentVisuals.AddChildEntity(childEntity);

                skeleton.AddComponentToBone(boneIndex, particle);
            }

            return particle;
        }
        public static void ApplyParticleToWeapon(Agent agent, string particleId, EquipmentIndex equipmentIndex, float elevateAmount, Skeleton skeleton, out GameEntity weaponEntityFromEquipmentSlot)
        {
            weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
            if (weaponEntityFromEquipmentSlot == null)
                return;

            MatrixFrame matrixFrame4 = new MatrixFrame(Mat3.Identity, default(Vec3));
            MatrixFrame boneLocalFrame2 = matrixFrame4.Elevate(elevateAmount);
            ParticleSystem component = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, weaponEntityFromEquipmentSlot, ref boneLocalFrame2);
            if (ParticleSystemManager.GetRuntimeIdByName(particleId) == -1)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Particle '{particleId}' doens't exist."));
                return;
            }

            
            int arcaneLevel = agent.Character.GetSkillValue(RFSkills.Arcane) / 30;
            if (arcaneLevel < 1)
                arcaneLevel = 1;
            for (int i = 1; i <= arcaneLevel; i++)
                skeleton.AddComponentToBone(Game.Current.DefaultMonster.MainHandItemBoneIndex, component);


        }




        public enum ParticleIntensity
        {
            Undefined,
            High,
            Medium,
            Low = 14
        }
    }
}

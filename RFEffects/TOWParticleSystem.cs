using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
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
            GameEntity tempChildEntities = null;
            ParticleSystem returnParticle = null;
            ExceptionHandler.HandleMethod(() =>
            {
                tempChildEntities = null;
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
                        {
                            returnParticle = particle;
                            return;
                        }
                        
                        tempChildEntities = childEntity;
                    }
                }
            });
            childEntities = tempChildEntities;
            return returnParticle;
        }

        public static ParticleSystem ApplyParticleToAgentBone(Agent agent, string particleId, sbyte boneIndex, out GameEntity childEntity, float elevationOffset = 0)
        {
            GameEntity tempChildEntity = null;
            ParticleSystem returnParticle = null;
            ExceptionHandler.HandleMethod(() =>
            {
                Skeleton skeleton = agent.AgentVisuals.GetSkeleton();
                Scene scene = Mission.Current.Scene;
                tempChildEntity = GameEntity.CreateEmpty(scene);
                MatrixFrame localFrame = new MatrixFrame(Mat3.Identity, default(Vec3));
                localFrame.Elevate(elevationOffset);
                if (ParticleSystemManager.GetRuntimeIdByName(particleId) == -1)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Particle '{particleId}' doens't exist."));
                    return;
                }

                ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, tempChildEntity, ref localFrame);
                if (particle != null)
                {
                    agent.AgentVisuals.AddChildEntity(tempChildEntity);

                    skeleton.AddComponentToBone(boneIndex, particle);
                }
            });

            childEntity = tempChildEntity;
            return returnParticle;
        }
        public static void ApplyParticleToWeapon(Agent agent, string particleId, EquipmentIndex equipmentIndex, float elevateAmount, Skeleton skeleton, out GameEntity weaponEntityFromEquipmentSlot)
        {
            GameEntity temporaryWeaponEntity = null;
            ExceptionHandler.HandleMethod(() =>
            {
                temporaryWeaponEntity = agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
                if (temporaryWeaponEntity == null)
                    return;

                MatrixFrame matrixFrame4 = new MatrixFrame(Mat3.Identity, default(Vec3));
                MatrixFrame boneLocalFrame2 = matrixFrame4.Elevate(elevateAmount);
                ParticleSystem component = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, temporaryWeaponEntity, ref boneLocalFrame2);
                if (ParticleSystemManager.GetRuntimeIdByName(particleId) == -1)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Particle '{particleId}' doens't exist."));
                    return;
                }

            
                int arcaneLevel = Campaign.Current != null ? agent.Character.GetSkillValue(RFSkills.Arcane) / 30 : 1;
                if (arcaneLevel < 1)
                    arcaneLevel = 1;
            
                for (int i = 1; i <= arcaneLevel; i++)
                    skeleton.AddComponentToBone(Game.Current.DefaultMonster.MainHandItemBoneIndex, component);
            });

            weaponEntityFromEquipmentSlot = temporaryWeaponEntity;
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

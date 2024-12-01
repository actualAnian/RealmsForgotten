using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public static class ExceptionHandler
    {
        public static void HandleMethod(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error: {exception.Message}"));
            }
        }
    }

    public class ADODFireArrowsMissionBehavior : MissionBehavior
    {
        private HashSet<string> _fireArrowItemIds = new HashSet<string>
        {
            "adod_fire_arrows",
        };

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex equipmentIndex, Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody, int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, equipmentIndex, position, velocity, orientation, hasRigidBody, forcedMissileIndex);

            MissionWeapon weapon = shooterAgent.Equipment[equipmentIndex];
            if (weapon.CurrentUsageItem != null && weapon.CurrentUsageItem.WeaponClass == WeaponClass.Arrow)
            {
                string itemId = weapon.Item.StringId;
                if (_fireArrowItemIds.Contains(itemId))
                {
                    Mission.Missile missile = Mission.Current.Missiles.LastOrDefault(m => m.ShooterAgent == shooterAgent && m.Weapon.Item.StringId == itemId);
                    if (missile != null)
                    {
                        ApplyFireEffectToMissile(missile);
                    }
                }
            }
        }

        private void ApplyFireEffectToMissile(Mission.Missile missile)
        {
            string particleEffectName = "psys_game_missile_flame";

            ExceptionHandler.HandleMethod(() =>
            {
                if (ParticleSystemManager.GetRuntimeIdByName(particleEffectName) == -1)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Particle '{particleEffectName}' doesn't exist."));
                    return;
                }

                MatrixFrame identityFrame = MatrixFrame.Identity;
                GameEntity particleEntity = GameEntity.CreateEmpty(missile.Entity.Scene);
                ParticleSystem particleSystem = ParticleSystem.CreateParticleSystemAttachedToEntity(particleEffectName, particleEntity, ref identityFrame);

                if (particleSystem != null)
                {
                    missile.Entity.AddChild(particleEntity);
                    missile.Entity.AddComponent(particleSystem);
                }
            });
        }
    }
}

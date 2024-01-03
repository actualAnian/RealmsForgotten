using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using static TaleWorlds.MountAndBlade.Mission;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(Mission), "MissileAreaDamageCallback")]
    public static class IncreaseAreaOfDamagePatch
    {
        private static FieldInfo _attackBlockedWithShield =
            AccessTools.Field(typeof(AttackCollisionData), "_attackBlockedWithShield");
        private static CombatLogData GetAttackCollisionResults(Agent attackerAgent, Agent victimAgent, GameEntity hitObject, float momentumRemaining, in MissionWeapon attackerWeapon, bool crushedThrough, bool cancelDamage, bool crushedThroughWithoutAgentCollision, ref AttackCollisionData attackCollisionData, out WeaponComponentData shieldOnBack, out CombatLogData combatLog)
        {
            AttackInformation attackInformation = new AttackInformation(attackerAgent, victimAgent, hitObject, in attackCollisionData, in attackerWeapon);

            object checkShieldCollisionData = attackCollisionData;
            if (attackInformation.VictimShield.CurrentUsageItem == null)
                _attackBlockedWithShield.SetValue(checkShieldCollisionData, false);

            attackCollisionData = (AttackCollisionData)checkShieldCollisionData;
            
            shieldOnBack = attackInformation.ShieldOnBack;
            MissionCombatMechanicsHelper.GetAttackCollisionResults(in attackInformation, crushedThrough, momentumRemaining, in attackerWeapon, cancelDamage, ref attackCollisionData, out combatLog, out var _);
            float num = attackCollisionData.InflictedDamage;
            if (num > 0f)
            {
                float num2 = MissionGameModels.Current.AgentApplyDamageModel.CalculateDamage(in attackInformation, in attackCollisionData, in attackerWeapon, num);
                combatLog.ModifiedDamage = MathF.Round(num2 - num);
                attackCollisionData.InflictedDamage = MathF.Round(num2);
            }
            else
            {
                combatLog.ModifiedDamage = 0;
                attackCollisionData.InflictedDamage = 0;
            }

            if (!attackCollisionData.IsFallDamage && attackInformation.IsFriendlyFire)
            {
                if (!attackInformation.IsAttackerAIControlled && GameNetwork.IsSessionActive)
                {
                    int num3 = (attackCollisionData.IsMissile ? MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent.GetIntValue() : MultiplayerOptions.OptionType.FriendlyFireDamageMeleeSelfPercent.GetIntValue());
                    attackCollisionData.SelfInflictedDamage = MathF.Round((float)attackCollisionData.InflictedDamage * ((float)num3 * 0.01f));
                    int num4 = (attackCollisionData.IsMissile ? MultiplayerOptions.OptionType.FriendlyFireDamageRangedFriendPercent.GetIntValue() : MultiplayerOptions.OptionType.FriendlyFireDamageMeleeFriendPercent.GetIntValue());
                    attackCollisionData.InflictedDamage = MathF.Round((float)attackCollisionData.InflictedDamage * ((float)num4 * 0.01f));
                    combatLog.InflictedDamage = attackCollisionData.InflictedDamage;
                }

                combatLog.IsFriendlyFire = true;
            }

            if (attackCollisionData.AttackBlockedWithShield && attackCollisionData.InflictedDamage > 0 && attackInformation.VictimShield.HitPoints - attackCollisionData.InflictedDamage <= 0)
            {
                attackCollisionData.IsShieldBroken = true;
            }

            if (!crushedThroughWithoutAgentCollision)
            {
                combatLog.BodyPartHit = attackCollisionData.VictimHitBodyPart;
                combatLog.IsVictimEntity = hitObject != null;
            }

            return combatLog;
        }

        public static float isWand = 0f;
        public static Blow CurrentBlow;
        private static MethodInfo RegisterBlow = AccessTools.Method(typeof(Mission), "RegisterBlow");


        [HarmonyPrefix]
        public static bool Prefix(ref AttackCollisionData collisionDataInput, ref Blow blowInput, Agent alreadyDamagedAgent, Agent shooterAgent, bool isBigExplosion,  Mission __instance)
        {
            bool isBomb = blowInput.WeaponRecord.WeaponClass == WeaponClass.Stone &&
                        shooterAgent.WieldedWeapon.Item?.StringId.Contains("anorit_fire") == true &&
                        shooterAgent.Character.IsHero;

            if (isWand > 0f || isBomb)
            {
                float num = isBigExplosion ? 2.8f : 1.2f;
                float num2 = isBigExplosion ? 1.6f : 1f;


                if (isBomb)
                {
                    CharacterObject shooterAgentCharacterObject = shooterAgent.Character as CharacterObject;
                    if (shooterAgentCharacterObject != null)
                    {
                        float factor = shooterAgentCharacterObject.GetPerkValue(RFPerks.Alchemy.NovicesDedication) ? RFPerks.Alchemy.NovicesDedication.PrimaryBonus :
                            (shooterAgentCharacterObject.GetPerkValue(RFPerks.Alchemy.ApprenticesLuck) ? RFPerks.Alchemy.ApprenticesDedication.PrimaryBonus :
                                (shooterAgentCharacterObject.GetPerkValue(RFPerks.Alchemy.AdeptsLuck) ? RFPerks.Alchemy.AdeptsDedication.PrimaryBonus :
                                    (shooterAgentCharacterObject.GetPerkValue(RFPerks.Alchemy.MastersLuck) ? RFPerks.Alchemy.MastersDedication.PrimaryBonus : 0)));
                        num *= factor;
                    }
                }
                else if (isWand > 0f)
                    num *= isWand;


                float num3 = 1f;
                if (collisionDataInput.MissileVelocity.LengthSquared < 484f)
                {
                    num2 *= 0.8f;
                    num3 = 0.5f;
                }

                AttackCollisionData attackCollisionData = collisionDataInput;
                blowInput.VictimBodyPart = collisionDataInput.VictimHitBodyPart;
                List<Agent> list = new List<Agent>();
                AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(__instance, blowInput.GlobalPosition.AsVec2, num, extendRangeByBiggestAgentCollisionPadding: true);
                while (searchStruct.LastFoundAgent != null)
                {
                    Agent lastFoundAgent = searchStruct.LastFoundAgent;
                    if (lastFoundAgent.CurrentMortalityState != Agent.MortalityState.Invulnerable && lastFoundAgent != shooterAgent && lastFoundAgent != alreadyDamagedAgent)
                    {
                        list.Add(lastFoundAgent);
                    }

                    AgentProximityMap.FindNext(__instance, ref searchStruct);
                }

                foreach (Agent item in list)
                {
                    Blow b = blowInput;
                    b.DamageCalculated = false;
                    attackCollisionData = collisionDataInput;
                    float num4 = float.MaxValue;
                    float num5 = 0f;
                    sbyte collisionBoneIndexForAreaDamage = -1;
                    Skeleton skeleton = item.AgentVisuals.GetSkeleton();
                    sbyte boneCount = skeleton.GetBoneCount();
                    MatrixFrame globalFrame = item.AgentVisuals.GetGlobalFrame();
                    for (sbyte b2 = 0; b2 < boneCount; b2 = (sbyte)(b2 + 1))
                    {
                        num5 = globalFrame.TransformToParent(skeleton.GetBoneEntitialFrame(b2).origin).DistanceSquared(blowInput.GlobalPosition);
                        if (num5 < num4)
                        {
                            collisionBoneIndexForAreaDamage = b2;
                            num4 = num5;
                        }
                    }

                    if (num4 <= num * num)
                    {
                        float num6 = MathF.Sqrt(num4);
                        float num7 = 1f;
                        float num8 = 1f;
                        if (num6 > num2)
                        {
                            num7 = MBMath.Lerp(1f, 3f, (num6 - num2) / (num - num2));
                            num8 = 1f / (num7 * num7);
                        }

                        num8 *= num3;
                        attackCollisionData.SetCollisionBoneIndexForAreaDamage(collisionBoneIndexForAreaDamage);

                        Dictionary<int, Missile> ____missiles =
                            (Dictionary<int, Missile>)AccessTools.Field(typeof(Mission), "_missiles").GetValue(__instance);
                        MissionWeapon attackerWeapon = ____missiles[attackCollisionData.AffectorWeaponSlotOrMissileIndex].Weapon;


                        GetAttackCollisionResults(shooterAgent, item, null, 1f, in attackerWeapon, crushedThrough: false, cancelDamage: false, crushedThroughWithoutAgentCollision: false, ref attackCollisionData, out var _, out var combatLog);
                        b.BaseMagnitude = attackCollisionData.BaseMagnitude;
                        b.MovementSpeedDamageModifier = attackCollisionData.MovementSpeedDamageModifier;
                        b.InflictedDamage = attackCollisionData.InflictedDamage;
                        b.SelfInflictedDamage = attackCollisionData.SelfInflictedDamage;
                        b.AbsorbedByArmor = attackCollisionData.AbsorbedByArmor;
                        b.DamageCalculated = true;
                        b.InflictedDamage = MathF.Round((float)b.InflictedDamage * num8);
                        b.SelfInflictedDamage = MathF.Round((float)b.SelfInflictedDamage * num8);
                        combatLog.ModifiedDamage = MathF.Round((float)combatLog.ModifiedDamage * num8);

                        b.BoneIndex = item.IsHuman ? blowInput.BoneIndex : Game.Current.DefaultMonster.SpineUpperBoneIndex;

                        CurrentBlow = blowInput;

                        RegisterBlow.Invoke(__instance, new object[] { (object)shooterAgent, (object)item, (object)null, (object)b, (object)attackCollisionData, (object)attackerWeapon, (object)combatLog });
                        CurrentBlow = default;
                    }
                }
                isWand = 0f;
                return false;
                
            }

            return true;
        }
    }
}

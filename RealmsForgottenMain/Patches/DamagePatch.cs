using HarmonyLib;
using RealmsForgotten.Career.Logic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using RealmsForgotten.ObjectExtensions;
using System;
using static TaleWorlds.MountAndBlade.Mission;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using RealmsForgotten.Career;
using RealmsForgotten.Career.Ability;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch]
    public static class DamagePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Agent), "HandleBlow")]
        public static bool PreHandleBlow(ref Blow b, Agent __instance)
        {
            Agent attacker = b.OwnerId != -1 ? Current.FindAgentWithIndex(b.OwnerId) : __instance;
            Agent victim = __instance;

            //if (!victim.IsHuman || !attacker.IsHuman)
            //{
            //    return true;
            //}
            if (victim == attacker)
                return true;
            int baseDamage = b.InflictedDamage;
            MissionWeapon missionWeapon = GetWeapon(victim, attacker, b);

            float[] damageCategories = new float[(int)DamageType.All + 1];
            float[] damageProportions = new float[Enum.GetValues(typeof(DamageType)).Length];
            float[] additionalDamagePercentages = new float[Enum.GetValues(typeof(DamageType)).Length];
            float[] resistancePercentages = new float[Enum.GetValues(typeof(DamageType)).Length];
            DamageType damageType = CalculateDefaultDamage(missionWeapon);
            if (Game.Current.GameType is Campaign)
            {
                //troop specific benefits
                if (CareerHelper.IsValidCareerMissionInteractionBetweenAgents(attacker, victim))
                {
                    PropertyMask property = PropertyMask.All;
                    WeaponClass? weapon = null;
                    if (missionWeapon.CurrentUsageItem != null)
                       weapon = missionWeapon.CurrentUsageItem.WeaponClass;

                    if (attacker.BelongsToMainParty())
                    {
                        property = PropertyMask.Attack;
                        var careerBonuses = CareerHelper.AddCareerPassivesForDamageValues(attacker, victim, property, weapon);
                        for (var index = 0; index < careerBonuses.Length; index++)
                        {
                            additionalDamagePercentages[index] += careerBonuses[index];
                        }
                    }

                    if (victim.BelongsToMainParty())
                    {
                        property = PropertyMask.Defense;
                        var careerBonuses = CareerHelper.AddCareerPassivesForDamageValues(attacker, victim, property, weapon);
                        for (var index = 0; index < careerBonuses.Length; index++)
                        {
                            resistancePercentages[index] += careerBonuses[index];
                        }
                    }
                    ClassAbility ability = PlayerCareerExtension.GetCareer().Ability;
                    if (ability.IsActiveInMission)
                    {
                        ability.OnTroopPreHit(attacker, victim, ref additionalDamagePercentages, ref resistancePercentages);
                    }
                }
            }
            float summedBonus = 0;
            summedBonus += additionalDamagePercentages[(int)damageType];
            summedBonus -= resistancePercentages[(int)damageType];
            if (victim.Character != null) 
            {
                (Func<BasicCharacterObject, bool> Check, float[] Resistances) match = Globals.RaceResistances.FirstOrDefault(entry => entry.Check(victim.Character));
                if (match.Resistances != null)
                    summedBonus -= match.Resistances[(int)damageType];
            }
            //summedBonus -= Globals.RaceResistances.FirstOrDefault(entry => entry.Check(victim.Character)).Resistances[(int)damageType];
            int resultDamage = (int)(baseDamage + baseDamage * summedBonus);

            b.InflictedDamage = resultDamage;

            if (victim.ShouldShrugOffDamage(b.InflictedDamage))
            {
                b.BlowFlag |= BlowFlags.ShrugOff;
            }
            if (attacker == Agent.Main || victim == Agent.Main)
                DisplayDamageResult(baseDamage, resultDamage, summedBonus, victim == Agent.Main);
            return true;
        }

        private static MissionWeapon GetWeapon(Agent victim, Agent attacker, Blow b)
        {
            MissionWeapon missionWeapon;
            int affectorWeaponSlotOrMissileIndex = b.WeaponRecord.AffectorWeaponSlotOrMissileIndex;
            if (b.IsMissile)
            {
                Type missionType = victim.Mission.GetType();
                System.Reflection.FieldInfo missilesField = AccessTools.Field(missionType, "_missiles");
                Dictionary<int, Missile> missilesValue = (Dictionary<int, Missile>)missilesField.GetValue(victim.Mission);
                missionWeapon = missilesValue[affectorWeaponSlotOrMissileIndex].Weapon;
            }
            else
            {
                missionWeapon = attacker != null && affectorWeaponSlotOrMissileIndex >= 0 ? attacker.Equipment[affectorWeaponSlotOrMissileIndex] : MissionWeapon.Invalid;
            }
            return missionWeapon;
        }
        public static DamageType CalculateDefaultDamage(MissionWeapon missionWeapon)
        {
            if (missionWeapon.CurrentUsageItem == null) return DamageType.Invalid;
            switch (missionWeapon.CurrentUsageItem.WeaponClass)
            {
                case WeaponClass.Stone:
                    if (missionWeapon.IsAlchemicalWeapon()) return DamageType.Alchemical;
                    else return DamageType.PhysicalRanged;
                case WeaponClass.Arrow or WeaponClass.Bolt or WeaponClass.Javelin or WeaponClass.ThrowingAxe or WeaponClass.ThrowingKnife:
                    return DamageType.PhysicalRanged;
                case WeaponClass.Cartridge:
                    return DamageType.Magical;
                default: // standard melee weapon
                    return DamageType.PhysicalMelee;
            }
        }

        private static bool IsAlchemicalWeapon(this MissionWeapon missionWeapon)
        {
            List<string> allAlchemicalTexts = new()
            {
                "rfmisc_anorit_fire",
                "rfmisc_alchemist",
                "poisoned"
            };
            string itemId = missionWeapon.Item.StringId;
            return allAlchemicalTexts.Any(sub => itemId.Contains(sub));
        }
        public static void DisplayDamageResult(int baseDamage, int resultDamage, float bonus, bool isVictim)
        {
            Color displaycolor;// = Color.White;
            string sign = bonus >= 0 ? "+" : "-";

            if (isVictim) displaycolor = Color.FromUint(9856100);
            else displaycolor = Colors.Cyan;


            var resultText = $"{resultDamage} damage was dealt which was {baseDamage} {sign} {bonus} from bonuses.";
            InformationManager.DisplayMessage(new InformationMessage(resultText, displaycolor));
        }

    }
}

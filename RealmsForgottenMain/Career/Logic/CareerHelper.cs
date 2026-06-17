using RealmsForgotten.ObjectExtensions;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Logic
{
    public static class CareerHelper
    {
        public static void ApplyBasicCareerPassives(ref ExplainedNumber number, PassiveEffectType passiveEffectType)
        {
            PlayerClassInfo info = PlayerCareerExtension.PlayerCareerInfo;
            if (info == null) return;
            List<string> choices = info.CareerChoices;
            foreach (var choiceID in choices)
            {
                CareerChoiceObject choice = RFCareerChoices.GetChoice(choiceID);

                if (choice?.Passive == null || choice.Passive.PassiveEffectType != passiveEffectType) continue;

                CareerChoiceObject.PassiveEffect passive = choice.Passive;

                if (!passive.IsValidCharacterObject(Hero.MainHero.CharacterObject)) continue;

                float value = passive.EffectMagnitude;
                TextObject? text = choice.BelongsToGroup?.Name;
                if (passive.Operation == OperationType.Multiply)
                    number.AddFactor(value, text);
                else if (passive.Operation == OperationType.Replace)
                    number = new(value);
                else
                    number.Add(value, text);
            }
        }
        public static void ApplyBasicCareerPassives(ref int number, PassiveEffectType passiveEffectType)
        {
            CharacterObject characterObject = Hero.MainHero.CharacterObject;
            var info = PlayerCareerExtension.PlayerCareerInfo;
            if (info == null) return;
            List<string> choices = info.CareerChoices;
            foreach (var choiceID in choices)
            {
                var choice = RFCareerChoices.GetChoice(choiceID);

                if (choice?.Passive == null || choice.Passive.PassiveEffectType != passiveEffectType) continue;

                var passive = choice.Passive;

                if (!passive.IsValidCharacterObject(characterObject)) continue;
                var value = passive.EffectMagnitude;
                var text = choice.BelongsToGroup?.Name;
                if (passive.Operation == OperationType.Multiply)
                {
                    number = (int)(number * value);
                    continue;
                }
                else if (passive.Operation == OperationType.Add)
                    number = (int)(number + value);
                else number = (int)value;
            }
        }

        public static float[] AddCareerPassivesForDamageValues(Agent attacker, Agent victim, PropertyMask mask, WeaponClass? weapon)
        {
            var damageValues = new float[(int)DamageType.All + 1];

            switch (mask)
            {
                case PropertyMask.Attack:
                    if (attacker.IsHero && attacker.IsMainAgent)
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.Damage, weapon);
                    else
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.TroopDamage, weapon);
                    break;
                case PropertyMask.Defense:
                    if (victim.IsHero && victim.IsMainAgent)
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.Resistance, weapon);
                    else
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.TroopResistance, weapon);
                    break;
                default:
                    break;
            }
            return damageValues;
        }
        private static void ApplyCareerPassivesForDamageValues(Agent agent, Agent victim, ref float[] values, PassiveEffectType type, WeaponClass? weapon)
        {
            var choices = PlayerCareerExtension.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                CareerChoiceObject choice = RFCareerChoices.GetChoice(choiceID);
                if (choice == null || choice.Passive == null || (choice.Passive.PassiveEffectType != type) || choice.Passive.DamageProportionTuple == null)
                    continue;
                List<WeaponClass>? perkWeaponClass = choice.Passive.DamageProportionTuple.WeaponClasses;
                
                //if (!choice.Passive.IsValidCombatInteraction(agent, victim, attackMask)) continue;
                if (perkWeaponClass != null && (weapon == null || !perkWeaponClass.Contains((WeaponClass)weapon)))
                        continue;
                var passive = choice.Passive;
                var damageType = passive.DamageProportionTuple.DamageType;
                values[(int)damageType] += (passive.DamageProportionTuple.Percent / 100);
            }
        }
        public static bool IsValidCareerMissionInteractionBetweenAgents(Agent affectorAgent, Agent affectedAgent)
        {
            if (Campaign.Current == null 
                || !PlayerCareerExtension.HasAnyCareer() 
                || affectorAgent == null
                || Agent.Main == null
                || affectorAgent.IsMount 
                || affectedAgent.IsMount) return false;
            return affectorAgent.BelongsToMainParty() || affectedAgent.BelongsToMainParty();
        }
    }
}

using RealmsForgotten.ObjectExtensions;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career.Logic
{
    public static class CareerHelper
    {
        public static void ApplyBasicCareerPassives(ref ExplainedNumber number, PassiveEffectType passiveEffectType, bool asFactor = true)
        {
            var info = PlayerCareerExtension.PlayerCareerInfo;
            if (info == null) return;
            List<string> choices = info.CareerChoices;
            foreach (var choiceID in choices)
            {
                var choice = RFCareerChoices.GetChoice(choiceID);

                if (choice?.Passive == null || choice.Passive.PassiveEffectType != passiveEffectType) continue;

                var passive = choice.Passive;

                if (!passive.IsValidCharacterObject(Hero.MainHero.CharacterObject)) continue;

                if (passive.WithFactorFlatSwitch)
                {
                    asFactor = !asFactor;
                }
                var value = passive.EffectMagnitude;
                var text = choice.BelongsToGroup.Name;
                if (passive.InterpretAsPercentage)
                {
                    value /= 100;
                }
                if (asFactor)
                {
                    number.AddFactor(value, text);
                    continue;
                }
                number.Add(value, text);
            }
        }
        public static void ApplyBasicCareerPassives(ref ExplainedNumber number, PassiveEffectType passiveEffectType, AttackTypeMask mask, bool asFactor = false)
        {
            CharacterObject characterObject = Hero.MainHero.CharacterObject;
            var info = PlayerCareerExtension.PlayerCareerInfo;
            if (info == null) return;
            List<string> choices = info.CareerChoices;
            foreach (var choiceID in choices)
            {
                var choice = RFCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && choice.Passive.PassiveEffectType == passiveEffectType)
                {
                    var value = choice.Passive.EffectMagnitude;
                    if (choice.Passive.InterpretAsPercentage)
                    {
                        value /= 100;
                    }
                    if (asFactor)
                    {
                        number.AddFactor(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                        return;
                    }
                    number.Add(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                }
            }
        }
        public static void ApplyBasicCareerPassives(ref int number, PassiveEffectType passiveEffectType, bool asFactor = true)
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

                if (passive.WithFactorFlatSwitch)
                {
                    asFactor = !asFactor;
                }
                var value = passive.EffectMagnitude;
                var text = choice.BelongsToGroup.Name;
                if (passive.InterpretAsPercentage)
                {
                    value /= 100;
                }
                if (asFactor)
                {
                    number = (int)(number * value);
                    continue;
                }
                number = (int)(number + value);
            }
        }

        public static float[] AddCareerPassivesForDamageValues(Agent attacker, Agent victim, PropertyMask mask)
        {
            var damageValues = new float[(int)DamageType.All + 1];

            switch (mask)
            {
                case PropertyMask.Attack:
                    if (attacker.IsHero && attacker.IsMainAgent)
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.Damage);
                    }
                    else
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.TroopDamage);
                    }
                    return damageValues;
                case PropertyMask.Defense:
                    if (victim.IsHero && victim.IsMainAgent)
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.Resistance);
                    }
                    else
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, PassiveEffectType.TroopResistance);
                    }

                    return damageValues;
                default:
                    return null;
            }
        }
        private static void ApplyCareerPassivesForDamageValues(Agent agent, Agent victim, ref float[] values, PassiveEffectType type)
        {
            if (type != PassiveEffectType.Damage &&
                type != PassiveEffectType.TroopDamage &&
                type != PassiveEffectType.Resistance &&
                type != PassiveEffectType.TroopResistance) return;

            var choices = PlayerCareerExtension.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = RFCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && (choice.Passive.PassiveEffectType == type))
                {
                    //if (!choice.Passive.IsValidCombatInteraction(agent, victim, attackMask)) continue;
                    var passive = choice.Passive;
                    var damageType = passive.DamageProportionTuple.DamageType;
                    values[(int)damageType] += (passive.DamageProportionTuple.Percent / 100);
                }
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

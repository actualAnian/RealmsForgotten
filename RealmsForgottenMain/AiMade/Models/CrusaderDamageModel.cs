using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Career
{
    public static class CrusaderDamageModel
    {
        private const float CrusaderBonus = 1.10f; // 10% bonus damage

        public static void CalculateDamage(CharacterObject attackerCharacterObject, CharacterObject victimCharacterObject, ref float baseResult)
        {
            if (attackerCharacterObject != null && attackerCharacterObject.HeroObject == Hero.MainHero && IsBandit(victimCharacterObject))
            {
                baseResult *= CrusaderBonus;
            }

            if (victimCharacterObject != null && victimCharacterObject.HeroObject == Hero.MainHero)
            {
                var divineShieldBehavior = Mission.Current.GetMissionBehavior<DivineShieldMissionBehavior>();
                if (divineShieldBehavior != null && divineShieldBehavior.IsShieldActive())
                {
                    baseResult *= 1 - divineShieldBehavior.GetDamageAbsorption();
                }
            }
        }

        public static bool IsBandit(CharacterObject character)
        {
            return character != null && character.Occupation == Occupation.Bandit;
        }
    }
}
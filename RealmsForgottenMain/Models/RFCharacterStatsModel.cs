using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.LordConversationsCampaignBehavior;

namespace RealmsForgotten.Models
{
    public class RFCharacterStatsModel : DefaultCharacterStatsModel
    {
        CharacterStatsModel baseModel;
        public RFCharacterStatsModel(CharacterStatsModel previousModel)
        {
            baseModel = previousModel;
        }

        public override ExplainedNumber MaxHitpoints(CharacterObject character, bool includeDescriptions = false)
        {
            ExplainedNumber value = base.MaxHitpoints(character, includeDescriptions);
            if (character != Hero.MainHero.CharacterObject) return value;
            CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.Health, false);
            return value;
        }
    }
}

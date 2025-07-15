using RealmsForgotten.Career.Logic;
using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.LordConversationsCampaignBehavior;
using TaleWorlds.CampaignSystem.Party;

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
            if (character.IsHero && character.HeroObject != Hero.MainHero && character.HeroObject.PartyBelongedTo == MobileParty.MainParty)
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.CompanionHealth, false);
            if (CharacterObject.PlayerCharacter != null &&  character.HeroObject == Hero.MainHero) 
                CareerHelper.ApplyBasicCareerPassives(ref value, PassiveEffectType.Health, false);
            return value;
        }
    }
}

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace RealmsForgotten.Models
{
    internal class RFDefaultCharacterDevelopmentModel : CharacterDevelopmentModel
    {
        private CharacterDevelopmentModel _baseModel;
        
        public RFDefaultCharacterDevelopmentModel(CharacterDevelopmentModel previousModel)
        {
            _baseModel = previousModel;
        }

        public override int MaxAttribute => _baseModel.MaxAttribute;

        public override int MaxFocusPerSkill => _baseModel.MaxFocusPerSkill;

        public override int MaxSkillRequiredForEpicPerkBonus => _baseModel.MaxSkillRequiredForEpicPerkBonus;

        public override int MinSkillRequiredForEpicPerkBonus => _baseModel.MinSkillRequiredForEpicPerkBonus;

        public override int FocusPointsPerLevel => _baseModel.FocusPointsPerLevel;

        public override int FocusPointsAtStart => _baseModel.FocusPointsAtStart;

        public override int AttributePointsAtStart => _baseModel.AttributePointsAtStart;

        public override int LevelsPerAttributePoint => _baseModel.LevelsPerAttributePoint;

        public override ExplainedNumber CalculateLearningLimit(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, SkillObject skill, bool includeDescriptions = false) => _baseModel.CalculateLearningLimit(characterAttributes, focusValue, skill, includeDescriptions);
        public override ExplainedNumber CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute> characterAttributes, int focusValue, int skillValue, SkillObject skill, bool includeDescriptions = false)
        {
            return _baseModel.CalculateLearningRate(characterAttributes, focusValue, skillValue, skill, includeDescriptions);
        }

        public override int GetMaxSkillPoint() => _baseModel.GetMaxSkillPoint();
        public override CharacterAttribute GetNextAttributeToUpgrade(Hero hero) => _baseModel.GetNextAttributeToUpgrade(hero);
        public override PerkObject GetNextPerkToChoose(Hero hero, PerkObject perk) => _baseModel.GetNextPerkToChoose(hero, perk);
        public override SkillObject GetNextSkillToAddFocus(Hero hero) => _baseModel.GetNextSkillToAddFocus(hero);
        public override int GetSkillLevelChange(Hero hero, SkillObject skill, float skillXp) => _baseModel.GetSkillLevelChange(hero, skill, skillXp);
        public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int newValue, out int traitLevel, out int traitXp) => _baseModel.GetTraitLevelForTraitXp(hero, trait, newValue, out traitLevel, out traitXp);
        public override int GetTraitXpRequiredForTraitLevel(TraitObject trait, int traitLevel) => _baseModel.GetTraitXpRequiredForTraitLevel(trait, traitLevel);
        public override int GetXpAmountForSkillLevelChange(Hero hero, SkillObject skill, int skillLevelChange) => _baseModel.GetXpAmountForSkillLevelChange(hero, skill, skillLevelChange);
        public override int GetXpRequiredForSkillLevel(int skillLevel) => _baseModel.GetXpRequiredForSkillLevel(skillLevel);
        public override int SkillsRequiredForLevel(int level) => _baseModel.SkillsRequiredForLevel(level);
    }
}
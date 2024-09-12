using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class RFDefaultCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
    {
        private CharacterDevelopmentModel _previousModel;
        
        public RFDefaultCharacterDevelopmentModel(CharacterDevelopmentModel previousModel)
        {
            _previousModel = previousModel;
        }
        public override float CalculateLearningRate(Hero hero, SkillObject skill)
        {
            float baseValue = _previousModel.CalculateLearningRate(hero, skill);
            if (hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("elvean") && skill == DefaultSkills.Athletics)
                return ((15f / 100f) * baseValue) + baseValue;
            return baseValue;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Models
{
    internal class RFDefaultCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
    {

        public override float CalculateLearningRate(Hero hero, SkillObject skill)
        {
            float baseValue = base.CalculateLearningRate(hero, skill);
            if (hero.CharacterObject.Race == FaceGen.GetRaceOrDefault("elvean") && skill == DefaultSkills.Athletics)
                return ((15f / 100f) * baseValue) + baseValue;
            return baseValue;
        }
    }
}

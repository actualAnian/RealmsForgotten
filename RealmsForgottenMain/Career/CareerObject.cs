using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.Career;
using RealmsForgotten.Career.Ability;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Career
{
    public class CareerObject : PropertyObject
    {
        private Predicate<Hero> _condition;
        // @TODO remove
        public int MaxCharge { get; private set; } = 0;
        public ClassAbility Ability { get; private set; }
        public string AbilityTemplateID { get; private set; } = string.Empty;
        public Type AbilityScriptType { get; private set; } = null;
        // @TODO remove
        public CareerChoiceObject RootNode { get; set; }
        public List<CareerChoiceGroupObject> ChoiceGroups { get; private set; } = new List<CareerChoiceGroupObject>();

        public List<CareerChoiceObject> AllChoices
        {
            get
            {
                List<CareerChoiceObject> result = new List<CareerChoiceObject>();
                ChoiceGroups.ForEach(x => x.Choices.ForEach(y => result.Add(y)));
                return result;
            }
        }

        public CareerObject(string stringId, ClassAbility ability) : base(stringId) { Ability = ability; }

        public override string ToString() => Name.ToString();

        public void Initialize(string name, Predicate<Hero> condition)
        {
            var description = GameTexts.FindText("class_description", StringId);
            Initialize(new TextObject(name), description);
            _condition = condition;
            AfterInitialized();
        }

        public bool IsConditionsMet(Hero hero)
        {
            return _condition != null && _condition(hero);
        }
    }
}
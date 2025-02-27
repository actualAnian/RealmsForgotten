using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.Career;
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

        public CareerObject(string stringId) : base(stringId) { }

        public override string ToString() => Name.ToString();

        public void Initialize(string name, Predicate<Hero> condition)
        {
            var description = GameTexts.FindText("class_description", StringId);
            base.Initialize(new TextObject(name), description);
            _condition = condition;
            AfterInitialized();
        }

        public bool IsConditionsMet(Hero hero)
        {
            return _condition != null && _condition(hero);
        }
        //public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Agent triggererAgent)
        //{
        //    Agent triggererAgent = Hero.MainHero.age;
        //    if (triggererAgent != null && triggererAgent.GetHero()?.GetExtendedInfo() != null)
        //    {

        //        var root = triggererAgent.GetHero().GetCareer().RootNode;
        //        var info = triggererAgent.GetHero().GetExtendedInfo();
        //        if (info.CareerID == StringId)
        //        {
        //            List<CareerChoiceObject> modifications = new List<CareerChoiceObject> { root };
        //            modifications.AddRange(AllChoices.Where(x => info.CareerChoices.Contains(x.StringId)));
        //            foreach (var choice in modifications)
        //            {
        //                if (choice.HasMutations())
        //                    choice.MutateTriggeredEffect(effect, triggererAgent);
        //            }
        //        }
        //    }
        //}

        //public void MutateStatusEffect(StatusEffectTemplate effect, Agent applierAgent)
        //{
        //    if (applierAgent != null && applierAgent.GetHero()?.GetExtendedInfo() != null)
        //    {
        //        var info = applierAgent.GetHero().GetExtendedInfo();
        //        if (info.CareerID != StringId) return;
        //        var choices = new List<CareerChoiceObject>();
        //        choices.Add(RootNode);
        //        choices.AddRange(AllChoices.Where(x => info.CareerChoices.Contains(x.StringId)));

        //        foreach (var choice in choices.Where(choice => choice.HasMutations()))
        //        {
        //            choice.MutateStatusEffect(effect, applierAgent);
        //        }
        //    }
        //}
    }
}
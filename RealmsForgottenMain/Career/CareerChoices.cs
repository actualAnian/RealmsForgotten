using RealmsForgotten.Career.CareerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using RealmsForgotten.Career;
using RealmsForgotten.Career.CareerTypes;

namespace RealmsForgotten.Career
{
    public class RFCareerChoices
    {
        public static RFCareerChoices Instance { get; private set; }

        private readonly List<RFCareerChoicesBase> _allCareers;

        public RFCareerChoices()
        {
            Instance = this;
            SetBasicTextVariables();

            _allCareers = new()
            {
                new MercenaryCareerChoices(RFCareers.Mercenary),
                //new GrailKnightCareerChoices(RFCareers.GrailKnight),
            };
        }



        public static CareerChoiceObject GetChoice(string id) => Game.Current.ObjectManager.GetObject<CareerChoiceObject>(x => x.StringId == id);


        public RFCareerChoicesBase GetCareerChoices(CareerObject id)
        {
            return _allCareers.FirstOrDefault(x => x.GetID() == id);
        }

        private void SetBasicTextVariables()
        {
            foreach (var type in Enum.GetValues(typeof(PassiveEffectType)).Cast<PassiveEffectType>())
            {
                GameTexts.SetVariable("TOR_CHOICE_" + type.ToString().ToUpper(), GameTexts.FindText("tor_careerchoice_basic", type.ToString()));
            }

        }
    }
}

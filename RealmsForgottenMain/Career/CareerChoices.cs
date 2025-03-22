using RealmsForgotten.Career.CareerTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace RealmsForgotten.Career
{
    public class RFCareerChoices
    {

        private readonly List<RFCareerChoicesBase> _allCareers;
        private static RFCareerChoices? instance;

        public RFCareerChoices()
        {
            instance = this; 
            _allCareers = new()
            {
                new MercenaryCareerChoices(RFCareers.Mercenary),
                //new GrailKnightCareerChoices(RFCareers.GrailKnight),
            };
        }

        public static RFCareerChoices Instance
        {
            get
            {
                instance ??= new RFCareerChoices();
                return instance;
            }
        }

        public static CareerChoiceObject GetChoice(string id) => Game.Current.ObjectManager.GetObject<CareerChoiceObject>(x => x.StringId == id);

        public RFCareerChoicesBase GetCareerChoices(CareerObject id)
        {
            return _allCareers.FirstOrDefault(x => x.GetID() == id);
        }
    }
}

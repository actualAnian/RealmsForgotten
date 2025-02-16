using System.Collections.Generic;

namespace RealmsForgotten.Career
{
    public class CareerButtons
    {
        private readonly Dictionary<string, CareerButtonBehaviorBase> _careerButtons = new();
        private static CareerButtons _instance;

        public static CareerButtons Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CareerButtons();
                    return _instance;
                }

                return _instance;
            }
        }

        public CareerButtons()
        {
            _careerButtons.Add(RFCareers.Mercenary.StringId, new MercenaryCareerButtonBehavior(RFCareers.Mercenary));
            //_careerButtons.Add(TORCareers.GrailKnight.StringId, new GrailKnightCareerButtonBehavior(TORCareers.GrailKnight));
        }

        public CareerButtonBehaviorBase GetCareerButton(CareerObject careerObject)
        {
            if (_careerButtons.ContainsKey(careerObject.StringId))
            {
                return _careerButtons[careerObject.StringId];
            }

            return null;
        }
    }
}
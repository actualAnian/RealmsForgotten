using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
    {
        public static class CareerManager
        {
        private static List<CareerObject> Careers = new List<CareerObject>();
        private static List<CareerChoiceObject> CareerChoices = new List<CareerChoiceObject>();

        public static void RegisterCareer(CareerObject career)
            {
                Careers.Add(career);
            }

            public static void RegisterCareerChoice(CareerChoiceObject choice)
            {
                CareerChoices.Add(choice);
            }

            public static CareerObject GetCareerById(string id)
            {
                return Careers.Find(career => career.Id == id);
            }

            public static List<CareerObject> GetAllCareers()
            {
                return Careers;
            }

            public static CareerChoiceObject GetCareerChoiceById(string id)
            {
                return CareerChoices.Find(choice => choice.Id == id);
            }

            public static List<CareerChoiceObject> GetAllCareerChoices()
            {
                return CareerChoices;
            }
        public static void SyncData(IDataStore dataStore)
        {
            _ = dataStore.SyncData("Careers", ref Careers);
            _ = dataStore.SyncData("CareerChoices", ref CareerChoices);
        }
    }
    }

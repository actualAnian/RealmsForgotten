using System.Collections.Generic;

namespace RealmsForgotten.AiMade.Career
{
    public static class CareerInitialization
    {
        public static void InitializeCareers()
        {
            var mercenaryCareer = new CareerObject
            {
                Type = CareerType.Mercenary,
                Name = "Mercenary",
                Tiers = new List<CareerTier>
                {
                    new CareerTier { Name = "Recruit", Benefit = "+5% damage with melee weapons", ProgressionRequirement = "Complete Battles", RequiredValue = 10 },
                    new CareerTier { Name = "Veteran", Benefit = "Troop wages reduced by 10%", ProgressionRequirement = "Gain Experience", RequiredValue = 5000 },
                    new CareerTier { Name = "Elite", Benefit = "15% bonus in looting when raiding villages", ProgressionRequirement = "Complete Contracts", RequiredValue = 4 },
                    new CareerTier { Name = "Commander", Benefit = "+20% reputation gain with factions", ProgressionRequirement = "Reach Steward Level", RequiredValue = 150 },
                    new CareerTier { Name = "Legendary", Benefit = "Unlock special ability 'Battle Cry'", ProgressionRequirement = "Win Battles", RequiredValue = 50 },
                    new CareerTier { Name = "Slayer", Benefit = "+10% damage to all enemies", ProgressionRequirement = "Kill Enemies", RequiredValue = 100 }
                }
            };
            CareerManager.RegisterCareer(mercenaryCareer);

            var knightCareer = new CareerObject
            {
                Type = CareerType.Knight,
                Name = "Knight",
                Tiers = new List<CareerTier>
                {
                    new CareerTier { Name = "Squire", Benefit = "+5% damage with swords", ProgressionRequirement = "Train", RequiredValue = 10 },
                    new CareerTier { Name = "Cavalier", Benefit = "+10% speed on horseback", ProgressionRequirement = "Gain Experience", RequiredValue = 3000 },
                    new CareerTier { Name = "Paladin", Benefit = "Heal 5% health after battle", ProgressionRequirement = "Complete Quests", RequiredValue = 3 },
                    new CareerTier { Name = "Crusader", Benefit = "+10% damage to bandits", ProgressionRequirement = "Defeat Bandits", RequiredValue = 50 },
                    new CareerTier { Name = "Champion", Benefit = "Unlock special ability 'Divine Shield'", ProgressionRequirement = "Win Tournaments", RequiredValue = 5 }
                }
            };
            CareerManager.RegisterCareer(knightCareer);
        }
    }
}



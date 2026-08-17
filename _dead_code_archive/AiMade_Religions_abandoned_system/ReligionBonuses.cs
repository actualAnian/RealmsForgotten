using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Religions
{
    [SaveableClass]
    public class ReligionBonuses
    {
        [SaveableField(1)]
        public int RelationBonus; // Bonus to relations with followers of the same religion

        [SaveableField(2)]
        public int RelationPenalty; // Penalty to relations with followers of different religions

        [SaveableField(3)]
        public float ProsperityBonus; // Bonus to settlement prosperity

        [SaveableField(4)]
        public float ProsperityPenalty; // Penalty to settlement prosperity

        [SaveableField(5)]
        public float LoyaltyBonus; // Bonus to settlement loyalty

        [SaveableField(6)]
        public float LoyaltyPenalty; // Penalty to settlement loyalty

        [SaveableField(7)]
        public float GrowthBonus; // Bonus to settlement growth

        [SaveableField(8)]
        public float GrowthPenalty; // Penalty to settlement growth

        [SaveableField(9)]
        public float RecruitmentBonus; // Bonus to settlement recruitment

        [SaveableField(10)]
        public float RecruitmentPenalty; // Penalty to settlement recruitment

        [SaveableField(11)]
        public float WorkshopProductionBonus; // Bonus to workshop production

        [SaveableField(12)]
        public float WorkshopProductionPenalty; // Penalty to workshop production

        [SaveableField(13)]
        public float MilitaryBuildingSpeedBonus; // Bonus to military building speed

        [SaveableField(14)]
        public float BuildingSpeedPenalty; // Penalty to building speed

        // Additional bonuses and penalties can be added here
    }
}

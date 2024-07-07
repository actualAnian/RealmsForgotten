using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
{
    public static class InitializeMercenaryContracts
    {
        public static void Initialize()
        {
            var raidVillagesContract = new MercenaryContract
            {
                Id = "raid_villages",
                Name = "Raid 5 Villages",
                Description = "Raid 5 villages successfully.",
                Progress = 0,
                Goal = 1
            };
            var attackCaravanContract = new MercenaryContract
            {
                Id = "attack_caravan",
                Name = "Attack a Merchant Caravan",
                Description = "Attack and loot a merchant caravan.",
                Progress = 0,
                Goal = 1
            };

            MercenaryContractManager.AssignContract(Hero.MainHero, raidVillagesContract);
            MercenaryContractManager.AssignContract(Hero.MainHero, attackCaravanContract);
        }
    }
}

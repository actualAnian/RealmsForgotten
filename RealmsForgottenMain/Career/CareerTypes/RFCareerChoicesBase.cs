using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.Career.CareerTypes
{
    public abstract class RFCareerChoicesBase
    {
        protected CareerObject CareerID = null;
        public CareerObject GetID()
        {
            return CareerID;
        }
        public RFCareerChoicesBase(CareerObject careerId)
        {
            CareerID = careerId;
            RegisterAll();
            InitializePassives();
            InitializeKeyStones();
        }

        protected RFCareerChoicesBase()
        {
            RegisterAll();
            InitializePassives();
            InitializeKeyStones();
        }

        protected abstract void RegisterAll();


        protected abstract void InitializeKeyStones();

        protected abstract void InitializePassives();
        public void UnlockCareerBenefits(int tier)
        {
            var mainhero = Hero.MainHero;
            var tierText = "CareerTier";
            if (PlayerCareerExtension.HasUnlockedCareerChoiceTier(tier)) return;
            switch (tier)
            {
                case 1:
                    UnlockCareerBenefitsTier1();
                    break;
                case 2:
                    UnlockCareerBenefitsTier2();
                    break;
                case 3:
                    UnlockCareerBenefitsTier3();
                    break;
            }
            PlayerCareerExtension.AddAttribute(tierText + tier);
        }

        protected virtual void UnlockCareerBenefitsTier1()
        {

        }

        protected virtual void UnlockCareerBenefitsTier2()
        {

        }

        protected virtual void UnlockCareerBenefitsTier3()
        {

        }

        public virtual void InitialCareerSetup()
        {

        }
    }
}

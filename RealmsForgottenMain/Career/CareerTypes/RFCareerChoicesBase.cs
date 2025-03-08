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
        }

        protected RFCareerChoicesBase()
        {
            RegisterAll();
            InitializePassives();
        }

        protected abstract void RegisterAll();

        protected abstract void InitializePassives();

        public virtual void InitialCareerSetup()
        {

        }
    }
}

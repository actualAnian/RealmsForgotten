using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public abstract class AbstractPointsSystem
    {
        [SaveableField(0)]  CareerPointsSaveableData pointsData = new();
        protected AbstractPointsSystem()
        {
        }

        public int AvailablePoints { get { return pointsData.availablePoints; } }
        public int SpentPoints { get { return pointsData.spentPoints; } }
        public void AddPoint() { pointsData.availablePoints++; }
        public bool SpendPoint() 
        {
            if (pointsData.availablePoints == 0) return false;
            pointsData.spentPoints++; pointsData.availablePoints--;
            return true;
        }
        internal void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("pointsData", ref pointsData);
        }
        public virtual void OnLevelUp(Hero hero, bool arg2) { }

        public void ReturnPoints(int points)
        {
            pointsData.availablePoints += points;
            pointsData.spentPoints -= points;
        }
        public bool HasAvailablePoints()
        {
            return pointsData.availablePoints != 0;
        }
    }
    public class LevelUpPointsSystem : AbstractPointsSystem
    {
        public LevelUpPointsSystem() : base()
        {
        }
        public override void OnLevelUp(Hero hero, bool arg2) { if (hero == Hero.MainHero) AddPoint(); }
    }
    public class CareerPointsSaveableData
    {
        [SaveableField(0)] public int spentPoints = 0;
        [SaveableField(1)] public int availablePoints = 0;
    }
}
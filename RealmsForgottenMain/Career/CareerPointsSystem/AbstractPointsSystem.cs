using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career.CareerPointsSystem
{
    public abstract class AbstractPointsSystem
    {
        [SaveableField(0)] protected CareerPointsSaveableData pointsData = new();
        protected AbstractPointsSystem() {}
        public abstract string Description { get; }
        public int AvailablePoints { get { return pointsData.availablePoints; } }
        public int SpentPoints { get { return pointsData.spentPoints; } }
        public void AddPoints(int toAdd = 1) { pointsData.availablePoints += toAdd; }
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
        public virtual void OnClanInfluenceChanged(Clan clan, float arg2) { }
        public virtual void OnRenownGained(Hero hero, int arg2, bool arg3) { }
        public virtual void OnMapEventEnded(MapEvent mapEvent) { }
        public virtual void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details) { }
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
}
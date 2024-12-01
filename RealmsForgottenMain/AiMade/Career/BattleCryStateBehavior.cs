using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
{
    public class BattleCryStateBehavior : CampaignBehaviorBase
    {
        private bool canUseBattleCry = true;
        private float lastBattleCryTime = float.MinValue;

        public bool CanUseBattleCry
        {
            get => canUseBattleCry;
            set => canUseBattleCry = value;
        }

        public float LastBattleCryTime
        {
            get => lastBattleCryTime;
            set => lastBattleCryTime = value;
        }

        public override void RegisterEvents()
        {
            // Register events if needed
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("canUseBattleCry", ref canUseBattleCry);
            dataStore.SyncData("lastBattleCryTime", ref lastBattleCryTime);
        }
    }
}


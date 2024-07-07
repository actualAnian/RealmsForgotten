using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Career
{
    public class DivineShieldStateBehavior : CampaignBehaviorBase
    {
        private bool canUseDivineShield = true;
        private float lastDivineShieldTime = float.MinValue;
        private bool isShieldActive = false;
        private float shieldEndTime = float.MinValue;

        public bool CanUseDivineShield
        {
            get => canUseDivineShield;
            set => canUseDivineShield = value;
        }

        public float LastDivineShieldTime
        {
            get => lastDivineShieldTime;
            set => lastDivineShieldTime = value;
        }

        public bool IsShieldActive
        {
            get => isShieldActive;
            set => isShieldActive = value;
        }

        public float ShieldEndTime
        {
            get => shieldEndTime;
            set => shieldEndTime = value;
        }

        public override void RegisterEvents()
        {
            // Register events if needed
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("canUseDivineShield", ref canUseDivineShield);
            dataStore.SyncData("lastDivineShieldTime", ref lastDivineShieldTime);
            dataStore.SyncData("isShieldActive", ref isShieldActive);
            dataStore.SyncData("shieldEndTime", ref shieldEndTime);
        }
    }
}
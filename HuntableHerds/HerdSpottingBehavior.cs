using RealmsForgotten.HuntableHerds.Models;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.HuntableHerds
{
    public class HerdSpottingBehavior : CampaignBehaviorBase
    {
        /// <summary>Campaign day (fractional) before which no new herd notification may appear.</summary>
        private float _nextSpottingDay = -1f;

        private static HerdSpottingBehavior? _instance;

        public HerdSpottingBehavior()
        {
            _instance = this;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // New key: absent in old saves, which just means "no cooldown pending".
            dataStore.SyncData("_rfHuntNextSpottingDay", ref _nextSpottingDay);
        }

        /// <summary>Called when the player accepts a hunt, so the map goes quiet for a while afterwards.</summary>
        public static void NotifyHuntAccepted()
        {
            try
            {
                _instance?.ApplyPostHuntCooldown();
            }
            catch (Exception)
            {
                // never let cooldown bookkeeping break the hunt
            }
        }

        private void ApplyPostHuntCooldown()
        {
            float today = (float)CampaignTime.Now.ToDays;
            float candidate = today + Settings.Instance.MinDaysBetweenHerdSpottings + Settings.Instance.ExtraCooldownDaysAfterHunt;
            if (candidate > _nextSpottingDay)
                _nextSpottingDay = candidate;
        }

        private void OnDailyTickParty(MobileParty party)
        {
            try
            {
                if (party == null || !party.IsMainParty || party.CurrentSettlement != null)
                    return;

                if (Settings.Instance.DailyChanceOfSpottingHerd <= 0f)
                    return;

                float today = (float)CampaignTime.Now.ToDays;
                if (_nextSpottingDay > 0f && today < _nextSpottingDay)
                    return;

                if (MBRandom.RandomFloat > Settings.Instance.DailyChanceOfSpottingHerd)
                    return;

                ShowHuntingHerdNotification(today);
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: herd spotting tick failed ({e.Message})", 255, 0, 0);
            }
        }

        private void ShowHuntingHerdNotification(float today)
        {
            HerdBuildData? herd = HerdBuildData.PickRandom(TryGetMainPartyTerrain());
            if (herd == null)
                return;

            // Cooldown starts the moment a notice is posted, whether or not the player inspects it.
            _nextSpottingDay = today + Settings.Instance.MinDaysBetweenHerdSpottings;

            string title = herd.MessageTitle;
            string notice = herd.NotifMessage;
            Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(
                new HerdMapNotification(herd, title, new TextObject(notice)));
        }

        private static TerrainType? TryGetMainPartyTerrain()
        {
            if (!Settings.Instance.FilterHerdsByTerrain)
                return null;
            try
            {
                return Campaign.Current.MapSceneWrapper.GetTerrainTypeAtPosition(MobileParty.MainParty.Position);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        public CustomSaveDefiner() : base(877885323) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(HerdMapNotification), 1);
            AddClassDefinition(typeof(HerdMapNotificationItemVM), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            //
        }
    }
}

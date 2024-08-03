using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Bannerlord.Module1.Stories;
using System; // Ensure this is the correct namespace for story behaviors

namespace Bannerlord.Module1
{
    public class StoryManager : CampaignBehaviorBase
    {
        private List<CampaignBehaviorBase> _storyBehaviors;
        private Dictionary<CampaignBehaviorBase, float> _lastTriggerTime;
        private float _nextTriggerTime;
        private Random _random;

        public StoryManager()
        {
            _storyBehaviors = new List<CampaignBehaviorBase>
            {
                new Story1Behavior(),
                new Story2Behavior()
                // Add more stories as needed
            };
            _lastTriggerTime = new Dictionary<CampaignBehaviorBase, float>();
            _random = new Random();
            _nextTriggerTime = GetRandomFutureTime();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastTriggerTime", ref _lastTriggerTime);
            dataStore.SyncData("_nextTriggerTime", ref _nextTriggerTime);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void Initialize()
        {
            InformationManager.DisplayMessage(new InformationMessage("STORY MANAGER INITIALIZED SUCCESSFULLY.", Colors.Green));
        }

        private void OnHourlyTick()
        {
            if (CampaignTime.Now.ToDays < _nextTriggerTime)
                return;

            // Find the next story to trigger
            foreach (var storyBehavior in _storyBehaviors)
            {
                if (!_lastTriggerTime.TryGetValue(storyBehavior, out var lastTriggerTime) ||
                    CampaignTime.Now.ToDays - lastTriggerTime >= 20)
                {
                    // Trigger the story
                    storyBehavior.RegisterEvents();
                    _lastTriggerTime[storyBehavior] = (float)CampaignTime.Now.ToDays;
                    _nextTriggerTime = GetRandomFutureTime();
                    break;
                }
            }
        }

        private float GetRandomFutureTime()
        {
            // Schedule the next trigger time to be between 20 and 40 days from now
            return (float)(CampaignTime.Now.ToDays + 20 + (float)_random.NextDouble() * 20);
        }
    }
}


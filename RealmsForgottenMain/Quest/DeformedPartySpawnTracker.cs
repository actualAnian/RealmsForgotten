using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using RealmsForgotten.Quest.FourthUpdate;

namespace RealmsForgotten.Quest
{
    public class DeformedWaveSniffer : CampaignBehaviorBase
    {
        private int _waveCount = 0;
        private CampaignTime _lastWaveTime = CampaignTime.Never;
        private HashSet<string> _observedDeformedIds = new();
        private bool _popupTriggered = false;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_waveCount", ref _waveCount);
            dataStore.SyncData("_popupTriggered", ref _popupTriggered);
            dataStore.SyncData("_lastWaveTime", ref _lastWaveTime);
            dataStore.SyncData("_observedDeformedIds", ref _observedDeformedIds);
        }

        private void OnDailyTick()
        {
            if (_popupTriggered)
                return;

            var newSpawnedToday = false;

            foreach (var party in MobileParty.All.ToList())
            {
                try
                {
                    if (party == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("⛔ Party is null."));
                        continue;
                    }

                    if (!party.IsActive)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"⚠️ Party {party?.Name} is not active."));
                        continue;
                    }

                    if (party.Party == null)
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"⚠️ Party {party?.Name} has null .Party reference."));
                        continue;
                    }

                    if (string.IsNullOrEmpty(party.StringId))
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"⚠️ Party {party?.Name} has null or empty ID."));
                        continue;
                    }

                    string id = party.StringId;

                    if (id.StartsWith("deformed_party_") && !_observedDeformedIds.Contains(id))
                    {
                        _observedDeformedIds.Add(id);
                        newSpawnedToday = true;

                        InformationManager.DisplayMessage(new InformationMessage($"✅ New deformed party detected: {id}"));
                    }
                }
                catch (Exception ex)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"❌ Exception in sniffer: {ex.Message}", Colors.Red));
                }
            }


            if (newSpawnedToday && CampaignTime.Now.GetDayOfYear != _lastWaveTime.GetDayOfYear)
            {
                _lastWaveTime = CampaignTime.Now;
                _waveCount++;
                InformationManager.DisplayMessage(new InformationMessage($"[DEBUG] Deformed wave detected: {_waveCount}"));

                if (_waveCount >= 3)
                {
                    _popupTriggered = true;

                    if (SeventhQuest.ActiveSeventhQuestInstance != null)
                    {
                        SeventhQuest.ActiveSeventhQuestInstance.ShowEighthQuestPrompt();
                    }
                    else
                    {
                        InformationManager.DisplayMessage(
                            new InformationMessage("⚠️ Could not trigger Eighth Quest — SeventhQuest instance is missing!", Colors.Red));
                    }
                }
            }

            // Optional cleanup of stale IDs (defensive)
            _observedDeformedIds.RemoveWhere(id => !MobileParty.All.Any(p => p?.StringId == id));
        }
    }
}

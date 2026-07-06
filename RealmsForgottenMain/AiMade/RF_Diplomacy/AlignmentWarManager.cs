using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RF_warsystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.RF_Diplomacy
{
    public class AlignmentWarBehavior : CampaignBehaviorBase
    {
        [SaveableField(0)] private bool _warStarted = false;
        [SaveableField(1)] private List<string> _goodKingdomIds = new();
        [SaveableField(2)] private List<string> _evilKingdomIds = new();
        [SaveableField(3)] private bool _savedIsActive = false;

        public static bool ShouldStartAlignmentWar = false;

        public static bool IsActive { get; set; }

        private void SyncRuntimeState()
        {
            IsActive = _warStarted || _savedIsActive;
        }

        public void EndWar()
        {
            IsActive = false;
            _warStarted = false;
            _savedIsActive = false;
            _goodKingdomIds.Clear();
            _evilKingdomIds.Clear();
        }
        public override void RegisterEvents()
        {
            SyncRuntimeState();
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            if (dataStore.IsSaving)
                _savedIsActive = IsActive;

            dataStore.SyncData("_warStarted", ref _warStarted);
            dataStore.SyncData("_goodKingdomIds", ref _goodKingdomIds);
            dataStore.SyncData("_evilKingdomIds", ref _evilKingdomIds);
            dataStore.SyncData("_savedIsActive", ref _savedIsActive);

            if (dataStore.IsLoading)
                SyncRuntimeState();
        }

        private void OnDailyTick()
        {
            SyncRuntimeState();

            if (_warStarted)
            {
                RFWarExternalIntentApi.ReinforceAlignmentWar(GetGoodKingdoms(), GetEvilKingdoms());
                return;
            }

            if (!ShouldStartAlignmentWar)
                return;

            StartGlobalAlignmentWar();
        }

        public void StartGlobalAlignmentWar()
        {
            if (_warStarted) return;

            IsActive = true;
            _warStarted = true;
            _savedIsActive = true;

            _goodKingdomIds.Clear();
            _evilKingdomIds.Clear();

            foreach (var kingdom in Kingdom.All)
            {
                if (kingdom == null || kingdom.IsEliminated || kingdom.Culture == null)
                    continue;

                if (kingdom.Culture.IsGoodCulture())
                    AddKingdomToSide(kingdom, isGood: true);
                else if (kingdom.Culture.IsEvilCulture())
                    AddKingdomToSide(kingdom, isGood: false);
            }

            RFWarExternalIntentApi.ReinforceAlignmentWar(GetGoodKingdoms(), GetEvilKingdoms());
            ShouldStartAlignmentWar = false;
            InformationManager.DisplayMessage(new InformationMessage("✅ Global alignment war has started!", Colors.Green));
        }


        public void AddKingdomToSide(Kingdom kingdom, bool isGood)
        {
            if (isGood)
            {
                if (_goodKingdomIds.Contains(kingdom.StringId)) return;
                _goodKingdomIds.Add(kingdom.StringId);

                foreach (var evilId in _evilKingdomIds)
                {
                    Kingdom evil = Kingdom.All.FirstOrDefault(k => k.StringId == evilId);
                    if (evil != null && !FactionManager.IsAtWarAgainstFaction(kingdom, evil))
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"⚔️ WAR: {kingdom.Name} vs {evil.Name}", Colors.Red));
                    }
                }
            }
            else
            {
                if (_evilKingdomIds.Contains(kingdom.StringId)) return;
                _evilKingdomIds.Add(kingdom.StringId);

                foreach (var goodId in _goodKingdomIds)
                {
                    Kingdom good = Kingdom.All.FirstOrDefault(k => k.StringId == goodId);
                    if (good != null && !FactionManager.IsAtWarAgainstFaction(kingdom, good))
                    {
                        InformationManager.DisplayMessage(new InformationMessage($"⚔️ WAR: {kingdom.Name} vs {good.Name}", Colors.Red));
                    }
                }
            }
        }

        public List<Kingdom> GetGoodKingdoms() => Kingdom.All.Where(k => _goodKingdomIds.Contains(k.StringId)).ToList();
        public List<Kingdom> GetEvilKingdoms() => Kingdom.All.Where(k => _evilKingdomIds.Contains(k.StringId)).ToList();

        public bool IsAlignmentWarPair(IFaction faction1, IFaction faction2)
        {
            if (!_warStarted && !_savedIsActive)
            {
                return false;
            }

            if (faction1 is not Kingdom kingdom1 || faction2 is not Kingdom kingdom2)
            {
                return false;
            }

            bool kingdom1Good = _goodKingdomIds.Contains(kingdom1.StringId);
            bool kingdom1Evil = _evilKingdomIds.Contains(kingdom1.StringId);
            bool kingdom2Good = _goodKingdomIds.Contains(kingdom2.StringId);
            bool kingdom2Evil = _evilKingdomIds.Contains(kingdom2.StringId);

            return (kingdom1Good && kingdom2Evil) || (kingdom1Evil && kingdom2Good);
        }
    }
}

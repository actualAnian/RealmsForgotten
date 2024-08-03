using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace RealmsForgotten.Behaviors
{
    public class BanditPartyGrowthBehavior : CampaignBehaviorBase
    {
        private int _lastUpdateDay = -1;
        private const int UpdateIntervalDays = 35; // Adjusted for testing
        private const float GrowthRate = 0.10f; // 10% growth rate

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastUpdateDay", ref _lastUpdateDay);
        }

        private void OnDailyTick()
        {
            int currentDay = CampaignTime.Now.GetDayOfYear;
            if (_lastUpdateDay < 0)
            {
                _lastUpdateDay = currentDay;
                return;
            }

            if (currentDay - _lastUpdateDay >= UpdateIntervalDays)
            {
                EnlargeBanditParties();
                _lastUpdateDay = currentDay;
            }
        }

        private void EnlargeBanditParties()
        {
            foreach (MobileParty party in MobileParty.All)
            {
                if (party.PartyComponent is BanditPartyComponent)
                {
                    int newTroopCount = (int)(party.MemberRoster.TotalManCount * (1 + GrowthRate));
                    IncreasePartySize(party, newTroopCount);
                }
            }
            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=BanditGrowth}Bandit parties have grown in size.").ToString()));
        }

        private void IncreasePartySize(MobileParty party, int newTroopCount)
        {
            int additionalTroops = newTroopCount - party.MemberRoster.TotalManCount;
            if (additionalTroops <= 0) return;

            // Increase the party size
            List<TroopRosterElement> banditTroops = new List<TroopRosterElement>();
            foreach (var element in party.MemberRoster.GetTroopRoster())
            {
                if (element.Character.Occupation == Occupation.Bandit)
                {
                    banditTroops.Add(element);
                }
            }

            while (additionalTroops > 0 && banditTroops.Count > 0)
            {
                var troop = banditTroops[Math.Min(additionalTroops, banditTroops.Count - 1)];
                int troopsToAdd = Math.Min(additionalTroops, troop.Number); // Correctly calculate the number of troops to add
                party.MemberRoster.AddToCounts(troop.Character, troopsToAdd, false, 0, 0, true, -1);
                additionalTroops -= troopsToAdd; // Decrease the additionalTroops by the number of troops added
            }
        }
    }
}
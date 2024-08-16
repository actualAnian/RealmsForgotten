using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;


namespace RealmsForgotten.AiMade
{
    public class BanditIncrease : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.OnHourlyTickParty));
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            this._stayTimers.Remove(destroyedParty);
        }

        private void OnHourlyTickParty(MobileParty party)
        {
            bool flag = !party.IsBandit || party.CurrentSettlement == null || !party.CurrentSettlement.IsHideout || !this.IsOverInfested(party.CurrentSettlement);
            if (!flag)
            {
                bool flag2 = !this._stayTimers.ContainsKey(party);
                if (flag2)
                {
                    this._stayTimers.Add(party, this.GenerateStayTimer());
                }
                bool flag3 = CampaignTime.Now <= this._stayTimers[party];
                if (!flag3)
                {
                    this._stayTimers.Remove(party);
                    SetPartyAiAction.GetActionForPatrollingAroundSettlement(party, party.CurrentSettlement);
                }
            }
        }

        private CampaignTime GenerateStayTimer()
        {
            return CampaignTime.HoursFromNow((float)MBRandom.RandomInt(0, 72));
        }

        private bool IsOverInfested(Settlement hideout)
        {
            return hideout.Parties.CountQ((MobileParty x) => x.IsBandit) >= Campaign.Current.Models.BanditDensityModel.NumberOfMinimumBanditPartiesInAHideoutToInfestIt + 1;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<Dictionary<MobileParty, CampaignTime>>("StayTimers", ref this._stayTimers);
        }

        private Dictionary<MobileParty, CampaignTime> _stayTimers = new Dictionary<MobileParty, CampaignTime>();
    }
}
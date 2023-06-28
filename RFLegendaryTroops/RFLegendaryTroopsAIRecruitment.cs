using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RFLegendaryTroops
{
    public class RFLegendaryTroopsAIRecruitment : CampaignBehaviorBase
    {
        private readonly int KING_PARTY_LEGENDARY_TROOPS = 10;
        public override void RegisterEvents()
        {

            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnMobilePartyCreated));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
        }


        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if(settlement.IsCastle&& mobileParty.IsRulerParty())
            {
                if (mobileParty.Party.NumberOfAllMembers < mobileParty.LimitedPartySize && mobileParty.CanPayMoreWage())
                    this.RecruitVolunteersFromNotable(mobileParty, settlement);
            }
        }


        private void OnMobilePartyCreated(MobileParty party)
        {
            
            if (party.IsRulerParty())
            {
                AddTroopsToRulerParty(party);
            }
        }

        private void AddTroopsToRulerParty(MobileParty party)
        {
            CharacterObject legendaryTroop = Helper.ChooseLegendaryTroop(party.LeaderHero.Clan.Kingdom.Culture);
            party.MemberRoster.AddToCounts(legendaryTroop, KING_PARTY_LEGENDARY_TROOPS);
        }
        private void RecruitVolunteersFromNotable(MobileParty mobileParty, Settlement settlement)
        {
            if (((float)mobileParty.Party.NumberOfAllMembers + 0.5f) / (float)mobileParty.LimitedPartySize <= 1f)
            {
                foreach (Hero notable in settlement.Notables)
                {
                    for(int i = 0; i < notable.VolunteerTypes.Length; ++i) // party leader is able to recruit every troop from notable
                    {
                        CharacterObject recruit = notable.VolunteerTypes[i];
                        if (recruit != null && mobileParty.LeaderHero.Gold > Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(recruit, mobileParty.LeaderHero, false) && 
                            mobileParty.PaymentLimit >= mobileParty.TotalWage + Campaign.Current.Models.PartyWageModel.GetCharacterWage(recruit) &&
                            mobileParty.LimitedPartySize > mobileParty.MemberRoster.TotalManCount)
                        {
                            this.GetRecruitVolunteerFromIndividual(mobileParty, notable.CurrentSettlement, recruit, notable, 1, i);
                        }

                        if (mobileParty.IsWageLimitExceeded())
                        {
                            break;
                        }
                    }
                }
            }
        }
        private void GetRecruitVolunteerFromIndividual(MobileParty kingsParty, Settlement castle, CharacterObject recruit, Hero notable, int number, int bitCode)
        {
            int troopRecruitmentCost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(recruit, kingsParty.LeaderHero, false);
            
            GiveGoldAction.ApplyBetweenCharacters(kingsParty.LeaderHero, null, troopRecruitmentCost, true);
            notable.VolunteerTypes[bitCode] = null;
            kingsParty.AddElementToMemberRoster(recruit, 1, false);
            CampaignEventDispatcher.Instance.OnTroopRecruited(kingsParty.LeaderHero, castle, notable, recruit, number);
        }
        public override void SyncData(IDataStore dataStore) {}
    }

}
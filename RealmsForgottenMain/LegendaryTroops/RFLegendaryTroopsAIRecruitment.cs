using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.LegendaryTroops
{
    public class RFLegendaryTroopsAIRecruitment : CampaignBehaviorBase
    {
        private readonly int KING_PARTY_LEGENDARY_TROOPS = 10;
        public override void RegisterEvents()
        {

            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(OnMobilePartyCreated));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(OnSettlementEntered));
        }


        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if(settlement.IsCastle&& mobileParty.IsRulerParty())
            {
                if (mobileParty.Party.NumberOfAllMembers < mobileParty.Party.PartySizeLimit && !mobileParty.IsWageLimitExceeded())
                    RecruitVolunteersFromNotable(mobileParty, settlement);
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
            if ((mobileParty.Party.NumberOfAllMembers + 0.5f) / mobileParty.Party.PartySizeLimit <= 1f)
            {
                foreach (Hero notable in settlement.Notables)
                {
                    for(int i = 0; i < notable.VolunteerTypes.Length; ++i) // party leader is able to recruit every troop from notable
                    {
                        CharacterObject recruit = notable.VolunteerTypes[i];
                        if (recruit != null && mobileParty.LeaderHero.Gold > Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(recruit, mobileParty.LeaderHero, false).ResultNumber &&
                            mobileParty.GetAvailableWageBudget() >= Campaign.Current.Models.PartyWageModel.GetCharacterWage(recruit))
                        {
                            GetRecruitVolunteerFromIndividual(mobileParty, notable.CurrentSettlement, recruit, notable, 1, i);
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
            int troopRecruitmentCost = (int)Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(recruit, kingsParty.LeaderHero, false).ResultNumber;
            
            GiveGoldAction.ApplyBetweenCharacters(kingsParty.LeaderHero, null, troopRecruitmentCost, true);
            notable.VolunteerTypes[bitCode] = null;
            kingsParty.AddElementToMemberRoster(recruit, 1, false);
            CampaignEventDispatcher.Instance.OnTroopRecruited(kingsParty.LeaderHero, castle, notable, recruit, number);
        }
        public override void SyncData(IDataStore dataStore) {}
    }

}
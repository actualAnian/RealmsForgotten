using Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.RFCustomBandits
{
        class AiSlaversPatrollingBehavior : CampaignBehaviorBase
        {
            private int HoursBeforeChoosingNewPatrolVillage { get; } = 1;

            private Dictionary<string, int> hoursSincelastDecision = new();
            public override void RegisterEvents()
            {
                CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, new Action<MobileParty, PartyThinkParams>(AiHourlyTick));
                CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, new Action<PartyBase>(RemovePartyFromDict));
            }

        private void RemovePartyFromDict(PartyBase party)
        {
            if(party.IsSlaverParty() && hoursSincelastDecision.ContainsKey(party.Id))
                hoursSincelastDecision.Remove(party.Id);
        }

        private void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
        {
            if (mobileParty.Party == null || !mobileParty.IsSlaverParty()) return;
            if (mobileParty.MapFaction.Culture.CanHaveSettlement && mobileParty.HomeSettlement.IsHideout && !mobileParty.HomeSettlement.Hideout.IsInfested)
            {
                Hideout settlement = SettlementHelper.FindNearestHideoutToMobileParty(mobileParty, MobileParty.NavigationType.Default);
                if (settlement != null)
                {
                    mobileParty.BanditPartyComponent.SetHomeHideout(settlement);
                }
            }

            AIBehaviorData item;
            ValueTuple<AIBehaviorData, float> valueTuple;
            string SlaversID = mobileParty.Party.Id;
            if (!hoursSincelastDecision.ContainsKey(SlaversID))
                hoursSincelastDecision[SlaversID] = 0;

            if (hoursSincelastDecision[SlaversID] != 0 && hoursSincelastDecision[SlaversID] % HoursBeforeChoosingNewPatrolVillage == 0 && mobileParty.TargetSettlement != null)
            {
                hoursSincelastDecision[SlaversID] = 0;
                Village nextSettlementToPatrol = SettlementHelper.FindNearestVillageToMobileParty(mobileParty, MobileParty.NavigationType.Default, (Settlement s) =>
                {
                    if (s.Id == mobileParty.TargetSettlement.Id) return false;
                    else return true;
                });

                if(nextSettlementToPatrol != null)
                {
                    item = new AIBehaviorData(nextSettlementToPatrol.Settlement, AiBehavior.PatrolAroundPoint, MobileParty.NavigationType.Default, false, false, false);
                    valueTuple = new ValueTuple<AIBehaviorData, float>(item, 0.3f);
                    p.AddBehaviorScore(valueTuple);
                    ++hoursSincelastDecision[SlaversID];
                    return;
                }
            }
            Settlement patrolSettlement = mobileParty.TargetSettlement ?? mobileParty.HomeSettlement;
            item = new AIBehaviorData(patrolSettlement, AiBehavior.PatrolAroundPoint, MobileParty.NavigationType.Default, false, false, false);
            valueTuple = new ValueTuple<AIBehaviorData, float>(item, 0.3f);
            p.AddBehaviorScore(valueTuple);
            ++hoursSincelastDecision[SlaversID];
        }
        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("rf_hours_since_decision_dict", ref hoursSincelastDecision);
        }
    }
}
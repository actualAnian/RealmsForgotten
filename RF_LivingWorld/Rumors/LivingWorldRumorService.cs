using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.WorldState.Refugees;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace RF_LivingWorld
{
    public sealed class LivingWorldRumorService
    {
        private const float SourceCooldownDays = 3f;
        private const float RumorLifetimeDays = 4f;

        private readonly List<LivingWorldRumorReport> _reports;
        private readonly List<LivingWorldRumorSourceState> _sources;

        public LivingWorldRumorService(
            List<LivingWorldRumorReport> reports,
            List<LivingWorldRumorSourceState> sources)
        {
            _reports = reports;
            _sources = sources;
        }

        public int ActiveCount => _reports.Count(report => !report.IsExpired);

        public void RemoveExpired()
        {
            _reports.RemoveAll(report => report == null || report.IsExpired);
            _sources.RemoveAll(source => source == null || string.IsNullOrEmpty(source.PartyId));
        }

        public bool TryGetOrCreate(MobileParty sourceParty, out LivingWorldRumorReport? report)
        {
            report = null;
            if (sourceParty == null || string.IsNullOrEmpty(sourceParty.StringId))
            {
                return false;
            }

            RemoveExpired();
            LivingWorldRumorSourceState? source = _sources.FirstOrDefault(state => state.PartyId == sourceParty.StringId);
            if (source != null && (CampaignTime.Now - source.LastSharedAt).ToDays < SourceCooldownDays)
            {
                report = _reports.LastOrDefault(item => item.SourcePartyId == sourceParty.StringId && !item.IsExpired);
                return report != null;
            }

            report = BuildFromRealState(sourceParty);
            if (report == null)
            {
                return false;
            }

            _reports.Add(report);
            if (source == null)
            {
                _sources.Add(new LivingWorldRumorSourceState(sourceParty.StringId, CampaignTime.Now));
            }
            else
            {
                source.MarkShared(CampaignTime.Now);
            }
            return true;
        }

        private static LivingWorldRumorReport? BuildFromRealState(MobileParty sourceParty)
        {
            float reliability = sourceParty.PartyComponent is LivingWorldPartyComponent component
                ? component.RumorReliability
                : 0.7f;

            if (sourceParty.PartyComponent is RefugeePartyComponent refugees && refugees.HomeVillage != null)
            {
                TextObject text = new("{=rf_lw_rumor_refugees}We fled {VILLAGE}. The roads near our old home are still unsafe.");
                text.SetTextVariable("VILLAGE", refugees.HomeVillage.Name);
                return Create(sourceParty, refugees.HomeVillage.StringId, LivingWorldRumorKind.RefugeesOnRoad,
                    reliability, text.ToString(), refugees.HomeVillage.GatePosition.X, refugees.HomeVillage.GatePosition.Y);
            }

            Settlement? siege = Settlement.All
                .Where(settlement => settlement != null && settlement.IsUnderSiege)
                .OrderBy(settlement => settlement.GatePosition.Distance(sourceParty.Position))
                .FirstOrDefault();
            if (siege != null)
            {
                TextObject text = new("{=rf_lw_rumor_siege}Travellers say {SETTLEMENT} is under siege. The roads around it will be dangerous.");
                text.SetTextVariable("SETTLEMENT", siege.Name);
                return Create(sourceParty, siege.StringId, LivingWorldRumorKind.SettlementUnderSiege,
                    reliability, text.ToString(), siege.GatePosition.X, siege.GatePosition.Y);
            }

            MobileParty? army = MobileParty.All
                .Where(party => party != null && party.IsActive && party != sourceParty
                    && party.Army != null && party.Army.LeaderParty == party)
                .OrderBy(party => party.Position.Distance(sourceParty.Position))
                .FirstOrDefault();
            if (army != null)
            {
                TextObject text = new("{=rf_lw_rumor_army}A large host led by {PARTY} was seen on the road.");
                text.SetTextVariable("PARTY", army.Name);
                return Create(sourceParty, army.StringId, LivingWorldRumorKind.ArmySighted,
                    reliability, text.ToString(), army.Position.X, army.Position.Y);
            }

            MobileParty? otherRefugees = MobileParty.All
                .FirstOrDefault(party => party != null && party.IsActive
                    && party != sourceParty && party.PartyComponent is RefugeePartyComponent);
            if (otherRefugees?.PartyComponent is RefugeePartyComponent other && other.HomeVillage != null)
            {
                TextObject text = new("{=rf_lw_rumor_refugee_road}Displaced families from {VILLAGE} have been seen on the roads.");
                text.SetTextVariable("VILLAGE", other.HomeVillage.Name);
                return Create(sourceParty, otherRefugees.StringId, LivingWorldRumorKind.RefugeesOnRoad,
                    reliability, text.ToString(), otherRefugees.Position.X, otherRefugees.Position.Y);
            }

            return null;
        }

        private static LivingWorldRumorReport Create(
            MobileParty source,
            string targetId,
            LivingWorldRumorKind kind,
            float reliability,
            string text,
            float x,
            float y)
        {
            return new LivingWorldRumorReport(
                source.StringId,
                targetId,
                kind,
                CampaignTime.Now,
                CampaignTime.Now + CampaignTime.Days(RumorLifetimeDays),
                reliability,
                text,
                x,
                y);
        }
    }
}

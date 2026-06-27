using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.AiMade
{
    public sealed class RFCampaignAITraceBehavior : CampaignBehaviorBase
    {
        private sealed class PartyAiState
        {
            public string PartyId = "-";
            public string LeaderId = "-";
            public string KingdomId = "-";
            public string CultureId = "-";
            public string Behavior = "-";
            public string TargetSettlementId = "-";
            public string TargetPartyId = "-";
            public string CurrentSettlementId = "-";
            public string BesiegedSettlementId = "-";
            public string ArmyLeaderId = "-";
            public string AttachedToId = "-";
            public string MapEventType = "-";
            public int ArmyPartyCount;
            public int WarCount;
            public int NearbySieges;
            public float NearestEnemyDistance = -1f;
            public int StrengthBucket;

            public string BuildSummary()
            {
                return $"party={PartyId} leader={LeaderId} kingdom={KingdomId} culture={CultureId} behavior={Behavior} targetSettlement={TargetSettlementId} targetParty={TargetPartyId} currentSettlement={CurrentSettlementId} besieged={BesiegedSettlementId} armyLeader={ArmyLeaderId} armyCount={ArmyPartyCount} attachedTo={AttachedToId} mapEvent={MapEventType} wars={WarCount} nearbySieges={NearbySieges} nearestEnemy={NearestEnemyDistance:0.0} strength={StrengthBucket}";
            }
        }

        private readonly Dictionary<string, PartyAiState> _lastStates = new Dictionary<string, PartyAiState>();

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnHourlyTickParty(MobileParty party)
        {
            if (!ShouldTrace(party))
            {
                return;
            }

            PartyAiState current = CaptureState(party);
            if (!_lastStates.TryGetValue(current.PartyId, out PartyAiState previous))
            {
                _lastStates[current.PartyId] = current;
                RFCampaignAITraceLog.Write("[CampaignAI] init | " + current.BuildSummary());
                return;
            }

            string delta = DescribeDelta(previous, current);
            if (string.IsNullOrEmpty(delta))
            {
                return;
            }

            _lastStates[current.PartyId] = current;
            RFCampaignAITraceLog.Write("[CampaignAI] change | " + delta + " | " + current.BuildSummary());
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (destroyedParty == null)
            {
                return;
            }

            string partyId = destroyedParty.StringId ?? "-";
            _lastStates.Remove(partyId);
            RFCampaignAITraceLog.Write($"[CampaignAI] destroyed | party={partyId} destroyer={destroyerParty?.MobileParty?.StringId ?? "-"}");
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            if (mapEvent == null)
            {
                return;
            }

            foreach (PartyBase involved in mapEvent.InvolvedParties.Where(x => x?.MobileParty != null))
            {
                MobileParty party = involved.MobileParty;
                if (!ShouldTrace(party))
                {
                    continue;
                }

                RFCampaignAITraceLog.Write($"[CampaignAI] mapEventStarted | party={party.StringId ?? "-"} leader={party.LeaderHero?.StringId ?? "-"} event={mapEvent.EventType} attackerLeader={mapEvent.AttackerSide?.LeaderParty?.MobileParty?.StringId ?? "-"} defenderLeader={mapEvent.DefenderSide?.LeaderParty?.MobileParty?.StringId ?? "-"}");
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent == null)
            {
                return;
            }

            foreach (PartyBase involved in mapEvent.InvolvedParties.Where(x => x?.MobileParty != null))
            {
                MobileParty party = involved.MobileParty;
                if (!ShouldTrace(party))
                {
                    continue;
                }

                RFCampaignAITraceLog.Write($"[CampaignAI] mapEventEnded | party={party.StringId ?? "-"} leader={party.LeaderHero?.StringId ?? "-"} event={mapEvent.EventType} attackerLeader={mapEvent.AttackerSide?.LeaderParty?.MobileParty?.StringId ?? "-"} defenderLeader={mapEvent.DefenderSide?.LeaderParty?.MobileParty?.StringId ?? "-"}");
            }
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            RFCampaignAITraceLog.Write($"[CampaignAI] warDeclared | faction1={faction1?.StringId ?? "-"} faction2={faction2?.StringId ?? "-"} detail={detail}");
        }

        private static bool ShouldTrace(MobileParty party)
        {
            return party != null
                && party.IsActive
                && party.IsLordParty
                && !party.IsMainParty
                && party.LeaderHero != null
                && !party.IsBandit;
        }

        private static PartyAiState CaptureState(MobileParty party)
        {
            return new PartyAiState
            {
                PartyId = party.StringId ?? "-",
                LeaderId = party.LeaderHero?.StringId ?? "-",
                KingdomId = (party.MapFaction as Kingdom)?.StringId ?? party.MapFaction?.StringId ?? "-",
                CultureId = party.LeaderHero?.Culture?.StringId ?? "-",
                Behavior = party.DefaultBehavior.ToString(),
                TargetSettlementId = party.TargetSettlement?.StringId ?? "-",
                TargetPartyId = party.TargetParty?.StringId ?? "-",
                CurrentSettlementId = party.CurrentSettlement?.StringId ?? "-",
                BesiegedSettlementId = party.BesiegedSettlement?.StringId ?? "-",
                ArmyLeaderId = party.Army?.LeaderParty?.StringId ?? "-",
                AttachedToId = party.AttachedTo?.StringId ?? "-",
                MapEventType = party.MapEvent?.EventType.ToString() ?? "-",
                ArmyPartyCount = party.Army?.Parties?.Count ?? 0,
                WarCount = party.MapFaction?.FactionsAtWarWith?.Count ?? 0,
                NearbySieges = CountNearbySieges(party),
                NearestEnemyDistance = GetNearestEnemyDistance(party),
                StrengthBucket = (int)Math.Round(party.Party?.EstimatedStrength ?? 0f)
            };
        }

        private static string DescribeDelta(PartyAiState previous, PartyAiState current)
        {
            List<string> parts = new List<string>();
            AppendChange(parts, "behavior", previous.Behavior, current.Behavior);
            AppendChange(parts, "targetSettlement", previous.TargetSettlementId, current.TargetSettlementId);
            AppendChange(parts, "targetParty", previous.TargetPartyId, current.TargetPartyId);
            AppendChange(parts, "currentSettlement", previous.CurrentSettlementId, current.CurrentSettlementId);
            AppendChange(parts, "besieged", previous.BesiegedSettlementId, current.BesiegedSettlementId);
            AppendChange(parts, "armyLeader", previous.ArmyLeaderId, current.ArmyLeaderId);
            AppendChange(parts, "attachedTo", previous.AttachedToId, current.AttachedToId);
            AppendChange(parts, "mapEvent", previous.MapEventType, current.MapEventType);

            if (previous.ArmyPartyCount != current.ArmyPartyCount)
            {
                parts.Add($"armyCount:{previous.ArmyPartyCount}->{current.ArmyPartyCount}");
            }

            if (previous.WarCount != current.WarCount)
            {
                parts.Add($"wars:{previous.WarCount}->{current.WarCount}");
            }

            if (previous.NearbySieges != current.NearbySieges)
            {
                parts.Add($"nearbySieges:{previous.NearbySieges}->{current.NearbySieges}");
            }

            if (Math.Abs(previous.NearestEnemyDistance - current.NearestEnemyDistance) >= 12f)
            {
                parts.Add($"nearestEnemy:{previous.NearestEnemyDistance:0.0}->{current.NearestEnemyDistance:0.0}");
            }

            if (Math.Abs(previous.StrengthBucket - current.StrengthBucket) >= 15)
            {
                parts.Add($"strength:{previous.StrengthBucket}->{current.StrengthBucket}");
            }

            return parts.Count == 0 ? string.Empty : string.Join(" | ", parts);
        }

        private static void AppendChange(List<string> parts, string name, string previous, string current)
        {
            if (!string.Equals(previous, current, StringComparison.Ordinal))
            {
                parts.Add($"{name}:{previous}->{current}");
            }
        }

        private static int CountNearbySieges(MobileParty party)
        {
            if (party == null)
            {
                return 0;
            }

            return Settlement.All.Count(settlement =>
                settlement != null
                && settlement.IsUnderSiege
                && party.Position.DistanceSquared(settlement.GatePosition) <= 1600f);
        }

        private static float GetNearestEnemyDistance(MobileParty party)
        {
            if (party?.MapFaction == null || party.MapEvent != null)
            {
                return -1f;
            }

            float nearestDistanceSquared = float.MaxValue;
            foreach (MobileParty otherParty in MobileParty.All)
            {
                if (otherParty == null
                    || !otherParty.IsActive
                    || otherParty == party
                    || otherParty.MapFaction == null
                    || !party.MapFaction.IsAtWarWith(otherParty.MapFaction))
                {
                    continue;
                }

                float distanceSquared = party.GetPosition2D.DistanceSquared(otherParty.GetPosition2D);
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearestDistanceSquared == float.MaxValue ? -1f : (float)Math.Sqrt(nearestDistanceSquared);
        }
    }
}

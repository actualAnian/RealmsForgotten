using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RF_ResourceZones
{
    /// <summary>
    /// Phase 3 of PLANO_RESOURCE_ZONES.md — factions treat the zones as
    /// AMBITIONS instead of roadside accidents:
    /// - AI lord parties deliberately hunt zones they can legitimately take
    ///   (bandit-held cleanup nearby, enemy-owned zones in wartime), scored by
    ///   value and distance. One assignment per lord, cooldown per zone.
    /// - AI-owned zones grow: after holding one long enough the owner upgrades
    ///   its tier, raising production, garrison cap — and how much everyone
    ///   else wants it.
    /// Deliberately nudge-based: SetMoveEngageParty once per day and the
    /// vanilla decision loop keeps running (the bandit-rally lesson — never
    /// fight the engine's initiative system on lord parties).
    /// </summary>
    public class ResourceZoneAmbitionBehavior : CampaignBehaviorBase
    {
        // Hunt radii in map units.
        private const float WarHuntRadius = 45f;
        private const float BanditCleanupRadius = 18f;
        private const float HunterStrengthAdvantage = 1.4f;
        private const int MaxSimultaneousHunts = 6;
        private const float AssignmentCooldownDays = 3f;

        // AI tier growth: days of uninterrupted ownership per upgrade.
        private const int DaysPerAiUpgrade = 18;

        // zoneId -> campaign day the last hunt order was issued.
        private readonly Dictionary<string, float> _lastHuntOrderDay = new();

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Hunt orders are daily nudges — nothing worth persisting.
        }

        private void OnDailyTick()
        {
            if (!ResourceZonesCampaignBehavior.RuntimeEnabled)
            {
                return;
            }

            ResourceZonesCampaignBehavior? zones = ResourceZonesCampaignBehavior.Instance;
            if (zones == null)
            {
                return;
            }

            List<(ResourceZoneRecord Record, MobileParty Party)> liveZones = zones.GetLiveZones();
            if (liveZones.Count == 0)
            {
                return;
            }

            GrowAiZones(liveZones);
            AssignHunters(liveZones);
        }

        // ── AI tier growth ───────────────────────────────────────────────────

        private static void GrowAiZones(List<(ResourceZoneRecord Record, MobileParty Party)> liveZones)
        {
            foreach ((ResourceZoneRecord record, MobileParty zoneParty) in liveZones)
            {
                Clan? owner = record.OwnerClan;
                if (owner == null || owner.IsEliminated || owner.IsBanditFaction
                    || owner == Clan.PlayerClan || record.Tier >= ResourceZoneRules.MaxTier)
                {
                    record.UpgradeProgressDays = 0;
                    continue;
                }

                record.UpgradeProgressDays++;
                if (record.UpgradeProgressDays < DaysPerAiUpgrade)
                {
                    continue;
                }

                record.UpgradeProgressDays = 0;
                record.Tier++;

                TextObject message = new("{=rf_zone_ai_upgraded}{CLAN} has expanded its operation at {ZONE} (tier {TIER}).");
                message.SetTextVariable("CLAN", owner.Name);
                message.SetTextVariable("ZONE", zoneParty.Name);
                message.SetTextVariable("TIER", record.Tier);
                InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Color.FromUint(0xFFB8A46Bu)));
            }
        }

        // ── Deliberate hunts ─────────────────────────────────────────────────

        private void AssignHunters(List<(ResourceZoneRecord Record, MobileParty Party)> liveZones)
        {
            float today = (float)CampaignTime.Now.ToDays;
            int assigned = 0;

            // Most valuable zones first: contested wealth drives the wars.
            foreach ((ResourceZoneRecord record, MobileParty zoneParty) in liveZones
                         .OrderByDescending(zone => zone.Record.Tier))
            {
                if (assigned >= MaxSimultaneousHunts)
                {
                    return;
                }

                if (_lastHuntOrderDay.TryGetValue(record.ZoneId, out float lastOrder)
                    && today - lastOrder < AssignmentCooldownDays)
                {
                    continue;
                }

                if (zoneParty.MapEvent != null)
                {
                    continue; // already being fought over
                }

                MobileParty? hunter = FindHunterFor(record, zoneParty);
                if (hunter == null)
                {
                    continue;
                }

                hunter.SetMoveEngageParty(zoneParty, MobileParty.NavigationType.All);
                _lastHuntOrderDay[record.ZoneId] = today;
                assigned++;
            }
        }

        private static MobileParty? FindHunterFor(ResourceZoneRecord record, MobileParty zoneParty)
        {
            bool banditHeld = record.OwnerClan == null || record.OwnerClan.IsBanditFaction;
            IFaction? ownerFaction = record.OwnerClan?.MapFaction;
            float zoneStrength = Math.Max(1, zoneParty.MemberRoster.TotalHealthyCount);

            MobileParty? best = null;
            float bestDistance = float.MaxValue;

            foreach (MobileParty candidate in MobileParty.All)
            {
                if (!candidate.IsLordParty || candidate.IsMainParty || !candidate.IsActive
                    || candidate.MapEvent != null || candidate.Army != null
                    || candidate.BesiegedSettlement != null
                    || candidate.CurrentSettlement != null
                    || candidate.LeaderHero == null)
                {
                    continue;
                }

                Clan? clan = candidate.ActualClan;
                if (clan == null || clan == Clan.PlayerClan || clan == record.OwnerClan)
                {
                    continue;
                }

                // Legitimacy: bandit squatters are fair game for everyone close
                // by; owned zones only in open war.
                float radius;
                if (banditHeld)
                {
                    radius = BanditCleanupRadius;
                }
                else if (ownerFaction != null && candidate.MapFaction?.IsAtWarWith(ownerFaction) == true)
                {
                    radius = WarHuntRadius;
                }
                else
                {
                    continue;
                }

                if (candidate.MemberRoster.TotalHealthyCount < zoneStrength * HunterStrengthAdvantage)
                {
                    continue;
                }

                float distance = (candidate.Position - zoneParty.Position).Length;
                if (distance < radius && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}

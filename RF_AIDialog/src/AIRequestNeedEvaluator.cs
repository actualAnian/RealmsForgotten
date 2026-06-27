using System;
using System.Collections.Generic;
using System.Linq;
using RealmsForgotten.AiMade.StrategicIntrigue.Campaign;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    public sealed class AIRequestNeedProfile
    {
        public string NeedLevel { get; set; } = "none";
        public bool ShouldOfferRequest { get; set; }
        public string LocalityScope { get; set; } = "local";
        public Settlement? AnchorSettlement { get; set; }
        public List<string> NeedDomains { get; } = new List<string>();
        public List<string> NeedSignals { get; } = new List<string>();
        public List<string> AllowedQuestKinds { get; } = new List<string>();
        public List<string> AllowedDeliveryItemIds { get; } = new List<string>();
        public List<string> Notes { get; } = new List<string>();
    }

    public static class AIRequestNeedEvaluator
    {
        public static AIRequestNeedProfile Evaluate(Hero npc)
        {
            var profile = new AIRequestNeedProfile();

            try
            {
                if (npc == null || !npc.IsAlive || npc.IsChild || npc == Hero.MainHero)
                    return profile;

                profile.AnchorSettlement = GetAnchorSettlement(npc);

                if (npc.IsPrisoner)
                {
                    profile.NeedSignals.Add("npc_is_prisoner");
                    profile.Notes.Add("The NPC is imprisoned and should not hand out ordinary work.");
                    return profile;
                }

                switch (npc.Occupation)
                {
                    case Occupation.Lord:
                        EvaluateLord(npc, profile);
                        break;
                    case Occupation.Merchant:
                    case Occupation.Artisan:
                    case Occupation.GangLeader:
                    case Occupation.RuralNotable:
                    case Occupation.Headman:
                        EvaluateNotable(npc, profile);
                        break;
                }

                FinalizeProfile(profile);
            }
            catch
            {
                return profile;
            }

            return profile;
        }

        public static List<Settlement> GetNearbySettlements(Hero npc, int maxCount = 5)
        {
            var result = new List<Settlement>();

            try
            {
                Settlement? anchorSettlement = GetAnchorSettlement(npc);
                CampaignVec2 anchorPosition = GetAnchorPosition(npc, anchorSettlement);

                result = Settlement.All
                    .Where(s => s != null
                             && !s.IsHideout)
                    .OrderBy(s => s.Position.DistanceSquared(anchorPosition))
                    .Take(maxCount)
                    .ToList();
            }
            catch { }

            return result;
        }

        private static void EvaluateLord(Hero npc, AIRequestNeedProfile profile)
        {
            int urgency = 0;
            var kingdom = npc.MapFaction as Kingdom;
            var clan = npc.Clan;

            try
            {
                var home = profile.AnchorSettlement;
                if (home != null && home.IsUnderSiege && home.OwnerClan == clan)
                {
                    AddNeed(profile, "war", "home_settlement_under_siege",
                        "scouting", "retaliation", "recruitment", "delivery_under_pressure");
                    profile.LocalityScope = "local";
                    urgency += 4;
                }
            }
            catch { }

            try
            {
                if (kingdom != null && HasImprisonedAlliedNoble(npc, kingdom))
                {
                    AddNeed(profile, "war", "captured_ally_noble", "rescue_prisoner_noble");
                    profile.Notes.Add("rescue_prisoner_noble is provisional: prefer a prison settlement or known holding place, not a made-up rescue mechanic.");
                    profile.LocalityScope = "regional";
                    urgency += 2;
                }
            }
            catch { }

            try
            {
                if (kingdom != null && Campaign.Current.Kingdoms.Any(k => !k.IsEliminated && k != kingdom && FactionManager.IsAtWarAgainstFaction(kingdom, k)))
                {
                    AddNeed(profile, "war", "active_war_pressure",
                        "scouting", "retaliation", "capture_prisoner", "recruitment");
                    profile.Notes.Add("For scouting requests, ask about enemy mobile parties or armies near local holdings/frontier; do not claim enemy patrols circle another faction's towns.");
                    if (profile.LocalityScope != "local")
                        profile.LocalityScope = "regional";
                    urgency += 2;
                }
            }
            catch { }

            try
            {
                bool insecureFief = false;
                bool poorFief = false;
                if (clan?.Fiefs != null)
                {
                    foreach (var town in clan.Fiefs)
                    {
                        if (town == null || town.Settlement == null) continue;
                        if (town.Security < 35f || town.Settlement.IsUnderSiege)
                            insecureFief = true;
                        if (town.Prosperity < 2500f || town.Loyalty < 40f)
                            poorFief = true;
                    }
                }

                if (insecureFief)
                {
                    AddNeed(profile, "security", "threatened_holdings",
                        "retaliation", "scouting", "recruitment");
                    urgency += 2;
                }

                if (poorFief)
                {
                    AddNeed(profile, "supply", "strained_holdings",
                        "delivery", "travel_report");
                    urgency += 1;
                }
            }
            catch { }

            try
            {
                var intrigue = Campaign.Current.GetCampaignBehavior<StrategicIntrigueCampaignBehavior>();
                var state = clan != null ? intrigue?.GetState(clan) : null;
                if (state != null && (state.Dissidence >= 55f || state.ClaimantAmbition >= 55f || state.VoteResentment >= 45f))
                {
                    AddSignalOnly(profile, "politics", "court_tension");
                    profile.Notes.Add("Political tension is real, but politics-only requests should stay as conversation until dedicated intrigue quest kinds exist.");
                    urgency += 1;
                }
            }
            catch { }

            ApplyUrgency(profile, urgency);
        }

        private static void EvaluateNotable(Hero npc, AIRequestNeedProfile profile)
        {
            int urgency = 0;
            var settlement = profile.AnchorSettlement;

            if (settlement == null)
                return;

            try
            {
                if (settlement.IsTown && settlement.Town != null)
                {
                    if (settlement.Town.Security < 35f)
                    {
                        AddNeed(profile, "security", "unsafe_town",
                            "retaliation", "delivery_under_pressure");
                        urgency += 2;
                    }

                    if (settlement.Town.Prosperity < 3000f)
                    {
                        AddNeed(profile, "trade", "weak_local_trade",
                            "delivery", "travel_report");
                        urgency += 1;
                    }
                }
            }
            catch { }

            try
            {
                if (settlement.MapFaction != null && Hero.MainHero?.MapFaction != null
                    && FactionManager.IsAtWarAgainstFaction(settlement.MapFaction, Hero.MainHero.MapFaction))
                {
                    AddNeed(profile, "war", "frontier_war_pressure",
                        "delivery_under_pressure", "travel_report");
                    profile.LocalityScope = "regional";
                    urgency += 1;
                }
            }
            catch { }

            try
            {
                int nearbyHideouts = CountNearbyHideouts(settlement.Position, 90f);
                if (nearbyHideouts > 0)
                {
                    AddNeed(profile, "local_crime", "nearby_hideout_threat",
                        "retaliation", "scouting");
                    urgency += Math.Min(2, nearbyHideouts);
                }
            }
            catch { }

            try
            {
                foreach (string itemId in QuestDeliveryRules.GetAllowedDeliveryItemIds(npc))
                    profile.AllowedDeliveryItemIds.Add(itemId);

                switch (npc.Occupation)
                {
                    case Occupation.Merchant:
                        AddNeed(profile, "trade", "merchant_route_concerns",
                            "delivery", "delivery_under_pressure", "travel_report");
                        urgency += 1;
                        break;
                    case Occupation.Artisan:
                        AddNeed(profile, "supply", "artisan_supply_strain",
                            "delivery");
                        urgency += 1;
                        break;
                    case Occupation.GangLeader:
                        AddNeed(profile, "local_crime", "urban_power_struggle",
                            "retaliation");
                        urgency += 1;
                        break;
                    case Occupation.RuralNotable:
                    case Occupation.Headman:
                        AddNeed(profile, "village_welfare", "commonfolk_pressure",
                            "delivery", "retaliation");
                        urgency += 1;
                        break;
                }
            }
            catch { }

            ApplyUrgency(profile, urgency);
        }

        private static void FinalizeProfile(AIRequestNeedProfile profile)
        {
            Dedupe(profile.NeedDomains);
            Dedupe(profile.NeedSignals);
            Dedupe(profile.AllowedQuestKinds);
            Dedupe(profile.AllowedDeliveryItemIds);
            Dedupe(profile.Notes);

            if (profile.AllowedDeliveryItemIds.Count == 0)
            {
                profile.AllowedQuestKinds.RemoveAll(k =>
                    k.Equals("delivery", StringComparison.OrdinalIgnoreCase) ||
                    k.Equals("delivery_under_pressure", StringComparison.OrdinalIgnoreCase));
            }

            if (!profile.AllowedQuestKinds.Any())
                profile.ShouldOfferRequest = false;
        }

        private static void ApplyUrgency(AIRequestNeedProfile profile, int urgency)
        {
            if (urgency >= 5)
            {
                profile.NeedLevel = "high";
                profile.ShouldOfferRequest = profile.AllowedQuestKinds.Any();
            }
            else if (urgency >= 3)
            {
                profile.NeedLevel = "medium";
                profile.ShouldOfferRequest = profile.AllowedQuestKinds.Any();
            }
            else if (urgency >= 1)
            {
                profile.NeedLevel = "low";
                profile.ShouldOfferRequest = false;
            }
        }

        private static void AddNeed(AIRequestNeedProfile profile, string domain, string signal, params string[] questKinds)
        {
            profile.NeedDomains.Add(domain);
            profile.NeedSignals.Add(signal);
            if (questKinds != null && questKinds.Length > 0)
                profile.AllowedQuestKinds.AddRange(questKinds);
        }

        private static void AddSignalOnly(AIRequestNeedProfile profile, string domain, string signal)
        {
            profile.NeedDomains.Add(domain);
            profile.NeedSignals.Add(signal);
        }

        private static Settlement? GetAnchorSettlement(Hero npc)
        {
            try
            {
                return npc.CurrentSettlement
                    ?? npc.HomeSettlement
                    ?? npc.BornSettlement
                    ?? npc.PartyBelongedTo?.CurrentSettlement
                    ?? Settlement.All
                        .Where(s => s != null && !s.IsHideout)
                        .OrderBy(s => s.Position.DistanceSquared(GetAnchorPosition(npc, null)))
                        .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static CampaignVec2 GetAnchorPosition(Hero npc, Settlement? anchorSettlement)
        {
            try
            {
                if (npc.PartyBelongedTo != null)
                    return npc.PartyBelongedTo.Position;
            }
            catch { }

            try
            {
                if (anchorSettlement != null)
                    return anchorSettlement.Position;
            }
            catch { }

            try
            {
                if (MobileParty.MainParty != null)
                    return MobileParty.MainParty.Position;
            }
            catch { }

            return CampaignVec2.Zero;
        }

        private static int CountNearbyHideouts(CampaignVec2 position, float radius)
        {
            try
            {
                float radiusSquared = radius * radius;
                return Settlement.All.Count(s => s.IsHideout && s.Position.DistanceSquared(position) <= radiusSquared);
            }
            catch
            {
                return 0;
            }
        }

        private static bool HasImprisonedAlliedNoble(Hero npc, Kingdom kingdom)
        {
            try
            {
                return Hero.AllAliveHeroes.Any(hero =>
                    hero != null
                    && hero != Hero.MainHero
                    && hero.IsLord
                    && hero.IsPrisoner
                    && hero.MapFaction == kingdom
                    && hero.Clan != null
                    && npc.Clan != null
                    && (hero.Clan == npc.Clan || hero.Clan.Kingdom == npc.Clan.Kingdom)
                    && hero.PartyBelongedToAsPrisoner != null
                    && hero.PartyBelongedToAsPrisoner.MapFaction != null
                    && FactionManager.IsAtWarAgainstFaction(kingdom, hero.PartyBelongedToAsPrisoner.MapFaction));
            }
            catch
            {
                return false;
            }
        }

        private static void Dedupe(List<string> list)
        {
            if (list.Count <= 1)
                return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!seen.Add(list[i]))
                    list.RemoveAt(i);
            }
        }
    }
}

//@ TODO might not be needed anymore
//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using TaleWorlds.CampaignSystem.CampaignBehaviors;
//using TaleWorlds.CampaignSystem;
//using TaleWorlds.Library;
//using TaleWorlds.CampaignSystem.Party;
//using TaleWorlds.CampaignSystem.Settlements;
//using System.Reflection;

//namespace RealmsForgotten.Patches
//{
//    [HarmonyPatch(typeof(BanditsCampaignBehavior), "TryToSpawnHideoutAndBanditHourly")]
//    public static class TryToSpawnHideoutAndBanditHourlyPatch
//    {
//        private sealed class BanditHideoutInfo
//        {
//            public List<MobileParty> Parties { get; set; } = new();
//            public int Value { get; set; } = 0;
//        }

//#pragma warning disable BHA0003 // Type was not found
//        private static readonly MethodInfo banditPartyHome = AccessTools.PropertySetter("BanditPartyComponent:Hideout");
//#pragma warning restore BHA0003

//        private static void TeleportAndInfestHideout(MobileParty banditParty, Hideout hideout)
//        {
//            Vec2 spawnPos = Campaign.Current.MapSceneWrapper.GetAccessiblePointNearPosition(hideout.Settlement.Position2D, 10);
//            banditParty.Position2D = spawnPos;
//            banditParty.Ai.SetMoveGoToSettlement(hideout.Settlement);
//            banditPartyHome?.Invoke(banditParty.BanditPartyComponent, new object[] { hideout });
//        }

//        public static List<Hideout> GetXHideouts(List<Hideout> hideouts, int count)
//        {
//            if (hideouts == null || hideouts.Count == 0 || count <= 0)
//                return new();

//            count = Math.Min(count, hideouts.Count);
//            var result = new List<Hideout>(hideouts);
//            var random = new Random();

//            for (int i = 0; i < count; i++)
//            {
//                int randomIndex = random.Next(i, result.Count);
//                (result[i], result[randomIndex]) = (result[randomIndex], result[i]);
//            }

//            return result.Take(count).ToList();
//        }

//        public static Dictionary<CultureObject, Tuple<List<MobileParty>, List<Hideout>>> GetClansBanditsHideouts()
//        {
//            Dictionary<CultureObject, Tuple<List<MobileParty>, List<Hideout>>> dict = new();

//            foreach (Clan faction in Clan.BanditFactions)
//            {
//                if (!faction.Culture.CanHaveSettlement)
//                    continue;

//                dict[faction.Culture] = new(new(), new());
//            }

//            foreach (MobileParty party in MobileParty.AllBanditParties)
//            {
//                if (!party.ActualClan.Culture.CanHaveSettlement)
//                    continue;

//                if (dict.TryGetValue(party.ActualClan.Culture, out var data))
//                    data.Item1.Add(party);
//            }

//            foreach (Hideout hideout in Hideout.All)
//            {
//                if (dict.TryGetValue(hideout.Settlement.Culture, out var data))
//                    data.Item2.Add(hideout);
//            }

//            return dict;
//        }

//        public static void TryToCreateNewHideoutsWithExcessBandits(List<MobileParty> factionsParties, List<Hideout> factionsHideouts)
//        {
//            if (factionsParties == null || factionsHideouts == null || !factionsHideouts.Any())
//                return;

//            Dictionary<Hideout, BanditHideoutInfo> banditsForHideout = factionsHideouts.ToDictionary(h => h, h => new BanditHideoutInfo());

//            foreach (MobileParty party in factionsParties)
//            {
//                var homeHideout = party.BanditPartyComponent?.Hideout;
//                if (homeHideout != null && banditsForHideout.ContainsKey(homeHideout))
//                {
//                    banditsForHideout[homeHideout].Parties.Add(party);
//                }
//            }

//            int totalBandits = banditsForHideout.Sum(kv => kv.Value.Parties.Count);
//            int banditsPerHideout = totalBandits / 3;

//            if (totalBandits > Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesAroundEachHideout)
//            {
//                var candidateHideouts = factionsHideouts.Where(h => !h.IsInfested).ToList();
//                var excessPartiesQueue = new Queue<MobileParty>();
//                var hideoutNeeds = new List<Tuple<Hideout, int>>();
//                var filledHideouts = new List<Hideout>();

//                foreach (var kv in banditsForHideout)
//                {
//                    Hideout hideout = kv.Key;
//                    BanditHideoutInfo info = kv.Value;

//                    int count = info.Parties.Count;
//                    if (count == 0) continue;

//                    for (int i = banditsPerHideout; i < count; i++)
//                        excessPartiesQueue.Enqueue(info.Parties[i]);

//                    int needed = Math.Max(0, banditsPerHideout - count);
//                    hideoutNeeds.Add(new Tuple<Hideout, int>(hideout, needed));
//                    filledHideouts.Add(hideout);
//                }

//                int additionalNeeded = Math.Max(0, 3 - hideoutNeeds.Count);
//                var unfilled = GetXHideouts(candidateHideouts.Except(filledHideouts).ToList(), additionalNeeded);

//                foreach (var h in unfilled)
//                    hideoutNeeds.Add(new Tuple<Hideout, int>(h, banditsPerHideout));

//                foreach (var (targetHideout, amount) in hideoutNeeds)
//                {
//                    for (int i = 0; i < amount; i++)
//                    {
//                        if (excessPartiesQueue.Count == 0)
//                            break;

//                        TeleportAndInfestHideout(excessPartiesQueue.Dequeue(), targetHideout);
//                    }
//                }
//            }
//        }

//        public static void Postfix()
//        {
//            var banditsPerCulture = GetClansBanditsHideouts();

//            foreach (var kv in banditsPerCulture)
//            {
//                List<Hideout> hideouts = kv.Value.Item2;
//                List<MobileParty> parties = kv.Value.Item1;

//                if (!hideouts.Any() || !parties.Any())
//                    continue;

//                int infestedCount = hideouts.Count(h => h.IsInfested);
//                if (infestedCount >= 3)
//                    continue;

//                TryToCreateNewHideoutsWithExcessBandits(parties, hideouts);
//            }
//        }
//    }
//}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using System.Reflection;

namespace RealmsForgotten.Patches
{
    [HarmonyPatch(typeof(BanditsCampaignBehavior), "TryToSpawnHideoutAndBanditHourly")]
    public static class TryToSpawnHideoutAndBanditHourlyPatch
    {
        private sealed class BanditHideoutInfo
        {
            public List<MobileParty> Parties { get; set; } = new();
            public int Value { get; set; } = 0;
        }

#pragma warning disable BHA0003 // Type was not found
        private static readonly MethodInfo banditPartyHome = AccessTools.PropertySetter("BanditPartyComponent:Hideout");
#pragma warning restore BHA0003 // Type was not found

        private static void TeleportAndInfestHideout(MobileParty banditParty, Hideout hideout)
        {

            Vec2 accessiblePointNearPosition = Campaign.Current.MapSceneWrapper.GetAccessiblePointNearPosition(hideout.Settlement.Position2D, 10);
            banditParty.Position2D = accessiblePointNearPosition;
            banditParty.Ai.SetMoveGoToSettlement(hideout.Settlement);
            banditPartyHome.Invoke(banditParty.BanditPartyComponent, new object[] {hideout});
        }
        public static List<Hideout> GetXHideouts(List<Hideout> hideouts, int count)
        {
            var random = new Random();
            var result = new List<Hideout>(hideouts);

            for (int i = 0; i < count; i++)
            {
                int randomIndex = random.Next(i, result.Count);
                (result[i], result[randomIndex]) = (result[randomIndex], result[i]);
            }

            return result.Take(count).ToList();
        }
        public static Dictionary<CultureObject, Tuple<List<MobileParty>, List<Hideout>>> GetClansBanditsHideouts()
        {
            Dictionary<CultureObject, Tuple<List<MobileParty>, List<Hideout>>> dict = new();
            foreach (Clan faction in Clan.BanditFactions)
            {
                if (!faction.Culture.CanHaveSettlement) continue;
                dict[faction.Culture] = new(new(), new());
            }
            foreach (MobileParty party in MobileParty.AllBanditParties)
            {
                if (!party.ActualClan.Culture.CanHaveSettlement) continue;
                dict[party.ActualClan.Culture].Item1.Add(party);
            }
            foreach(Hideout hideout in Hideout.All)
            {
                //if (hideout.MapFaction.StringId == "looters") continue;
                dict[hideout.Settlement.Culture].Item2.Add(hideout);
            }
            return dict;
        }
        public static void TryToCreateNewHideoutsWithExcessBandits(List<MobileParty> factionsParties, List<Hideout> factionsHideouts)
        {
            Dictionary<Hideout, BanditHideoutInfo> banditsForHideout = new();
            foreach (Hideout hideout in factionsHideouts)
                banditsForHideout.Add(hideout, new());
            foreach (MobileParty party in factionsParties)
            {
                banditsForHideout[party.BanditPartyComponent.Hideout].Parties.Add(party);
            }
            int allBandits = 0;
            for (int i = 0; i < banditsForHideout.Count(); i++)
            {
                BanditHideoutInfo item = banditsForHideout.Values.ToList()[i];
                int banditsInHIdeout = item.Parties.Count();
                allBandits += banditsInHIdeout;
                item.Value = banditsInHIdeout;
            }
            int banditsPerHideout = allBandits / 3;
            if (allBandits > Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesAroundEachHideout)
            {
                IEnumerable<Hideout> hideoutChosen = factionsHideouts.Where(h => !h.IsInfested);
                int hideoutToTakeFrom = 0;
                Queue<MobileParty> bpartiesToMove = new();
                List<Tuple<Hideout, int>> NOBanditsHideoutNeeds = new();
                List<Hideout> filledHideouts = new();
                foreach (KeyValuePair<Hideout, BanditHideoutInfo> valuePair in banditsForHideout)
                {
                    BanditHideoutInfo banditData = valuePair.Value;
                    int banditNumber = banditData.Value;
                    if (banditNumber == 0) continue;
                    for (int i = banditsPerHideout; i < banditNumber; i++)
                    {
                        bpartiesToMove.Enqueue(banditData.Parties[i]);
                    }
                    int banditsToReceive = Math.Max(0, banditsPerHideout - banditNumber);
                    NOBanditsHideoutNeeds.Add(new(valuePair.Key, banditsToReceive));
                    filledHideouts.Add(valuePair.Key);
                };

                factionsHideouts = GetXHideouts(factionsHideouts.Except(filledHideouts).ToList(), 3 - NOBanditsHideoutNeeds.Count);
                factionsHideouts.ForEach(h => NOBanditsHideoutNeeds.Add(new(h, banditsPerHideout)));
                foreach (Tuple<Hideout, int> t in NOBanditsHideoutNeeds)
                {
                    for (int i = 0; i < t.Item2; i++)
                    {
                        TeleportAndInfestHideout(bpartiesToMove.Dequeue(), t.Item1);
                    }
                }
                ++hideoutToTakeFrom;
            }

        }
        public static void Postfix()
        {
            Dictionary<CultureObject, Tuple<List<MobileParty>, List<Hideout>>> banditsPerClan = GetClansBanditsHideouts();
            foreach (KeyValuePair<CultureObject, Tuple<List<MobileParty>, List<Hideout>>> ClanData in banditsPerClan)
            {
                List<Hideout> factionsHideouts = ClanData.Value.Item2;
                List<MobileParty> factionsParties = ClanData.Value.Item1;
                if (!factionsHideouts.Any()) continue;
                if (factionsHideouts.Where(h => h.IsInfested).Count() >= 3) continue;
                if (!factionsParties.Any()) continue;
                TryToCreateNewHideoutsWithExcessBandits(factionsParties, factionsHideouts);
            }
        }
    }
}
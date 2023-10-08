using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace RealmsForgotten.RFCustomBandits
{
    public class SlaversRosterBehavior : CampaignBehaviorBase
    {
        private int currentNoSmallSlaverParties = 0;
        private int currentNoBigSlaverParties = 0;
        private readonly int maxNumberOfBigSlaverParties = 10;
        private readonly int maxNumberOfSmallSlaverParties = 20;
        public static int ChangeTotalSizeLimitIfSlavers(PartyBase party)
        {
            if (party.IsSlaverParty())
            {
                return 50;
            }
            return 0;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            //CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, SpawnDesertersIfPossible);
        }
        public void DailyTick()
        {
            var random = new Random();
            if (1-MathF.Log(currentNoSmallSlaverParties, maxNumberOfSmallSlaverParties) > random.NextDouble())
                SpawnSmallSlaverParty();
            if (1-MathF.Log(currentNoBigSlaverParties, maxNumberOfBigSlaverParties) > random.NextDouble())
                SpawnBigSlaverParty();
        }

        private void SpawnBigSlaverParty()
        {
            currentNoBigSlaverParties += 1;
            PartyTemplateObject troopTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("enslavers_template");
            SpawnSlavers(troopTemplate);
        }

        private void SpawnSmallSlaverParty()
        {
            currentNoSmallSlaverParties += 1;
            PartyTemplateObject troopTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("enslavers_template");
            SpawnSlavers(troopTemplate);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("numer_of_small_slaver_parties", ref currentNoSmallSlaverParties);
            dataStore.SyncData("numer_of_big_slaver_parties", ref currentNoBigSlaverParties);
            //dataStore.SyncData("deserter_troops_global_pool", ref _desertedTroops);
        }
        public void SpawnSlavers(PartyTemplateObject troopTemplate)
        {
            IEnumerable<Hideout> infestedHideouts = Hideout.All.WhereQ((Hideout h) => h.IsInfested);
            if (!infestedHideouts.Any()) return;
            Hideout randomHideout = infestedHideouts.ElementAt(MBRandom.RandomInt(0, infestedHideouts.Count()));

            //Clan looterClan = Clan.All.WhereQ((Clan c) => c.StringId == "looters").Single();
            Clan looterClan = Clan.All.WhereQ((Clan c) => c.StringId == "athas_enslavers").Single();
            MobileParty SlaverParty = MobileParty.CreateParty("Slavers", new SlaversBanditPartyComponent(randomHideout, false), delegate (MobileParty mobileParty)
            {
                mobileParty.ActualClan = looterClan;
            });

            if (randomHideout != null)
            {
                float num = 45f * 1.5f;
                SlaverParty.InitializeMobilePartyAtPosition(troopTemplate, randomHideout.Settlement.GatePosition, 50);
                Vec2 vec = SlaverParty.Position2D;
                float radiusAroundPlayerPartySquared = 20;
                for (int i = 0; i < 15; i++)
                {
                    Vec2 vec2 = MobilePartyHelper.FindReachablePointAroundPosition(vec, num, 0f);
                    if (vec2.DistanceSquared(MobileParty.MainParty.Position2D) > radiusAroundPlayerPartySquared)
                    {
                        vec = vec2;
                        break;
                    }
                }
                if (vec != SlaverParty.Position2D)
                {
                    SlaverParty.Position2D = vec;
                }
                SlaverParty.Party.SetVisualAsDirty();
                int initialGold = (int)(10f * (float)SlaverParty.Party.MemberRoster.TotalManCount * (0.5f + 1f * MBRandom.RandomFloat));
                SlaverParty.InitializePartyTrade(initialGold);
                foreach (ItemObject itemObject in Items.All)
                {
                    if (itemObject.IsFood)
                    {
                        int num3 = 8;
                        int num2 = MBRandom.RoundRandomized((float)SlaverParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)num3 * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                        if (num2 > 0)
                        {
                            SlaverParty.ItemRoster.AddToCounts(itemObject, num2);
                        }
                    }
                }
                SlaverParty.Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
                SlaverParty.Ai.SetMovePatrolAroundPoint(randomHideout.Settlement.Position2D);
            }
        }
    }
}
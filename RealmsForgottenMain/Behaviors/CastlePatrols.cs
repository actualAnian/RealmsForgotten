using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Behaviors
{
    public class CastlePatrols : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        private void OnHourlyTick()
        {
            foreach (var settlement in Settlement.All.Where(s => s.IsCastle && s.OwnerClan != null && s.OwnerClan.Leader != Hero.MainHero))
            {
                ManagePatrolsForCastle(settlement);
            }
        }

        private void ManagePatrolsForCastle(Settlement castle)
        {
            var existingPatrol = castle.Parties.FirstOrDefault(p => p.PartyComponent is CastlePatrolPartyComponent);
            if (existingPatrol == null)
            {
                CreatePatrolParty(castle);
            }
            else
            {
                ((CastlePatrolPartyComponent)existingPatrol.PartyComponent).UpdatePatrolStatus(existingPatrol);
            }
        }

        private void CreatePatrolParty(Settlement castle)
        {
            var patrolParty = MobileParty.CreateParty("castle_patrol", new CastlePatrolPartyComponent(castle), (party) => {
                party.InitializeMobilePartyAtPosition(castle.Culture.DefaultPartyTemplate, castle.Position2D); // Use DefaultPartyTemplate
                party.AddElementToMemberRoster(castle.Culture.EliteBasicTroop, 30);
                party.ItemRoster.AddToCounts(DefaultItems.Grain, 50); // Provide some starting food
                party.IsVisible = false; // Make the party less visible to players
                party.SetCustomName(new TextObject($"Defenders of {castle.Name}"));
            });
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }

    public class CastlePatrolPartyComponent : PartyComponent
    {
        public override TextObject Name { get; }
        public override Settlement HomeSettlement { get; }
        public override Hero PartyOwner { get; }

        public CastlePatrolPartyComponent(Settlement homeSettlement)
        {
            HomeSettlement = homeSettlement;
            PartyOwner = homeSettlement.OwnerClan.Leader;
            Name = new TextObject($"Defenders of {homeSettlement.Name}");
        }

        public void UpdatePatrolStatus(MobileParty patrolParty)
        {
            if (patrolParty.Food < 1 || patrolParty.MemberRoster.TotalManCount < 20)
            {
                patrolParty.Ai.SetMoveGoToSettlement(HomeSettlement);
            }
            else
            {
                SearchAndEngageEnemies(patrolParty);
            }
        }

        private void SearchAndEngageEnemies(MobileParty patrolParty)
        {
            var enemiesNearby = MobileParty.All.Where(mp => mp.IsBandit && mp.IsActive && mp.CurrentSettlement == null &&
                                                            HomeSettlement.GatePosition.Distance(mp.Position2D) <= 10f).ToList();
            if (enemiesNearby.Count > 0)
            {
                patrolParty.Ai.SetMoveEngageParty(enemiesNearby.First());
            }
            else
            {
                patrolParty.Ai.SetMoveGoToSettlement(HomeSettlement);
            }
        }
    }
}
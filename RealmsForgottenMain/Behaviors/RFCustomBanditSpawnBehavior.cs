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

namespace RealmsForgotten.Behaviors
{
    public class RFCustomBanditSpawnBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, (string ClanId, string PartyTemplateId)> banditFactions = new Dictionary<string, (string, string)>
        {
            { "forest_bandit", ("forest_bandits", "forest_bandits_party_template") },
            { "mountain_bandit", ("mountain_bandits", "mountain_bandits_party_template") },
            { "desert_bandit", ("desert_bandits", "desert_bandits_party_template") },
            { "steppe_bandit", ("steppe_bandits", "steppe_bandits_party_template") },
            { "sea_raider", ("sea_raiders", "sea_raiders_party_template") }
        };

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            // Initialization code if needed
        }

        private void HourlyTick()
        {
            foreach (var banditFaction in banditFactions)
            {
                TrySpawnBanditParty(banditFaction.Key);
            }
        }

        private void TrySpawnBanditParty(string banditType)
        {
            var hideout = Hideout.All.FirstOrDefault(h => h.StringId == $"{banditType}_hideout");
            if (hideout == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR: {banditType} hideout not found.", Colors.Red));
                return;
            }

            Vec2 spawnPosition = hideout.Settlement.Position2D;
            var (clanId, partyTemplateId) = banditFactions[banditType];

            Clan banditClan = Clan.All.FirstOrDefault(clan => clan.StringId == clanId);
            if (banditClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR: {banditType} clan not found.", Colors.Red));
                return;
            }

            PartyTemplateObject partyTemplate = MBObjectManager.Instance.GetObject<PartyTemplateObject>(partyTemplateId);
            if (partyTemplate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"ERROR: {banditType} party template not found.", Colors.Red));
                return;
            }

            MobileParty banditParty = MobileParty.CreateParty($"{banditType}_party", new BanditPartyComponent(), delegate (MobileParty mobileParty) { });
            banditParty.InitializeMobilePartyAroundPosition(partyTemplate, spawnPosition, 2f);
            banditParty.SetCustomName(new TextObject($"{banditType} Party"));
            banditParty.IsVisible = true;

            InformationManager.DisplayMessage(new InformationMessage($"Spawned {banditType} party near {hideout.Settlement.Name}.", Colors.Green));
        }

        private class BanditPartyComponent : PartyComponent
        {
            public BanditPartyComponent() : base() { }

            public override Hero PartyOwner => null;

            public override TextObject Name => new TextObject("Bandit Party");

            public override Settlement HomeSettlement => null;
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Sync data if needed
        }
    }
}


using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Behaviors
{
    public class AggressiveSturgiaBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, int> lastWarDeclarationDays = new Dictionary<string, int>();

        private CampaignTime lastProcessedTime;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("lastWarDeclarationDays", ref lastWarDeclarationDays);
            dataStore.SyncData("lastProcessedTime", ref lastProcessedTime);
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            lastProcessedTime = CampaignTime.Now;
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            if (lastProcessedTime == default)
            {
                lastProcessedTime = CampaignTime.Now;
            }
        }

        private void OnDailyTick()
        {
            // Only run every 5 days
            if (CampaignTime.Now.ToDays - lastProcessedTime.ToDays < 5)
                return;

            lastProcessedTime = CampaignTime.Now;

            var sturgiaKingdom = Kingdom.All.FirstOrDefault(k => k.Culture.StringId == "sturgia");

            if (sturgiaKingdom == null)
                return;

            if (!lastWarDeclarationDays.ContainsKey(sturgiaKingdom.StringId))
            {
                lastWarDeclarationDays[sturgiaKingdom.StringId] = CampaignTime.Now.GetDayOfYear;
            }

            int daysSinceLastWar = CampaignTime.Now.GetDayOfYear - lastWarDeclarationDays[sturgiaKingdom.StringId];

            if (daysSinceLastWar > 15)
            {
                DeclareWarOnSpecificFactions(sturgiaKingdom);
                lastWarDeclarationDays[sturgiaKingdom.StringId] = CampaignTime.Now.GetDayOfYear;
            }
        }

        private void DeclareWarOnSpecificFactions(Kingdom sturgiaKingdom)
        {
            var potentialEnemies = Kingdom.All
                .Where(k => k != sturgiaKingdom && !k.IsAtWarWith(sturgiaKingdom) &&
                            (k.Culture.StringId == "vlandia" || k.Culture.StringId == "battania" || k.Culture.StringId == "empire"))
                .ToList();

            if (potentialEnemies.Any())
            {
                var chosenEnemy = potentialEnemies[MBRandom.RandomInt(potentialEnemies.Count)];
                FactionManager.DeclareWar(sturgiaKingdom, chosenEnemy);

                // ✅ No InformationManager popup here to avoid pausing
                MBInformationManager.AddQuickInformation(new TextObject($"Sturgia has declared war on {chosenEnemy.Name}!"));
            }
        }
    }
}

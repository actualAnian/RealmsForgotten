
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Behaviors
{
    public class MyModEnlistmentBehaviorExtension : CampaignBehaviorBase
    {
        private const int DefaultPenalty = -10;

        public void ApplyDesertionPenalty(Hero deserter)
        {
            foreach (var notable in deserter.CurrentSettlement.Notables)
            {
                ChangeRelationAction.ApplyPlayerRelation(notable, DefaultPenalty);
            }
            InformationManager.DisplayMessage(new InformationMessage("You have deserted your party. Penalties applied."));
        }

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Synchronize any enlistment extension-related data
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (mapEvent.IsPlayerMapEvent)
            {
                // Check for desertion or disobedience during battles, etc.
            }
        }
    }
}

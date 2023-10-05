using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using RealmsForgotten.RFCustomSettlements;
using TaleWorlds.MountAndBlade;

namespace RFCustomSettlements
{
    public class ArenaCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
        }
        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            this.AddDialogs(campaignGameStarter);
        }
        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddDialogLine("rf_arena_explain", "start", "rf_arena_understood", "Explain what happens in the arena", new ConversationSentence.OnConditionDelegate(this.test), null, 100, null);
            campaignGameStarter.AddPlayerLine("rf_arena_understood", "rf_arena_understood", "close_window", "I see", null, new ConversationSentence.OnConsequenceDelegate(this.StartArenaMission), 100, null, null);
        }
        private void StartArenaMission()
        {
            Mission.Current.EndMission();
            ArenaSettlementStateHandler.currentState = ArenaSettlementStateHandler.ArenaState.FightStage1;
        }
        private bool test()
        {
            return Settlement.CurrentSettlement != null &&  Settlement.CurrentSettlement.SettlementComponent != null && Settlement.CurrentSettlement.SettlementComponent is RFCustomSettlement && CharacterObject.OneToOneConversationCharacter.StringId == "spc_notable_empire_8";
        }
        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}

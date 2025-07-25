using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.CampaignSystem.AgentOrigins;

namespace RealmsForgotten.AiMade.Adventurer
{
    public class ALordDialogueCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AddLordDuelDialogs(starter);
        }

        private void AddLordDuelDialogs(CampaignGameStarter starter)
        {
            starter.AddDialogLine("rf_lord_duel_start", "lord_start", "rf_lord_duel_ask",
                "{=rf_ld_greeting}You seem confident. What do you want?",
                LordDuelCondition, null);

            starter.AddPlayerLine("rf_lord_duel_challenge", "rf_lord_duel_ask", "rf_lord_duel_confirm",
                "{=rf_ld_challenge}I challenge you to a duel!", null, null);

            starter.AddDialogLine("rf_lord_duel_confirm_line", "rf_lord_duel_confirm", "close_window",
                "{=rf_ld_confirm}Very well. Let us settle this.", null, StartDuel);
        }

        private bool LordDuelCondition()
        {
            return Hero.OneToOneConversationHero != null &&
                   Hero.OneToOneConversationHero.IsLord &&
                   Settlement.CurrentSettlement?.IsTown == true;
        }

        private void StartDuel()
        {
            Hero opponent = Hero.OneToOneConversationHero;
            if (opponent == null || Settlement.CurrentSettlement == null)
                return;

            string sceneName = Settlement.CurrentSettlement.LocationComplex
                ?.GetLocationWithId("arena")?.GetSceneName(0) ?? "arena_empire_a";

            InformationManager.DisplayMessage(new InformationMessage($"⚔ Duel initiated with {opponent.Name}"));

            MissionState.OpenNew(
                "DuelMission",
                new MissionInitializerRecord(sceneName)
                {
                    PlayingInCampaignMode = true
                },
                mission => new MissionBehavior[]
                {
                    new MissionOptionsComponent(),
                    new SimpleDuelMissionLogic(opponent)
                }
            );
        }
    }
}
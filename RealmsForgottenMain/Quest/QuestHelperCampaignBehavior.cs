using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.Quest
{
    public class QuestHelperCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }
        private void OnGameLoaded(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("hideout_place", "attack", "KILL THEM",
                new GameMenuOption.OnConditionDelegate(this.game_menu_attack_hideout_parties_on_condition),
                new GameMenuOption.OnConsequenceDelegate(this.game_menu_encounter_attack_on_consequence), false, -1, false, null);

        }
        private void game_menu_encounter_attack_on_consequence(MenuCallbackArgs args)
        {
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            CampaignMission.OpenHideoutBattleMission("forest_hideout_003", null);
        }

        private bool game_menu_attack_hideout_parties_on_condition(MenuCallbackArgs args)
        {
            return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsHideout;
        }

        public override void SyncData(IDataStore dataStore)
        {
            throw new NotImplementedException();
        }
    }
}

using System.Linq;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Village_Inn_Quests
{
    public class WerewolfVillageMenuBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Adiciona uma opção dinâmica no menu da vila
            starter.AddGameMenuOption(
                "village",
                "rf_werewolf_confront_option",
                "{=rf_werewolf_confront}Confront the beast (at night)",
                Condition_ShowAndEnable,
                Consequence_Start,
                isLeave: false,
                index: 1);
        }

        private bool Condition_ShowAndEnable(MenuCallbackArgs args)
        {
            var quest = Campaign.Current?.QuestManager?.Quests
                ?.FirstOrDefault(q => q is WerewolfQuest wq && wq.IsActiveAndNotResolved) as WerewolfQuest;

            if (quest == null)
                return false; // não mostra a opção

            // precisa estar no menu da vila e na vila-alvo
            if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != quest.TargetSettlement)
                return false;

            // mostra a opção sempre que estiver na vila certa;
            // habilita somente à noite + esperando (como Poachers)
            bool isNight = CampaignTime.Now.IsNightTime;
            bool isWaiting = PlayerEncounter.Current != null && PlayerEncounter.Current.IsPlayerWaiting;

            if (isNight && isWaiting)
            {
                args.IsEnabled = true;
                return true; // mostra habilitado
            }

            // mostra desabilitado com uma dica
            args.IsEnabled = false;
            args.Tooltip = new TextObject("{=rf_werewolf_wait_night}Wait in the village until nightfall.");
            return true;
        }

        private void Consequence_Start(MenuCallbackArgs args)
        {
            var quest = Campaign.Current?.QuestManager?.Quests
                ?.FirstOrDefault(q => q is WerewolfQuest wq && wq.IsActiveAndNotResolved) as WerewolfQuest;

            if (quest == null || Settlement.CurrentSettlement == null)
                return;

            var village = Settlement.CurrentSettlement;

            // --- OPÇÃO A: manter o seu menu de emboscada (se você quiser esse passo de RP) ---
            // GameMenu.SwitchToMenu("rf_werewolf_ambush_menu");

            // --- OPÇÃO B (recomendada se o menu continuar problemático): começa a batalha direto ---
            quest.StartQuestBattle(village);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}

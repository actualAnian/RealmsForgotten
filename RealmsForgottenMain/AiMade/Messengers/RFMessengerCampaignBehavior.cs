using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.Messengers
{
    /// <summary>
    /// Registra o sistema de mensageiros na campanha: opção de diálogo com qualquer herói
    /// ("posso te mandar um mensageiro?"), tick horário da viagem, e persistência.
    /// O botão da enciclopédia vive em RFMessengerEncyclopediaUI.
    /// </summary>
    public class RFMessengerCampaignBehavior : CampaignBehaviorBase
    {
        private readonly RFMessengerManager _manager = new RFMessengerManager();

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            _manager.SyncData(dataStore);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            _manager.Initialize();
            AddDialogOptions(starter);
        }

        private void OnHourlyTick()
        {
            _manager.UpdateMessengers();
        }

        public void SendMessenger(Hero targetHero) => _manager.SendMessenger(targetHero);

        public bool CanSendMessenger(Hero targetHero, out TextObject reason)
            => _manager.CanSendMessenger(targetHero, out reason);

        private void AddDialogOptions(CampaignGameStarter starter)
        {
            starter.AddPlayerLine(
                "rf_msgr_send_init",
                "hero_main_options",
                "rf_msgr_send_confirm",
                "{=rf_msgr_dlg_init}I may need to reach you later. Would you receive a messenger from me?",
                () =>
                {
                    Hero hero = Hero.OneToOneConversationHero;
                    return hero != null && hero != Hero.MainHero && _manager.CanSendMessenger(hero, out _);
                },
                null);

            starter.AddDialogLine(
                "rf_msgr_send_confirm",
                "rf_msgr_send_confirm",
                "rf_msgr_send_choice",
                "{=rf_msgr_dlg_accept}Of course. My people will see your messenger through to me.",
                null,
                null);

            starter.AddPlayerLine(
                "rf_msgr_send_choice_yes",
                "rf_msgr_send_choice",
                "rf_msgr_sent",
                "{=rf_msgr_dlg_send}Send the messenger ({COST} denars)",
                () =>
                {
                    GameTexts.SetVariable("COST", RFMessengerManager.MessengerGoldCost);
                    return Hero.MainHero.Gold >= RFMessengerManager.MessengerGoldCost;
                },
                () =>
                {
                    Hero hero = Hero.OneToOneConversationHero;
                    if (hero != null)
                    {
                        _manager.SendMessenger(hero);
                    }
                });

            starter.AddPlayerLine(
                "rf_msgr_send_choice_no",
                "rf_msgr_send_choice",
                "rf_msgr_decline_response",
                "{=rf_msgr_dlg_decline}On second thought, never mind.",
                null,
                null);

            starter.AddDialogLine(
                "rf_msgr_decline_response",
                "rf_msgr_decline_response",
                "hero_main_options",
                "{=rf_msgr_dlg_decline_ack}As you wish.",
                null,
                null);

            starter.AddDialogLine(
                "rf_msgr_sent",
                "rf_msgr_sent",
                "hero_main_options",
                "{=rf_msgr_dlg_sent}I will be expecting your messenger, then.",
                null,
                null);
        }
    }
}

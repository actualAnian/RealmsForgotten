using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;


namespace RealmsForgotten.AiMade.TradePact
{
    public class EconomicPactBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnDailyTick()
        {
            EconomicPactManager.DailyTick();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "economic_pact_menu", "Master of Coins", args =>
            {
                var currentSettlement = Hero.MainHero.CurrentSettlement;
                var ownerFaction = currentSettlement?.OwnerClan?.Kingdom;
                var playerFaction = Hero.MainHero.MapFaction;

                // ✅ Requisitos:
                // 1. Ser líder de facção
                bool isRuler = playerFaction?.Leader == Hero.MainHero;

                // 2. Possuir ao menos uma cidade (town)
                bool hasTown = Settlement.All
                    .Where(s => s.IsTown)
                    .Any(s => s.OwnerClan?.Leader == Hero.MainHero);

                // 3. Cidade não pertence ao jogador e não estão em guerra
                bool isValidTarget = currentSettlement != null
                    && ownerFaction != null
                    && ownerFaction != playerFaction
                    && !FactionManager.IsAtWarAgainstFaction(playerFaction, ownerFaction)
                    && !EconomicPactManager.HasActivePact(playerFaction, ownerFaction);

                return isRuler && hasTown && isValidTarget;
            },
            args =>
            {
                GameMenu.SwitchToMenu("economic_pact_interface");
            });

            starter.AddGameMenu("economic_pact_interface", "You meet with the Master of Coins to discuss a trade pact.", null);

            starter.AddGameMenuOption("economic_pact_interface", "confirm_pact", "Propose Trade Pact", args => true,
            args =>
            {
                var otherFaction = Hero.MainHero.CurrentSettlement.OwnerClan.Kingdom;
                var playerFaction = Hero.MainHero.MapFaction;

                if (Hero.MainHero.Gold >= 10000)
                {
                    Hero.MainHero.ChangeHeroGold(-10000);
                    EconomicPactManager.AddPact(playerFaction, otherFaction, 30, "General Goods");

                    InformationManager.ShowInquiry(new InquiryData(
                        "Trade Pact Signed",
                        $"You have signed a trade pact with {otherFaction.Name}.\n\n" +
                        $"Benefits:\n" +
                        $"- 10% discount when buying goods from this faction\n" +
                        $"- 10% discount for them when buying goods from you\n" +
                        $"- Trade routes will prioritize this faction\n\n" +
                        $"Duration: 30 days",
                         true, 
                        false, 
                        "Continue", 
                        "", 
                         () => GameMenu.SwitchToMenu("town"), 
                        null 
                ));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "You don't have enough gold to secure a trade pact."));
                }

                GameMenu.SwitchToMenu("town");
            });

            starter.AddGameMenuOption("economic_pact_interface", "cancel", "Cancel", args => true,
                args => GameMenu.SwitchToMenu("town"));
        }
    }
}

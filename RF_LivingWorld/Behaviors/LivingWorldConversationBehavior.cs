using RealmsForgotten.WorldState.Refugees;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldConversationBehavior : CampaignBehaviorBase
    {
        private const int ProvisionPrice = 100;
        private const int AnimalPrice = 75;
        private const int ProvisionCount = 5;
        private const int DonationCount = 2;
        private const int MeatPrice = 50;
        private const int MedicinalPlantsPrice = 75;
        private const int DowryGift = 100;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore) { }

        private static void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddDialogLine(
                "rf_lw_greeting", "start", "rf_lw_hub",
                "{=rf_lw_greeting}{RF_LW_GREETING}",
                IsLivingWorldConversation, null);

            starter.AddPlayerLine(
                "rf_lw_ask_news", "rf_lw_hub", "rf_lw_rumor",
                "{=rf_lw_ask_news}What news have you heard on the road?",
                null, PrepareRumor);
            starter.AddDialogLine(
                "rf_lw_rumor_answer", "rf_lw_rumor", "rf_lw_hub",
                "{=rf_lw_rumor_answer}{RF_LW_RUMOR_TEXT}",
                null, null);

            starter.AddPlayerLine(
                "rf_lw_buy_provisions", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_buy_provisions}I will buy five sacks of provisions for 100 denars.",
                CanBuyProvisions, BuyProvisions);
            starter.AddPlayerLine(
                "rf_lw_buy_animal", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_buy_animal}I will buy one animal for 75 denars.",
                CanBuyAnimal, BuyAnimal);
            starter.AddPlayerLine(
                "rf_lw_donate_grain", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_donate_grain}Take two sacks of grain for the road.",
                CanDonateGrain, DonateGrain);
            starter.AddPlayerLine(
                "rf_lw_buy_meat", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_buy_meat}I will buy one portion of meat for 50 denars.",
                CanBuyMeat, BuyMeat);
            starter.AddPlayerLine(
                "rf_lw_buy_medicinal_plants", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_buy_medicinal_plants}I will buy medicinal plants for 75 denars.",
                CanBuyMedicinalPlants, BuyMedicinalPlants);
            starter.AddPlayerLine(
                "rf_lw_tax_route", "rf_lw_hub", "rf_lw_route_answer",
                "{=rf_lw_tax_route}Where do you come from, and where are these taxes going?",
                IsTaxCollectorConversation, PrepareRouteAnswer);
            starter.AddPlayerLine(
                "rf_lw_prisoner_route", "rf_lw_hub", "rf_lw_prisoner_answer",
                "{=rf_lw_prisoner_route}Who are you transporting?",
                IsPrisonerEscortConversation, PreparePrisonerAnswer);
            starter.AddPlayerLine(
                "rf_lw_dowry_gift", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_dowry_gift}Accept 100 denars as a gift for the wedding.",
                CanGiveDowryGift, GiveDowryGift);
            starter.AddDialogLine(
                "rf_lw_purchase_done", "rf_lw_purchase_done", "rf_lw_hub",
                "{=rf_lw_purchase_done}A fair bargain. May it serve you well.",
                null, null);
            starter.AddDialogLine(
                "rf_lw_route_answer", "rf_lw_route_answer", "rf_lw_hub",
                "{=rf_lw_route_answer}{RF_LW_ROUTE_TEXT}",
                null, null);
            starter.AddDialogLine(
                "rf_lw_prisoner_answer", "rf_lw_prisoner_answer", "rf_lw_hub",
                "{=rf_lw_prisoner_answer}{RF_LW_PRISONER_TEXT}",
                null, null);
            starter.AddPlayerLine(
                "rf_lw_leave", "rf_lw_hub", "close_window",
                "{=rf_lw_leave}Safe travels.",
                null, null);

            // Reuses the existing refugee conversation hub; it does not create
            // or replace the refugee system.
            starter.AddPlayerLine(
                "rf_lw_refugee_ask_news", "rf_refugee_hub", "rf_lw_refugee_rumor",
                "{=rf_lw_refugee_ask_news}What did you see before you fled?",
                IsRefugeeConversation, PrepareRumor);
            starter.AddDialogLine(
                "rf_lw_refugee_rumor", "rf_lw_refugee_rumor", "rf_refugee_hub",
                "{=rf_lw_rumor_answer}{RF_LW_RUMOR_TEXT}",
                null, null);

        }

        private static bool IsLivingWorldConversation()
        {
            if (MobileParty.ConversationParty?.PartyComponent is not LivingWorldPartyComponent component)
            {
                return false;
            }

            string greeting = component.PartyType switch
            {
                LivingWorldPartyType.Merchant => new TextObject("{=rf_lw_merchant_greeting}We trade where the great caravans will not. What do you need, traveller?").ToString(),
                LivingWorldPartyType.Herder => new TextObject("{=rf_lw_herder_greeting}Easy now. The herd has had a long road, but we can still spare a moment.").ToString(),
                LivingWorldPartyType.Pilgrim => new TextObject("{=rf_lw_pilgrim_greeting}We walk in faith, and welcome a peaceful traveller.").ToString(),
                LivingWorldPartyType.Leper => new TextObject("{=rf_lw_leper_greeting}Keep your distance if you wish. We mean no harm.").ToString(),
                LivingWorldPartyType.Hunter => new TextObject("{=rf_lw_hunter_greeting}The woods have been kind to us today. What brings you to this road?").ToString(),
                LivingWorldPartyType.ReligiousProcession => new TextObject("{=rf_lw_procession_greeting}Our procession travels under a sacred purpose. Speak, traveller.").ToString(),
                LivingWorldPartyType.Healer => new TextObject("{=rf_lw_healer_greeting}We carry remedies for those who need them. How may we help?").ToString(),
                LivingWorldPartyType.TaxCollector => new TextObject("{=rf_lw_tax_greeting}These chests are accounted for. State your business.").ToString(),
                LivingWorldPartyType.PrisonerEscort => new TextObject("{=rf_lw_prisoner_greeting}Keep clear of the prisoners. This is official business.").ToString(),
                LivingWorldPartyType.DowryProcession => new TextObject("{=rf_lw_dowry_greeting}We carry gifts for a wedding. May the road treat us kindly.").ToString(),
                _ => new TextObject("{=rf_lw_wandering_greeting}The road is long. What do you need?").ToString()
            };
            MBTextManager.SetTextVariable("RF_LW_GREETING", greeting);
            return true;
        }

        private static bool IsRefugeeConversation() =>
            MobileParty.ConversationParty?.PartyComponent is RefugeePartyComponent;

        private static void PrepareRumor()
        {
            MobileParty source = MobileParty.ConversationParty;
            string text = new TaleWorlds.Localization.TextObject("{=rf_lw_no_rumor}Nothing certain. The road has been quiet for once.").ToString();
            if (LivingWorldCampaignBehavior.Instance?.Rumors.TryGetOrCreate(source, out LivingWorldRumorReport? report) == true
                && report != null)
            {
                string reliability = report.Reliability >= 0.8f
                    ? new TaleWorlds.Localization.TextObject("{=rf_lw_reliable}I trust the source. ").ToString()
                    : new TaleWorlds.Localization.TextObject("{=rf_lw_uncertain}Take this with caution. ").ToString();
                text = reliability + report.Text;
            }
            MBTextManager.SetTextVariable("RF_LW_RUMOR_TEXT", text);
        }

        private static bool CanBuyProvisions()
        {
            MobileParty party = MobileParty.ConversationParty;
            return party?.PartyComponent is LivingWorldPartyComponent component
                && component.PartyType == LivingWorldPartyType.Merchant
                && Hero.MainHero.Gold >= ProvisionPrice
                && party.ItemRoster.GetItemNumber(DefaultItems.Grain) >= ProvisionCount;
        }

        private static void BuyProvisions()
        {
            MobileParty party = MobileParty.ConversationParty;
            if (!CanBuyProvisions())
            {
                return;
            }

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, ProvisionPrice, true);
            party.ItemRoster.AddToCounts(DefaultItems.Grain, -ProvisionCount);
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, ProvisionCount);
        }

        private static bool CanBuyAnimal()
        {
            MobileParty party = MobileParty.ConversationParty;
            if (party?.PartyComponent is not LivingWorldPartyComponent component
                || component.PartyType != LivingWorldPartyType.Herder
                || Hero.MainHero.Gold < AnimalPrice)
            {
                return false;
            }

            ItemObject animal = MBObjectManager.Instance?.GetObject<ItemObject>(LivingWorldPartyComponent.HerdItemId(component.HerdVariant));
            return animal != null && party.ItemRoster.GetItemNumber(animal) > 0;
        }

        private static void BuyAnimal()
        {
            MobileParty party = MobileParty.ConversationParty;
            if (!CanBuyAnimal() || party.PartyComponent is not LivingWorldPartyComponent component)
            {
                return;
            }

            ItemObject animal = MBObjectManager.Instance.GetObject<ItemObject>(LivingWorldPartyComponent.HerdItemId(component.HerdVariant));
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, AnimalPrice, true);
            party.ItemRoster.AddToCounts(animal, -1);
            MobileParty.MainParty.ItemRoster.AddToCounts(animal, 1);
        }

        private static bool CanDonateGrain()
        {
            MobileParty party = MobileParty.ConversationParty;
            return party?.PartyComponent is LivingWorldPartyComponent component
                && (component.PartyType == LivingWorldPartyType.Pilgrim
                    || component.PartyType == LivingWorldPartyType.Leper
                    || component.PartyType == LivingWorldPartyType.ReligiousProcession)
                && MobileParty.MainParty?.ItemRoster.GetItemNumber(DefaultItems.Grain) >= DonationCount;
        }

        private static void DonateGrain()
        {
            MobileParty party = MobileParty.ConversationParty;
            if (!CanDonateGrain() || party == null || MobileParty.MainParty == null)
            {
                return;
            }

            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, -DonationCount);
            party.ItemRoster.AddToCounts(DefaultItems.Grain, DonationCount);
        }

        private static bool CanBuyMeat() => CanBuyItem(LivingWorldPartyType.Hunter, "meat", MeatPrice);

        private static void BuyMeat() => BuyItem("meat", MeatPrice);

        private static bool CanBuyMedicinalPlants() => CanBuyItem(LivingWorldPartyType.Healer, "medicinal_plants", MedicinalPlantsPrice);

        private static void BuyMedicinalPlants() => BuyItem("medicinal_plants", MedicinalPlantsPrice);

        private static bool CanBuyItem(LivingWorldPartyType type, string itemId, int price)
        {
            MobileParty party = MobileParty.ConversationParty;
            ItemObject item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
            return party?.PartyComponent is LivingWorldPartyComponent component
                && component.PartyType == type
                && item != null
                && Hero.MainHero.Gold >= price
                && party.ItemRoster.GetItemNumber(item) > 0;
        }

        private static void BuyItem(string itemId, int price)
        {
            MobileParty party = MobileParty.ConversationParty;
            ItemObject item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
            if (party == null || item == null || Hero.MainHero.Gold < price || party.ItemRoster.GetItemNumber(item) <= 0)
            {
                return;
            }

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, price, true);
            party.ItemRoster.AddToCounts(item, -1);
            MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
        }

        private static bool IsTaxCollectorConversation() => IsConversationType(LivingWorldPartyType.TaxCollector);

        private static bool IsPrisonerEscortConversation() => IsConversationType(LivingWorldPartyType.PrisonerEscort);

        private static bool IsConversationType(LivingWorldPartyType type) =>
            MobileParty.ConversationParty?.PartyComponent is LivingWorldPartyComponent component
            && component.PartyType == type;

        private static void PrepareRouteAnswer()
        {
            LivingWorldPartyComponent component = (LivingWorldPartyComponent)MobileParty.ConversationParty.PartyComponent;
            TextObject answer = new("{=rf_lw_tax_route_answer}We left {ORIGIN} and are taking the taxes to {DESTINATION}.");
            answer.SetTextVariable("ORIGIN", component.Origin?.Name ?? new TextObject("{=rf_lw_unknown}unknown"));
            answer.SetTextVariable("DESTINATION", component.Destination?.Name ?? new TextObject("{=rf_lw_unknown}unknown"));
            MBTextManager.SetTextVariable("RF_LW_ROUTE_TEXT", answer);
        }

        private static void PreparePrisonerAnswer()
        {
            MobileParty party = MobileParty.ConversationParty;
            LivingWorldPartyComponent component = (LivingWorldPartyComponent)party.PartyComponent;
            TextObject answer = new("{=rf_lw_prisoner_answer_text}We are taking {COUNT} prisoners to {DESTINATION}.");
            answer.SetTextVariable("COUNT", party.PrisonRoster.TotalManCount);
            answer.SetTextVariable("DESTINATION", component.Destination?.Name ?? new TextObject("{=rf_lw_unknown}unknown"));
            MBTextManager.SetTextVariable("RF_LW_PRISONER_TEXT", answer);
        }

        private static bool CanGiveDowryGift() => IsConversationType(LivingWorldPartyType.DowryProcession)
            && Hero.MainHero.Gold >= DowryGift;

        private static void GiveDowryGift()
        {
            if (CanGiveDowryGift())
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, DowryGift, true);
            }
        }
    }
}

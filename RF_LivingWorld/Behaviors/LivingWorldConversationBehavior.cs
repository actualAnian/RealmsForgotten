using System;
using RealmsForgotten.WorldState.Refugees;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RF_LivingWorld
{
    public sealed class LivingWorldConversationBehavior : CampaignBehaviorBase
    {
        private const int ProvisionPrice = 100;
        // Piso do preco do animal e valor de emergencia quando o item nao tem valor no XML.
        private const int AnimalPrice = 75;
        private const int MinimumAnimalPrice = 25;
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
            // --- negociacao do animal: o jogador pergunta, o pastor decide se vende e cota
            //     o preco, e so entao vem o aceite. Ver AskAboutAnimal mais abaixo.
            starter.AddPlayerLine(
                "rf_lw_ask_animal", "rf_lw_hub", "rf_lw_animal_answer",
                "{=rf_lw_ask_animal}Would you part with one of your animals?",
                CanAskAboutAnimal, AskAboutAnimal);
            starter.AddDialogLine(
                "rf_lw_animal_refuse", "rf_lw_animal_answer", "rf_lw_hub",
                "{=rf_lw_animal_refuse}{RF_LW_ANIMAL_ANSWER}",
                HerderRefusesToSell, null);
            starter.AddDialogLine(
                "rf_lw_animal_offer", "rf_lw_animal_answer", "rf_lw_animal_deal",
                "{=rf_lw_animal_offer}{RF_LW_ANIMAL_ANSWER}",
                HerderMadeAnOffer, null);
            starter.AddPlayerLine(
                "rf_lw_animal_haggle", "rf_lw_animal_deal", "rf_lw_animal_haggle_answer",
                "{=rf_lw_animal_haggle}That is more than the beast is worth. I say {RF_LW_ANIMAL_COUNTER} denars.",
                CanHaggleForAnimal, HaggleForAnimal);
            starter.AddDialogLine(
                "rf_lw_animal_haggle_answer", "rf_lw_animal_haggle_answer", "rf_lw_animal_deal",
                "{=rf_lw_animal_haggle_answer}{RF_LW_ANIMAL_ANSWER}",
                null, null);
            starter.AddPlayerLine(
                "rf_lw_animal_accept", "rf_lw_animal_deal", "rf_lw_purchase_done",
                "{=rf_lw_animal_accept}Done. {RF_LW_ANIMAL_PRICE} denars, and I will take her now.",
                CanAffordOffer, BuyAnimal);
            starter.AddPlayerLine(
                "rf_lw_animal_no_coin", "rf_lw_animal_deal", "rf_lw_animal_declined",
                "{=rf_lw_animal_no_coin}I do not carry that kind of coin. Another time.",
                CannotAffordOffer, ClearOffer);
            starter.AddPlayerLine(
                "rf_lw_animal_decline", "rf_lw_animal_deal", "rf_lw_animal_declined",
                "{=rf_lw_animal_decline}Keep her. The road is long enough without a beast in tow.",
                CanAffordOffer, ClearOffer);
            starter.AddDialogLine(
                "rf_lw_animal_declined", "rf_lw_animal_declined", "rf_lw_hub",
                "{=rf_lw_animal_declined}As you like. She walks with us, then.",
                null, null);
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
                "{=rf_lw_purchase_done}{RF_LW_PURCHASE_TEXT}",
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

            // Rede de seguranca: se por algum motivo a transacao nao chegar a acontecer, a
            // fala de fechamento ainda tem texto em vez de sair vazia.
            MBTextManager.SetTextVariable("RF_LW_PURCHASE_TEXT",
                new TextObject("{=rf_lw_purchase_generic}A fair bargain. May it serve you well.").ToString());
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

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, ProvisionPrice, disableNotification: false);
            party.ItemRoster.AddToCounts(DefaultItems.Grain, -ProvisionCount);
            MobileParty.MainParty.ItemRoster.AddToCounts(DefaultItems.Grain, ProvisionCount);
            AnnouncePurchase(DefaultItems.Grain, ProvisionCount, ProvisionPrice);
        }

        // ------------------------------------------------------------ negocio do animal
        //
        // O fluxo antigo era uma linha unica ("I will buy one animal for 75 denars") que
        // debitava o ouro em silencio. Agora o jogador pergunta, o pastor decide se vende,
        // poe um preco vindo do valor real do bicho e da folga do rebanho, e ainda da para
        // pechinchar uma vez usando Comercio.

        private static MobileParty _offerParty;
        private static ItemObject _offerAnimal;
        private static int _offerPrice;
        private static bool _offerHaggled;

        private static bool CanAskAboutAnimal()
        {
            MobileParty party = MobileParty.ConversationParty;
            if (party?.PartyComponent is not LivingWorldPartyComponent component
                || component.PartyType != LivingWorldPartyType.Herder)
            {
                return false;
            }

            ItemObject animal = MBObjectManager.Instance?.GetObject<ItemObject>(LivingWorldPartyComponent.HerdItemId(component.HerdVariant));
            return animal != null && party.ItemRoster.GetItemNumber(animal) > 0;
        }

        /// <summary>
        /// Monta a oferta: com quem, qual bicho, quanto custa e se o pastor topa vender.
        /// Roda quando o jogador faz a pergunta, antes de a resposta do pastor ser escolhida.
        /// </summary>
        private static void AskAboutAnimal()
        {
            ClearOffer();

            MobileParty party = MobileParty.ConversationParty;
            if (party?.PartyComponent is not LivingWorldPartyComponent component)
            {
                return;
            }

            ItemObject animal = MBObjectManager.Instance?.GetObject<ItemObject>(LivingWorldPartyComponent.HerdItemId(component.HerdVariant));
            if (animal == null)
            {
                return;
            }

            int herdSize = party.ItemRoster.GetItemNumber(animal);
            TextObject destination = component.Destination?.Name ?? new TextObject("{=rf_lw_market}the market");

            // Rebanho no fim: o pastor nao vende, ele ainda precisa entregar o que sobrou.
            if (herdSize <= 1)
            {
                TextObject refusal = new("{=rf_lw_animal_refusal}She is the last of them, and {DESTINATION} is expecting a herd, not a story. Not this one, traveller.");
                refusal.SetTextVariable("DESTINATION", destination);
                MBTextManager.SetTextVariable("RF_LW_ANIMAL_ANSWER", refusal.ToString());
                return;
            }

            _offerParty = party;
            _offerAnimal = animal;
            _offerPrice = QuotePrice(animal, herdSize);
            _offerHaggled = false;

            TextObject offer = herdSize <= 3
                ? new TextObject("{=rf_lw_animal_offer_scarce}I could, but the herd is thin this season and every head is spoken for. A good {ANIMAL} would cost you {PRICE} denars.")
                : new TextObject("{=rf_lw_animal_offer_plenty}Aye, the herd can spare one. A sound {ANIMAL}, {PRICE} denars, and she is yours before we reach {DESTINATION}.");
            offer.SetTextVariable("ANIMAL", animal.Name);
            offer.SetTextVariable("PRICE", _offerPrice);
            offer.SetTextVariable("DESTINATION", destination);

            PublishOffer(offer);
        }

        /// <summary>
        /// Preco do bicho: valor real do item com a margem do pastor, ajustado pela folga do
        /// rebanho. Rebanho curto encarece, rebanho grande alivia.
        ///
        /// Antes era 75 denares fixos para qualquer animal — o que fazia uma vaca (valor 200)
        /// sair de graca e um porco (valor 60) sair caro. Agora porco, ovelha, mula e vaca
        /// custam coisas diferentes, como devem.
        /// </summary>
        private static int QuotePrice(ItemObject animal, int herdSize)
        {
            float price = animal.Value > 0 ? animal.Value * 1.15f : AnimalPrice;

            if (herdSize <= 3)
            {
                price *= 1.3f;
            }
            else if (herdSize >= 12)
            {
                price *= 0.9f;
            }

            return Math.Max(MinimumAnimalPrice, (int)Math.Round(price));
        }

        private static bool HerderMadeAnOffer() => IsOfferValid();

        private static bool HerderRefusesToSell() => !IsOfferValid();

        private static bool IsOfferValid()
            => _offerAnimal != null
               && _offerParty != null
               && _offerParty == MobileParty.ConversationParty
               && _offerParty.ItemRoster.GetItemNumber(_offerAnimal) > 0;

        private static bool CanAffordOffer() => IsOfferValid() && Hero.MainHero.Gold >= _offerPrice;

        private static bool CannotAffordOffer() => IsOfferValid() && Hero.MainHero.Gold < _offerPrice;

        private static bool CanHaggleForAnimal()
        {
            if (!IsOfferValid() || _offerHaggled)
            {
                return false;
            }

            MBTextManager.SetTextVariable("RF_LW_ANIMAL_COUNTER", CounterOffer());
            return true;
        }

        private static int CounterOffer() => Math.Max(1, (int)Math.Round(_offerPrice * 0.8f));

        /// <summary>
        /// Uma pechincha por oferta, decidida por Comercio. Sucesso derruba o preco para a
        /// contraproposta; fracasso mantem o preco (o pastor nao se ofende, so nao cede).
        /// </summary>
        private static void HaggleForAnimal()
        {
            if (!IsOfferValid())
            {
                return;
            }

            _offerHaggled = true;

            int counter = CounterOffer();
            int trade = Hero.MainHero.GetSkillValue(DefaultSkills.Trade);
            float chance = MBMath.ClampFloat(0.15f + trade / 500f, 0.15f, 0.8f);
            bool convinced = MBRandom.RandomFloat < chance;

            TextObject answer;
            if (convinced)
            {
                _offerPrice = counter;
                answer = new TextObject("{=rf_lw_haggle_win}Hah. You have handled livestock before, that much is plain. {PRICE} denars, and I will hear no more of it.");
                Hero.MainHero.AddSkillXp(DefaultSkills.Trade, 15f);
            }
            else
            {
                answer = new TextObject("{=rf_lw_haggle_fail}She has walked further than you have, traveller. {PRICE} denars is my price, take it or leave it.");
                Hero.MainHero.AddSkillXp(DefaultSkills.Trade, 5f);
            }

            answer.SetTextVariable("PRICE", _offerPrice);
            PublishOffer(answer);
        }

        private static void BuyAnimal()
        {
            if (!CanAffordOffer())
            {
                return;
            }

            MobileParty party = _offerParty;
            ItemObject animal = _offerAnimal;
            int price = _offerPrice;

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, price, disableNotification: false);
            party.ItemRoster.AddToCounts(animal, -1);
            MobileParty.MainParty.ItemRoster.AddToCounts(animal, 1);
            AnnouncePurchase(animal, 1, price);
            ClearOffer();
        }

        /// <summary>Publica a fala do pastor e deixa o preco vigente visivel para as opcoes.</summary>
        private static void PublishOffer(TextObject line)
        {
            MBTextManager.SetTextVariable("RF_LW_ANIMAL_ANSWER", line.ToString());
            MBTextManager.SetTextVariable("RF_LW_ANIMAL", _offerAnimal?.Name ?? new TextObject("{=rf_lw_beast}beast"));
            MBTextManager.SetTextVariable("RF_LW_ANIMAL_PRICE", _offerPrice);
        }

        private static void ClearOffer()
        {
            _offerParty = null;
            _offerAnimal = null;
            _offerPrice = 0;
            _offerHaggled = false;
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
            AnnounceDonation(DefaultItems.Grain, DonationCount);
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

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, price, disableNotification: false);
            party.ItemRoster.AddToCounts(item, -1);
            MobileParty.MainParty.ItemRoster.AddToCounts(item, 1);
            AnnouncePurchase(item, 1, price);
        }

        /// <summary>
        /// Confirmacao do negocio: a fala de fechamento passa a dizer o que mudou de mao e
        /// por quanto, e o mesmo texto sai no log de mensagens.
        ///
        /// Antes disso (relato de jogador, 2026-08-18) o jogador comprava um animal e nao
        /// via nada: o ouro saia calado porque as chamadas de GiveGoldAction passavam
        /// "true" no ultimo parametro, que e o <c>disableNotification</c>, e a fala do
        /// vendedor era um "A fair bargain" generico sem valor nenhum.
        /// </summary>
        private static void AnnouncePurchase(ItemObject item, int count, int price)
        {
            TextObject line = new("{=rf_lw_purchase_summary}{COUNT} {ITEM} for {PRICE} denars. A fair bargain, may it serve you well.");
            line.SetTextVariable("COUNT", count);
            line.SetTextVariable("ITEM", item?.Name ?? new TextObject("{=rf_lw_goods}goods"));
            line.SetTextVariable("PRICE", price);
            Announce(line);
        }

        private static void AnnounceDonation(ItemObject item, int count)
        {
            TextObject line = new("{=rf_lw_donation_summary}{COUNT} {ITEM} handed over. You have our thanks, traveller.");
            line.SetTextVariable("COUNT", count);
            line.SetTextVariable("ITEM", item?.Name ?? new TextObject("{=rf_lw_goods}goods"));
            Announce(line);
        }

        private static void AnnounceGift(int amount)
        {
            TextObject line = new("{=rf_lw_gift_summary}{PRICE} denars for the wedding. The couple will hear of your name.");
            line.SetTextVariable("PRICE", amount);
            Announce(line);
        }

        private static void Announce(TextObject line)
        {
            string text = line.ToString();
            MBTextManager.SetTextVariable("RF_LW_PURCHASE_TEXT", text);
            InformationManager.DisplayMessage(new InformationMessage(text));
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
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, DowryGift, disableNotification: false);
                AnnounceGift(DowryGift);
            }
        }
    }
}

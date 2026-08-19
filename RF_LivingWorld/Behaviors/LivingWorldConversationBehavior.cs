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

            // --- negociacao unificada: pastor (animal), mercador (provisoes), cacador
            //     (carne) e curandeiro (plantas). O jogador pergunta, o vendedor decide se
            //     vende e cota o preco (valor real do item + folga do estoque), da para
            //     pechinchar uma vez por Comercio, e so entao vem o aceite.
            starter.AddPlayerLine(
                "rf_lw_ask_animal", "rf_lw_hub", "rf_lw_offer_answer",
                "{=rf_lw_ask_animal}Would you part with one of your animals?",
                CanAskAnimal, AskAnimal);
            starter.AddPlayerLine(
                "rf_lw_ask_provisions", "rf_lw_hub", "rf_lw_offer_answer",
                "{=rf_lw_ask_provisions}Could you spare some provisions from your stores?",
                CanAskProvisions, AskProvisions);
            starter.AddPlayerLine(
                "rf_lw_ask_meat", "rf_lw_hub", "rf_lw_offer_answer",
                "{=rf_lw_ask_meat}That is fresh game you carry. Would you sell a portion?",
                CanAskMeat, AskMeat);
            starter.AddPlayerLine(
                "rf_lw_ask_plants", "rf_lw_hub", "rf_lw_offer_answer",
                "{=rf_lw_ask_plants}I have wounded to tend. Would you sell some of your remedies?",
                CanAskPlants, AskPlants);
            starter.AddDialogLine(
                "rf_lw_offer_refuse", "rf_lw_offer_answer", "rf_lw_hub",
                "{=rf_lw_offer_refuse}{RF_LW_OFFER_ANSWER}",
                SellerRefused, null);
            starter.AddDialogLine(
                "rf_lw_offer_made", "rf_lw_offer_answer", "rf_lw_offer_deal",
                "{=rf_lw_offer_made}{RF_LW_OFFER_ANSWER}",
                SellerMadeAnOffer, null);
            starter.AddPlayerLine(
                "rf_lw_offer_haggle", "rf_lw_offer_deal", "rf_lw_offer_haggle_answer",
                "{=rf_lw_offer_haggle}That is steep for goods on the road. I say {RF_LW_OFFER_COUNTER} denars.",
                CanHaggle, Haggle);
            starter.AddDialogLine(
                "rf_lw_offer_haggle_answer", "rf_lw_offer_haggle_answer", "rf_lw_offer_deal",
                "{=rf_lw_offer_haggle_answer}{RF_LW_OFFER_ANSWER}",
                null, null);
            starter.AddPlayerLine(
                "rf_lw_offer_accept", "rf_lw_offer_deal", "rf_lw_purchase_done",
                "{=rf_lw_offer_accept}Done. {RF_LW_OFFER_PRICE} denars.",
                CanAffordOffer, AcceptOffer);
            starter.AddPlayerLine(
                "rf_lw_offer_no_coin", "rf_lw_offer_deal", "rf_lw_offer_declined",
                "{=rf_lw_offer_no_coin}I do not carry that kind of coin. Another time.",
                CannotAffordOffer, ClearOffer);
            starter.AddPlayerLine(
                "rf_lw_offer_decline", "rf_lw_offer_deal", "rf_lw_offer_declined",
                "{=rf_lw_offer_decline}Keep it. The price does not suit me.",
                CanAffordOffer, ClearOffer);
            starter.AddDialogLine(
                "rf_lw_offer_declined", "rf_lw_offer_declined", "rf_lw_hub",
                "{=rf_lw_offer_declined}As you like, traveller. The road provides for those who pay.",
                null, null);
            starter.AddPlayerLine(
                "rf_lw_donate_grain", "rf_lw_hub", "rf_lw_purchase_done",
                "{=rf_lw_donate_grain}Take two sacks of grain for the road.",
                CanDonateGrain, DonateGrain);
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

        // ------------------------------------------------------- negociacao unificada
        //
        // Uma oferta por vez: com quem, qual item, quantos, preco cotado (valor real do
        // item + margem + folga do estoque, nunca abaixo do piso da categoria) e se a
        // pechincha ja foi usada. Cada vendedor tem so a pergunta e o texto proprios.

        private static MobileParty _offerParty;
        private static ItemObject _offerItem;
        private static int _offerCount;
        private static int _offerPrice;
        private static bool _offerHaggled;

        private static bool IsSellerOfType(LivingWorldPartyType type, string itemId, int minStock, out MobileParty party, out ItemObject item)
        {
            party = MobileParty.ConversationParty;
            item = null;
            if (party?.PartyComponent is not LivingWorldPartyComponent component || component.PartyType != type)
            {
                return false;
            }
            item = MBObjectManager.Instance?.GetObject<ItemObject>(itemId);
            return item != null && party.ItemRoster.GetItemNumber(item) >= minStock;
        }

        private static int QuotePrice(ItemObject item, int count, int stock, int floorPrice)
        {
            float price = item.Value > 0 ? item.Value * count * 1.15f : floorPrice;

            if (stock <= count * 2)
            {
                price *= 1.3f;      // estoque curto encarece
            }
            else if (stock >= count * 6)
            {
                price *= 0.9f;      // fartura alivia
            }

            return Math.Max(floorPrice, (int)Math.Round(price));
        }

        private static void OpenOffer(MobileParty party, ItemObject item, int count, int floorPrice, TextObject offerLine)
        {
            _offerParty = party;
            _offerItem = item;
            _offerCount = count;
            _offerPrice = QuotePrice(item, count, party.ItemRoster.GetItemNumber(item), floorPrice);
            _offerHaggled = false;

            offerLine.SetTextVariable("ITEM", item.Name);
            offerLine.SetTextVariable("COUNT", count);
            offerLine.SetTextVariable("PRICE", _offerPrice);
            PublishOffer(offerLine);
        }

        private static void Refuse(TextObject line)
        {
            ClearOffer();
            MBTextManager.SetTextVariable("RF_LW_OFFER_ANSWER", line.ToString());
        }

        // ------------------------------------------------------------ os 4 vendedores

        private static bool CanAskAnimal()
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

        private static void AskAnimal()
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
                Refuse(refusal);
                return;
            }

            TextObject offer = herdSize <= 3
                ? new TextObject("{=rf_lw_animal_offer_scarce}I could, but the herd is thin this season and every head is spoken for. A good {ITEM} would cost you {PRICE} denars.")
                : new TextObject("{=rf_lw_animal_offer_plenty}Aye, the herd can spare one. A sound {ITEM}, {PRICE} denars, and she is yours before we reach {DESTINATION}.");
            offer.SetTextVariable("DESTINATION", destination);
            OpenOffer(party, animal, 1, AnimalPrice, offer);
        }

        private static bool CanAskProvisions()
            => IsSellerOfType(LivingWorldPartyType.Merchant, "grain", 1, out _, out _);

        private static void AskProvisions()
        {
            ClearOffer();
            if (!IsSellerOfType(LivingWorldPartyType.Merchant, "grain", 1, out MobileParty party, out ItemObject grain))
            {
                return;
            }

            int stock = party.ItemRoster.GetItemNumber(grain);
            if (stock < ProvisionCount)
            {
                Refuse(new TextObject("{=rf_lw_provisions_refusal}Barely enough left for our own bellies, traveller. Try the next caravan."));
                return;
            }

            OpenOffer(party, grain, ProvisionCount, ProvisionPrice,
                new TextObject("{=rf_lw_provisions_offer}We trade where the great caravans will not - and that has a price. {COUNT} sacks of {ITEM}, {PRICE} denars."));
        }

        private static bool CanAskMeat()
            => IsSellerOfType(LivingWorldPartyType.Hunter, "meat", 1, out _, out _);

        private static void AskMeat()
        {
            ClearOffer();
            if (!IsSellerOfType(LivingWorldPartyType.Hunter, "meat", 1, out MobileParty party, out ItemObject meat))
            {
                return;
            }

            OpenOffer(party, meat, 1, MeatPrice,
                new TextObject("{=rf_lw_meat_offer}Tracked since dawn and dressed by my own knife. A portion of {ITEM} for {PRICE} denars."));
        }

        private static bool CanAskPlants()
            => IsSellerOfType(LivingWorldPartyType.Healer, "medicinal_plants", 1, out _, out _);

        private static void AskPlants()
        {
            ClearOffer();
            if (!IsSellerOfType(LivingWorldPartyType.Healer, "medicinal_plants", 1, out MobileParty party, out ItemObject plants))
            {
                return;
            }

            OpenOffer(party, plants, 1, MedicinalPlantsPrice,
                new TextObject("{=rf_lw_plants_offer}Gathered where the roads do not go, and worth every step. {ITEM} for {PRICE} denars."));
        }

        // ------------------------------------------------------------------ fluxo comum

        private static bool SellerMadeAnOffer() => IsOfferValid();

        private static bool SellerRefused() => !IsOfferValid();

        private static bool IsOfferValid()
            => _offerItem != null
               && _offerParty != null
               && _offerParty == MobileParty.ConversationParty
               && _offerParty.ItemRoster.GetItemNumber(_offerItem) >= _offerCount;

        private static bool CanAffordOffer() => IsOfferValid() && Hero.MainHero.Gold >= _offerPrice;

        private static bool CannotAffordOffer() => IsOfferValid() && Hero.MainHero.Gold < _offerPrice;

        private static bool CanHaggle()
        {
            if (!IsOfferValid() || _offerHaggled)
            {
                return false;
            }

            MBTextManager.SetTextVariable("RF_LW_OFFER_COUNTER", CounterOffer());
            return true;
        }

        private static int CounterOffer() => Math.Max(1, (int)Math.Round(_offerPrice * 0.8f));

        /// <summary>
        /// Uma pechincha por oferta, decidida por Comercio. Sucesso derruba o preco para a
        /// contraproposta; fracasso mantem o preco (o vendedor nao se ofende, so nao cede).
        /// </summary>
        private static void Haggle()
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
                answer = new TextObject("{=rf_lw_haggle_win}Hah. You bargain like one of us, traveller. {PRICE} denars, and I will hear no more of it.");
                Hero.MainHero.AddSkillXp(DefaultSkills.Trade, 15f);
            }
            else
            {
                answer = new TextObject("{=rf_lw_haggle_fail}The road sets my prices, not you. {PRICE} denars, take it or leave it.");
                Hero.MainHero.AddSkillXp(DefaultSkills.Trade, 5f);
            }

            answer.SetTextVariable("PRICE", _offerPrice);
            PublishOffer(answer);
        }

        private static void AcceptOffer()
        {
            if (!CanAffordOffer())
            {
                return;
            }

            MobileParty party = _offerParty;
            ItemObject item = _offerItem;
            int count = _offerCount;
            int price = _offerPrice;

            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, price, disableNotification: false);
            party.ItemRoster.AddToCounts(item, -count);
            MobileParty.MainParty.ItemRoster.AddToCounts(item, count);
            AnnouncePurchase(item, count, price);
            ClearOffer();
        }

        /// <summary>Publica a fala do vendedor e deixa item/preco visiveis para as opcoes.</summary>
        private static void PublishOffer(TextObject line)
        {
            MBTextManager.SetTextVariable("RF_LW_OFFER_ANSWER", line.ToString());
            MBTextManager.SetTextVariable("RF_LW_OFFER_ITEM", _offerItem?.Name ?? new TextObject("{=rf_lw_goods}goods"));
            MBTextManager.SetTextVariable("RF_LW_OFFER_PRICE", _offerPrice);
        }

        private static void ClearOffer()
        {
            _offerParty = null;
            _offerItem = null;
            _offerCount = 0;
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

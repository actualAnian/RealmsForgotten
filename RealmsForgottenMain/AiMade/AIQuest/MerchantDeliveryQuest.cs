using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using RealmsForgotten.AiMade;
using Helpers;

namespace RealmsForgotten.AiMade.AIQuest
{
    public class MerchantDeliveryQuest : QuestBase
    {
        [SaveableField(0)]
        private Settlement _destination;

        [SaveableField(1)]
        private CampaignTime _questStartTime;

        [SaveableField(2)]
        private string _caravanPartyId;

        [SaveableField(3)]
        private bool _mercenariesSpawned = false;

        private const int CargoAmount = 20;
        private const int RewardGold = 2000;

        private MobileParty CaravanParty =>
            !string.IsNullOrEmpty(_caravanPartyId)
                ? MobileParty.All.FirstOrDefault(p => p.StringId == _caravanPartyId)
                : null;

        public MerchantDeliveryQuest(string questId, Hero questGiver, CampaignTime duration, Settlement destination)
            : base(questId, questGiver, CampaignTime.Now + duration, RewardGold)
        {
            _destination = destination;
            InitializeQuestOnCreation();
        }

        protected override void OnStartQuest()
        {
            _questStartTime = CampaignTime.Now;

            CreateCaravanEscort();
            AddCargoToPlayer();

            AddDiscreteLog(
                new TextObject("Deliver Merchant Goods"),
                new TextObject($"Deliver the merchant's goods safely to {(_destination != null ? _destination.Name.ToString() : "Unknown destination")}."),

                0, 1
            );

            InformationManager.DisplayMessage(new InformationMessage("🚚 You are now escorting the merchant's goods!", Colors.Yellow));
        }

        protected override void HourlyTick()
        {
            var party = CaravanParty;

            if (party == null || !party.IsActive)
            {
                FailQuest("❌ The caravan was destroyed!");
                return;
            }

            if (_destination == null)
            {
                FailQuest("❌ Quest destination data was lost.");
                return;
            }

            float distanceToDestination = MobileParty.MainParty?.Position.Distance(_destination.Position) ?? float.MaxValue;

            if (!_mercenariesSpawned && distanceToDestination < 10f)
            {
                SpawnMercenaries();
                _mercenariesSpawned = true;
            }

            if (distanceToDestination < 5f)
            {
                CompleteDelivery();
            }
        }

        protected override void OnTimedOut()
        {
            FailQuest("⏳ You took too long to deliver the merchant's goods!");
        }

        protected override void OnFinalize()
        {
            var party = CaravanParty;
            if (party != null && party.IsActive)
                DestroyPartyAction.Apply(null, party);
        }

        private void CompleteDelivery()
        {
            RemoveCargoFromPlayer();

            var party = CaravanParty;
            if (party != null && party.IsActive)
                DestroyPartyAction.Apply(null, party);

            CompleteQuestWithSuccess();
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);

            InformationManager.DisplayMessage(new InformationMessage($"✅ You successfully delivered the merchant's goods and earned {RewardGold} gold!", Colors.Green));
        }

        private void CreateCaravanEscort()
        {
            var template = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("escort_caravan");

            if (template == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: escort_caravan template not found!", Colors.Red));
                return;
            }

            CampaignVec2 spawnPos = MobileParty.MainParty.Position + new Vec2(2f, 2f);

            string partyId = $"merchant_caravan_escort_{MBRandom.RandomInt(100000, 999999)}";
            MobileParty party = MobileParty.CreateParty(partyId, null);
            _caravanPartyId = partyId;

            party.InitializeMobilePartyAroundPosition(template, spawnPos, 1f);
            party.Party.SetCustomName(new TextObject("Merchant Caravan"));
            party.IsVisible = true;
            party.Ai?.SetDoNotMakeNewDecisions(true);
            party.IsActive = true;

            MobileParty.MainParty?.AttachedParties.Add(party);
        }

        private void AddCargoToPlayer()
        {
            ItemObject cargo = MBObjectManager.Instance.GetObject<ItemObject>("stolen_goods");
            if (cargo != null)
            {
                PartyBase.MainParty.ItemRoster.AddToCounts(cargo, CargoAmount);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Failed to find 'stolen_goods' item!", Colors.Red));
            }
        }

        private void RemoveCargoFromPlayer()
        {
            ItemObject cargo = MBObjectManager.Instance.GetObject<ItemObject>("stolen_goods");
            if (cargo != null)
            {
                PartyBase.MainParty.ItemRoster.AddToCounts(cargo, -CargoAmount);
            }
        }

        private void SpawnMercenaries()
        {
            CharacterObject looter = CharacterObject.Find("looter");
            if (looter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Looter not found!", Colors.Red));
                return;
            }

            Clan looterClan = Clan.BanditFactions.FirstOrDefault(c => c.StringId == "looters");
            if (looterClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Looter clan not found!", Colors.Red));
                return;
            }

            PartyTemplateObject looterTemplate = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("looters_template");
            if (looterTemplate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("ERROR: Looter template not found!", Colors.Red));
                return;
            }

            CampaignVec2 spawnPos = MobileParty.MainParty?.Position + new Vec2(3f, 3f) ?? CampaignVec2.Zero;

            MobileParty mercenaryParty = BanditPartyComponent.CreateLooterParty("mercenary_attack_party", looterClan, SettlementHelper.FindNearestSettlementToPoint(spawnPos), false, looterTemplate, spawnPos);
            mercenaryParty.InitializeMobilePartyAroundPosition(looterTemplate, spawnPos, 1f);

            mercenaryParty.MemberRoster.AddToCounts(looter, 25);
            mercenaryParty.Party.SetCustomName(new TextObject("Mercenary Raiders"));
            mercenaryParty.IsVisible = true;

            if (CaravanParty != null && CaravanParty.IsActive && mercenaryParty.Ai != null)
            {
                mercenaryParty.SetMoveEngageParty(CaravanParty, MobileParty.NavigationType.All);
                mercenaryParty.Ai.SetDoNotMakeNewDecisions(true);
            }

            InformationManager.DisplayMessage(new InformationMessage("⚔️ Mercenaries are attacking your caravan!", Colors.Red));
        }

        private void FailQuest(string reason)
        {
            var party = CaravanParty;
            if (party != null && party.IsActive)
                DestroyPartyAction.Apply(null, party);

            CompleteQuestWithFail();
            InformationManager.DisplayMessage(new InformationMessage(reason, Colors.Red));
        }

        public override TextObject Title => new TextObject("{=MerchantDeliveryQuestTitle}Merchant Delivery Quest");
        public override bool IsSpecialQuest => false;
        public override bool IsRemainingTimeHidden => false;

        protected override void SetDialogs() { }
        protected override void InitializeQuestOnGameLoad() { }
    }
}
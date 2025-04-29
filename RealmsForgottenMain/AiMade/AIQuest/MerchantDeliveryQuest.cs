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

namespace RealmsForgotten.AiMade.AIQuest
{
    public class MerchantDeliveryQuest : QuestBase
    {
        [SaveableField(0)]
        private Settlement _destination;

        [SaveableField(1)]
        private CampaignTime _questStartTime;

        private MobileParty _caravanParty;
        private bool _mercenariesSpawned = false;

        private const int CargoAmount = 20;
        private const int RewardGold = 2000;

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
                new TextObject($"Deliver the merchant's goods safely to {_destination.Name}."),
                0, 1
            );

            InformationManager.DisplayMessage(new InformationMessage("🚚 You are now escorting the merchant's goods!", Colors.Yellow));
        }

        protected override void OnTimedOut()
        {
            FailQuest("⏳ You took too long to deliver the merchant's goods!");
        }

        protected override void OnFinalize()
        {
            if (_caravanParty == null || !_caravanParty.IsActive)
            {
                _caravanParty.RemoveParty();
            }
        }

        protected override void HourlyTick()
        {
            if (_caravanParty == null || !_caravanParty.IsActive)
            {
                FailQuest("❌ The caravan was destroyed!");
                return;
            }

            float distanceToDestination = MobileParty.MainParty.Position2D.Distance(_destination.Position2D);

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

        private void CompleteDelivery()
        {
            RemoveCargoFromPlayer();

            if (_caravanParty == null || !_caravanParty.IsActive)
            {
                _caravanParty.RemoveParty();
            }

            CompleteQuestWithSuccess();
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, RewardGold);

            InformationManager.DisplayMessage(new InformationMessage($"✅ You successfully delivered the merchant's goods and earned {RewardGold} gold!", Colors.Green));
        }

        private void CreateCaravanEscort()
        {
            var template = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("escort_caravan");

            Vec2 spawnPos = MobileParty.MainParty.Position2D + new Vec2(2f, 2f);

            _caravanParty = MobileParty.CreateParty("merchant_caravan_escort", null);
            _caravanParty.InitializeMobilePartyAroundPosition(template, spawnPos, 1f);
            _caravanParty.SetCustomName(new TextObject("Merchant Caravan"));
            _caravanParty.IsVisible = true;
            _caravanParty.Ai.SetDoNotMakeNewDecisions(true);

            MobileParty.MainParty.AttachedParties.Add(_caravanParty);
            _caravanParty.IsActive = true;
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

            Vec2 spawnPos = MobileParty.MainParty.Position2D + new Vec2(3f, 3f);

            MobileParty mercenaryParty = BanditPartyComponent.CreateLooterParty("mercenary_attack_party", looterClan, null, false);
            mercenaryParty.InitializeMobilePartyAroundPosition(looterTemplate, spawnPos, 1f);

            mercenaryParty.MemberRoster.AddToCounts(looter, 25);
            mercenaryParty.SetCustomName(new TextObject("Mercenary Raiders"));
            mercenaryParty.IsVisible = true;

            mercenaryParty.Ai.SetMoveEngageParty(_caravanParty);
            mercenaryParty.Ai.SetDoNotMakeNewDecisions(true);

            InformationManager.DisplayMessage(new InformationMessage("⚔️ Mercenaries are attacking your caravan!", Colors.Red));
        }

        private void FailQuest(string reason)
        {
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
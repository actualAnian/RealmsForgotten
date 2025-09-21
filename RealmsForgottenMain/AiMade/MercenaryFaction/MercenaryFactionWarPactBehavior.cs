using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.MercenaryFaction
{
    internal class MercenaryFactionWarPactBehavior : CampaignBehaviorBase
    {
        // --- CONFIGURATION ---
        private const int ContractUpfrontFee = 75000;
        private const int ContractDailyCost = 8000;
        private const int ContractDurationDays = 30;

        private static readonly Dictionary<string, string> KingdomHqMap = new()
        {
            { "town_KTG5", "katogai" },
            { "town_CB7", "valthorne" },
            { "town_THM3", "tharnmar" },
        };

        // --- STATE TRACKING ---
        private Kingdom _contractedKingdom;
        private CampaignTime _contractEndDate;
        private List<IFaction> _playerEnemiesAtSigning;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_contractedKingdom", ref _contractedKingdom);
            dataStore.SyncData("_contractEndDate", ref _contractEndDate);
            dataStore.SyncData("_playerEnemiesAtSigning", ref _playerEnemiesAtSigning);
        }

        private void OnGameLoaded(CampaignGameStarter starter)
        {
            if (_playerEnemiesAtSigning == null)
            {
                _playerEnemiesAtSigning = new List<IFaction>();
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "contract_kingdom_root", "Contract Kingdom for War",
                args => CanSignWarContract(args),
                args => ShowContractInquiry(),
                isLeave: false);
        }

        private bool CanSignWarContract(MenuCallbackArgs args)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            // FIX: The property is named 'IsEnabled', not 'optionIsEnabled'.
            args.IsEnabled = false;

            if (currentSettlement != null && KingdomHqMap.TryGetValue(currentSettlement.StringId, out string kingdomId))
            {
                Kingdom kingdomForHire = Kingdom.All.FirstOrDefault(k => k.StringId == kingdomId);

                if (kingdomForHire != null)
                {
                    // FIX: 'IsAtWar' doesn't exist. We check the 'Stances' collection instead.
                    bool isPlayerAtWar = Hero.MainHero.MapFaction != null && Hero.MainHero.MapFaction.FactionsAtWarWith.Count > 0;

                    args.IsEnabled = isPlayerAtWar && _contractedKingdom == null && kingdomForHire != Hero.MainHero.MapFaction;

                    if (!isPlayerAtWar) args.Tooltip = new TextObject("You must be at war to sign this contract.");
                    else if (_contractedKingdom != null) args.Tooltip = new TextObject("You already have an active kingdom contract.");
                    else if (kingdomForHire == Hero.MainHero.MapFaction) args.Tooltip = new TextObject("You cannot hire your own kingdom.");
                }
            }
            return true;
        }

        private void ShowContractInquiry()
        {
            Kingdom kingdomForHire = Kingdom.All.FirstOrDefault(k => k.StringId == KingdomHqMap[Settlement.CurrentSettlement.StringId]);

            TextObject title = new TextObject("War Contract Proposal");
            TextObject text = new TextObject("The ruler of {KINGDOM_NAME} offers their armies to your cause. For an upfront fee of {UPFRONT_FEE}{GOLD_ICON} and a daily tribute of {DAILY_COST}{GOLD_ICON}, their entire kingdom will declare war on your enemies for {DURATION} days. Do you accept this alliance?");
            text.SetTextVariable("KINGDOM_NAME", kingdomForHire.Name);
            text.SetTextVariable("UPFRONT_FEE", ContractUpfrontFee);
            text.SetTextVariable("DAILY_COST", ContractDailyCost);
            text.SetTextVariable("DURATION", ContractDurationDays);

            InformationManager.ShowInquiry(new InquiryData(title.ToString(), text.ToString(),
                true, true, "Sign Contract", "Decline",
                () => ActivateContract(kingdomForHire), null));
        }

        private void ActivateContract(Kingdom kingdomForHire)
        {
            if (Hero.MainHero.Gold < ContractUpfrontFee)
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford the upfront fee.", Colors.Red));
                return;
            }

            Hero.MainHero.ChangeHeroGold(-ContractUpfrontFee);
            _contractedKingdom = kingdomForHire;
            _contractEndDate = CampaignTime.DaysFromNow(ContractDurationDays);

            _playerEnemiesAtSigning = Hero.MainHero.MapFaction.FactionsAtWarWith;

            foreach (var enemy in _playerEnemiesAtSigning)
            {
                if (!_contractedKingdom.IsAtWarWith(enemy))
                {
                    DeclareWarAction.ApplyByKingdomDecision(_contractedKingdom, enemy);
                }
            }

            // --- FIX: This is the new logic to force the AI to act ---
            // 1. Find a suitable enemy settlement to target.
            Settlement targetEnemySettlement = Settlement.All.FirstOrDefault(s => s.IsFortification && _playerEnemiesAtSigning.Contains(s.MapFaction));

            // 2. Find a suitable leader for the army (the kingdom's ruler is the best choice).
            Hero armyLeader = _contractedKingdom.Leader;

            // 3. If we have a target and the leader can lead an army, command the kingdom to create one.
            if (targetEnemySettlement != null && armyLeader != null && armyLeader.PartyBelongedTo != null && !armyLeader.IsPrisoner)
            {
                // This method exists in your Kingdom.cs file.
                _contractedKingdom.CreateArmy(armyLeader, targetEnemySettlement, Army.ArmyTypes.Patrolling);
            }

            TextObject message = new TextObject("{KINGDOM_NAME} has entered your wars and is raising an army! They will fight on your behalf for {DURATION} days.");
            message.SetTextVariable("KINGDOM_NAME", kingdomForHire.Name);
            message.SetTextVariable("DURATION", ContractDurationDays);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Green));
        }

        private void OnDailyTick()
        {
            if (_contractedKingdom == null) return;

            if (CampaignTime.Now >= _contractEndDate)
            {
                EndContract("The contract with {KINGDOM_NAME} has expired. They have returned to peace.");
                return;
            }

            if (Hero.MainHero.Gold >= ContractDailyCost)
            {
                Hero.MainHero.ChangeHeroGold(-ContractDailyCost);
            }
            else
            {
                EndContract("You can no longer afford the contract. {KINGDOM_NAME} has abandoned your cause.");
            }
        }

        private void EndContract(string message)
        {
            TextObject endMessage = new TextObject(message);
            endMessage.SetTextVariable("KINGDOM_NAME", _contractedKingdom.Name);
            InformationManager.DisplayMessage(new InformationMessage(endMessage.ToString(), Colors.Yellow));

            foreach (var enemy in _playerEnemiesAtSigning)
            {
                if (_contractedKingdom.IsAtWarWith(enemy))
                {
                    MakePeaceAction.Apply(_contractedKingdom, enemy);
                }
            }

            _contractedKingdom = null;
            _playerEnemiesAtSigning.Clear();
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public class ContractNotificationBehavior : CampaignBehaviorBase
    {
        private List<MercenaryContract> availableContracts = new List<MercenaryContract>();
        private MercenaryContract activeContract;
        private CampaignTime nextNotificationTime;
        private bool isCareerAccepted = false;

        private readonly List<string> contractOrder = new List<string>
        {
            "convert_bandits",
            "attack_caravan",
            "raid_villages",
            "defeat_lord_party"
        };
        private int currentContractIndex = 0;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            BanditConversionManager.BanditConverted += OnBanditConverted;
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            InformationManager.DisplayMessage(new InformationMessage("OnSessionLaunched called"));
            InitializeMercenaryContracts();

            var careerSelectionBehavior = Campaign.Current.GetCampaignBehavior<CareerSelectionBehavior>();
            if (careerSelectionBehavior != null && careerSelectionBehavior.GetSelectedCareerType() == "Mercenary")
            {
                StartContractNotifications();
            }
        }

        private void InitializeMercenaryContracts()
        {
            InformationManager.DisplayMessage(new InformationMessage("Initializing Mercenary Contracts"));
            availableContracts.Add(new MercenaryContract
            {
                Id = "convert_bandits",
                Name = "Convert 20 Bandits",
                Description = "Convert 20 bandits into your army.",
                Progress = 0,
                Goal = 20
            });

            availableContracts.Add(new MercenaryContract
            {
                Id = "attack_caravan",
                Name = "Attack a Merchant Caravan",
                Description = "Attack and loot a merchant caravan.",
                Progress = 0,
                Goal = 1
            });

            availableContracts.Add(new MercenaryContract
            {
                Id = "raid_villages",
                Name = "Raid 5 Villages",
                Description = "Raid 5 villages successfully.",
                Progress = 0,
                Goal = 5
            });

            availableContracts.Add(new MercenaryContract
            {
                Id = "defeat_lord_party",
                Name = "Defeat a Lord's Party",
                Description = "Defeat a lord's party in battle.",
                Progress = 0,
                Goal = 1
            });
        }

        public void StartContractNotifications()
        {
            InformationManager.DisplayMessage(new InformationMessage("Starting Contract Notifications"));
            isCareerAccepted = true;
            ScheduleNextNotification(20); // Schedule the first contract notification after 20 days
        }

        private void ScheduleNextNotification(int days)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Scheduling next notification in {days} days"));
            nextNotificationTime = CampaignTime.Now + CampaignTime.Days(days);
        }

        private void OnDailyTick()
        {
            InformationManager.DisplayMessage(new InformationMessage("Daily Tick Event Triggered"));
            if (isCareerAccepted && CampaignTime.Now >= nextNotificationTime && activeContract == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Daily tick check passed, assigning next contract"));
                AssignNextContract();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage($"Conditions not met for assigning contract: isCareerAccepted={isCareerAccepted}, now={CampaignTime.Now}, nextNotificationTime={nextNotificationTime}, activeContract={activeContract != null}"));
            }
        }

        private void AssignNextContract()
        {
            if (currentContractIndex < contractOrder.Count)
            {
                var nextContractId = contractOrder[currentContractIndex];
                activeContract = availableContracts.FirstOrDefault(c => c.Id == nextContractId);
                if (activeContract != null)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"You have received the contract: {activeContract.Name}."));
                    currentContractIndex++;
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Failed to find the next contract"));
                }
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("No more contracts available"));
            }
        }

        private void OnBanditConverted(object sender, BanditConversionEvent e)
        {
            if (activeContract != null && activeContract.Id == "convert_bandits" && !activeContract.IsCompleted)
            {
                activeContract.Progress += e.BanditCount;
                InformationManager.DisplayMessage(new InformationMessage($"Contract progress: {activeContract.Progress}/{activeContract.Goal}"));

                if (activeContract.IsCompleted)
                {
                    CompleteContract();
                }
            }
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (activeContract != null && !activeContract.IsCompleted)
            {
                if (activeContract.Id == "attack_caravan" && mapEvent.IsPlayerParticipating())
                {
                    if (mapEvent.HasCaravan() && mapEvent.WinningSide == BattleSideEnum.Attacker)
                    {
                        activeContract.Progress++;
                        InformationManager.DisplayMessage(new InformationMessage($"Contract progress: {activeContract.Progress}/{activeContract.Goal}"));

                        if (activeContract.IsCompleted)
                        {
                            CompleteContract();
                        }
                    }
                }
                else if (activeContract.Id == "defeat_lord_party" && mapEvent.IsPlayerParticipating())
                {
                    if (mapEvent.DefenderSide.Parties.Any(p => p.Party.MobileParty?.PartyComponent is LordPartyComponent) && mapEvent.WinningSide == BattleSideEnum.Attacker)
                    {
                        activeContract.Progress++;
                        InformationManager.DisplayMessage(new InformationMessage($"Contract progress: {activeContract.Progress}/{activeContract.Goal}"));

                        if (activeContract.IsCompleted)
                        {
                            CompleteContract();
                        }
                    }
                }
            }
        }

        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
        {
            if (activeContract != null && !activeContract.IsCompleted && raidEvent.MapEventSettlement.IsVillage && winnerSide == BattleSideEnum.Attacker)
            {
                activeContract.Progress++;
                InformationManager.DisplayMessage(new InformationMessage($"Contract progress: {activeContract.Progress}/{activeContract.Goal}"));
                if (activeContract.IsCompleted)
                {
                    CompleteContract();
                }
            }
        }

        private void CompleteContract()
        {
            InformationManager.DisplayMessage(new InformationMessage($"Contract completed: {activeContract.Name}"));
            GiveGoldReward();
            InformationManager.ShowInquiry(new InquiryData(
                "Contract Completed",
                $"You have successfully completed the contract: {activeContract.Name}. You receive your reward.",
                true,
                false,
                "OK",
                null,
                null,
                null
            ));
            activeContract = null;
            ScheduleNextNotification(15); // Schedule the next contract notification after 15 days
        }

        private void GiveGoldReward()
        {
            int rewardGold = 1000; // Default reward

            switch (activeContract.Id)
            {
                case "raid_villages":
                    rewardGold = 1000;
                    break;
                case "attack_caravan":
                    rewardGold = 1500; // Increased reward for attacking a caravan
                    break;
                case "defeat_lord_party":
                    rewardGold = 2000; // Reward for defeating a lord's party
                    break;
                case "convert_bandits":
                    rewardGold = 1800; // Reward for converting 20 bandits
                    break;
            }

            Hero.MainHero.ChangeHeroGold(rewardGold);
            InformationManager.DisplayMessage(new InformationMessage($"You have received {rewardGold} gold as a reward."));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("activeContract", ref activeContract);
            dataStore.SyncData("currentContractIndex", ref currentContractIndex);
            dataStore.SyncData("nextNotificationTime", ref nextNotificationTime);
            dataStore.SyncData("isCareerAccepted", ref isCareerAccepted);
        }
    }
}






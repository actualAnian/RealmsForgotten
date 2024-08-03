using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using System.Linq;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.LinQuick;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.ObjectSystem;
using TaleWorlds.InputSystem;
using static TaleWorlds.CampaignSystem.CharacterDevelopment.DefaultPerks;


namespace Bannerlord.Module1
{
    public class MerchantEventBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject MerchantTitleText = new TextObject("{=MerchantTitle}A Distressed Merchant");
        private static readonly TextObject MerchantText = new TextObject("{=MerchantText}You meet a distressed merchant on the road with a broken wagon. He asks for help and says: \"Greetings, my lord. I need your help to deliver these goods to a lord in Valtoria. Please, could you deliver them for me? The lord will reward you generously!\"");
        private static readonly TextObject AcceptText = new TextObject("{=Accept}Accept and carry the goods");
        private static readonly TextObject DeclineText = new TextObject("{=Decline}Decline and move on");

        private Settlement targetSettlement;
        private int mercenaryAttackCount = 0;
        private const int MaxMercenaryAttacks = 1;
        private bool questAccepted = false;
        private MobileParty mercenaryParty;
        private bool caravanSpawned = false;

        private CampaignTime nextTriggerTime;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("nextTriggerTime", ref nextTriggerTime);
            dataStore.SyncData("targetSettlement", ref targetSettlement);
            dataStore.SyncData("mercenaryAttackCount", ref mercenaryAttackCount);
            dataStore.SyncData("questAccepted", ref questAccepted);
            dataStore.SyncData("mercenaryParty", ref mercenaryParty);
            dataStore.SyncData("caravanSpawned", ref caravanSpawned);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            nextTriggerTime = CampaignTime.Now + CampaignTime.Days(45);
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            if (nextTriggerTime == null || nextTriggerTime == CampaignTime.Zero)
                nextTriggerTime = CampaignTime.Now + CampaignTime.Days(45);
        }

        private void DailyTick()
        {
            if (CampaignTime.Now >= nextTriggerTime && !questAccepted)
            {
                CreateMerchantPopUp();
                nextTriggerTime = CampaignTime.Now + CampaignTime.Days(30);
            }
        }

        private void CreateMerchantPopUp()
        {
            targetSettlement = GetRandomTown();
            if (targetSettlement == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Target town not found.", Colors.Red));
                return;
            }

            InformationManager.ShowInquiry(new InquiryData(
                MerchantTitleText.ToString(),
                MerchantText.ToString() + $" Deliver them to {targetSettlement.Name}?",
                true,
                true,
                AcceptText.ToString(),
                DeclineText.ToString(),
                OnAccept,
                OnDecline
            ));
        }

        private void OnAccept()
        {
            questAccepted = true;
            StartMerchantMission();
            InformationManager.DisplayMessage(new InformationMessage($"You have accepted to help the merchant. Deliver the goods to {targetSettlement.Name}.", Colors.Yellow));
        }

        private void OnDecline()
        {
            InformationManager.DisplayMessage(new InformationMessage("You have declined to help the merchant.", Colors.Red));
        }

        private void StartMerchantMission()
        {
            if (targetSettlement == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Target town not found.", Colors.Red));
                return;
            }

            // Add logic for handling the transportation of goods
            // Example: Add items to the player's inventory here
            questAccepted = true; // Ensure quest is marked as accepted
        }

        private void EndMerchantMission()
        {
            questAccepted = false; // Mark the quest as no longer active
            targetSettlement = null; // Clear the target settlement
            mercenaryAttackCount = 0; // Reset the count of mercenary attacks
            caravanSpawned = false; // Ensure the caravan is marked as not spawned
            InformationManager.DisplayMessage(new InformationMessage("The merchant mission has ended.", Colors.Green));
        }

        private Settlement GetRandomTown()
        {
            var towns = Settlement.All.Where(s => s.IsTown).ToList();
            return towns.Any() ? towns[MBRandom.RandomInt(towns.Count)] : null;
        }
    }
}
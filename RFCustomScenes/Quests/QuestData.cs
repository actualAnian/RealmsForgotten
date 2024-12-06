using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace RFCustomSettlements.Quests
{
    public class QuestData
    {
        public string QuestId { get; }
        public string QuestGiverId { get; }
        public string QuestLogText { get; }
        public CompletedWhen CompleteCondition { get; }
        public QuestCompleteConsequence CompleteConsequence { get; }

        public QuestData(string questId, string questGiverId, string questLogId, CompletedWhen when, QuestCompleteConsequence consequence)
        {
            QuestId = questId;
            QuestGiverId = questGiverId;
            QuestLogText = questLogId;
            CompleteCondition = when;
            CompleteConsequence = consequence;
        }
    }


    public class InInventory
    {
        public string ItemId { get; set; }
        public int Amount { get; set; }
        public InInventory(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }
    }

    public class HasKilled
    {
        public string TroopId { get; set; }
        public int Amount { get; set; }
        public HasKilled(string TroopId, int amount)
        {
            this.TroopId = TroopId;
            Amount = amount;
        }
    }

    public class HasPrisoners
    {
        public string PrisonerId { get; set; }
        public int Amount { get; set; }
        public HasPrisoners(string prisonerId, int amount)
        {
            PrisonerId = prisonerId;
            Amount = amount;
        }
    }

    public class RemoveItem
    {
        public string ItemId { get; set; }
        public int Amount { get; set; }
        public RemoveItem(string itemId, int amount)
        {
            ItemId = itemId;
            Amount = amount;
        }

    }

    public class RemoveTroop
    {
        public string TroopId { get; set; }
        public int Amount { get; set; }
        public RemoveTroop(string TroopId, int amount)
        {
            this.TroopId = TroopId;
            Amount = amount;
        }
    }

    public class RemovePrisoners
    {
        public string PrisonerId { get; set; }
        public int Amount { get; set; }

        public RemovePrisoners(string prisonerId, int amount)
        {
            PrisonerId = prisonerId;
            Amount = amount;
        }
    }

    public class CompletedWhen
    {
        public CompletedWhen(Dictionary<string, int>? inInventoryList, Dictionary<string, int>? hasKilledList, Dictionary<string, int>? hasPrisonersList)
        {
            InInventoryList = inInventoryList;
            HasKilledList = hasKilledList;
            HasPrisonersList = hasPrisonersList;
        }

        public Dictionary<string, int>? InInventoryList { get; }
        public Dictionary<string, int>? HasKilledList { get; }
        public Dictionary<string, int>? HasPrisonersList { get; }
    }

    public class QuestCompleteConsequence
    {
        public QuestCompleteConsequence(Dictionary<string, int>? removeItemList, Dictionary<string, int>? removeTroopList, Dictionary<string, int>? removePrisonersList, Dictionary<string, int>? addItemList, Dictionary<string, int>? addTroopList, int? renownAmount)
        {
            RemoveItemList = removeItemList;
            RemoveTroopList = removeTroopList;
            RemovePrisonersList = removePrisonersList;
            AddItemList = addItemList;
            AddTroopList = addTroopList;
            if (renownAmount != null)
                RenownAmount = (int)renownAmount;
            if (removeItemList != null && removeItemList.ContainsKey("gold"))
            {
                LoseGoldAmount = removeItemList["gold"];
                removeItemList.Remove("gold");
            }
            if (addItemList != null && addItemList.ContainsKey("gold"))
            {
                ReceiveGoldAmount = addItemList["gold"];
                addItemList.Remove("gold");
            }
        }

        public Dictionary<string, int>? RemoveItemList { get; }
        public Dictionary<string, int>? RemoveTroopList { get; }
        public Dictionary<string, int>? RemovePrisonersList { get; }
        public Dictionary<string, int>? AddItemList { get; }
        public Dictionary<string, int>? AddTroopList { get; }
        public int RenownAmount { get; } = 0;
        public int ReceiveGoldAmount { get; } = 0;
        public int LoseGoldAmount { get; } = 0;

    }
}

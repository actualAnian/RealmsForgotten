using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace RFCustomSettlements.Quests
{
    public class QuestData
    {
        public string QuestId { get; }
        public string QuestGiverId { get; }

        public string QuestLogText { get; }
        public string CompleteCondition { get; }
        public string CompleteConsequence { get; }

        public QuestData(string questId, string questGiverId, string questLogId)
        {
            QuestId = questId;
            QuestGiverId = questGiverId;
            QuestLogText = questLogId;
            CompleteCondition = CreateCondition();
            CompleteConsequence = CreateConsequence();
        }

        private string CreateConsequence()
        {
            return "";
        }

        private string CreateCondition()
        {
            return "";
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
        public string CreatureId { get; set; }
        public int Amount { get; set; }
        public HasKilled(string creatureId, int amount)
        {
            CreatureId = creatureId;
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
        public string CreatureId { get; set; }
        public int Amount { get; set; }
        public RemoveTroop(string creatureId, int amount)
        {
            CreatureId = creatureId;
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
        public List<InInventory> InInventoryList { get; set; } = new();
        public List<HasKilled> HasKilledList { get; set; } = new();
        public List<HasPrisoners> HasPrisonersList { get; set; } = new();
    }

    public class CompleteConsequence
    {
        public List<RemoveItem> RemoveItemList { get; set; } = new List<RemoveItem>();
        public List<RemoveTroop> RemoveTroopList { get; set; } = new List<RemoveTroop>();
        public List<RemovePrisoners> RemovePrisonersList { get; set; } = new List<RemovePrisoners>();
    }

}

}

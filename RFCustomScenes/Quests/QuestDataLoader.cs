using RealmsForgotten;
using RealmsForgotten.RFCustomSettlements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace RFCustomSettlements.Quests
{
    public static class QuestDataLoader
    {
        private static readonly string xmlFileName = "quests.xml";
        private static string _mainPath = System.IO.Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);
        private static readonly string xmlFilePath = System.IO.Path.Combine(_mainPath, xmlFileName);
        public static void LoadQuestData()
        {
            var doc = XDocument.Load(xmlFilePath);
            List<QuestData> allQuests = doc.Descendants("Quest")
                      .Select(ParseQuest)
                      .ToList();
            foreach (QuestData item in allQuests)
                CustomSettlementsCampaignBehavior.AllQuests.Add(item.QuestId, item);
        }
        private static QuestData ParseQuest(XElement questElement)
        {
            string questId = questElement.Element("QuestId")?.Value?.Trim() ?? throw new Exception($"Error parsing {xmlFileName}. QuestId is missing.");
            string questGiverId = questElement.Element("QuestGiverId")?.Value?.Trim() ?? throw new Exception($"Error parsing {xmlFileName}. QuestGiverId is missing for quest {questId}.");
            string questLogText = questElement.Element("Text")?.Value?.Trim() ?? throw new Exception($"Error parsing {xmlFileName}. Text is missing for quest {questId}.");

            string errorMessageFistPart = $"Error parsing {xmlFileName}, quest with id {questId}";

            var inInventoryList = ParseConditionDictionary(questElement.Element("CompletedWhen")?.Elements("InInventory"), "ItemId", "Amount", errorMessageFistPart);
            var hasKilledList = ParseConditionDictionary(questElement.Element("CompletedWhen")?.Elements("HasKilled"), "TroopId", "Amount", errorMessageFistPart);
            var hasPrisonersList = ParseConditionDictionary(questElement.Element("CompletedWhen")?.Elements("HasPrisoners"), "PrisonerId", "Amount", errorMessageFistPart);
            CompletedWhen completedWhen = new(inInventoryList, hasKilledList, hasPrisonersList);

            var removeItemList = ParseConditionDictionary(questElement.Element("CompleteConsequence")?.Elements("RemoveItem"), "ItemId", "Amount", errorMessageFistPart);
            var removeTroopList = ParseConditionDictionary(questElement.Element("CompleteConsequence")?.Elements("RemoveTroop"), "TroopId", "Amount", errorMessageFistPart);
            var removePrisonersList = ParseConditionDictionary(questElement.Element("CompleteConsequence")?.Elements("RemovePrisoners"), "PrisonerId", "Amount", errorMessageFistPart);
            var addItemList = ParseConditionDictionary(questElement.Element("CompleteConsequence")?.Elements("AddToInventory"), "ItemId", "Amount", errorMessageFistPart);
            var addTroopList = ParseConditionDictionary(questElement.Element("CompleteConsequence")?.Elements("AddTroops"), "TroopId", "Amount", errorMessageFistPart);
            string? renown = questElement.Element("AddRenown")?.Value?.Trim();
            int renownAmount = 0;
            if (renown != null && !int.TryParse(renown, out renownAmount))
            {
                throw new Exception(errorMessageFistPart + $" Invalid value for AddRenown.");
            }
            QuestCompleteConsequence completeConsequence = new(removeItemList, removeTroopList, removePrisonersList, addItemList, addTroopList, renownAmount);
            return new QuestData(questId, questGiverId, questLogText, completedWhen, completeConsequence);
        }

        private static Dictionary<string, int>? ParseConditionDictionary(IEnumerable<XElement>? elements, string keyElement, string valueElement, string errorMessageFistPart)
        {
            if (elements == null)
                return null;
            Dictionary<string, int> result = new();
            foreach (XElement element in elements)
            {
                string key = element.Element(keyElement)?.Value?.Trim()
                    ?? throw new Exception(errorMessageFistPart + $" missing tag: {keyElement}.");
                string valueString = element.Element(valueElement)?.Value?.Trim()
                    ?? throw new Exception(errorMessageFistPart + $" missing {valueElement} for {keyElement}: {key}.");
                if (!int.TryParse(valueString, out int value))
                {
                    throw new Exception(errorMessageFistPart + $" Invalid {valueElement} value for {keyElement}: {key}.");
                }
                result[key] = value;
            }

            return result;
        }

    }

}

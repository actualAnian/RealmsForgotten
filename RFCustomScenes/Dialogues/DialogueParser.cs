using RealmsForgotten;
using RealmsForgotten.RFCustomSettlements;
using RFCustomSettlements.Quests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace RFCustomSettlements.Dialogues
{
    internal class DialogueLine
    {
        ConversationSentence.OnConditionDelegate? condition;
        ConversationSentence.OnConsequenceDelegate? consequence;
        readonly string text;
        readonly string lineId;
        readonly string inputId;
        readonly string goToLineId;
        private readonly bool player;

        public DialogueLine(string text, string lineId, string goToId, bool player, string inputId, string? condition = null, string? consequence = null)
        {
            this.text = text;
            this.lineId = lineId;
            this.goToLineId = goToId;
            this.player = player;
            this.inputId = inputId;
            CreateCondition(condition);
            CreateConsequence(consequence);
        }

        private readonly Dictionary<string, Action<string, string?>> ConsequencesDict = new()
        {
            { "START", (questId, empty) => {CustomSettlementQuest.Start(questId); } },
            { "COMPLETE", (questId, empty) => {
                if (!CustomSettlementQuest.IsQuestActive(questId))
                    InformationManager.DisplayMessage(new InformationMessage($"Error completing the quest {questId}, it is not active", null));
                CustomSettlementQuest.GetQuest(questId)?.CompleteQuest(); }
            },
            { "STATE", (stateId, action) => {
                if (action == "INCREMENT")
                    Helper.IncrementDialogueState(stateId);
                else if (action == "RESET")
                    Helper.GetDialogueState(stateId); }
            }
        };
        private readonly Dictionary<string, Func<string[], bool>> ConditionsDict = new()
        {
            { "EVALUATE", (data) => {
                string questId = data[1];
                if (!CustomSettlementQuest.IsQuestActive(questId)) return false;
                CustomSettlementQuest? quest = CustomSettlementQuest.GetQuest(questId);
                if (quest != null) return quest.Evaluate();
                return false; }
            },
            { "STATE", (data) => {
                try
                {
                    string stateId = data[1];
                    string keyword = data[2];
                    if (keyword == "CREATE")
                    {
                        if (Helper.ContainsDialogueState(stateId)) return false;
                        Helper.AddDialogueState(stateId);
                        return true;
                    }
                    if (keyword == "IS")
                    {
                        int number = int.Parse(data[3]);
                        if (number == 0 && !Helper.ContainsDialogueState(stateId) )
                            Helper.AddDialogueState(stateId);
                        return number == Helper.GetDialogueState(stateId);
                    }
                    throw new Exception("incorrect Keyword, expected \"IS\" or \"CREATE\"");
                }
                catch (Exception)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"Error parsing the condition for the dialogue in {data}", null));
                    return false;
                }
            }},
            { "npcId", (data) => {
                if (data[1] == "IS")
                {
                    return CharacterObject.OneToOneConversationCharacter.StringId == data[2];
                }

                InformationManager.DisplayMessage(new InformationMessage($"Error parsing the condition for the dialogue in {data}", null));
                return false;
            } }
        };
        private void CreateConsequence(string? consequence)
        {
            if (consequence == null)
            {
                this.consequence = null;
                return;
            }
            try
            {
                Regex regex = new(@"\[(.*?)\]");
                List<Action> consequences = new();
                foreach (var item in Regex.Split(consequence, Regex.Escape("AND")))
                {
                    string[] data = item.Trim().Split(' ');
                    string objectId = data[1];
                    string? action = data.Count() == 3 ? data[2] : null;
                    if (!ConsequencesDict.ContainsKey(data[0]))
                        throw new Exception($"Unrecognised consequence while parsing: {item}");
                    consequences.Add(() => ConsequencesDict[data[0]](objectId, action));
                }

                Action finalCondition = () =>
                {
                    foreach (var consequence in consequences)
                    {
                        consequence();
                    }
                };
                this.consequence = delegate () {
                    finalCondition();
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing custom settlements dialogues consequence: {consequence}. message: {ex.Message}");
            }
        }
        private void CreateCondition(string? condition)
        {
            if (condition == null)
            {
                this.condition = null;
                return;
            }
            try
            {
                Regex regex = new(@"\[(.*?)\]");
                List<Func<bool>> conditions = new();
                foreach (var item in Regex.Split(condition, Regex.Escape("AND")))
                {
                    string[] data = item.Trim().Split(' ');
                    if (!ConditionsDict.ContainsKey(data[0]))
                        throw new Exception($"Unrecognised condition while parsing: {item}");
                    if (data[0] == "npcId") RFConversationLogic.AddNpcAsTalkable(data[2]);
                    conditions.Add(() => ConditionsDict[data[0]](data));
                    //if (data[0] == "npcId" && data[1] == "IS")
                    //{
                    //    RFConversationLogic.AddNpcAsTalkable(data[2]); 
                    //    conditions.Add(() => CharacterObject.OneToOneConversationCharacter.StringId == data[2]);

                    //}
                    //if (data[0] == "State")
                    //{
                    //    string stateId = data[1];
                    //    if (data[2] == "IS")
                    //    {
                    //        if (!int.TryParse(data[3], out int value)) throw new Exception($"Error parsing custom settlements dialogues, State {stateId} has to be an integer");
                    //        if (value == 0) Helper.AddDialogueState(stateId);
                    //        else conditions.Add(() => Helper.GetDialogueState(stateId) == value);
                    //    }
                    //    else
                    //        throw new Exception($"unrecognized pattern {item}");
                    //}
                    //if (data[0] == "START")
                    //{
                    //    string questId = data[1];
                    //    conditions.Add(() => CustomSettlementQuest.Start(questId));
                    //}
                    //if (data[0] == "Evaluate")
                    //{
                    //    string questId = data[1];
                    //    conditions.Add(() => CustomSettlementQuest.ActiveQuests[questId].Evaluate());
                    //}
                }
                Func<bool> finalCondition = () =>
                {
                    foreach (var condition in conditions)
                    {
                        if (!condition()) return false;
                    }
                    return true;
                };
                this.condition = delegate () {
                    return finalCondition();
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing custom settlements dialogues condition: {condition}. message: {ex.Message}");
            }
        }

        public ConversationSentence.OnConditionDelegate? Condition => condition;
        public ConversationSentence.OnConsequenceDelegate? Consequence => consequence;

        public string Text => text;

        public string LineId => lineId;

        public string GoToLineId => goToLineId;

        public bool IsPlayerLine => player;

        public string InputId => inputId;
    }
    internal class DialogueParser
    {
        public static List<DialogueLine> allDialogues = new();
        public static void Deserialize()
        {
            Dictionary<string, int> inputsAmount = new();
            string mainPath = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

            string xmlFileName = Path.Combine(mainPath, "dialogues.xml");
            XElement SettlementBandits = XElement.Load(xmlFileName);
            XmlDocument xmlDocument = new();
            xmlDocument.Load(xmlFileName);
            foreach (XElement node in SettlementBandits.Descendants("Dialogue"))
            {
                string? condition = default;
                string? consequence = default;
                string text, lineId, goToId, inputId;
                if (node.Attribute("condition") != null)
                    condition = node.Attribute("condition").Value;
                if (node.Attribute("consequence") != null)
                    consequence = node.Attribute("consequence").Value;
                text = node.Element("text").Value;
                inputId = node.Element("inputId").Value;
                goToId = node.Element("goToId").Value;
                //if (node.Attribute("isStartLine") != null && bool.Parse(node.Attribute("isStartLine").Value) == true) inputId = "start";
                //else inputId = lineId;// node.Element("inputId").Value;
                bool player = bool.Parse(node.Element("isPlayerLine").Value);

                if (!inputsAmount.ContainsKey(inputId))
                    inputsAmount[inputId] = 0;
                lineId = inputId + inputsAmount[inputId];
                allDialogues.Add(new(text, lineId, goToId, player, inputId, condition, consequence));
            }
        }
    }
}

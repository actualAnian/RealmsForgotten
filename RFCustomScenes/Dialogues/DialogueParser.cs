using RealmsForgotten;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using static System.Net.Mime.MediaTypeNames;

namespace RFCustomSettlements.Dialogues
{
    internal class DialogueLine
    {
        ConversationSentence.OnConditionDelegate? condition;
        readonly string text;
        readonly string lineId;
        readonly string inputId;
        readonly string goToLineId;
        private readonly bool player;

        public DialogueLine(string text, string lineId, string goToId, bool player, string inputId, string? condition = null)
        {
            this.text = text;
            this.lineId = lineId;
            this.goToLineId = goToId;
            this.player = player;
            this.inputId = inputId;
            CreateCondition(condition);
        }

        private void CreateCondition(string? condition)
        {
            if (condition == null) this.condition = null;
            else
            {
                string[] data = condition.Split(' ');
                if (data[0] == "npcId" && data[1] == "IS")
                { 
                    this.condition = delegate () { return CharacterObject.OneToOneConversationCharacter.StringId == data[2]; };
                    RFConversationLogic.AddNpcAsTalkable(data[2]);
                }
            }
        }

        public ConversationSentence.OnConditionDelegate? Condition => condition;

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

            string mainPath = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

            string xmlFileName = Path.Combine(mainPath, "dialogues.xml");
            XElement SettlementBandits = XElement.Load(xmlFileName);
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(xmlFileName);
            foreach (XElement node in SettlementBandits.Descendants("Dialogue"))
            {
                string? condition = default;
                string text, lineId, goToId, inputId;
                if (node.Attribute("condition") != null)
                    condition = node.Attribute("condition").Value;
                text = node.Element("text").Value;
                lineId = node.Element("lineId").Value;
                goToId = node.Element("goToId").Value;
                if (node.Attribute("isStartLine") != null && bool.Parse(node.Attribute("isStartLine").Value) == true) inputId = "start";
                else inputId = lineId;
                bool player = bool.Parse(node.Element("isPlayerLine").Value);
                allDialogues.Add(new(text, lineId, goToId, player, inputId, condition));
            }
        }
    }
}

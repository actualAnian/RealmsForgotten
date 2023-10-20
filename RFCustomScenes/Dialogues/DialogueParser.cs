using RealmsForgotten;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RFCustomSettlements.Dialogues
{
    internal class DialogueLine
    {
        readonly string condition;
        readonly string text;
        readonly string lineId;
        readonly string goToLineId;

        public DialogueLine(string condition, string text, string lineId, string goToLineId)
        {
            this.condition = condition;
            this.text = text;
            this.lineId = lineId;
            this.goToLineId = goToLineId;
        }
    }
    internal class DialogueParser
    {
        public static void Deserialize()
        {

            //string mainPath = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

            //string xmlFileName = Path.Combine(mainPath, "dialogues.xml");

            //XElement SettlementBandits = XElement.Load(xmlFileName);

            //foreach (XElement tree in SettlementBandits.Descendants("DialogueTree"))
            //{

            //}
        }
    }
}

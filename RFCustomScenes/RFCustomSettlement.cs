using RFCustomSettlements;
using System.Xml;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFCustomSettlements
{
    public class RFCustomSettlement : SettlementComponent
    {
        protected override void OnInventoryUpdated(ItemRosterElement item, int count) { }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            BackgroundCropPosition = float.Parse(node.Attributes["background_crop_position"].Value);
            BackgroundMeshName = node.Attributes["background_mesh"].Value;
            WaitMeshName = node.Attributes["wait_mesh"].Value;
            CustomScene = node.Attributes["custom_scene"].Value;

            var tempEnterStart = node.Attributes["enter_start"];
            var tempEnterEnd = node.Attributes["enter_end"];
            if (tempEnterStart != null && tempEnterEnd != null)
            {
                EnterStart = int.Parse(tempEnterStart.Value);
                EnterEnd = int.Parse(tempEnterEnd.Value);
                CanEnterAnytime = false;
            }
            else 
            {
                CanEnterAnytime = true;
                EnterStart = 0;
                EnterEnd = 24;
            }

            var tempMaxPlayersideTroops = node.Attributes["max_player_side_troops"];
            if (tempMaxPlayersideTroops != null)
                MaxPlayersideTroops = int.Parse(tempMaxPlayersideTroops.Value);
            else
                MaxPlayersideTroops = 1;

            var settType = node.Attributes["type"];
            if (settType == null || settType.Value == "exploration")
            {
                StateHandler = new ExploreSettlementStateHandler(this);
            }
            else
            {
                StateHandler = new ArenaSettlementStateHandler(this);
            }
        }
        [SaveableProperty(500)]
        public bool IsVisible { get; set; }
        public string CustomScene { get; private set; }
        public int MaxPlayersideTroops { get; private set; }
        public bool CanEnterAnytime { get; private set; }
        public int EnterStart { get; private set; }
        public int EnterEnd { get; private set; }
        internal ISettlementStateHandler StateHandler { get; private set; }   

    }
}

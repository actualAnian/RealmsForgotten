using System;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFCustomSettlements
{

    public enum CustomSettlementType
    {
        Town,
        Castle,
        Village,
        Misc
    }
    public class RFCustomSettlement : SettlementComponent
    {
        protected override void OnInventoryUpdated(ItemRosterElement item, int count)
        {
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            base.BackgroundCropPosition = float.Parse(node.Attributes["background_crop_position"].Value);
            base.BackgroundMeshName = node.Attributes["background_mesh"].Value;
            base.WaitMeshName = node.Attributes["wait_mesh"].Value;
            this.RuinType = (CustomSettlementType)Enum.Parse(typeof(CustomSettlementType), node.Attributes["type"].Value, true);
        }

        public RFCustomSettlement()
        {
            this.IsRaided = false;
            this.HasBandits = true;
            this.IsSpotted = false;
        }

        public void ResetRuin()
        {
            this.IsRaided = false;
            this.HasBandits = true;
            this.LastRaided = CampaignTime.Never;
            this.RaidProgress = 0f;
            this.LastTick = 0f;
        }

        [SaveableProperty(500)]
        public bool IsRaided { get; set; }

        [SaveableProperty(501)]
        public float RaidProgress { get; set; }

        [SaveableProperty(502)]
        public float LastTick { get; set; }

        [SaveableProperty(503)]
        public string RuinSettlementID { get; set; }

        public CustomSettlementType RuinType { get; set; }

        [SaveableProperty(505)]
        public bool HasBandits { get; set; }

        [SaveableProperty(506)]
        public CampaignTime LastRaided { get; set; }

        [SaveableProperty(507)]
        public bool IsSpotted { get; set; }
    }
}

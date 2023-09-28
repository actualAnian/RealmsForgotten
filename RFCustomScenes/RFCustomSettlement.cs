using System;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.RFCustomSettlements
{
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
            CustomScene = node.Attributes["custom_scene"].Value;
            try
            {
                canEnterAnytime = bool.Parse(node.Attributes["can_enter_anytime"].Value);
                enterStart = int.Parse(node.Attributes["enter_start"].Value);
                enterEnd = int.Parse(node.Attributes["enter_end"].Value);
            }
            catch
            {
                canEnterAnytime = true;
                enterStart = 0;
                enterEnd = 24;
            }
        }

        //public RFCustomSettlement()
        //{
        //    this.IsRaided = false;
        //    this.HasBandits = true;
        //    this.IsVisible = false;
        //}

        //[SaveableProperty(500)]
        //public bool IsRaided { get; set; }

        [SaveableProperty(500)]
        public bool IsVisible { get; set; }
        public string? CustomScene { get; set; }

        public bool canEnterAnytime;
        public int enterStart;
        public int enterEnd;
    }
}

using HuntableHerds.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFCustomSettlements
{
    public class CustomSettlementBuildData
    {
        public class NpcData
        {
            public string Id { get; private set; }
            public int TagId { get; private set; }
            public string ActionSet { get; private set; }
            public NpcData(string id, string TagId, string actionSet)
            {
                this.Id = id;
                this.TagId = int.Parse(TagId);
                this.ActionSet = actionSet;
            }

        }

        public class RFBanditData
        {
            private readonly int _amount;
            private readonly string _id;
            private readonly string? _dropDataId;
            //private readonly string _dropDataId2;

            public RFBanditData(string id, string value2, string dropDataId)
            {
                _dropDataId = dropDataId;
                _id = id;
                _amount = int.Parse(value2);
            }
            public RFBanditData(string id, string value2)
            {
                _dropDataId = null;
                _id = id;
                _amount = int.Parse(value2);
            }

            public string Id { get => _id; }
            public int Amount { get => _amount; }
            public ItemDropsData? ItemDropsData 
            {
                get 
                {
                    if (_dropDataId == null || !AllItemDropsData.ContainsKey(_dropDataId)) return null;
                    return AllItemDropsData[_dropDataId];
                }
            }
        }
        internal static readonly Dictionary<string, CustomSettlementBuildData> allCustomSettlementBuildDatas = new ();
        public readonly Dictionary<int, List<RFBanditData>> stationaryAreasBandits;
        public readonly Dictionary<int, RFBanditData> patrolAreasBandits;

        public readonly bool canEnterOnlyAtSpecialHours;
        public readonly int enterStartHour;
        public readonly int enterEndHour;
        public List<NpcData> allNpcs { get; private set; }
        public static Dictionary<string, ItemDropsData> AllItemDropsData { get; } = new();

        private static string _mainPath = System.IO.Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

        private static readonly string _banditsXmlFileName = System.IO.Path.Combine(_mainPath, "settlement_bandits.xml");
        private static readonly string _itemDropsXmlFileName = System.IO.Path.Combine(_mainPath, "item_drops.xml");
        public CustomSettlementBuildData(Dictionary<int, List<RFBanditData>> _stationaryAreasBandits, Dictionary<int, RFBanditData> _patrolAreasBandits, List<NpcData>Npcs, bool _canEnterOnlyAtSpecialHours = false, int _enterStartHour = 0, int _enterEndHour = 24)
        {
            stationaryAreasBandits = _stationaryAreasBandits;
            patrolAreasBandits = _patrolAreasBandits;
            canEnterOnlyAtSpecialHours = _canEnterOnlyAtSpecialHours;
            enterStartHour = _enterStartHour;
            enterEndHour = _enterEndHour;
            allNpcs = Npcs;
        }
        public static void BuildItemDrops()
        {
            XmlDocument xmlDoc = new();
            xmlDoc.Load(_itemDropsXmlFileName);

            XmlNodeList itemDropsDataNodes = xmlDoc.SelectNodes("/AllItemDrops/ItemDropsData");
            foreach (XmlNode itemDropsDataNode in itemDropsDataNodes)
            {
                string dropsId = itemDropsDataNode.SelectSingleNode("DropsId").InnerText;
                XmlNodeList itemDropNodes = itemDropsDataNode.SelectNodes("ItemDrops/ItemDrop");

                List<ItemDrop> itemDrops = new List<ItemDrop>();
                
                foreach (XmlNode itemDropNode in itemDropNodes)
                {
                    string itemId = itemDropNode.SelectSingleNode("ItemId").InnerText;
                    int amountMin = int.Parse(itemDropNode.SelectSingleNode("AmountMin").InnerText);
                    int amountMax = int.Parse(itemDropNode.SelectSingleNode("AmountMax").InnerText);
                    double dropChance = double.Parse(itemDropNode.SelectSingleNode("DropChance").InnerText);

                    ItemDrop itemDrop = new ItemDrop(itemId, amountMin, amountMax, dropChance);
                    itemDrops.Add(itemDrop);
                }
                ItemDropsData itemDropsData = new ItemDropsData(itemDrops, dropsId);
                AllItemDropsData.Add(dropsId, itemDropsData);
            }
        }
        public static void BuildAll()
        {

            XElement SettlementBandits = XElement.Load(_banditsXmlFileName);

            foreach (XElement element in SettlementBandits.Descendants("CustomScene"))
            {
                Dictionary<int, List<RFBanditData>> buildStationaryAreasBandits = new();
                Dictionary<int, RFBanditData> buildPatrolAreasBandits = new();
                string sceneId;

                sceneId = element.Element("id").Value;
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("CommonArea"))
                {
                    foreach (XElement xElement2 in xElement.Descendants("Bandit"))
                    {
                        XElement dropId = xElement2.Element("lootId");
                        string? lootId = dropId?.Value;
                        RFBanditData bd = new(xElement2.Element("id").Value, xElement2.Element("amount").Value, lootId);
                        int areaIndex = int.Parse(xElement.Element("areaIndex").Value);
                        if(buildStationaryAreasBandits.ContainsKey(areaIndex))
                            buildStationaryAreasBandits[areaIndex].Add(bd);
                        else
                            buildStationaryAreasBandits[areaIndex] = new List<RFBanditData>() { bd };
                    }
                }
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("PatrolArea"))
                {
                    XElement dropId = xElement.Element("Bandit").Element("lootId");
                    string? lootId = dropId?.Value;
                    RFBanditData bd = new(xElement.Element("Bandit").Element("id").Value, xElement.Element("Bandit").Element("amount").Value, lootId);
                    buildPatrolAreasBandits.Add(int.Parse(xElement.Element("areaIndex").Value), bd);
                }
                XElement NpcElement = element.Descendants("Npcs").FirstOrDefault();
                List<NpcData> NpcsList = new();
                if (NpcElement != null)
                {
                    foreach (XElement Npc in NpcElement.Descendants("Npc"))
                    {
                        NpcsList.Add(new(Npc.Element("NpcId").Value, Npc.Element("TagId").Value, Npc.Element("ActionSet").Value));
                    }
                }
                //                CustomSettlementBuildData buildData = new(buildStationaryAreasBandits, buildPatrolAreasBandits, maxPlayersideTroops);
                CustomSettlementBuildData buildData = new(buildStationaryAreasBandits, buildPatrolAreasBandits, NpcsList, true, 8, 12);

                allCustomSettlementBuildDatas.Add(sceneId, buildData);
            }
        }
    }
}

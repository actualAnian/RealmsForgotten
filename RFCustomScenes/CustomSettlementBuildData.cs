using HuntableHerds.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static RealmsForgotten.RFCustomSettlements.CustomSettlementBuildData.BehaviorTreeData;

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
        public class BehaviorTreeData
        {
            public class Param
            {
                public string Typename { get; private set; }
                public string Val { get; private set; }
                public Param(string typename, string val)
                {
                    Typename = typename;
                    Val = val;
                }
            }
            public BehaviorTreeData(string name, List<Param> pparams)
            {
                Name = name;
                _params = pparams;
            }

            public string Name { get; private set; }
            private readonly List<Param> _params;

            private static object ConvertParam(Param param)
            {
                return param.Typename switch
                {
                    "int" => int.Parse(param.Val, CultureInfo.InvariantCulture),
                    "float" => float.Parse(param.Val, CultureInfo.InvariantCulture),
                    "double" => double.Parse(param.Val, CultureInfo.InvariantCulture),
                    "bool" => bool.Parse(param.Val),
                    "string" => param.Val,
                    _ => throw new InvalidOperationException(
                        $"Unsupported param type '{param.Typename}'"
                    )
                };
            }
            public object[] Params
            {
                get
                {
                    if (_params == null || _params.Count == 0)
                        return Array.Empty<object>();
                    var result = new object[_params.Count];
                    for (int i = 0; i < _params.Count; i++)
                        result[i] = ConvertParam(_params[i]);
                    return result;
                }
            }
        }

        public class RFBanditData
        {
            private readonly int _amount;
            private readonly string _id;
            private readonly string? _dropDataId;
            private BehaviorTreeData? _btData;
            public RFBanditData(string id, string amount, string? dropDataId = null, BehaviorTreeData? treeData = null)
            {
                _dropDataId = dropDataId;
                _id = id;
                _amount = int.Parse(amount);
                _btData = treeData;
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
            public BehaviorTreeData? TreeData { get => _btData; }
        }
        public static readonly Dictionary<string, CustomSettlementBuildData> AllCustomSettlementBuildDatas = new();
        public Dictionary<int, List<RFBanditData>> StationaryAreasBandits { get; private set; }
        public Dictionary<int, RFBanditData> PatrolAreasBandits { get; private set; }
        public Dictionary<int, RFBanditData> DynamicPatrolAreasBandits { get; private set; }

        public readonly bool canEnterOnlyAtSpecialHours;
        public int EnterStartHour { get; private set; }
        public int EnterEndHour { get; private set; }
        public List<NpcData> AllNpcs { get; private set; }
        public static Dictionary<string, ItemDropsData> AllItemDropsData { get; } = new();

        private static readonly string _mainPath = System.IO.Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

        private static readonly string _banditsXmlFileName = System.IO.Path.Combine(_mainPath, "settlement_bandits.xml");
        private static readonly string _itemDropsXmlFileName = System.IO.Path.Combine(_mainPath, "item_drops.xml");
        public CustomSettlementBuildData(Dictionary<int, List<RFBanditData>> stationaryAreasBandits, 
            Dictionary<int, RFBanditData> patrolAreasBandits,
            Dictionary<int, RFBanditData> dynamicPatrolAreasBandits, 
            List<NpcData> npcs, 
            bool canEnterOnlyAtSpecialHours = false, 
            int enterStartHour = 0, 
            int enterEndHour = 24)
        {
            StationaryAreasBandits = stationaryAreasBandits;
            PatrolAreasBandits = patrolAreasBandits;
            DynamicPatrolAreasBandits = dynamicPatrolAreasBandits;
            this.canEnterOnlyAtSpecialHours = canEnterOnlyAtSpecialHours;
            EnterStartHour = enterStartHour;
            EnterEndHour = enterEndHour;
            AllNpcs = npcs;
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

                string? lootAreaSize = itemDropsDataNode.SelectSingleNode("LootAreaSize")?.InnerText;
                ItemDropsData itemDropsData = new(itemDrops, dropsId, lootAreaSize);
                AllItemDropsData.Add(dropsId, itemDropsData);
            }
        }
        private static BehaviorTreeData? TryParseBehaviorTreeData(XElement banditElement)
        {
            var btElement = banditElement.Element("BehaviorTree");
            if (btElement == null) return null;
            string name = btElement.Attribute("name")!.Value;
            var treeParams = btElement.Elements("Param")
                .Select(p => new Param(
                    p.Attribute("type")?.Value ?? "string",
                    p.Attribute("value")!.Value
                ))
                .ToList();
            return new BehaviorTreeData(name, treeParams);
        }
        public static void BuildAll()
        {
            XElement SettlementBandits = XElement.Load(_banditsXmlFileName);
            foreach (XElement element in SettlementBandits.Descendants("CustomScene"))
            {
                Dictionary<int, List<RFBanditData>> buildStationaryAreasBandits = new();
                Dictionary<int, RFBanditData> buildPatrolAreasBandits = new();
                Dictionary<int, RFBanditData> buildDynamicPatrolAreasBandits = new();
                string sceneId;

                sceneId = element.Element("id").Value;
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("CommonArea"))
                {
                    foreach (XElement xElement2 in xElement.Descendants("Bandit"))
                    {
                        XElement dropId = xElement2.Element("lootId");
                        string? lootId = dropId?.Value;
                        RFBanditData bd = new(xElement2.Element("id").Value, xElement2.Element("amount").Value, lootId, treeData: TryParseBehaviorTreeData(xElement2));
                        int areaIndex = int.Parse(xElement.Element("areaIndex").Value);
                        if (buildStationaryAreasBandits.ContainsKey(areaIndex))
                            buildStationaryAreasBandits[areaIndex].Add(bd);
                        else
                            buildStationaryAreasBandits[areaIndex] = new List<RFBanditData>() { bd };
                    }
                }
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("PatrolArea"))
                {
                    XElement dropId = xElement.Element("Bandit").Element("lootId");
                    string? lootId = dropId?.Value;
                    RFBanditData bd = new(xElement.Element("Bandit").Element("id").Value, xElement.Element("Bandit").Element("amount").Value, lootId, treeData: TryParseBehaviorTreeData(xElement));
                    buildPatrolAreasBandits.Add(int.Parse(xElement.Element("areaIndex").Value), bd);
                }
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("DynamicPatrolArea"))
                {
                    XElement dropId = xElement.Element("Bandit").Element("lootId");
                    string? lootId = dropId?.Value;
                    RFBanditData bd = new(xElement.Element("Bandit").Element("id").Value, xElement.Element("Bandit").Element("amount").Value, lootId, treeData: TryParseBehaviorTreeData(xElement));
                    buildDynamicPatrolAreasBandits.Add(int.Parse(xElement.Element("areaIndex").Value), bd);
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
                CustomSettlementBuildData buildData = new(buildStationaryAreasBandits, buildPatrolAreasBandits, buildDynamicPatrolAreasBandits, NpcsList, true, 8, 12);

                AllCustomSettlementBuildDatas.Add(sceneId, buildData);
            }
        }
    }
}

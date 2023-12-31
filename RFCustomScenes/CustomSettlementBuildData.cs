﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.Engine;
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

            public RFBanditData(string id, string value2)
            {
                _id = id;
                _amount = int.Parse(value2);
            }

            public string Id { get => _id; }
            public int Amount { get => _amount; }
        }
        internal static readonly Dictionary<string, CustomSettlementBuildData> allCustomSettlementBuildDatas = new ();
        public readonly Dictionary<int, List<RFBanditData>> stationaryAreasBandits;
        public readonly Dictionary<int, RFBanditData> patrolAreasBandits;

        public readonly bool canEnterOnlyAtSpecialHours;
        public readonly int enterStartHour;
        public readonly int enterEndHour;
        public List<NpcData> allNpcs { get; private set; }
        public CustomSettlementBuildData(Dictionary<int, List<RFBanditData>> _stationaryAreasBandits, Dictionary<int, RFBanditData> _patrolAreasBandits, List<NpcData>Npcs, bool _canEnterOnlyAtSpecialHours = false, int _enterStartHour = 0, int _enterEndHour = 24)
        {
            stationaryAreasBandits = _stationaryAreasBandits;
            patrolAreasBandits = _patrolAreasBandits;
            canEnterOnlyAtSpecialHours = _canEnterOnlyAtSpecialHours;
            enterStartHour = _enterStartHour;
            enterEndHour = _enterEndHour;
            allNpcs = Npcs;
        }
        public static void BuildAll()
        {
            string mainPath = System.IO.Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

            string xmlFileName = System.IO.Path.Combine(mainPath, "settlement_bandits.xml");

            XElement SettlementBandits = XElement.Load(xmlFileName);

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
                        RFBanditData bd = new(xElement2.Element("id").Value, xElement2.Element("amount").Value);
                        int areaIndex = int.Parse(xElement.Element("areaIndex").Value);
                        if(buildStationaryAreasBandits.ContainsKey(areaIndex))
                            buildStationaryAreasBandits[areaIndex].Add(bd);
                        else
                            buildStationaryAreasBandits[areaIndex] = new List<RFBanditData>() { bd };
                    }
                }
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("PatrolArea"))
                {
                    RFBanditData bd = new(xElement.Element("Bandit").Element("id").Value, xElement.Element("Bandit").Element("amount").Value);
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

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.Core;

namespace RealmsForgotten.RFCustomSettlements
{
    public class CustomSettlementBuildData
    {
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

        public Dictionary<int, List<RFBanditData>> stationaryAreasBandits;
        public Dictionary<int, RFBanditData> patrolAreasBandits;
        public CustomSettlementBuildData(Dictionary<int, List<RFBanditData>> buildStationaryAreasBandits, Dictionary<int, RFBanditData> buildPatrolAreasBandits)
        {
            stationaryAreasBandits = buildStationaryAreasBandits;
            patrolAreasBandits = buildPatrolAreasBandits;
        }

        public static void BuildAll()
        {
            Dictionary<int, List<RFBanditData>> buildStationaryAreasBandits = new();
            Dictionary<int, RFBanditData> buildPatrolAreasBandits = new();
            string sceneId;

            string mainPath = Path.GetDirectoryName(Globals.realmsForgottenAssembly.Location);

            string xmlFileName = Path.Combine(mainPath, "settlement_bandits.xml");

            XElement SettlementBandits = XElement.Load(xmlFileName);

            foreach (XElement element in SettlementBandits.Descendants("CustomScene"))
            {
                sceneId = element.Element("id").Value;
                foreach (XElement xElement in element.Descendants("Bandits").Descendants("CommonArea"))
                {
                    foreach(XElement xElement2 in xElement.Descendants("Bandit"))
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

                CustomSettlementBuildData buildData = new(buildStationaryAreasBandits, buildPatrolAreasBandits);
                allCustomSettlementBuildDatas.Add(sceneId, buildData);
            }
        }
    }
}

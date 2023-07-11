using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace HuntableHerds.Models {
    public class HerdBuildData {
        public string NotifMessage;
        public string MessageTitle;
        public string Message;
        public string SpawnId;
        public int TotalAmountInHerd;
        public bool IsPassive;
        public float StartingHealth;
        public float MaxSpeed;
        public float HitboxRange;
        public int DamageToPlayer;
        public float SightRange;
        public bool FleeOnAttacked;
        public List<(string, (int, int))> ItemDrops;
        public List<string> SceneIds;

        private static List<HerdBuildData> allHuntableAgentBuildDatas = new();
        public static HerdBuildData? CurrentHerdBuildData;

        public HerdBuildData(string notifMessage, string messageTitle, string message, string spawnId, int totalAmountInHerd, bool isPassive, float startingHealth, float maxSpeed, float hitboxRange, int damageToPlayer, float sightRange, bool fleeOnAttacked, List<(string, (int, int))> itemDropsIdAndCount, List<string> sceneIds) {
            NotifMessage = notifMessage;
            MessageTitle = messageTitle;
            Message = message;
            SpawnId = spawnId;
            TotalAmountInHerd = totalAmountInHerd;
            IsPassive = isPassive;
            StartingHealth = startingHealth;
            MaxSpeed = maxSpeed;
            HitboxRange = hitboxRange;
            DamageToPlayer = damageToPlayer;
            SightRange = sightRange;
            FleeOnAttacked = fleeOnAttacked;
            ItemDrops = itemDropsIdAndCount;
            SceneIds = sceneIds;

            CurrentHerdBuildData = this;
        }

        public ItemRoster GetCopyOfItemDrops() {
            ItemRoster itemRoster = new();
            foreach ((string, (int, int)) pair in ItemDrops) {
                ItemObject? item = null;
                try {
                    item = Game.Current.ObjectManager.GetObject<ItemObject>(pair.Item1);
                }
                catch (NullReferenceException) {
                    continue;
                }
                if (item == null)
                    continue;

                int amount = pair.Item2.Item1;
                if (pair.Item2.Item2 > pair.Item2.Item1)
                    amount = MBRandom.RandomInt(pair.Item2.Item1, pair.Item2.Item2 + 1);

                itemRoster.AddToCounts(item, amount);
            }
            return itemRoster;
        }

        public static void BuildAll() {
            allHuntableAgentBuildDatas.Clear();

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string xmlFileName = Path.Combine(assemblyFolder, "hunting_herds.xml");

            XElement huntingHerds = XElement.Load(xmlFileName);

            foreach (XElement element in huntingHerds.Descendants("Herd")) {
                string notifMessage = element.Element("notifMessage").Value;
                string messageTitle = element.Element("messageTitle").Value;
                string message = element.Element("message").Value;
                string spawnId = element.Element("spawnId").Value;
                int totalAmountInHerd = (int)element.Element("totalAmountInHerd");
                bool isPassive = element.Element("isPassive").Value.ToLower() == "true" ? true : false;
                float startingHealth = (float)element.Element("startingHealth");
                float maxSpeed = (float)element.Element("maxSpeed");
                float hitboxRange = (float)element.Element("hitboxRange");
                int damageToPlayer = (int)element.Element("damageToPlayer");
                float sightRange = (float)element.Element("sightRange");
                bool fleeOnAttacked = element.Element("fleeOnAttacked").Value.ToLower() == "true" ? true : false;

                List<(string, (int, int))> itemDrops = new();
                XElement? itemDropsElement = element.Element("ItemDrops");
                if (itemDropsElement != null)
                    foreach (XElement itemDrop in itemDropsElement.Descendants("ItemDrop")) {
                        int amount = (int)itemDrop.Element("amount");
                        int maxAmount = amount;
                        XElement? maxAmountNode = itemDrop.Element("maxAmount");
                        if (maxAmountNode != null)
                            maxAmount = (int)maxAmountNode;
                        itemDrops.Add((itemDrop.Element("itemId").Value, (amount, maxAmount)));
                    }

                List<string> sceneIds = new();
                XElement? sceneIdsElement = element.Element("SceneIds");
                if (sceneIdsElement != null)
                    foreach (XElement sceneId in sceneIdsElement.Descendants("sceneId"))
                        sceneIds.Add(sceneId.Value);

                HerdBuildData buildData = new(notifMessage, messageTitle, message, spawnId, totalAmountInHerd, isPassive, startingHealth, maxSpeed, hitboxRange, damageToPlayer, sightRange, fleeOnAttacked, itemDrops, sceneIds);
                allHuntableAgentBuildDatas.Add(buildData);
            }
        }

        public static void Randomize() {
            int randomIndex = MBRandom.RandomInt(0, allHuntableAgentBuildDatas.Count);
            CurrentHerdBuildData = allHuntableAgentBuildDatas[randomIndex];
        }
    }
}

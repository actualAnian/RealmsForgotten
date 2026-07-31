using HuntableHerds.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
namespace RealmsForgotten.HuntableHerds.Models
{
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
        ItemDropsData ItemDrops;
        public List<string> SceneIds;
        // Terrain types this herd can be spotted on. Empty = any terrain
        // (backward compatible: a herd without a <terrains> tag still appears
        // everywhere). Parsed from a comma-separated list of TerrainType names.
        public List<TerrainType> Terrains = new();

        public static List<HerdBuildData> allHuntableAgentBuildDatas = new();
        public static HerdBuildData? CurrentHerdBuildData;


        public HerdBuildData(string notifMessage, string messageTitle, string message, string spawnId, int totalAmountInHerd, bool isPassive, float startingHealth, float maxSpeed, float hitboxRange, int damageToPlayer, float sightRange, bool fleeOnAttacked, ItemDropsData itemDropsIdAndCount, List<string> sceneIds) {
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

        public ItemDropsData GetCopyOfItemDrops() {
            return ItemDrops;
        }

        public static void BuildAll() {
            allHuntableAgentBuildDatas.Clear();

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string xmlFileName = Path.Combine(assemblyFolder, "hunting_herds.xml");

            // Missing/malformed XML must not CTD at boot (this runs from
            // OnBeforeInitialModuleScreenSetAsRoot). Leave the herd list empty.
            if (!File.Exists(xmlFileName))
                return;

            XElement huntingHerds;
            try
            {
                huntingHerds = XElement.Load(xmlFileName);
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[HuntableHerds] Failed to load hunting_herds.xml: {ex.Message}");
                return;
            }

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

                List<ItemDrop> item = new();
                XElement? itemDropsElement = element.Element("ItemDrops");
                if (itemDropsElement != null)
                    foreach (XElement itemDrop in itemDropsElement.Descendants("ItemDrop")) {
                        int amount = (int)itemDrop.Element("amount");
                        int maxAmount = amount;
                        XElement? maxAmountNode = itemDrop.Element("maxAmount");
                        if (maxAmountNode != null)
                            maxAmount = (int)maxAmountNode;
                        item.Add(new(itemDrop.Element("itemId").Value, amount, maxAmount, 1));
                        //itemDrops.Add((itemDrop.Element("itemId").Value, (amount, maxAmount)));
                    }

                ItemDropsData itemDrops = new(item,  $"HH_{spawnId}_item_drops");
                List<string> sceneIds = new();
                XElement? sceneIdsElement = element.Element("SceneIds");
                if (sceneIdsElement != null)
                    foreach (XElement sceneId in sceneIdsElement.Descendants("sceneId"))
                        sceneIds.Add(sceneId.Value);

                HerdBuildData buildData = new(notifMessage, messageTitle, message, spawnId, totalAmountInHerd, isPassive, startingHealth, maxSpeed, hitboxRange, damageToPlayer, sightRange, fleeOnAttacked, itemDrops, sceneIds);

                // Optional <terrains>Forest,Swamp</terrains> — comma-separated
                // TerrainType names. Absent/empty = spotted on any terrain.
                XElement? terrainsElement = element.Element("terrains");
                if (terrainsElement != null && !string.IsNullOrWhiteSpace(terrainsElement.Value))
                {
                    foreach (string name in terrainsElement.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (Enum.TryParse(name.Trim(), true, out TerrainType terrain))
                            buildData.Terrains.Add(terrain);
                    }
                }

                allHuntableAgentBuildDatas.Add(buildData);
            }
        }

        public static void Randomize() {
            int randomIndex = MBRandom.RandomInt(0, allHuntableAgentBuildDatas.Count);
            CurrentHerdBuildData = allHuntableAgentBuildDatas[randomIndex];
        }

        /// <summary>
        /// Picks a herd appropriate to the terrain: candidates are herds with no
        /// terrain restriction (appear anywhere) plus herds whose terrain list
        /// contains this terrain. Returns false if there is nothing to spot here
        /// (e.g. a herd list that is entirely terrain-locked to other biomes) so
        /// the caller can suppress the notification. Falls back to the old
        /// any-herd behaviour only if NO herd declares terrain at all.
        /// </summary>
        public static bool RandomizeForTerrain(TerrainType terrain) {
            if (allHuntableAgentBuildDatas.Count == 0)
                return false;

            bool anyTerrainTagged = false;
            List<HerdBuildData> candidates = new();
            foreach (HerdBuildData herd in allHuntableAgentBuildDatas) {
                if (herd.Terrains.Count == 0) {
                    candidates.Add(herd); // no restriction — fits any terrain
                } else {
                    anyTerrainTagged = true;
                    if (herd.Terrains.Contains(terrain))
                        candidates.Add(herd);
                }
            }

            // Nobody tagged terrain at all → preserve the old random behaviour.
            if (!anyTerrainTagged) {
                Randomize();
                return true;
            }

            if (candidates.Count == 0)
                return false; // nothing lives on this terrain — no sighting

            CurrentHerdBuildData = candidates[MBRandom.RandomInt(0, candidates.Count)];
            return true;
        }
    }
}

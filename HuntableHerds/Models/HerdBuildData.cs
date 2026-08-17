using HuntableHerds.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TaleWorlds.Core;

namespace RealmsForgotten.HuntableHerds.Models
{
    public class HerdBuildData
    {
        /// <summary>Stable position of this entry inside <see cref="allHuntableAgentBuildDatas"/>, assigned by <see cref="BuildAll"/>.</summary>
        public int Index;

        /// <summary>All &lt;notifMessage&gt; variants declared for this herd (at least one).</summary>
        public readonly List<string> NotifMessages = new();

        /// <summary>All &lt;messageTitle&gt; variants declared for this herd (at least one).</summary>
        public readonly List<string> MessageTitles = new();

        /// <summary>All &lt;message&gt; variants declared for this herd (at least one).</summary>
        public readonly List<string> Messages = new();

        /// <summary>Terrains this herd may be spotted on. Empty = any terrain.</summary>
        public readonly List<TerrainType> Terrains = new();

        public string SpawnId;
        public int TotalAmountInHerd;
        public bool IsPassive;
        public float StartingHealth;
        public float MaxSpeed;
        public float HitboxRange;
        public int DamageToPlayer;
        public float SightRange;
        public bool FleeOnAttacked;
        readonly ItemDropsData ItemDrops;
        public List<string> SceneIds;

        public static List<HerdBuildData> allHuntableAgentBuildDatas = new();

        /// <summary>
        /// The herd of the hunt the player is currently on. It is set when a hunt is ACCEPTED (or
        /// when an RF settlement scene spawns wildlife), never as a side effect of showing a
        /// notification: the notification carries its own herd, see <see cref="HerdMapNotification"/>.
        /// </summary>
        public static HerdBuildData? CurrentHerdBuildData;

        // Legacy single-value accessors, kept so nothing outside breaks. They pick a random variant.
        public string NotifMessage => PickRandomString(NotifMessages, "Herd spotted");
        public string MessageTitle => PickRandomString(MessageTitles, "Herd Spotted");
        public string Message => PickRandomString(Messages, "Your scouts spotted the tracks of wild beasts. Do you pursue a hunt?");

        public HerdBuildData(IEnumerable<string> notifMessages, IEnumerable<string> messageTitles, IEnumerable<string> messages,
                             string spawnId, int totalAmountInHerd, bool isPassive, float startingHealth, float maxSpeed,
                             float hitboxRange, int damageToPlayer, float sightRange, bool fleeOnAttacked,
                             ItemDropsData itemDropsIdAndCount, List<string> sceneIds, IEnumerable<TerrainType>? terrains = null)
        {
            NotifMessages.AddRange(notifMessages);
            MessageTitles.AddRange(messageTitles);
            Messages.AddRange(messages);
            if (terrains != null)
                Terrains.AddRange(terrains);

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

            // NOTE: deliberately does NOT touch CurrentHerdBuildData. Building the data must never
            // change which herd the player is hunting.
        }

        public ItemDropsData GetCopyOfItemDrops()
        {
            return ItemDrops;
        }

        /// <summary>Number of animals that may be alive at the same time, clamped by <see cref="Settings.MaxAliveAnimalsPerHunt"/>.</summary>
        public int GetAliveAnimalCap()
        {
            int cap = Settings.Instance.MaxAliveAnimalsPerHunt;
            if (TotalAmountInHerd <= 0)
                return 1;
            return TotalAmountInHerd > cap ? cap : TotalAmountInHerd;
        }

        public bool MatchesTerrain(TerrainType terrain)
        {
            return Terrains.Count == 0 || Terrains.Contains(terrain);
        }

        private static string PickRandomString(List<string> options, string fallback)
        {
            if (options == null || options.Count == 0)
                return fallback;
            if (options.Count == 1)
                return options[0];
            return options[MBRandom.RandomInt(0, options.Count)];
        }

        public static void BuildAll()
        {
            allHuntableAgentBuildDatas.Clear();

            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string xmlFileName = Path.Combine(assemblyFolder, "hunting_herds.xml");

            XElement huntingHerds = XElement.Load(xmlFileName);

            foreach (XElement element in huntingHerds.Descendants("Herd"))
            {
                try
                {
                    List<string> notifMessages = ReadAllStrings(element, "notifMessage");
                    List<string> messageTitles = ReadAllStrings(element, "messageTitle");
                    List<string> messages = ReadAllStrings(element, "message");
                    string spawnId = element.Element("spawnId").Value;
                    int totalAmountInHerd = (int)element.Element("totalAmountInHerd");
                    bool isPassive = element.Element("isPassive").Value.ToLower() == "true";
                    float startingHealth = (float)element.Element("startingHealth");
                    float maxSpeed = (float)element.Element("maxSpeed");
                    float hitboxRange = (float)element.Element("hitboxRange");
                    int damageToPlayer = (int)element.Element("damageToPlayer");
                    float sightRange = (float)element.Element("sightRange");
                    bool fleeOnAttacked = element.Element("fleeOnAttacked").Value.ToLower() == "true";

                    List<ItemDrop> item = new();
                    XElement? itemDropsElement = element.Element("ItemDrops");
                    if (itemDropsElement != null)
                        foreach (XElement itemDrop in itemDropsElement.Descendants("ItemDrop"))
                        {
                            int amount = (int)itemDrop.Element("amount");
                            int maxAmount = amount;
                            XElement? maxAmountNode = itemDrop.Element("maxAmount");
                            if (maxAmountNode != null)
                                maxAmount = (int)maxAmountNode;
                            item.Add(new(itemDrop.Element("itemId").Value, amount, maxAmount, 1));
                        }

                    ItemDropsData itemDrops = new(item, $"HH_{spawnId}_item_drops");
                    List<string> sceneIds = new();
                    XElement? sceneIdsElement = element.Element("SceneIds");
                    if (sceneIdsElement != null)
                        foreach (XElement sceneId in sceneIdsElement.Descendants("sceneId"))
                            sceneIds.Add(sceneId.Value);

                    List<TerrainType> terrains = ReadTerrains(element);

                    HerdBuildData buildData = new(notifMessages, messageTitles, messages, spawnId, totalAmountInHerd,
                                                  isPassive, startingHealth, maxSpeed, hitboxRange, damageToPlayer,
                                                  sightRange, fleeOnAttacked, itemDrops, sceneIds, terrains)
                    {
                        Index = allHuntableAgentBuildDatas.Count
                    };
                    allHuntableAgentBuildDatas.Add(buildData);
                }
                catch (Exception e)
                {
                    SubModule.PrintDebugMessage($"HuntableHerds: could not read a <Herd> entry from hunting_herds.xml ({e.Message})", 255, 0, 0);
                }
            }

            // Keep a sane default so code that still reads the static before any hunt starts is safe.
            if (CurrentHerdBuildData == null && allHuntableAgentBuildDatas.Count > 0)
                CurrentHerdBuildData = allHuntableAgentBuildDatas[0];
        }

        /// <summary>
        /// Reads every occurrence of <paramref name="name"/> (the element is repeatable so a herd can
        /// declare several text variants). Always returns at least one entry when the element exists.
        /// </summary>
        private static List<string> ReadAllStrings(XElement herd, string name)
        {
            List<string> values = new();
            foreach (XElement child in herd.Elements(name))
            {
                string value = child.Value?.Trim() ?? string.Empty;
                if (value.Length > 0)
                    values.Add(value);
            }
            return values;
        }

        private static List<TerrainType> ReadTerrains(XElement herd)
        {
            List<TerrainType> terrains = new();
            IEnumerable<XElement> nodes = herd.Elements("terrain");
            XElement? container = herd.Element("Terrains");
            if (container != null)
                nodes = nodes.Concat(container.Elements("terrain"));

            foreach (XElement node in nodes)
            {
                string raw = node.Value?.Trim() ?? string.Empty;
                if (raw.Length == 0)
                    continue;
                if (Enum.TryParse(raw, true, out TerrainType parsed))
                    terrains.Add(parsed);
                else
                    SubModule.PrintDebugMessage($"HuntableHerds: unknown <terrain> value \"{raw}\" in hunting_herds.xml", 255, 200, 0);
            }
            return terrains;
        }

        /// <summary>Picks a random herd, preferring the ones allowed on <paramref name="terrain"/>.</summary>
        public static HerdBuildData? PickRandom(TerrainType? terrain)
        {
            if (allHuntableAgentBuildDatas.Count == 0)
                return null;

            if (terrain.HasValue && Settings.Instance.FilterHerdsByTerrain)
            {
                List<HerdBuildData> matching = allHuntableAgentBuildDatas.Where(h => h.MatchesTerrain(terrain.Value)).ToList();
                if (matching.Count > 0)
                    return matching[MBRandom.RandomInt(0, matching.Count)];
            }

            return allHuntableAgentBuildDatas[MBRandom.RandomInt(0, allHuntableAgentBuildDatas.Count)];
        }

        /// <summary>
        /// Resolves the herd a saved notification points at. Falls back to the spawn id and finally
        /// to a random herd, so a hunting_herds.xml edited between saves can never NRE.
        /// </summary>
        public static HerdBuildData? Resolve(int index, string? spawnId)
        {
            if (index >= 0 && index < allHuntableAgentBuildDatas.Count)
            {
                HerdBuildData candidate = allHuntableAgentBuildDatas[index];
                if (string.IsNullOrEmpty(spawnId) || candidate.SpawnId == spawnId)
                    return candidate;
            }

            if (!string.IsNullOrEmpty(spawnId))
            {
                HerdBuildData? bySpawnId = allHuntableAgentBuildDatas.FirstOrDefault(h => h.SpawnId == spawnId);
                if (bySpawnId != null)
                    return bySpawnId;
            }

            return PickRandom(null);
        }

        /// <summary>Legacy helper: randomizes the static "current herd". Kept for compatibility.</summary>
        public static void Randomize()
        {
            HerdBuildData? picked = PickRandom(null);
            if (picked != null)
                CurrentHerdBuildData = picked;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    /// <summary>
    /// External append-only memory for narrative AI state.
    ///
    /// The campaign save should only hold mechanical state needed to keep the
    /// game working: active requests, objective progress, ids, flags, rewards.
    /// Long-form narrative memory lives here instead, under Documents, so it can
    /// grow without bloating or destabilizing Bannerlord save data.
    /// </summary>
    public static class AIMemoryStore
    {
        private const int MaxTextLength = 1000;

        private static readonly object _lock = new object();

        private static readonly string MemoryDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Configs",
            "RealmsForgotten",
            "AI_Memory");

        private static readonly string NpcMemoryPath = Path.Combine(MemoryDirectory, "npc_memories.jsonl");
        private static readonly string WorldEventPath = Path.Combine(MemoryDirectory, "world_events.jsonl");

        public static string DirectoryPath => MemoryDirectory;

        public static void AddNpcMemory(string heroId, string note, int day)
        {
            if (string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(note))
                return;

            Append(NpcMemoryPath, new AIMemoryRecord
            {
                Kind = "npc_memory",
                SubjectId = heroId.Trim(),
                Day = day,
                Text = Clean(note),
                CampaignKey = GetCampaignKey(),
                CreatedUtc = DateTime.UtcNow.ToString("o")
            });
        }

        public static void AddWorldEvent(string description, int day)
        {
            if (string.IsNullOrWhiteSpace(description))
                return;

            Append(WorldEventPath, new AIMemoryRecord
            {
                Kind = "world_event",
                SubjectId = "world",
                Day = day,
                Text = Clean(description),
                CampaignKey = GetCampaignKey(),
                CreatedUtc = DateTime.UtcNow.ToString("o")
            });
        }

        public static List<AIMemoryRecord> GetNpcMemories(string heroId, int maxCount = 8, int maxDays = 0)
        {
            if (string.IsNullOrWhiteSpace(heroId))
                return new List<AIMemoryRecord>();

            return ReadRecent(
                NpcMemoryPath,
                maxCount,
                maxDays,
                r => r.Kind == "npc_memory" &&
                     r.SubjectId.Equals(heroId, StringComparison.OrdinalIgnoreCase));
        }

        public static List<AIMemoryRecord> GetWorldEvents(int maxCount = 40, int maxDays = 0)
        {
            return ReadRecent(
                WorldEventPath,
                maxCount,
                maxDays,
                r => r.Kind == "world_event");
        }

        private static void Append(string path, AIMemoryRecord record)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(MemoryDirectory);
                    string json = JsonConvert.SerializeObject(record, Formatting.None);
                    File.AppendAllText(path, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIMemoryStore append failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static List<AIMemoryRecord> ReadRecent(
            string path,
            int maxCount,
            int maxDays,
            Func<AIMemoryRecord, bool> predicate)
        {
            var result = new List<AIMemoryRecord>();

            try
            {
                if (!File.Exists(path))
                    return result;

                int currentDay = CurrentDay();
                string campaignKey = GetCampaignKey();
                var lines = File.ReadAllLines(path);

                for (int i = lines.Length - 1; i >= 0 && result.Count < maxCount; i--)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    AIMemoryRecord? record = null;
                    try { record = JsonConvert.DeserializeObject<AIMemoryRecord>(line); }
                    catch { }

                    if (record == null || string.IsNullOrWhiteSpace(record.Text))
                        continue;

                    if (!string.IsNullOrWhiteSpace(campaignKey) &&
                        !string.IsNullOrWhiteSpace(record.CampaignKey) &&
                        !record.CampaignKey.Equals(campaignKey, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (maxDays > 0 && currentDay > 0 && currentDay - record.Day > maxDays)
                        continue;

                    if (predicate(record))
                        result.Add(record);
                }

                result.Reverse();
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIMemoryStore read failed: {ex.GetType().Name}: {ex.Message}");
            }

            return result;
        }

        private static string Clean(string text)
        {
            text = (text ?? "")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();

            return text.Length <= MaxTextLength ? text : text.Substring(0, MaxTextLength) + "...";
        }

        private static int CurrentDay()
        {
            try
            {
                return (int)Campaign.Current.Models.CampaignTimeModel.CampaignStartTime.ElapsedDaysUntilNow;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetCampaignKey()
        {
            try
            {
                string heroId = Hero.MainHero?.StringId ?? "";
                string clanId = Hero.MainHero?.Clan?.StringId ?? "";
                string campaignId = Campaign.Current?.UniqueGameId ?? "";

                if (!string.IsNullOrWhiteSpace(campaignId))
                    return campaignId;
                if (!string.IsNullOrWhiteSpace(heroId) || !string.IsNullOrWhiteSpace(clanId))
                    return $"{heroId}:{clanId}";
            }
            catch { }

            return "";
        }
    }

    public sealed class AIMemoryRecord
    {
        [JsonProperty("kind")]
        public string Kind { get; set; } = "";

        [JsonProperty("subject_id")]
        public string SubjectId { get; set; } = "";

        [JsonProperty("day")]
        public int Day { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; } = "";

        [JsonProperty("campaign_key")]
        public string CampaignKey { get; set; } = "";

        [JsonProperty("created_utc")]
        public string CreatedUtc { get; set; } = "";
    }
}

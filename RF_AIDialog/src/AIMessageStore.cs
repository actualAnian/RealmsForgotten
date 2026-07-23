using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;

namespace RF_AIDialog
{
    public static class AIMessageStore
    {
        private static readonly object _lock = new object();

        private static readonly string MessageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Configs",
            "RealmsForgotten",
            "AI_Messages");

        private static readonly string StorePath = Path.Combine(MessageDirectory, "message_threads.json");

        public static void EnsureInitialized()
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(MessageDirectory);
                    if (!File.Exists(StorePath))
                        File.WriteAllText(StorePath, JsonConvert.SerializeObject(new AIMessageStoreData(), Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIMessageStore initialize failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public static AIMessageThread GetOrCreateThread(string heroAId, string heroBId, string topic, bool requiresMeeting, int currentDay)
        {
            lock (_lock)
            {
                var data = ReadUnsafe();
                string a = Normalize(heroAId);
                string b = Normalize(heroBId);

                var existing = data.Threads.FirstOrDefault(t =>
                    ParticipantsMatch(t, a, b) &&
                    string.Equals(t.Topic ?? "", topic ?? "", StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                    return existing;

                var created = new AIMessageThread
                {
                    ThreadId = $"msg_{Guid.NewGuid():N}",
                    ParticipantAId = a,
                    ParticipantBId = b,
                    Topic = string.IsNullOrWhiteSpace(topic) ? "general" : topic.Trim(),
                    RequiresMeeting = requiresMeeting,
                    CreatedDay = currentDay,
                    LastUpdatedDay = currentDay,
                    CampaignKey = GetCampaignKey()
                };

                data.Threads.Add(created);
                WriteUnsafe(data);
                return created;
            }
        }

        public static void AddMessage(AIMessageRecord message)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.MessageId))
                return;

            lock (_lock)
            {
                var data = ReadUnsafe();
                data.Messages.RemoveAll(m => string.Equals(m.MessageId, message.MessageId, StringComparison.OrdinalIgnoreCase));
                data.Messages.Add(message);

                var thread = data.Threads.FirstOrDefault(t => string.Equals(t.ThreadId, message.ThreadId, StringComparison.OrdinalIgnoreCase));
                if (thread != null)
                {
                    thread.LastUpdatedDay = Math.Max(thread.LastUpdatedDay, message.ArrivalDay > 0 ? message.ArrivalDay : message.SentDay);
                    thread.RequiresMeeting = thread.RequiresMeeting || message.RequiresMeeting;
                }

                WriteUnsafe(data);
            }
        }

        public static void RemoveMessage(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return;

            lock (_lock)
            {
                var data = ReadUnsafe();
                data.Messages.RemoveAll(m => string.Equals(m.MessageId, messageId.Trim(), StringComparison.OrdinalIgnoreCase));
                WriteUnsafe(data);
            }
        }

        public static AIMessageRecord? GetMessage(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return null;

            lock (_lock)
            {
                var data = ReadUnsafe();
                return data.Messages.FirstOrDefault(m =>
                    string.Equals(m.MessageId, messageId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    CampaignMatches(m.CampaignKey));
            }
        }

        public static List<AIMessageRecord> GetThreadMessages(string threadId, int maxCount = 8)
        {
            if (string.IsNullOrWhiteSpace(threadId))
                return new List<AIMessageRecord>();

            lock (_lock)
            {
                var data = ReadUnsafe();
                return data.Messages
                    .Where(m => string.Equals(m.ThreadId, threadId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                CampaignMatches(m.CampaignKey))
                    .OrderBy(m => m.SentDay)
                    .ThenBy(m => m.CreatedUtc)
                    .Take(Math.Max(1, maxCount))
                    .ToList();
            }
        }

        public static bool HasOpenInitiativeThreadForSender(string senderHeroId, string recipientHeroId)
        {
            if (string.IsNullOrWhiteSpace(senderHeroId) || string.IsNullOrWhiteSpace(recipientHeroId))
                return false;

            string sender = Normalize(senderHeroId);
            string recipient = Normalize(recipientHeroId);

            lock (_lock)
            {
                var data = ReadUnsafe();
                return data.Messages.Any(m =>
                    CampaignMatches(m.CampaignKey) &&
                    string.Equals(m.SenderHeroId, sender, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.RecipientHeroId, recipient, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.Topic ?? "", "initiative", StringComparison.OrdinalIgnoreCase) &&
                    (m.State == AIMessageState.Drafting ||
                     m.State == AIMessageState.InTransit ||
                     m.State == AIMessageState.Arrived ||
                     // Read only counts as open while it still awaits a reply.
                     // A read-and-done letter (RequiresReply=false) is terminal;
                     // treating it as open permanently blocked new letters.
                     (m.State == AIMessageState.Read && m.RequiresReply)));
            }
        }

        public static bool HasOpenInitiativeThreadForRecipient(string recipientHeroId)
        {
            if (string.IsNullOrWhiteSpace(recipientHeroId))
                return false;

            string recipient = Normalize(recipientHeroId);

            lock (_lock)
            {
                var data = ReadUnsafe();
                return data.Messages.Any(m =>
                    CampaignMatches(m.CampaignKey) &&
                    string.Equals(m.RecipientHeroId, recipient, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.Topic ?? "", "initiative", StringComparison.OrdinalIgnoreCase) &&
                    (m.State == AIMessageState.Drafting ||
                     m.State == AIMessageState.InTransit ||
                     m.State == AIMessageState.Arrived ||
                     // Read only counts as open while it still awaits a reply
                     // (see HasOpenInitiativeThreadForSender).
                     (m.State == AIMessageState.Read && m.RequiresReply)));
            }
        }

        public static bool HasRecentInitiativeForRecipient(string recipientHeroId, int currentDay, int lookbackDays)
        {
            if (string.IsNullOrWhiteSpace(recipientHeroId))
                return false;

            string recipient = Normalize(recipientHeroId);
            lookbackDays = Math.Max(1, lookbackDays);

            lock (_lock)
            {
                var data = ReadUnsafe();
                return data.Messages.Any(m =>
                    CampaignMatches(m.CampaignKey) &&
                    string.Equals(m.RecipientHeroId, recipient, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.Topic ?? "", "initiative", StringComparison.OrdinalIgnoreCase) &&
                    Math.Max(m.SentDay, m.ArrivalDay) >= currentDay - lookbackDays);
            }
        }

        public static List<AIMessageRecord> MarkDueMessagesArrived(int currentDay)
        {
            var arrived = new List<AIMessageRecord>();

            lock (_lock)
            {
                var data = ReadUnsafe();
                foreach (var message in data.Messages)
                {
                    if (!CampaignMatches(message.CampaignKey))
                        continue;

                    if (message.State != AIMessageState.InTransit)
                        continue;

                    if (message.ArrivalDay > currentDay)
                        continue;

                    message.State = AIMessageState.Arrived;
                    arrived.Add(Clone(message));
                }

                if (arrived.Count > 0)
                    WriteUnsafe(data);
            }

            return arrived;
        }

        public static List<AIMessageRecord> GetMessagesNeedingReminder(string recipientHeroId, int currentDay, int reminderIntervalDays)
        {
            string recipient = Normalize(recipientHeroId);
            reminderIntervalDays = Math.Max(1, reminderIntervalDays);

            lock (_lock)
            {
                var data = ReadUnsafe();
                return data.Messages
                    .Where(m => CampaignMatches(m.CampaignKey))
                    .Where(m => string.Equals(m.RecipientHeroId, recipient, StringComparison.OrdinalIgnoreCase))
                    .Where(m => m.State == AIMessageState.Arrived ||
                                (m.RequiresReply && m.State == AIMessageState.Read))
                    .Where(m => currentDay - Math.Max(m.LastReminderDay, m.ArrivalDay) >= reminderIntervalDays)
                    .OrderBy(m => m.ArrivalDay)
                    .Select(Clone)
                    .ToList();
            }
        }

        public static void MarkNotificationShown(string messageId, int currentDay)
        {
            UpdateMessage(messageId, m =>
            {
                m.NotificationShown = true;
                m.LastReminderDay = currentDay;
            });
        }

        public static void MarkRead(string messageId, int currentDay)
        {
            UpdateMessage(messageId, m =>
            {
                if (m.State == AIMessageState.Arrived)
                    m.State = AIMessageState.Read;
                if (m.ReadDay < 0)
                    m.ReadDay = currentDay;
            });
        }

        public static void MarkReplied(string messageId, int currentDay)
        {
            UpdateMessage(messageId, m =>
            {
                m.State = AIMessageState.Replied;
                m.RepliedDay = currentDay;
            });
        }

        public static void MarkIgnored(string messageId, int currentDay)
        {
            UpdateMessage(messageId, m =>
            {
                m.State = AIMessageState.Ignored;
                m.LastReminderDay = currentDay;
            });
        }

        /// <summary>
        /// Marks every Drafting record as Failed. A Drafting record that
        /// survived to disk means the async generation task never completed
        /// (game closed/crashed mid-generation); left as Drafting it counts as
        /// an open initiative thread forever and blocks all new letters.
        /// Call once on game load. Returns how many were expired.
        /// </summary>
        public static int ExpireStaleDrafts()
        {
            lock (_lock)
            {
                var data = ReadUnsafe();
                int count = 0;
                foreach (var m in data.Messages)
                {
                    if (m.State == AIMessageState.Drafting)
                    {
                        m.State = AIMessageState.Failed;
                        count++;
                    }
                }

                if (count > 0)
                    WriteUnsafe(data);

                return count;
            }
        }

        private static void UpdateMessage(string messageId, Action<AIMessageRecord> mutator)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return;

            lock (_lock)
            {
                var data = ReadUnsafe();
                var message = data.Messages.FirstOrDefault(m =>
                    string.Equals(m.MessageId, messageId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    CampaignMatches(m.CampaignKey));
                if (message == null)
                    return;

                mutator(message);
                WriteUnsafe(data);
            }
        }

        private static AIMessageStoreData ReadUnsafe()
        {
            try
            {
                if (!File.Exists(StorePath))
                    return new AIMessageStoreData();

                var data = JsonConvert.DeserializeObject<AIMessageStoreData>(File.ReadAllText(StorePath))
                           ?? new AIMessageStoreData();

                data.Threads ??= new List<AIMessageThread>();
                data.Messages ??= new List<AIMessageRecord>();
                return data;
            }
            catch
            {
                return new AIMessageStoreData();
            }
        }

        private static void WriteUnsafe(AIMessageStoreData data)
        {
            Directory.CreateDirectory(MessageDirectory);
            // Temp + rename: a crash mid-write used to leave a truncated file,
            // which ReadUnsafe silently turns into an EMPTY store — every
            // letter and thread the player ever had, gone. The rename is
            // atomic-enough on NTFS; the payload is never half-written.
            string tempPath = StorePath + ".tmp";
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            if (File.Exists(StorePath))
            {
                File.Replace(tempPath, StorePath, null);
            }
            else
            {
                File.Move(tempPath, StorePath);
            }
        }

        private static bool ParticipantsMatch(AIMessageThread thread, string a, string b)
        {
            return (string.Equals(thread.ParticipantAId, a, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(thread.ParticipantBId, b, StringComparison.OrdinalIgnoreCase)) ||
                   (string.Equals(thread.ParticipantAId, b, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(thread.ParticipantBId, a, StringComparison.OrdinalIgnoreCase));
        }

        private static bool CampaignMatches(string recordCampaignKey)
        {
            string campaignKey = GetCampaignKey();
            if (string.IsNullOrWhiteSpace(campaignKey) || string.IsNullOrWhiteSpace(recordCampaignKey))
                return true;

            return string.Equals(campaignKey, recordCampaignKey, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCampaignKey()
        {
            try
            {
                string campaignId = Campaign.Current?.UniqueGameId ?? "";
                if (!string.IsNullOrWhiteSpace(campaignId))
                    return campaignId;

                string heroId = Hero.MainHero?.StringId ?? "";
                string clanId = Hero.MainHero?.Clan?.StringId ?? "";
                if (!string.IsNullOrWhiteSpace(heroId) || !string.IsNullOrWhiteSpace(clanId))
                    return $"{heroId}:{clanId}";
            }
            catch { }

            return "";
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }

        private static AIMessageRecord Clone(AIMessageRecord source)
        {
            return JsonConvert.DeserializeObject<AIMessageRecord>(
                       JsonConvert.SerializeObject(source))
                   ?? new AIMessageRecord();
        }
    }
}

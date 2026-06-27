using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    public static class AIMessageState
    {
        public const string Drafting = "drafting";
        public const string InTransit = "in_transit";
        public const string Arrived = "arrived";
        public const string Read = "read";
        public const string Replied = "replied";
        public const string Ignored = "ignored";
        public const string Failed = "failed";
    }

    public sealed class AIMessageThread
    {
        [JsonProperty("thread_id")]
        public string ThreadId { get; set; } = "";

        [JsonProperty("participant_a")]
        public string ParticipantAId { get; set; } = "";

        [JsonProperty("participant_b")]
        public string ParticipantBId { get; set; } = "";

        [JsonProperty("topic")]
        public string Topic { get; set; } = "general";

        [JsonProperty("requires_meeting")]
        public bool RequiresMeeting { get; set; }

        [JsonProperty("created_day")]
        public int CreatedDay { get; set; }

        [JsonProperty("last_updated_day")]
        public int LastUpdatedDay { get; set; }

        [JsonProperty("campaign_key")]
        public string CampaignKey { get; set; } = "";
    }

    public sealed class AIMessageRecord
    {
        [JsonProperty("message_id")]
        public string MessageId { get; set; } = "";

        [JsonProperty("thread_id")]
        public string ThreadId { get; set; } = "";

        [JsonProperty("sender_hero_id")]
        public string SenderHeroId { get; set; } = "";

        [JsonProperty("sender_name")]
        public string SenderName { get; set; } = "";

        [JsonProperty("recipient_hero_id")]
        public string RecipientHeroId { get; set; } = "";

        [JsonProperty("recipient_name")]
        public string RecipientName { get; set; } = "";

        [JsonProperty("topic")]
        public string Topic { get; set; } = "general";

        [JsonProperty("state")]
        public string State { get; set; } = AIMessageState.InTransit;

        [JsonProperty("requires_reply")]
        public bool RequiresReply { get; set; }

        [JsonProperty("requires_meeting")]
        public bool RequiresMeeting { get; set; }

        [JsonProperty("sent_day")]
        public int SentDay { get; set; }

        [JsonProperty("arrival_day")]
        public int ArrivalDay { get; set; }

        [JsonProperty("read_day")]
        public int ReadDay { get; set; } = -1;

        [JsonProperty("replied_day")]
        public int RepliedDay { get; set; } = -1;

        [JsonProperty("last_reminder_day")]
        public int LastReminderDay { get; set; } = -1;

        [JsonProperty("notification_shown")]
        public bool NotificationShown { get; set; }

        [JsonProperty("parent_message_id")]
        public string ParentMessageId { get; set; } = "";

        [JsonProperty("message_text")]
        public string MessageText { get; set; } = "";

        [JsonProperty("campaign_key")]
        public string CampaignKey { get; set; } = "";

        [JsonProperty("created_utc")]
        public string CreatedUtc { get; set; } = DateTime.UtcNow.ToString("o");
    }

    internal sealed class AIMessageStoreData
    {
        [JsonProperty("threads")]
        public List<AIMessageThread> Threads { get; set; } = new List<AIMessageThread>();

        [JsonProperty("messages")]
        public List<AIMessageRecord> Messages { get; set; } = new List<AIMessageRecord>();
    }

    public sealed class AILetterResponse
    {
        [JsonProperty("message_text")]
        public string MessageText { get; set; } = "";

        [JsonProperty("topic")]
        public string Topic { get; set; } = "general";

        [JsonProperty("requires_reply")]
        public bool RequiresReply { get; set; } = true;

        [JsonProperty("escalate_to_meeting")]
        public bool EscalateToMeeting { get; set; }
    }
}

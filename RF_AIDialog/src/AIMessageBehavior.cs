using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    public class AIMessageBehavior : CampaignBehaviorBase
    {
        public static AIMessageBehavior? Instance { get; private set; }
        private bool _popupOpen;

        public override void RegisterEvents()
        {
            Instance = this;
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnGameLoadFinished()
        {
            AIMessageStore.EnsureInitialized();
        }

        private void OnDailyTick()
        {
            AIMessageStore.EnsureInitialized();

            try
            {
                int currentDay = CurrentDay();
                var newlyArrived = AIMessageStore.MarkDueMessagesArrived(currentDay);
                if (newlyArrived.Count > 0)
                {
                    foreach (var message in newlyArrived.Where(IsForPlayer))
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"[Letters] A messenger from {ResolveHeroName(message.SenderHeroId, message.SenderName)} has arrived.",
                            Color.FromUint(0xFF_A0_D0_FFu)));
                    }
                }

                if (_popupOpen)
                    return;

                if (Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false)
                    return;

                var pending = AIMessageStore
                    .GetMessagesNeedingReminder(Hero.MainHero.StringId, currentDay, AIConfig.LettersReplyReminderDays)
                    .FirstOrDefault();

                if (pending != null)
                    ShowArrivalInquiry(pending, currentDay);
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIMessageBehavior.OnDailyTick failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public bool QueueInitiativeLetter(Hero sender, string reason, NPCContext? context)
        {
            try
            {
                if (!AIConfig.LettersEnabled || sender == null || Hero.MainHero == null)
                    return false;

                if (AIMessageStore.HasOpenInitiativeThreadForSender(sender.StringId, Hero.MainHero.StringId))
                    return false;

                int currentDay = CurrentDay();
                var thread = AIMessageStore.GetOrCreateThread(sender.StringId, Hero.MainHero.StringId, "initiative", false, currentDay);
                var draft = new AIMessageRecord
                {
                    MessageId = $"msg_{Guid.NewGuid():N}",
                    ThreadId = thread.ThreadId,
                    SenderHeroId = sender.StringId,
                    SenderName = sender.Name?.ToString() ?? sender.StringId,
                    RecipientHeroId = Hero.MainHero.StringId,
                    RecipientName = Hero.MainHero.Name?.ToString() ?? "Player",
                    Topic = "initiative",
                    State = AIMessageState.Drafting,
                    RequiresReply = true,
                    SentDay = currentDay,
                    ArrivalDay = currentDay + MessengerTravelCalculator.EstimateDays(sender, Hero.MainHero),
                    CampaignKey = Campaign.Current?.UniqueGameId ?? "",
                    CreatedUtc = DateTime.UtcNow.ToString("o")
                };

                AIMessageStore.AddMessage(draft);
                string prompt = LetterPromptBuilder.BuildInitiativePrompt(sender, context, reason);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        string raw = await AIClient.AskAsync(
                            AIConfig.APIModelName,
                            prompt,
                            reason,
                            maxTokens: 320).ConfigureAwait(false);

                        string? json = JsonCleaner.ExtractJson(raw);
                        var parsed = !string.IsNullOrWhiteSpace(json)
                            ? JsonConvert.DeserializeObject<AILetterResponse>(json)
                            : null;

                        if (parsed == null || string.IsNullOrWhiteSpace(parsed.MessageText))
                            throw new Exception("Letter JSON was empty.");

                        draft.MessageText = parsed.MessageText.Trim();
                        draft.Topic = string.IsNullOrWhiteSpace(parsed.Topic) ? "initiative" : parsed.Topic.Trim();
                        draft.RequiresReply = parsed.RequiresReply;
                        draft.RequiresMeeting = parsed.EscalateToMeeting;
                        draft.State = AIMessageState.InTransit;
                        AIMessageStore.AddMessage(draft);
                        RFAIDebug.Log($"Letter queued from {sender.StringId} to player | topic={draft.Topic} | arrives={draft.ArrivalDay}");
                    }
                    catch (Exception ex)
                    {
                        RFAIDebug.Log($"QueueInitiativeLetter generation failed: {ex.GetType().Name}: {ex.Message}");
                        AIMessageStore.RemoveMessage(draft.MessageId);
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QueueInitiativeLetter failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private void ShowArrivalInquiry(AIMessageRecord message, int currentDay)
        {
            _popupOpen = true;
            AIMessageStore.MarkNotificationShown(message.MessageId, currentDay);

            string senderName = ResolveHeroName(message.SenderHeroId, message.SenderName);
            InformationManager.ShowInquiry(new InquiryData(
                titleText: "Messenger Arrived",
                text: $"A messenger from {senderName} has arrived.\n\nDo you want to read the message now?",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Read Now",
                negativeText: "Later",
                affirmativeAction: () =>
                {
                    _popupOpen = false;
                    ShowLetter(message.MessageId);
                },
                negativeAction: () =>
                {
                    _popupOpen = false;
                    AIMessageStore.MarkNotificationShown(message.MessageId, currentDay);
                }));
        }

        private void ShowLetter(string messageId)
        {
            var message = AIMessageStore.GetMessage(messageId);
            if (message == null)
                return;

            int currentDay = CurrentDay();
            AIMessageStore.MarkRead(message.MessageId, currentDay);
            message = AIMessageStore.GetMessage(messageId) ?? message;

            string senderName = ResolveHeroName(message.SenderHeroId, message.SenderName);
            string body = $"{message.MessageText}\n\nDelivered after {Math.Max(1, message.ArrivalDay - message.SentDay)} day(s) on the road.";

            bool canReply = message.RequiresReply &&
                            (message.State == AIMessageState.Arrived || message.State == AIMessageState.Read);

            _popupOpen = true;
            InformationManager.ShowInquiry(new InquiryData(
                titleText: $"Letter from {senderName}",
                text: body,
                isAffirmativeOptionShown: canReply,
                isNegativeOptionShown: true,
                affirmativeText: canReply ? "Reply" : "Close",
                negativeText: canReply ? "Close" : "Done",
                affirmativeAction: canReply
                    ? () =>
                    {
                        _popupOpen = false;
                        ShowReplyInquiry(message);
                    }
                    : () => { _popupOpen = false; },
                negativeAction: () => { _popupOpen = false; }));
        }

        private void ShowReplyInquiry(AIMessageRecord message)
        {
            string senderName = ResolveHeroName(message.SenderHeroId, message.SenderName);
            InformationManager.ShowTextInquiry(new TextInquiryData(
                titleText: $"Reply to {senderName}",
                text: "Compose your messenger reply. It will travel across the realm before an answer can return.",
                isAffirmativeOptionShown: true,
                isNegativeOptionShown: true,
                affirmativeText: "Send Letter",
                negativeText: "Cancel",
                affirmativeAction: replyText => QueuePlayerReply(message, replyText),
                negativeAction: () => { },
                shouldInputBeObfuscated: false,
                textCondition: null,
                soundEventPath: "",
                defaultInputText: ""));
        }

        private void QueuePlayerReply(AIMessageRecord inboundMessage, string replyText)
        {
            if (string.IsNullOrWhiteSpace(replyText))
                return;

            try
            {
                Hero? sender = ResolveHero(inboundMessage.SenderHeroId);
                Hero player = Hero.MainHero;
                int currentDay = CurrentDay();
                int outboundDelay = MessengerTravelCalculator.EstimateDays(player, sender);
                int returnDelay = MessengerTravelCalculator.EstimateDays(sender, player);

                var playerMessage = new AIMessageRecord
                {
                    MessageId = $"msg_{Guid.NewGuid():N}",
                    ThreadId = inboundMessage.ThreadId,
                    SenderHeroId = player.StringId,
                    SenderName = player.Name?.ToString() ?? "Player",
                    RecipientHeroId = inboundMessage.SenderHeroId,
                    RecipientName = ResolveHeroName(inboundMessage.SenderHeroId, inboundMessage.SenderName),
                    Topic = inboundMessage.Topic,
                    State = AIMessageState.InTransit,
                    RequiresReply = false,
                    SentDay = currentDay,
                    ArrivalDay = currentDay + outboundDelay,
                    ParentMessageId = inboundMessage.MessageId,
                    MessageText = replyText.Trim(),
                    CampaignKey = Campaign.Current?.UniqueGameId ?? "",
                    CreatedUtc = DateTime.UtcNow.ToString("o")
                };
                AIMessageStore.AddMessage(playerMessage);
                AIMessageStore.MarkReplied(inboundMessage.MessageId, currentDay);

                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Letters] Your reply is on the road to {playerMessage.RecipientName}.",
                    Color.FromUint(0xFF_A0_D0_FFu)));

                if (sender == null || sender.IsDead)
                    return;

                NPCContext? context = NPCContextStore.Instance?.GetExisting(sender.StringId);
                List<AIMessageRecord> history = AIMessageStore.GetThreadMessages(inboundMessage.ThreadId, 8);
                int replyArrivalDay = currentDay + outboundDelay + returnDelay;
                string prompt = LetterPromptBuilder.BuildReplyPrompt(sender, context, replyText.Trim(), history);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        string raw = await AIClient.AskAsync(
                            AIConfig.APIModelName,
                            prompt,
                            replyText.Trim(),
                            maxTokens: 320).ConfigureAwait(false);

                        string? json = JsonCleaner.ExtractJson(raw);
                        var parsed = !string.IsNullOrWhiteSpace(json)
                            ? JsonConvert.DeserializeObject<AILetterResponse>(json)
                            : null;

                        if (parsed == null || string.IsNullOrWhiteSpace(parsed.MessageText))
                            throw new Exception("Reply letter JSON was empty.");

                        var response = new AIMessageRecord
                        {
                            MessageId = $"msg_{Guid.NewGuid():N}",
                            ThreadId = inboundMessage.ThreadId,
                            SenderHeroId = sender.StringId,
                            SenderName = sender.Name?.ToString() ?? sender.StringId,
                            RecipientHeroId = player.StringId,
                            RecipientName = player.Name?.ToString() ?? "Player",
                            Topic = string.IsNullOrWhiteSpace(parsed.Topic) ? inboundMessage.Topic : parsed.Topic.Trim(),
                            State = AIMessageState.InTransit,
                            RequiresReply = parsed.RequiresReply,
                            RequiresMeeting = parsed.EscalateToMeeting,
                            SentDay = currentDay + outboundDelay,
                            ArrivalDay = replyArrivalDay,
                            ParentMessageId = playerMessage.MessageId,
                            MessageText = parsed.MessageText.Trim(),
                            CampaignKey = Campaign.Current?.UniqueGameId ?? "",
                            CreatedUtc = DateTime.UtcNow.ToString("o")
                        };

                        AIMessageStore.AddMessage(response);
                        RFAIDebug.Log($"Reply letter queued from {sender.StringId} to player | topic={response.Topic} | arrives={response.ArrivalDay}");
                    }
                    catch (Exception ex)
                    {
                        RFAIDebug.Log($"QueuePlayerReply follow-up generation failed: {ex.GetType().Name}: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"QueuePlayerReply failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static bool IsForPlayer(AIMessageRecord message)
        {
            return message != null &&
                   Hero.MainHero != null &&
                   string.Equals(message.RecipientHeroId, Hero.MainHero.StringId, StringComparison.OrdinalIgnoreCase);
        }

        private static Hero? ResolveHero(string heroId)
        {
            try
            {
                return Hero.FindFirst(h => h != null &&
                                           h.StringId.Equals(heroId ?? "", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveHeroName(string heroId, string fallbackName)
        {
            Hero? hero = ResolveHero(heroId);
            if (hero?.Name != null)
                return hero.Name.ToString();

            return string.IsNullOrWhiteSpace(fallbackName) ? "Unknown Sender" : fallbackName;
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
    }
}

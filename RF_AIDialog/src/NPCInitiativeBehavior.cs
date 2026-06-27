using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    /// <summary>
    /// Evaluates daily conditions for each living lord/notable and sets a
    /// PendingInitiativeReason on their NPCContext when they have a reason to
    /// seek out the player. The dialog system picks this up and presents a
    /// special option: "You seem like you have something on your mind..."
    ///
    /// Trigger priority, first match wins:
    ///   1. Pending request follow-up
    ///   2. Siege distress
    ///   3. Prisoner ransom
    ///   4. War coordination
    ///   5. Grievance
    /// </summary>
    public class NPCInitiativeBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTickHero);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnDailyTickHero(Hero hero)
        {
            try
            {
                if (!ShouldEvaluate(hero)) return;

                var store = NPCContextStore.Instance;
                if (store == null) return;

                var existingContext = store.GetExisting(hero.StringId);
                var ctx = existingContext ?? new NPCContext { HeroId = hero.StringId };
                int currentDay = CurrentDay();

                // Do not overwrite an existing pending initiative.
                if (ctx.HasPendingInitiative) return;
                if (ctx.LastInitiativeLetterDay > -100000 &&
                    currentDay - ctx.LastInitiativeLetterDay < NPCContext.InitiativeLetterCooldownDays)
                    return;

                if (!HasEstablishedContact(hero, ctx))
                    return;

                string? reason = EvaluateInitiative(hero, ctx);
                if (reason == null) return;

                if (existingContext == null)
                    ctx = store.GetOrCreate(hero);

                if (TryRouteInitiativeToLetter(hero, reason, ctx))
                {
                    ctx.PendingInitiativeReason = null;
                    ctx.LastInitiativeLetterDay = currentDay;
                    store.MarkDirty(ctx);
                    return;
                }

                ctx.PendingInitiativeReason = reason;
                store.MarkDirty(ctx);

                ShowAudienceNotification(hero);
            }
            catch { /* never crash on tick */ }
        }

        private static void ShowAudienceNotification(Hero hero)
        {
            try
            {
                string heroName = hero?.Name?.ToString() ?? "Someone";

                InformationManager.DisplayMessage(new InformationMessage(
                    $"{heroName} seeks an audience with you.",
                    Color.FromUint(0xFF_A0_D0_FFu)));

                bool inConversation = Campaign.Current?.ConversationManager?.IsConversationInProgress ?? false;
                if (inConversation)
                    return;

                InformationManager.ShowInquiry(new InquiryData(
                    titleText:                "Audience Requested",
                    text:                     $"A messenger reports that {heroName} seeks an audience with you.\n\nSpeak with them to learn what is on their mind.",
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown:    false,
                    affirmativeText:          "Understood",
                    negativeText:             "",
                    affirmativeAction:        () => { },
                    negativeAction:           null));
            }
            catch { }
        }

        private static bool ShouldEvaluate(Hero hero)
        {
            if (hero == null || hero == Hero.MainHero) return false;
            if (!hero.IsAlive || hero.IsChild) return false;

            var occ = hero.Occupation;
            return occ == Occupation.Lord
                || occ == Occupation.Merchant
                || occ == Occupation.RuralNotable
                || occ == Occupation.GangLeader
                || occ == Occupation.Artisan
                || occ == Occupation.Headman;
        }

        private static bool TryRouteInitiativeToLetter(Hero hero, string reason, NPCContext ctx)
        {
            try
            {
                if (!AIConfig.LettersEnabled)
                    return false;

                if (hero == null || !hero.IsLord)
                    return false;

                if (Hero.MainHero == null || MessengerTravelCalculator.IsLikelyNearby(hero, Hero.MainHero))
                    return false;

                int currentDay = CurrentDay();
                if (AIMessageStore.HasOpenInitiativeThreadForRecipient(Hero.MainHero.StringId))
                    return false;

                if (AIMessageStore.HasRecentInitiativeForRecipient(
                        Hero.MainHero.StringId,
                        currentDay,
                        NPCContext.GlobalInitiativeLetterSpacingDays))
                    return false;

                return AIMessageBehavior.Instance?.QueueInitiativeLetter(hero, reason, ctx) == true;
            }
            catch
            {
                return false;
            }
        }

        private static string? EvaluateInitiative(Hero hero, NPCContext ctx)
        {
            var player = Hero.MainHero;
            if (player == null) return null;

            int relation = (int)hero.GetRelationWithPlayer();
            bool politicallyAligned = ArePoliticallyAligned(hero, player);

            // 0. Pending request follow-up. If this NPC made a request and
            // has not heard back in 12+ days, seek the player out.
            try
            {
                if (ctx.HasPendingRequest)
                {
                    int currentDay = 0;
                    try
                    {
                        currentDay = (int)Campaign.Current.Models.CampaignTimeModel
                            .CampaignStartTime.ElapsedDaysUntilNow;
                    }
                    catch { }

                    int daysWaiting = currentDay - ctx.PendingRequest!.DayIssued;
                    if (daysWaiting >= 12)
                    {
                        return $"You are following up on a request you made {daysWaiting} days ago: " +
                               $"\"{ctx.PendingRequest.Description}\" - " +
                               "The player has not yet returned. Press them on it directly, but stay in character. " +
                               "You may be impatient, concerned, or understanding depending on your nature.";
                    }
                }
            }
            catch { }

            // 1. Siege distress.
            try
            {
                var home = hero.HomeSettlement ?? hero.BornSettlement;
                if (home != null &&
                    home.IsUnderSiege &&
                    home.OwnerClan == hero.Clan &&
                    (relation >= 5 || politicallyAligned))
                    return $"{home.Name} is under siege. You are desperate and need the player's military aid or counsel immediately.";
            }
            catch { }

            // 2. Prisoner ransom.
            try
            {
                bool holdsClansman = MobileParty.MainParty.PrisonRoster
                    .GetTroopRoster()
                    .Any(e => e.Character.IsHero
                           && e.Character.HeroObject?.Clan == hero.Clan
                           && e.Character.HeroObject != hero);

                if (holdsClansman && relation >= 0)
                    return "The player holds a member of your clan as prisoner. You want to negotiate their release through coin, trade, or appeal to honor.";
            }
            catch { }

            // 3. War coordination.
            try
            {
                if (hero.MapFaction is Kingdom npcKingdom
                    && player.MapFaction is Kingdom playerKingdom
                    && !FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom)
                    && relation >= 15
                    && IsStrategicallyImportantLord(hero))
                {
                    var sharedEnemy = Campaign.Current.Kingdoms
                        .FirstOrDefault(k => !k.IsEliminated
                                          && k != npcKingdom
                                          && k != playerKingdom
                                          && FactionManager.IsAtWarAgainstFaction(npcKingdom, k)
                                          && FactionManager.IsAtWarAgainstFaction(playerKingdom, k));

                    if (sharedEnemy != null)
                        return $"You and the player share a common enemy: {sharedEnemy.Name}. You want to coordinate your war effort and discuss joint strategy.";
                }
            }
            catch { }

            // 4. Grievance: relation dropped by 15 or more since last known conversation.
            try
            {
                if (ctx.LastKnownRelation != 0)
                {
                    int drop = ctx.LastKnownRelation - relation;
                    if (drop >= 15 && relation < 10)
                        return $"Your relationship with the player has deteriorated sharply (was {ctx.LastKnownRelation}, now {relation}). You want to understand what went wrong and, if possible, mend it.";
                }
            }
            catch { }

            return null;
        }

        private static int CurrentDay()
        {
            try
            {
                return (int)Campaign.Current.Models.CampaignTimeModel
                    .CampaignStartTime.ElapsedDaysUntilNow;
            }
            catch
            {
                return 0;
            }
        }

        private static bool HasEstablishedContact(Hero hero, NPCContext ctx)
        {
            try
            {
                if (hero == null)
                    return false;

                if (hero.HasMet)
                    return true;

                if (ctx.HasPendingRequest || ctx.LastKnownRelation != 0)
                    return true;

                if (!string.IsNullOrWhiteSpace(ctx.GeneratedPersonality))
                    return true;

                return ctx.RecentHistory != null && ctx.RecentHistory.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool ArePoliticallyAligned(Hero hero, Hero player)
        {
            try
            {
                if (hero == null || player == null)
                    return false;

                if (hero.Clan == player.Clan || hero.MapFaction == player.MapFaction)
                    return true;

                return hero.MapFaction is Kingdom heroKingdom &&
                       player.MapFaction is Kingdom playerKingdom &&
                       heroKingdom == playerKingdom;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsStrategicallyImportantLord(Hero hero)
        {
            try
            {
                if (hero == null || !hero.IsLord)
                    return false;

                if (hero.MapFaction?.Leader == hero)
                    return true;

                if (hero.Clan?.Leader == hero)
                    return true;

                return hero.PartyBelongedTo?.Army?.LeaderParty?.LeaderHero == hero;
            }
            catch
            {
                return false;
            }
        }
    }
}

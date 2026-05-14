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
    /// Trigger priority (first match wins, one per NPC per day):
    ///   1. Siege distress   — NPC's home settlement is besieged
    ///   2. Prisoner ransom  — player holds a clansman of this NPC
    ///   3. War coordination — NPC and player share a common enemy
    ///   4. Grievance        — relation dropped ≥ 15 since last conversation
    /// </summary>
    public class NPCInitiativeBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTickHero);
        }

        public override void SyncData(IDataStore dataStore) { }

        // ── Daily evaluation ──────────────────────────────────────────────

        private void OnDailyTickHero(Hero hero)
        {
            try
            {
                if (!ShouldEvaluate(hero)) return;

                var store = NPCContextStore.Instance;
                if (store == null) return;

                var existingContext = store.GetExisting(hero.StringId);
                var ctx = existingContext ?? new NPCContext { HeroId = hero.StringId };

                // Don't overwrite an existing pending initiative
                if (ctx.HasPendingInitiative) return;

                string? reason = EvaluateInitiative(hero, ctx);
                if (reason == null) return;

                if (existingContext == null)
                {
                    ctx = store.GetOrCreate(hero);
                }

                ctx.PendingInitiativeReason = reason;
                store.MarkDirty(ctx);

                // HUD notification — same blue as the response notification
                InformationManager.DisplayMessage(new InformationMessage(
                    $"💬 {hero.Name} seeks an audience with you.",
                    Color.FromUint(0xFF_A0_D0_FFu)));
            }
            catch { /* never crash on tick */ }
        }

        // ── Guard ─────────────────────────────────────────────────────────

        private static bool ShouldEvaluate(Hero hero)
        {
            if (hero == null || hero == Hero.MainHero) return false;
            if (!hero.IsAlive || hero.IsChild)         return false;

            // Only lords and notables — companions handled separately
            var occ = hero.Occupation;
            return occ == Occupation.Lord
                || occ == Occupation.Merchant
                || occ == Occupation.RuralNotable
                || occ == Occupation.GangLeader
                || occ == Occupation.Artisan
                || occ == Occupation.Headman;
        }

        // ── Condition evaluation ──────────────────────────────────────────

        private static string? EvaluateInitiative(Hero hero, NPCContext ctx)
        {
            var player = Hero.MainHero;
            if (player == null) return null;

            int relation = (int)hero.GetRelationWithPlayer();

            // ── 0. Pending request follow-up (highest priority) ───────────
            // If this NPC made a request and hasn't heard back in 12+ days, seek the player out.
            try
            {
                if (ctx.HasPendingRequest)
                {
                    int currentDay = 0;
                    try { currentDay = (int)Campaign.Current.Models.CampaignTimeModel
                              .CampaignStartTime.ElapsedDaysUntilNow; } catch { }

                    int daysWaiting = currentDay - ctx.PendingRequest!.DayIssued;
                    if (daysWaiting >= 12)
                    {
                        return $"You are following up on a request you made {daysWaiting} days ago: " +
                               $"\"{ctx.PendingRequest.Description}\" — " +
                               $"The player has not yet returned. Press them on it directly, but stay in character. " +
                               $"You may be impatient, concerned, or understanding depending on your nature.";
                    }
                }
            }
            catch { }

            // ── 1. Siege distress ─────────────────────────────────────────
            try
            {
                var home = hero.HomeSettlement ?? hero.BornSettlement;
                if (home != null && home.IsUnderSiege && home.OwnerClan == hero.Clan)
                    return $"{home.Name} is under siege. You are desperate and need the player's military aid or counsel immediately.";
            }
            catch { }

            // ── 2. Prisoner ransom ────────────────────────────────────────
            try
            {
                bool holdsClansman = MobileParty.MainParty.PrisonRoster
                    .GetTroopRoster()
                    .Any(e => e.Character.IsHero
                           && e.Character.HeroObject?.Clan == hero.Clan
                           && e.Character.HeroObject != hero);

                if (holdsClansman)
                    return "The player holds a member of your clan as prisoner. You want to negotiate their release — through coin, trade, or appeal to honor.";
            }
            catch { }

            // ── 3. War coordination ───────────────────────────────────────
            try
            {
                if (hero.MapFaction is Kingdom npcKingdom
                    && player.MapFaction is Kingdom playerKingdom
                    && !FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom)
                    && relation > 10)
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

            // ── 4. Grievance (relation dropped ≥ 15) ─────────────────────
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
    }
}

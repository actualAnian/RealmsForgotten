using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RF_warsystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten
{
    public class CapitulationSystemBehavior : CampaignBehaviorBase
    {
        private Dictionary<Kingdom, CampaignTime> _lastCapitulation = new Dictionary<Kingdom, CampaignTime>();
        private Dictionary<Kingdom, CampaignTime> _lastAidAppeal = new Dictionary<Kingdom, CampaignTime>();
        private readonly List<string> NonCapitulatingNations = new()
        {
            "aserai",
            "aserai_a",
            "aserai_b",
            "aserai_c",
            "aserai_d",
            "aserai_e",

        };
        // --- CONFIG ---
        private const float GraceDays = 25f; // No capitulations during first X days

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckCapitulations);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastCapitulation", ref _lastCapitulation);
            dataStore.SyncData("_lastAidAppeal", ref _lastAidAppeal);
        }

        private void CheckCapitulations()
        {
            foreach (var weak in Kingdom.All.ToList())
            {
                if (weak == null || weak.IsEliminated || weak.Leader == null)
                    continue;

                if (!CanCapitulate(weak))
                    continue;

                IEnumerable<Kingdom> enemies = weak.FactionsAtWarWith
                    .Where(f => f.IsKingdomFaction)
                    .Cast<Kingdom>()
                    .OrderByDescending(k => k.CurrentTotalStrength);

                foreach (var strong in enemies)
                {
                    if (strong == null || strong.IsEliminated)
                        continue;

                    if (TrySeekProtectiveAid(weak, strong))
                    {
                        _lastAidAppeal[weak] = CampaignTime.Now;
                        break;
                    }

                    if (!ShouldCapitulate(weak, strong))
                        continue;

                    if (weak == Hero.MainHero.Clan.Kingdom)
                    {
                        ShowPlayerSurrenderInquiry(weak, strong);
                    }
                    else if (strong == Hero.MainHero.Clan.Kingdom)
                    {
                        ShowAIDemandInquiry(weak, strong);
                    }
                    else
                    {
                        ApplyCapitulation(weak, strong);
                    }

                    _lastCapitulation[weak] = CampaignTime.Now;
                    break;
                }
            }
        }
        private bool CanCapitulate(Kingdom weak)
        {
            if (NonCapitulatingNations.Contains(weak.StringId))
                return false;
            if (CampaignTime.Now.ToDays < GraceDays)
                return false;
            if (_lastCapitulation.TryGetValue(weak, out var last))
            {
                return (CampaignTime.Now - last).ToDays > 15f;
            }
            return true;
        }

        private bool ShouldCapitulate(Kingdom weak, Kingdom strong)
        {
            int weakFiefs = weak.Fiefs.Count();
            float strengthRatio = strong.CurrentTotalStrength / (weak.CurrentTotalStrength + 1f);

            return weakFiefs <= 2 && strengthRatio >= 3.0f;
        }

        private bool TrySeekProtectiveAid(Kingdom weak, Kingdom strong)
        {
            if (weak == null || strong == null || weak.IsEliminated || strong.IsEliminated)
                return false;

            if (weak == Hero.MainHero.Clan?.Kingdom || strong == Hero.MainHero.Clan?.Kingdom)
                return false;

            int weakFiefs = weak.Fiefs.Count();
            if (weakFiefs > 2)
                return false;

            float strengthRatio = strong.CurrentTotalStrength / (weak.CurrentTotalStrength + 1f);
            if (strengthRatio < 2.25f)
                return false;

            if (_lastAidAppeal.TryGetValue(weak, out CampaignTime lastAid)
                && (CampaignTime.Now - lastAid).ToDays < 12f)
                return false;

            List<Kingdom> helpers = Kingdom.All
                .Where(k => CanConsiderHelper(k, weak, strong))
                .OrderByDescending(k => ScoreHelperKingdom(k, weak, strong))
                .ToList();

            if (helpers.Count == 0)
                return false;

            Kingdom helper = helpers[0];
            float helperScore = ScoreHelperKingdom(helper, weak, strong);
            if (helperScore < 45f)
                return false;

            bool absorbAsProtectedVassal =
                weakFiefs <= 1
                && strengthRatio >= 4f
                && string.Equals(helper.Culture?.StringId, weak.Culture?.StringId, StringComparison.OrdinalIgnoreCase)
                && helper.CurrentTotalStrength >= weak.CurrentTotalStrength * 1.5f;

            if (absorbAsProtectedVassal)
            {
                AbsorbKingdomAsProtectedVassal(weak, helper, strong);
                return true;
            }

            List<Kingdom> cultureBlocHelpers = helpers
                .Where(k => SharesCulture(k, weak) && ScoreHelperKingdom(k, weak, strong) >= 45f)
                .ToList();

            List<Kingdom> mobilizedHelpers = cultureBlocHelpers.Count > 0
                ? cultureBlocHelpers
                : new List<Kingdom> { helper };

            bool declaredAnyWar = false;
            List<Kingdom> helpersNeedingWar = new();
            foreach (Kingdom mobilizedHelper in mobilizedHelpers)
            {
                if (FactionManager.IsAtWarAgainstFaction(mobilizedHelper, strong))
                    continue;

                helpersNeedingWar.Add(mobilizedHelper);
            }

            if (helpersNeedingWar.Count > 0)
            {
                RFWarExternalIntentApi.ReinforceCollectiveDefense(strong, weak, helpersNeedingWar);
                declaredAnyWar = true;
            }

            if (!declaredAnyWar)
                return false;

            string helperNames = string.Join(", ", mobilizedHelpers.Select(x => x.Name.ToString()));
            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name} has appealed to {helperNames} for aid against {strong.Name}. They enter the war to protect the threatened realm.",
                Colors.Cyan));
            return true;
        }

        private bool CanConsiderHelper(Kingdom helper, Kingdom weak, Kingdom strong)
        {
            if (helper == null || helper.IsEliminated || helper == weak || helper == strong)
                return false;

            if (helper.Leader == null || helper.Clans.Count == 0)
                return false;

            if (FactionManager.IsAtWarAgainstFaction(helper, weak))
                return false;

            return SharesFrontier(helper, weak) || SharesFrontier(helper, strong) || SharesCulture(helper, weak);
        }

        private float ScoreHelperKingdom(Kingdom helper, Kingdom weak, Kingdom strong)
        {
            float score = 0f;

            if (SharesCulture(helper, weak))
                score += 25f;
            if (SharesFrontier(helper, weak))
                score += 20f;
            if (SharesFrontier(helper, strong))
                score += 12f;
            if (!FactionManager.IsAtWarAgainstFaction(helper, strong))
                score += 8f;
            if (helper.FactionsAtWarWith.Count(x => x.IsKingdomFaction) <= 1)
                score += 10f;
            if (helper.CurrentTotalStrength >= weak.CurrentTotalStrength * 1.5f)
                score += 10f;
            if (helper.CurrentTotalStrength >= strong.CurrentTotalStrength * 0.5f)
                score += 10f;

            return score;
        }

        private static bool SharesCulture(Kingdom left, Kingdom right)
        {
            return left?.Culture?.StringId != null
                && string.Equals(left.Culture.StringId, right?.Culture?.StringId, StringComparison.OrdinalIgnoreCase);
        }

        private static bool SharesFrontier(Kingdom left, Kingdom right)
        {
            if (left?.Fiefs == null || right?.Fiefs == null)
                return false;

            foreach (Town leftFief in left.Fiefs)
            {
                Vec2 leftPos = leftFief?.Settlement?.GetPosition2D ?? Vec2.Zero;
                foreach (Town rightFief in right.Fiefs)
                {
                    Vec2 rightPos = rightFief?.Settlement?.GetPosition2D ?? Vec2.Zero;
                    if (leftPos.Distance(rightPos) <= 120f)
                        return true;
                }
            }

            return false;
        }

        private void AbsorbKingdomAsProtectedVassal(Kingdom weak, Kingdom helper, Kingdom strong)
        {
            if (!FactionManager.IsAtWarAgainstFaction(helper, strong))
            {
                RFWarExternalIntentApi.ReinforceCollectiveDefense(strong, weak, new[] { helper });
            }

            foreach (var clan in weak.Clans.ToList())
            {
                if (clan.IsUnderMercenaryService)
                    continue;

                ChangeKingdomAction.ApplyByJoinToKingdom(clan, helper, showNotification: false);
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name}, facing destruction by {strong.Name}, has sworn itself to {helper.Name} in exchange for protection.",
                Colors.Yellow));

            DestroyKingdomAction.Apply(weak);
        }

        private void ShowPlayerSurrenderInquiry(Kingdom playerKingdom, Kingdom victor)
        {
            TextObject title = new TextObject("Demand for Surrender");
            TextObject text = new TextObject(
                "{VICTOR_LEADER} of {VICTOR_KINGDOM} demands your unconditional surrender. " +
                "Your kingdom is on the brink of collapse. You can accept to become a vassal " +
                "under their rule, or refuse and face the consequences.");

            text.SetTextVariable("VICTOR_LEADER", victor.Leader.Name);
            text.SetTextVariable("VICTOR_KINGDOM", victor.Name);

            InformationManager.ShowInquiry(new InquiryData(
                title.ToString(),
                text.ToString(),
                true,
                true,
                "Accept Vassalage",
                "Refuse and Fight!",
                () => ApplyPlayerVassalage(playerKingdom, victor),
                () => ApplyPlayerConcessions(playerKingdom, victor)
            ));
        }

        private void ShowAIDemandInquiry(Kingdom weak, Kingdom strong)
        {
            InformationManager.ShowInquiry(new InquiryData(
                $"{weak.Name} Offers Capitulation",
                $"{weak.Leader.Name} seeks to surrender. Do you accept their unconditional surrender?",
                true,
                true,
                "Accept",
                "Decline",
                () => ApplyCapitulation(weak, strong),
                null
            ));
        }

        private void ApplyPlayerVassalage(Kingdom playerKingdom, Kingdom victor)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"You have bent the knee. Your kingdom has been dissolved, and Clan {Clan.PlayerClan.Name} now serves {victor.Name}.",
                Colors.Yellow));

            foreach (var clan in playerKingdom.Clans.ToList())
            {
                ChangeKingdomAction.ApplyByJoinToKingdom(clan, victor);
            }

            DestroyKingdomAction.Apply(playerKingdom);
        }

        private void ApplyPlayerConcessions(Kingdom playerKingdom, Kingdom victor)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"You refused to surrender, but at a great cost. Your vassals have abandoned you for the cause of {victor.Name}!",
                Colors.Red));

            foreach (var clan in playerKingdom.Clans.ToList())
            {
                if (clan != Clan.PlayerClan)
                {
                    ChangeKingdomAction.ApplyByJoinToKingdom(clan, victor);
                }
            }

            RFWarExternalIntentApi.RequestCoalitionPeace(playerKingdom, victor);
        }

        private void ApplyCapitulation(Kingdom weak, Kingdom strong)
        {
            int tribute = MBRandom.RandomInt(3000, 8000);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name} has capitulated to {strong.Name}! Tribute paid: {tribute} gold."));

            GiveGoldAction.ApplyBetweenCharacters(weak.Leader, strong.Leader, tribute, false);

            foreach (var clan in weak.Clans.ToList())
            {
                if (clan.IsUnderMercenaryService)
                    continue;

                ChangeKingdomAction.ApplyByJoinToKingdom(clan, strong, showNotification: false);
            }

            DestroyKingdomAction.Apply(weak);
        }
    }
}

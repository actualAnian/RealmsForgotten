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
        /// <summary>
        /// External exemption hook (set via reflection by RF_Homesteads'
        /// nomad-kingdom feature). A kingdom for which this returns true is
        /// never pressured into capitulation — a nomad kingdom has 0 fiefs BY
        /// DESIGN and would otherwise be permanently eligible (fiefs <= 2).
        /// </summary>
        public static Func<Kingdom, bool> IsKingdomExemptFromCapitulation;

        private const int CapitulationAuditMaxLines = 120;
        private static int _remainingAuditLines = CapitulationAuditMaxLines;
        private Dictionary<Kingdom, CampaignTime> _lastCapitulation = new Dictionary<Kingdom, CampaignTime>();
        private Dictionary<Kingdom, CampaignTime> _lastAidAppeal = new Dictionary<Kingdom, CampaignTime>();
        private Dictionary<string, CampaignTime> _capitulationPressureSince = new Dictionary<string, CampaignTime>();
        private CampaignTime _lastWorldCapitulationAt = CampaignTime.Zero;
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
        private const float GraceDays = 90f; // No capitulations during first X days
        private const float DirectCapitulationPressureDays = 21f;
        private const float ProtectedVassalPressureDays = 14f;
        private const float GlobalCapitulationCooldownDays = 10f;
        private const float AidAppealCooldownDays = 20f;
        private const float AidStrengthRatio = 2.75f;
        private const float DirectCapitulationStrengthRatioOneFief = 3.75f;
        private const float DirectCapitulationStrengthRatioTwoFiefs = 4.5f;
        private const float ProtectedVassalStrengthRatio = 5f;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, CheckCapitulations);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastCapitulation", ref _lastCapitulation);
            dataStore.SyncData("_lastAidAppeal", ref _lastAidAppeal);
            dataStore.SyncData("_capitulationPressureSince", ref _capitulationPressureSince);
            dataStore.SyncData("_lastWorldCapitulationAt", ref _lastWorldCapitulationAt);

            _lastCapitulation ??= new Dictionary<Kingdom, CampaignTime>();
            _lastAidAppeal ??= new Dictionary<Kingdom, CampaignTime>();
            _capitulationPressureSince ??= new Dictionary<string, CampaignTime>();
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

                    AuditCapitulation(
                        $"CHECK weak={weak.StringId} strong={strong.StringId} weakFiefs={weak.Fiefs.Count()} weakStrength={weak.CurrentTotalStrength:F1} strongStrength={strong.CurrentTotalStrength:F1}");

                    if (TrySeekProtectiveAid(weak, strong))
                    {
                        AuditCapitulation($"AID weak={weak.StringId} strong={strong.StringId} result=protective_aid");
                        _lastAidAppeal[weak] = CampaignTime.Now;
                        break;
                    }

                    if (!ShouldCapitulate(weak, strong))
                    {
                        AuditCapitulation($"REJECT weak={weak.StringId} strong={strong.StringId} reason=threshold_not_met");
                        continue;
                    }

                    if (weak == Hero.MainHero.Clan.Kingdom)
                    {
                        AuditCapitulation($"PLAYER weak={weak.StringId} strong={strong.StringId} action=show_surrender_inquiry");
                        ShowPlayerSurrenderInquiry(weak, strong);
                    }
                    else if (strong == Hero.MainHero.Clan.Kingdom)
                    {
                        AuditCapitulation($"PLAYER strong={strong.StringId} weak={weak.StringId} action=show_ai_demand_inquiry");
                        ShowAIDemandInquiry(weak, strong);
                    }
                    else
                    {
                        AuditCapitulation($"CAPITULATE weak={weak.StringId} strong={strong.StringId} action=apply");
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
            if (_lastWorldCapitulationAt != CampaignTime.Zero
                && (CampaignTime.Now - _lastWorldCapitulationAt).ToDays < GlobalCapitulationCooldownDays)
                return false;
            if (_lastCapitulation.TryGetValue(weak, out var last))
            {
                return (CampaignTime.Now - last).ToDays > 30f;
            }
            return true;
        }

        private bool ShouldCapitulate(Kingdom weak, Kingdom strong)
        {
            if (IsKingdomExemptFromCapitulation?.Invoke(weak) == true)
            {
                ClearCapitulationPressure(weak, strong);
                return false;
            }

            int weakFiefs = weak.Fiefs.Count();
            float strengthRatio = strong.CurrentTotalStrength / (weak.CurrentTotalStrength + 1f);
            float requiredStrengthRatio = weakFiefs <= 1
                ? DirectCapitulationStrengthRatioOneFief
                : DirectCapitulationStrengthRatioTwoFiefs;

            AuditCapitulation($"THRESHOLD weak={weak.StringId} strong={strong.StringId} weakFiefs={weakFiefs} strengthRatio={strengthRatio:F2} required={requiredStrengthRatio:F2}");
            if (weakFiefs > 2 || strengthRatio < requiredStrengthRatio)
            {
                ClearCapitulationPressure(weak, strong);
                return false;
            }

            return HasSustainedCapitulationPressure(weak, strong, DirectCapitulationPressureDays);
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
            if (strengthRatio < AidStrengthRatio)
                return false;

            if (_lastAidAppeal.TryGetValue(weak, out CampaignTime lastAid)
                && (CampaignTime.Now - lastAid).ToDays < AidAppealCooldownDays)
                return false;

            List<Kingdom> helpers = Kingdom.All
                .Where(k => CanConsiderHelper(k, weak, strong))
                .OrderByDescending(k => ScoreHelperKingdom(k, weak, strong))
                .ToList();

            AuditCapitulation(
                $"AID_SCAN weak={weak.StringId} strong={strong.StringId} helpers={(helpers.Count == 0 ? "none" : string.Join(",", helpers.Select(x => x.StringId)))}");

            if (helpers.Count == 0)
                return false;

            Kingdom helper = helpers[0];
            float helperScore = ScoreHelperKingdom(helper, weak, strong);
            if (helperScore < 45f)
            {
                AuditCapitulation($"AID_REJECT weak={weak.StringId} helper={helper.StringId} score={helperScore:F1}");
                return false;
            }

            bool absorbAsProtectedVassal =
                weakFiefs <= 1
                && strengthRatio >= ProtectedVassalStrengthRatio
                && string.Equals(helper.Culture?.StringId, weak.Culture?.StringId, StringComparison.OrdinalIgnoreCase)
                && helper.CurrentTotalStrength >= weak.CurrentTotalStrength * 1.5f;

            if (absorbAsProtectedVassal
                && HasSustainedCapitulationPressure(weak, strong, ProtectedVassalPressureDays))
            {
                AuditCapitulation($"AID_PROTECTED_VASSAL weak={weak.StringId} helper={helper.StringId} strong={strong.StringId}");
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
            {
                AuditCapitulation($"AID_REJECT weak={weak.StringId} strong={strong.StringId} reason=no_helper_joined_war");
                return false;
            }

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
                {
                    AuditCapitulation($"SKIP weak={weak.StringId} clan={clan.StringId} reason=mercenary_service target={helper.StringId} mode=protected_vassal");
                    continue;
                }

                ChangeKingdomAction.ApplyByJoinToKingdom(clan, helper, showNotification: false);
                AuditCapitulation($"MOVE weak={weak.StringId} clan={clan.StringId} target={helper.StringId} mode=protected_vassal resultKingdom={clan.Kingdom?.StringId ?? "null"} eliminated={clan.IsEliminated}");
            }

            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name}, facing destruction by {strong.Name}, has sworn itself to {helper.Name} in exchange for protection.",
                Colors.Yellow));

            AuditCapitulation($"DESTROY_KINGDOM weak={weak.StringId} mode=protected_vassal helper={helper.StringId}");
            _lastCapitulation[weak] = CampaignTime.Now;
            _lastWorldCapitulationAt = CampaignTime.Now;
            ClearAllCapitulationPressureFor(weak);
            DestroyKingdomAction.Apply(weak);
        }

        private void ShowPlayerSurrenderInquiry(Kingdom playerKingdom, Kingdom victor)
        {
            // victor.Leader (the winning kingdom's ruler) can be null during a
            // regency/leader-death window — only the weak side was guarded before.
            if (victor?.Leader == null)
                return;

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

            _lastCapitulation[playerKingdom] = CampaignTime.Now;
            _lastWorldCapitulationAt = CampaignTime.Now;
            ClearAllCapitulationPressureFor(playerKingdom);
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
            // Both leaders must exist for the tribute transfer.
            if (weak?.Leader == null || strong?.Leader == null)
                return;

            int tribute = MBRandom.RandomInt(3000, 8000);

            InformationManager.DisplayMessage(new InformationMessage(
                $"{weak.Name} has capitulated to {strong.Name}! Tribute paid: {tribute} gold."));

            GiveGoldAction.ApplyBetweenCharacters(weak.Leader, strong.Leader, tribute, false);

            foreach (var clan in weak.Clans.ToList())
            {
                if (clan.IsUnderMercenaryService)
                {
                    AuditCapitulation($"SKIP weak={weak.StringId} clan={clan.StringId} reason=mercenary_service target={strong.StringId} mode=direct_capitulation");
                    continue;
                }

                ChangeKingdomAction.ApplyByJoinToKingdom(clan, strong, showNotification: false);
                AuditCapitulation($"MOVE weak={weak.StringId} clan={clan.StringId} target={strong.StringId} mode=direct_capitulation resultKingdom={clan.Kingdom?.StringId ?? "null"} eliminated={clan.IsEliminated}");
            }

            AuditCapitulation($"DESTROY_KINGDOM weak={weak.StringId} mode=direct_capitulation strong={strong.StringId}");
            _lastCapitulation[weak] = CampaignTime.Now;
            _lastWorldCapitulationAt = CampaignTime.Now;
            ClearAllCapitulationPressureFor(weak);
            DestroyKingdomAction.Apply(weak);
        }

        private bool HasSustainedCapitulationPressure(Kingdom weak, Kingdom strong, float requiredDays)
        {
            string key = GetCapitulationPressureKey(weak, strong);
            if (!_capitulationPressureSince.TryGetValue(key, out CampaignTime since))
            {
                _capitulationPressureSince[key] = CampaignTime.Now;
                AuditCapitulation($"PRESSURE_START weak={weak.StringId} strong={strong.StringId} requiredDays={requiredDays:F1}");
                return false;
            }

            float days = (float)(CampaignTime.Now - since).ToDays;
            if (days < requiredDays)
            {
                AuditCapitulation($"PRESSURE_WAIT weak={weak.StringId} strong={strong.StringId} days={days:F1} requiredDays={requiredDays:F1}");
                return false;
            }

            return true;
        }

        private void ClearCapitulationPressure(Kingdom weak, Kingdom strong)
        {
            _capitulationPressureSince.Remove(GetCapitulationPressureKey(weak, strong));
        }

        private void ClearAllCapitulationPressureFor(Kingdom kingdom)
        {
            string prefix = $"{kingdom?.StringId}|";
            foreach (string key in _capitulationPressureSince.Keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                _capitulationPressureSince.Remove(key);
            }
        }

        private static string GetCapitulationPressureKey(Kingdom weak, Kingdom strong)
        {
            return $"{weak?.StringId ?? "none"}|{strong?.StringId ?? "none"}";
        }

        private static void AuditCapitulation(string message)
        {
            if (_remainingAuditLines <= 0)
                return;

            // RF Diagnostics MCM page (default OFF).
            if (!RealmsForgotten.Diagnostics.RFLogSwitchboard.IsEnabled(
                    RealmsForgotten.Diagnostics.RFLogSwitchboard.Capitulation))
                return;

            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logDirectory = System.IO.Path.Combine(documentsPath, "Mount and Blade II Bannerlord", "Configs", "ModLogs");
                System.IO.Directory.CreateDirectory(logDirectory);

                string logPath = System.IO.Path.Combine(logDirectory, "RF_CapitulationAudit.log");
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                System.IO.File.AppendAllText(logPath, line);
                _remainingAuditLines--;

                if (_remainingAuditLines == 0)
                {
                    System.IO.File.AppendAllText(
                        logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] AUDIT_DISABLED limit_reached={CapitulationAuditMaxLines}{Environment.NewLine}");
                }
            }
            catch
            {
            }
        }
    }
}

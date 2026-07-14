using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade
{
    /// <summary>
    /// Bandit mutual aid: when a WEAK bandit party is being hunted by a clearly
    /// stronger party (a lord, a patrol, or the player), it looks for another
    /// bandit party nearby. Both then run TOWARD each other; when they meet,
    /// the two bands MERGE into a single larger one (bounded by a cap).
    ///
    /// The engine's short-term initiative AI recomputes a flee-away direction
    /// far more often than any hourly order we issue — plain SetMoveGoToPoint
    /// gets overridden and the bands scatter (observed in testing). So while a
    /// rally is active both parties get Ai.DisableAi() (initiative thinking is
    /// skipped for disabled AI, the movement order executes untouched) and are
    /// ALWAYS released again: on merge, when the threat is gone, on battle, on
    /// timeout, and on session load (ids persisted for the sweep).
    /// </summary>
    public class BanditMutualAidBehavior : CampaignBehaviorBase
    {
        // A band this size or smaller is "weak" and will call for help.
        private const int WeakPartySize = 25;
        // The hunter must be at least this much stronger to trigger the rally.
        private const float ThreatStrengthRatio = 1.3f;
        // How far away the hunter can be and still count as an active threat.
        private const float ThreatScanRadius = 7f;
        // The player counts as a hunter inside this radius even without a
        // click-target on the band (TargetParty is not always set while chasing).
        private const float PlayerThreatRadius = 5f;
        // How far we search for a bandit ally.
        private const float AllyScanRadius = 12f;
        // Bands merge when they get this close to each other.
        private const float RallyMeetDistance = 2.5f;
        // Never merge into a band larger than this (no bandit doomstacks).
        private const int MergedMaxSize = 70;
        // Safety: a rally never keeps AI disabled longer than this.
        private const float RallyTimeoutHours = 36f;
        // Show a flavor message when the merge happens close to the player.
        private const float NearPlayerMessageRadius = 12f;

        private sealed class Rally
        {
            public MobileParty A;
            public MobileParty B;
            public CampaignTime Expires;
        }

        private readonly List<Rally> _rallies = new();

        // Persisted only so a session load can release parties we had disabled
        // (the Rally list itself is runtime-only by design — rallies re-trigger
        // naturally from fresh threats).
        private List<string> _ralliedPartyIds = new();

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_ralliedPartyIds", ref _ralliedPartyIds);
            _ralliedPartyIds ??= new List<string>();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Release anything a previous session left AI-disabled.
            foreach (string id in _ralliedPartyIds.ToList())
            {
                MobileParty party = MobileParty.All.FirstOrDefault(p => p?.StringId == id);
                if (party != null && party.Ai.IsDisabled)
                {
                    party.Ai.EnableAi();
                }
            }
            _ralliedPartyIds.Clear();
            _rallies.Clear();
        }

        private void OnHourlyTick()
        {
            try
            {
                UpdateActiveRallies();
                ScanForNewRallies();
            }
            catch (Exception ex)
            {
                Debug.Print("[RF] BanditMutualAidBehavior tick failed: " + ex.Message);
            }
        }

        // ------------------------------------------------------------------
        // Active rallies: converge, merge, or release.
        // ------------------------------------------------------------------

        private void UpdateActiveRallies()
        {
            for (int i = _rallies.Count - 1; i >= 0; i--)
            {
                Rally rally = _rallies[i];
                MobileParty a = rally.A;
                MobileParty b = rally.B;

                bool aGone = a == null || !a.IsActive;
                bool bGone = b == null || !b.IsActive;
                bool inBattle = (!aGone && a.MapEvent != null) || (!bGone && b.MapEvent != null);
                bool expired = rally.Expires.IsPast;

                if (aGone || bGone || inBattle || expired)
                {
                    ReleaseRally(i);
                    continue;
                }

                // Threat over for both? Disperse back to normal bandit life.
                if (FindHunterFor(a) == null && FindHunterFor(b) == null)
                {
                    ReleaseRally(i);
                    continue;
                }

                float distance = a.Position.Distance(b.Position);
                if (distance <= RallyMeetDistance)
                {
                    int combined = a.MemberRoster.TotalHealthyCount + b.MemberRoster.TotalHealthyCount;
                    if (combined <= MergedMaxSize)
                    {
                        MergeBands(a, b);
                    }
                    // Either way they are together now — release so vanilla AI
                    // takes over (they fight or flee as one).
                    ReleaseRally(i);
                    continue;
                }

                // Still converging: refresh the meeting point (both are moving).
                CampaignVec2 rallyPoint = MidPoint(a, b);
                a.SetMoveGoToPoint(rallyPoint, MobileParty.NavigationType.Default);
                b.SetMoveGoToPoint(rallyPoint, MobileParty.NavigationType.Default);
            }
        }

        private void ReleaseRally(int index)
        {
            Rally rally = _rallies[index];
            ReleaseParty(rally.A);
            ReleaseParty(rally.B);
            _rallies.RemoveAt(index);
        }

        private void ReleaseParty(MobileParty party)
        {
            if (party == null)
            {
                return;
            }
            if (party.IsActive && party.Ai.IsDisabled)
            {
                party.Ai.EnableAi();
            }
            _ralliedPartyIds.Remove(party.StringId);
        }

        private bool IsInRally(MobileParty party)
        {
            foreach (Rally rally in _rallies)
            {
                if (rally.A == party || rally.B == party)
                {
                    return true;
                }
            }
            return false;
        }

        // ------------------------------------------------------------------
        // Detection of new rallies.
        // ------------------------------------------------------------------

        private void ScanForNewRallies()
        {
            List<MobileParty> bandits = new();
            foreach (MobileParty party in MobileParty.All)
            {
                if (party != null && party.IsActive && party.IsBandit
                    && party.MapEvent == null && party.CurrentSettlement == null
                    && !party.IsBanditBossParty && party.Army == null
                    && party.MemberRoster.TotalHealthyCount > 0
                    && !IsInRally(party))
                {
                    bandits.Add(party);
                }
            }

            if (bandits.Count < 2)
            {
                return;
            }

            foreach (MobileParty bandit in bandits)
            {
                if (IsInRally(bandit) || bandit.MemberRoster.TotalHealthyCount > WeakPartySize)
                {
                    continue;
                }

                if (FindHunterFor(bandit) == null)
                {
                    continue;
                }

                MobileParty ally = FindClosestAlly(bandit, bandits);
                if (ally == null)
                {
                    continue;
                }

                StartRally(bandit, ally);
            }
        }

        private void StartRally(MobileParty a, MobileParty b)
        {
            a.Ai.DisableAi();
            b.Ai.DisableAi();
            CampaignVec2 rallyPoint = MidPoint(a, b);
            a.SetMoveGoToPoint(rallyPoint, MobileParty.NavigationType.Default);
            b.SetMoveGoToPoint(rallyPoint, MobileParty.NavigationType.Default);

            _rallies.Add(new Rally
            {
                A = a,
                B = b,
                Expires = CampaignTime.HoursFromNow(RallyTimeoutHours)
            });
            if (!_ralliedPartyIds.Contains(a.StringId))
            {
                _ralliedPartyIds.Add(a.StringId);
            }
            if (!_ralliedPartyIds.Contains(b.StringId))
            {
                _ralliedPartyIds.Add(b.StringId);
            }
        }

        private static CampaignVec2 MidPoint(MobileParty a, MobileParty b)
        {
            Vec2 mid = new((a.Position.X + b.Position.X) * 0.5f,
                           (a.Position.Y + b.Position.Y) * 0.5f);
            return new CampaignVec2(mid, isOnLand: true);
        }

        private static MobileParty FindHunterFor(MobileParty bandit)
        {
            // Healthy headcount as the strength proxy — version-proof and good
            // enough for "the hunter is clearly bigger than this band".
            float banditStrength = Math.Max(1, bandit.MemberRoster.TotalHealthyCount);

            // The player: explicit click-target, or simply bearing down on the
            // band (TargetParty is not reliably set during manual pursuit).
            MobileParty main = MobileParty.MainParty;
            if (main != null && main.IsActive
                && main.MemberRoster.TotalHealthyCount >= banditStrength * ThreatStrengthRatio)
            {
                float mainDistance = main.Position.Distance(bandit.Position);
                if ((main.TargetParty == bandit && mainDistance <= ThreatScanRadius)
                    || mainDistance <= PlayerThreatRadius)
                {
                    return main;
                }
            }

            foreach (MobileParty hunter in MobileParty.All)
            {
                if (hunter == null || !hunter.IsActive || hunter.IsBandit || hunter == main
                    || hunter.MapEvent != null)
                {
                    continue;
                }
                if (hunter.ShortTermBehavior != AiBehavior.EngageParty
                    || hunter.ShortTermTargetParty != bandit)
                {
                    continue;
                }
                if (hunter.Position.Distance(bandit.Position) > ThreatScanRadius)
                {
                    continue;
                }
                if (hunter.MemberRoster.TotalHealthyCount < banditStrength * ThreatStrengthRatio)
                {
                    continue;
                }
                return hunter;
            }

            return null;
        }

        private MobileParty FindClosestAlly(MobileParty bandit, List<MobileParty> bandits)
        {
            MobileParty best = null;
            float bestDistance = AllyScanRadius;
            bool bestSameClan = false;

            foreach (MobileParty candidate in bandits)
            {
                if (candidate == bandit || !candidate.IsActive || IsInRally(candidate))
                {
                    continue;
                }

                float distance = bandit.Position.Distance(candidate.Position);
                if (distance > AllyScanRadius)
                {
                    continue;
                }

                // Same clan (same bandit type) is preferred — mixing looters
                // into sea raiders is a last resort.
                bool sameClan = candidate.ActualClan == bandit.ActualClan;
                if (best == null
                    || (sameClan && !bestSameClan)
                    || (sameClan == bestSameClan && distance < bestDistance))
                {
                    best = candidate;
                    bestDistance = distance;
                    bestSameClan = sameClan;
                }
            }

            return best;
        }

        private static void MergeBands(MobileParty first, MobileParty second)
        {
            MobileParty bigger = first.MemberRoster.TotalManCount >= second.MemberRoster.TotalManCount ? first : second;
            MobileParty smaller = bigger == first ? second : first;

            bigger.MemberRoster.Add(smaller.MemberRoster);
            if (smaller.PrisonRoster != null && smaller.PrisonRoster.TotalManCount > 0)
            {
                bigger.PrisonRoster.Add(smaller.PrisonRoster);
            }
            if (smaller.ItemRoster != null)
            {
                foreach (ItemRosterElement element in smaller.ItemRoster)
                {
                    bigger.ItemRoster.AddToCounts(element.EquipmentElement, element.Amount);
                }
            }

            DestroyPartyAction.Apply(null, smaller);

            MobileParty main = MobileParty.MainParty;
            if (main != null && main.Position.Distance(bigger.Position) <= NearPlayerMessageRadius)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    new TextObject("{=rf_bandit_mutual_aid}Hunted bandits have banded together into a larger warband!").ToString(),
                    Colors.Yellow));
            }
        }
    }
}

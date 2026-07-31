using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.BannerWorks
{
    /// <summary>
    /// RF banner editor (reimplementation of the Omen Editor banner tool):
    /// pick any realm or house, edit its banner in the game's own FULL banner
    /// builder (3D scene, layer UI — the native BannerBuilderState screen), and
    /// the result is applied to the campaign and persisted.
    ///
    /// The native builder is a sandbox: its Done button discards the result.
    /// The trick is the pending-target handshake — we set the target, push the
    /// native state seeded with the target's current banner code, and the two
    /// Harmony postfixes below catch ExecuteDone/ExecuteCancel to route the
    /// resulting code into <see cref="RFBannerWorksStore"/>. When no target is
    /// pending (the player opened the builder some other way) the postfixes do
    /// nothing, so the native tool keeps behaving exactly as shipped.
    ///
    /// Opened with Ctrl+B on the campaign map.
    /// </summary>
    internal static class RFBannerWorksEditor
    {
        private static string _pendingClanId;
        private static string _pendingKingdomId;

        internal static bool HasPendingTarget => !string.IsNullOrEmpty(_pendingClanId) || !string.IsNullOrEmpty(_pendingKingdomId);

        // ── Entry: picker flow ───────────────────────────────────────────────
        internal static void OpenPicker()
        {
            if (Campaign.Current == null || Game.Current?.GameStateManager == null)
            {
                return;
            }

            var options = new List<InquiryElement>
            {
                new InquiryElement("realms", "Realm banners", null),
                new InquiryElement("houses", "House banners", null),
            };
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "RF Banner Editor",
                "Choose what to edit. The change applies to the whole campaign map and persists across sessions.",
                options, true, 1, 1, "Continue", "Cancel",
                selected =>
                {
                    if (selected != null && selected.Count > 0 && selected[0].Identifier is string choice)
                    {
                        if (choice == "realms")
                        {
                            OpenRealmList();
                        }
                        else
                        {
                            OpenClanList();
                        }
                    }
                },
                null));
        }

        private static void OpenRealmList()
        {
            var options = Kingdom.All
                .Where(k => k != null && !k.IsEliminated && k.Banner != null)
                .OrderBy(k => k.Name?.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(k => new InquiryElement(k.StringId, k.Name?.ToString() ?? k.StringId,
                    new BannerImageIdentifier(k.Banner)))
                .ToList();
            if (options.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No realms available.", Colors.Red));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Edit Realm Banner", string.Empty, options, true, 1, 1, "Edit", "Back",
                selected =>
                {
                    if (selected != null && selected.Count > 0 && selected[0].Identifier is string kingdomId)
                    {
                        Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k?.StringId == kingdomId);
                        if (kingdom?.Banner != null)
                        {
                            OpenBuilder(kingdom.Banner.BannerCode, null, kingdom.StringId);
                        }
                    }
                },
                _ => OpenPicker()));
        }

        private static void OpenClanList()
        {
            var options = Clan.All
                .Where(c => c != null && !c.IsEliminated && c.Banner != null && !c.IsBanditFaction
                            && (c.Kingdom != null || c == Clan.PlayerClan))
                .OrderBy(c => c.Kingdom?.Name?.ToString() ?? "~", StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Name?.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(c => new InquiryElement(
                    c.StringId,
                    (c.Kingdom != null ? c.Kingdom.Name?.ToString() + " — " : string.Empty) + (c.Name?.ToString() ?? c.StringId),
                    new BannerImageIdentifier(c.Banner)))
                .ToList();
            if (options.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No houses available.", Colors.Red));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Edit House Banner", string.Empty, options, true, 1, 1, "Edit", "Back",
                selected =>
                {
                    if (selected != null && selected.Count > 0 && selected[0].Identifier is string clanId)
                    {
                        Clan clan = Clan.All.FirstOrDefault(c => c?.StringId == clanId);
                        if (clan?.Banner != null)
                        {
                            OpenBuilder(clan.Banner.BannerCode, clan.StringId, null);
                        }
                    }
                },
                _ => OpenPicker()));
        }

        private static void OpenBuilder(string initialBannerCode, string clanId, string kingdomId)
        {
            try
            {
                _pendingClanId = clanId;
                _pendingKingdomId = kingdomId;
                BannerBuilderState state = Game.Current.GameStateManager.CreateState<BannerBuilderState>(new object[] { initialBannerCode });
                Game.Current.GameStateManager.PushState(state);
            }
            catch (Exception ex)
            {
                ClearPendingTarget();
                InformationManager.DisplayMessage(new InformationMessage("Banner editor failed: " + ex.Message, Colors.Red));
            }
        }

        // ── Result handshake (called from the Harmony postfixes) ─────────────
        internal static void OnBuilderDone(string bannerCode)
        {
            string clanId = _pendingClanId;
            string kingdomId = _pendingKingdomId;
            ClearPendingTarget();
            if (string.IsNullOrWhiteSpace(bannerCode))
            {
                return;
            }

            bool ok;
            string error;
            string label;
            if (!string.IsNullOrEmpty(kingdomId))
            {
                Kingdom kingdom = Kingdom.All.FirstOrDefault(k => k?.StringId == kingdomId);
                label = kingdom?.Name?.ToString() ?? kingdomId;
                ok = RFBannerWorksStore.ApplyAndPersistKingdom(kingdom, bannerCode, out error);
            }
            else
            {
                Clan clan = Clan.All.FirstOrDefault(c => c?.StringId == clanId);
                label = clan?.Name?.ToString() ?? clanId;
                ok = RFBannerWorksStore.ApplyAndPersistClan(clan, bannerCode, out error);
            }

            InformationManager.DisplayMessage(ok
                ? new InformationMessage("Banner of " + label + " updated across the campaign map.", Colors.Green)
                : new InformationMessage("Could not save the banner: " + error, Colors.Red));
        }

        internal static void ClearPendingTarget()
        {
            _pendingClanId = null;
            _pendingKingdomId = null;
        }
    }

    /// <summary>Ctrl+B on the campaign map opens the picker. Edge-triggered so
    /// holding the keys opens a single popup.</summary>
    internal static class RFBannerWorksHotkey
    {
        private static bool _wasDown;

        internal static void Tick()
        {
            if (Campaign.Current == null)
            {
                _wasDown = false;
                return;
            }

            string topScreen = ScreenManager.TopScreen?.GetType().Name;
            bool onMap = !string.IsNullOrEmpty(topScreen)
                && topScreen.IndexOf("MapScreen", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDown = onMap
                && Input.IsKeyDown(InputKey.B)
                && (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl));
            if (isDown && !_wasDown)
            {
                RFBannerWorksEditor.OpenPicker();
            }
            _wasDown = isDown;
        }
    }

    /// <summary>Re-applies stored banner overrides on every session launch.</summary>
    public sealed class RFBannerWorksBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, _ => RFBannerWorksStore.ApplyAll());
        }

        public override void SyncData(IDataStore dataStore)
        {
            // nothing in saves: the overrides live in Configs/RF_BannerWorks
        }
    }

    /// <summary>Catches the native builder's Done when — and only when — an RF
    /// target is pending. Applied by the SubModule's attribute sweep.</summary>
    [HarmonyPatch(typeof(BannerBuilderVM), "ExecuteDone")]
    internal static class BannerBuilderVM_ExecuteDone_RFBannerWorksPatch
    {
        private static void Postfix(BannerBuilderVM __instance)
        {
            if (RFBannerWorksEditor.HasPendingTarget)
            {
                RFBannerWorksEditor.OnBuilderDone(__instance?.CurrentBanner?.BannerCode);
            }
        }
    }

    [HarmonyPatch(typeof(BannerBuilderVM), "ExecuteCancel")]
    internal static class BannerBuilderVM_ExecuteCancel_RFBannerWorksPatch
    {
        private static void Postfix()
        {
            RFBannerWorksEditor.ClearPendingTarget();
        }
    }
}

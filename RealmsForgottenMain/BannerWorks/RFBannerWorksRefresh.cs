using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

namespace RealmsForgotten.BannerWorks
{
    /// <summary>
    /// The two refresh problems the native banner pipeline has, and their fixes.
    /// Both are caching bugs in the engine, not in our editor.
    ///
    /// 1. EDITOR PREVIEW ONE ACTION BEHIND. <c>Banner</c> caches two derived
    ///    values as [CachedData]: <c>_bannerCode</c> (the serialized string) and
    ///    <c>_bannerVisual</c> (what renders the tableau/mesh). The builder
    ///    mutates its banner IN PLACE, and only <c>GetBannerDataAtIndex</c>
    ///    happens to null <c>_bannerCode</c> — <c>_bannerVisual</c> is never
    ///    invalidated at all. So a color change leaves both caches stale (the UI
    ///    keeps showing the old code) and the next edit, which does touch
    ///    GetBannerDataAtIndex, reveals BOTH changes at once. Fix: invalidate
    ///    both caches on every VM refresh, which is the single funnel every
    ///    mutation path (item, color, layer add/remove) goes through.
    ///
    /// 2. MAP BANNERS ONLY AFTER A RELOAD. Map nameplates build their banner
    ///    image once and rebuild it only when <c>_isPartyBannerDirty</c> is set —
    ///    which vanilla sets on init, settlement-owner change and kingdom change,
    ///    never on "the clan's banner object was replaced". Fix: after applying,
    ///    force every party and settlement nameplate to refresh with
    ///    forceUpdate = true, which rebuilds the banner image regardless of the
    ///    flag. The 3D party icons already rebuild from SetVisualAsDirty.
    ///
    /// Both patches are scoped: the builder patch only acts while an RF edit is
    /// in flight, so the native banner builder is untouched for any other use.
    /// </summary>
    internal static class RFBannerWorksRefresh
    {
        private static bool _bannerFieldsResolved;
        private static FieldInfo _bannerCodeField;
        private static FieldInfo _bannerVisualField;

        /// <summary>Drops Banner's cached code and visual so the next read
        /// re-serializes and re-renders. Never throws.</summary>
        internal static void InvalidateBannerCaches(Banner banner)
        {
            if (banner == null)
            {
                return;
            }

            // GUARDA: nunca durante uma missão. `_bannerVisual` é o objeto NATIVO que
            // renderiza a bandeira; anulá-lo enquanto a cena desenha deixa o engine
            // com uma referência pendente — a classe exata de defeito que produz
            // geometria corrompida em batalha. O editor só abre no mapa (hotkey
            // Ctrl+B é restrita ao MapScreen), então esta guarda não custa nada.
            if (Mission.Current != null)
            {
                return;
            }

            try
            {
                if (!_bannerFieldsResolved)
                {
                    _bannerFieldsResolved = true;
                    _bannerCodeField = AccessTools.Field(typeof(Banner), "_bannerCode");
                    _bannerVisualField = AccessTools.Field(typeof(Banner), "_bannerVisual");
                }

                _bannerCodeField?.SetValue(banner, null);
                _bannerVisualField?.SetValue(banner, null);
            }
            catch
            {
                // stale preview is a cosmetic problem; never risk the screen
            }
        }

        // ── Map-side refresh ─────────────────────────────────────────────────
        private static bool _nameplateFieldsResolved;
        private static FieldInfo _partyNameplatesDataSourceField;
        private static FieldInfo _settlementNameplatesDataSourceField;
        private static PropertyInfo _partyNameplateListProperty;
        private static PropertyInfo _settlementNameplateListProperty;

        /// <summary>
        /// Forces every map nameplate to rebuild its banner image. Best-effort:
        /// if the UI shape changed in a future version the banners simply stay
        /// stale until the next load, exactly as they do today.
        /// </summary>
        internal static void RefreshMapNameplates()
        {
            // GUARDA: nameplates são do MAPA; forçar rebuild delas com uma missão
            // ativa mexe em visual fora de contexto sem nenhum ganho.
            if (Campaign.Current == null || MapScreen.Instance == null || Mission.Current != null)
            {
                return;
            }

            try
            {
                if (!_nameplateFieldsResolved)
                {
                    _nameplateFieldsResolved = true;
                    _partyNameplatesDataSourceField = AccessTools.Field(typeof(GauntletMapPartyNameplateView), "_dataSource");
                    _settlementNameplatesDataSourceField = AccessTools.Field(typeof(GauntletMapSettlementNameplateView), "_dataSource");
                }

                RefreshNameplateList(
                    MapScreen.Instance.GetMapView<GauntletMapPartyNameplateView>(),
                    _partyNameplatesDataSourceField,
                    ref _partyNameplateListProperty);

                RefreshNameplateList(
                    MapScreen.Instance.GetMapView<GauntletMapSettlementNameplateView>(),
                    _settlementNameplatesDataSourceField,
                    ref _settlementNameplateListProperty);
            }
            catch
            {
                // see summary: cosmetic, never fatal
            }
        }

        private static void RefreshNameplateList(object view, FieldInfo dataSourceField, ref PropertyInfo listProperty)
        {
            if (view == null || dataSourceField == null)
            {
                return;
            }

            object dataSource = dataSourceField.GetValue(view);
            if (dataSource == null)
            {
                return;
            }

            listProperty ??= dataSource.GetType().GetProperty("Nameplates", BindingFlags.Public | BindingFlags.Instance);
            if (listProperty?.GetValue(dataSource) is not IEnumerable nameplates)
            {
                return;
            }

            foreach (object nameplate in nameplates)
            {
                if (nameplate == null)
                {
                    continue;
                }
                try
                {
                    // Mark the banner dirty where the field exists (parties), then
                    // force a full property refresh — forceUpdate rebuilds the
                    // banner image even when the flag is not honoured.
                    AccessTools.Field(nameplate.GetType(), "_isPartyBannerDirty")?.SetValue(nameplate, true);
                    MethodInfo refresh = AccessTools.Method(nameplate.GetType(), "RefreshDynamicProperties", new[] { typeof(bool) });
                    refresh?.Invoke(nameplate, new object[] { true });
                }
                catch
                {
                    // one bad nameplate must not stop the rest
                }
            }
        }
    }

    /// <summary>
    /// Fix 1: invalidate the mutated banner's cached code/visual on every refresh
    /// of the builder VM, so the editor preview reflects the change that was just
    /// made instead of the previous one. Only active while an RF banner edit is in
    /// flight — the vanilla builder keeps its exact stock behaviour otherwise.
    /// </summary>
    [HarmonyPatch(typeof(BannerBuilderVM), "Refresh")]
    internal static class BannerBuilderVM_Refresh_RFLivePreviewPatch
    {
        private static void Prefix(BannerBuilderVM __instance)
        {
            if (RFBannerWorksEditor.HasPendingTarget)
            {
                RFBannerWorksRefresh.InvalidateBannerCaches(__instance?.CurrentBanner);
            }
        }
    }
}

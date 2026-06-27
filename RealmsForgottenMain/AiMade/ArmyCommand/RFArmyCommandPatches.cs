using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using SandBox.GauntletUI;
using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Tutorial;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

namespace RealmsForgotten.AiMade.ArmyCommand;

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyCommandOpenArmyManagementPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return new[]
        {
            AccessTools.Method(typeof(GauntletMapBarGlobalLayer), "OpenArmyManagement"),
            AccessTools.Method(typeof(GauntletMapOverlayView), "OpenArmyManagement"),
            AccessTools.Method(typeof(GauntletKingdomScreen), "OpenArmyManagement")
        }.Where(method => method != null);
    }

    private static void Postfix()
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            RFLogger.Log("[RFArmyCommand] OpenArmyManagement postfix ran without UI context.");
            return;
        }

        RFLogger.Log("[RFArmyCommand] OpenArmyManagement postfix refreshing widgets.");
        context.MovieIsLoaded = true;
        context.CurrentArmyManagementVMMixIn?.UpdateWidgets();
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyCommandMapScreenPatch
{
    private static readonly Type MapOverlayTypeEnum = AccessTools.TypeByName("SandBox.View.Map.MapOverlayType");
    private static readonly FieldInfo ArmyOverlayField = AccessTools.Field(typeof(MapScreen), "_armyOverlay");
    private static readonly FieldInfo MapViewsContainerField = AccessTools.Field(typeof(MapScreen), "_mapViewsContainer");
    private static readonly MethodInfo AddArmyOverlayMethod = AccessTools.DeclaredMethod(typeof(MapScreen), "AddArmyOverlay");
    private static readonly MethodInfo OnArmyLeftMethod = AccessTools.DeclaredMethod(typeof(MapView), "OnArmyLeft");
    private static readonly MethodInfo OnDispersePlayerLeadedArmyMethod = AccessTools.DeclaredMethod(typeof(MapView), "OnDispersePlayerLeadedArmy");

    private static MethodBase TargetMethod()
    {
        try
        {
            InterfaceMapping map = typeof(MapScreen).GetInterfaceMap(typeof(IMapStateHandler));
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i].Name == "OnRefreshState")
                {
                    return map.TargetMethods[i];
                }
            }
        }
        catch
        {
        }

        return AccessTools.Method(typeof(MapScreen), "OnRefreshState");
    }

    private static void Postfix(MapScreen __instance)
    {
        RefreshArmyOverlay(__instance);
    }

    public static void RefreshArmyOverlay(MapScreen mapScreen = null)
    {
        try
        {
            mapScreen ??= MapScreen.Instance;
            if (mapScreen == null
                || Game.Current?.GameStateManager?.ActiveState is not MapState
                || ArmyOverlayField == null
                || MapViewsContainerField == null
                || AddArmyOverlayMethod == null
                || OnArmyLeftMethod == null
                || OnDispersePlayerLeadedArmyMethod == null)
            {
                return;
            }

            bool shouldShow = RFArmyCommandHelpers.ShouldShowArmyOverlayForPlayer();
            MapView currentArmyOverlay = ArmyOverlayField.GetValue(mapScreen) as MapView;
            if (shouldShow && currentArmyOverlay == null)
            {
                object overlayTypeArgument = MapOverlayTypeEnum != null
                    ? Enum.ToObject(MapOverlayTypeEnum, 1)
                    : 1;
                AddArmyOverlayMethod.Invoke(mapScreen, new[] { overlayTypeArgument });
            }
            else if (!shouldShow && currentArmyOverlay != null)
            {
                MapViewsContainer container = MapViewsContainerField.GetValue(mapScreen) as MapViewsContainer;
                if (container == null)
                {
                    return;
                }

                container.ForeachReverse(view => OnArmyLeftMethod.Invoke(view, null));
                container.ForeachReverse(view => OnDispersePlayerLeadedArmyMethod.Invoke(view, null));
            }
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[RFArmyCommand] RefreshArmyOverlay failed: {ex}");
        }
    }
}

[HarmonyPatch(typeof(ArmyMenuOverlayVM), "ExecuteOpenArmyManagement")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayExecuteOpenArmyManagementPatch
{
    private static bool Prefix(ArmyMenuOverlayVM __instance)
    {
        __instance.OpenArmyManagement();
        return false;
    }
}

[HarmonyPatch(typeof(ArmyMenuOverlayVM), "get_ArmyToUse")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayArmyToUsePatch
{
    private static bool Prefix(ref Army __result)
    {
        Army overlayArmy = RFArmyCommandHelpers.GetPlayerOverlayArmy();
        if (overlayArmy != null)
        {
            __result = overlayArmy;
            return false;
        }

        if (RFArmyOverlayUIContext.Instance == null)
        {
            return true;
        }

        Army selectedArmy = RFArmyCommandHelpers.GetPreferredSelectedArmy(RFArmyOverlayUIContext.Instance.SelectedArmy);
        if (selectedArmy != null)
        {
            RFArmyOverlayUIContext.Instance.SelectedArmy = selectedArmy;
            __result = selectedArmy;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ArmyMenuOverlayVM), "get_IsPlayerArmyLeader")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayGetIsPlayerArmyLeaderPatch
{
    private static bool Prefix(ref bool __result)
    {
        Army army = RFArmyCommandHelpers.GetPlayerOverlayArmy()
            ?? RFArmyCommandHelpers.GetPreferredSelectedArmy(RFArmyOverlayUIContext.Instance?.SelectedArmy);
        __result = Hero.MainHero.IsKingdomLeader || RFArmyCommandHelpers.IsSameParty(army?.LeaderParty, MobileParty.MainParty);
        return false;
    }
}

[HarmonyPatch(typeof(MapBarVM), "GetIsGatherArmyVisible")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyMapBarVisibilityPatch
{
    private static void Postfix(ref bool __result)
    {
        if (__result && RFArmyCommandHelpers.ShouldShowArmyOverlayForPlayer())
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(ArmyMenuOverlayVM), "OnFinalize")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayFinalizePatch
{
    private static void Postfix()
    {
        (RFArmyOverlayUIContext.Instance?.CurrentArmyOverlayVMMixIn as BaseViewModelMixin<ArmyMenuOverlayVM>)?.OnFinalize();
        RFArmyOverlayUIContext.Instance?.UnregisterInstance();
    }
}

[HarmonyPatch(typeof(ArmyMenuOverlayVM), "OnPartyAttachedAnotherParty")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayPartyAttachedPatch
{
    private static readonly FieldInfo IsVisualsDirtyField = AccessTools.Field(typeof(ArmyMenuOverlayVM), "_isVisualsDirty");

    private static bool Prefix(ArmyMenuOverlayVM __instance, MobileParty party)
    {
        Army overlayArmy = RFArmyCommandHelpers.GetPreferredSelectedArmy(RFArmyOverlayUIContext.Instance?.SelectedArmy)
            ?? MobileParty.MainParty?.Army;

        if (RFArmyCommandHelpers.IsSameArmy(party?.AttachedTo?.Army, overlayArmy))
        {
            IsVisualsDirtyField?.SetValue(__instance, true);
            RFArmyOverlayUIContext.Instance?.CurrentArmyOverlayVMMixIn?.UpdateLeftArmyOverlay();
        }

        return false;
    }
}

[HarmonyPatch(typeof(ArmyOverlayWidget), "OnArmyListPageCountChanged")]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayPageCountPatch
{
    private static void Postfix(ArmyOverlayWidget __instance)
    {
        if (__instance.Overlay?.Id != "ACOverlayWidget")
        {
            return;
        }

        __instance.Overlay.PositionXOffset = 40f;
        if (__instance.ExtendButton is Widget extendButton)
        {
            extendButton.PositionXOffset = 0f;
        }
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayGatherPatch
{
    [HarmonyPatch(typeof(Army), "Gather", new[] { typeof(Settlement), typeof(MBReadOnlyList<MobileParty>) })]
    [HarmonyPostfix]
    private static void Postfix(Army __instance)
    {
        RFArmyOverlayUIContext.Instance?.CurrentArmyOverlayVMMixIn?.OnArmyGathered(__instance);
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyOverlayDispersePatch
{
    [HarmonyPatch(typeof(Army), "DisperseInternal", new[] { typeof(Army.ArmyDispersionReason) })]
    [HarmonyPostfix]
    private static void Postfix(Army __instance)
    {
        RFArmyOverlayUIContext.Instance?.CurrentArmyOverlayVMMixIn?.OnArmyDisband(__instance);
    }
}

internal static class RFArmyCanManagePatch
{
    public static MethodInfo ResolveTargetMethod()
    {
        Type campaignUiHelperType = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.CampaignUIHelper");
        if (campaignUiHelperType == null)
        {
            return null;
        }

        MethodInfo exactMatch = AccessTools.Method(
            campaignUiHelperType,
            "GetCanManageCurrentArmyWithReason",
            new[] { typeof(TextObject).MakeByRefType() });
        if (exactMatch != null)
        {
            return exactMatch;
        }

        return AccessTools.Method(campaignUiHelperType, "GetCanManageCurrentArmyWithReason");
    }

    public static bool Apply(Harmony harmony)
    {
        MethodInfo original = ResolveTargetMethod();
        if (original == null)
        {
            RFLogger.Log("[RFArmyCommand] CampaignUIHelper.GetCanManageCurrentArmyWithReason was not found. Army command visibility patch skipped.");
            return false;
        }

        HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(original);
        if (patchInfo?.Prefixes?.Any(prefix => prefix.owner == harmony.Id && prefix.PatchMethod?.DeclaringType == typeof(RFArmyCanManagePatch)) == true)
        {
            RFLogger.Log("[RFArmyCommand] CampaignUIHelper.GetCanManageCurrentArmyWithReason is already patched.");
            return true;
        }

        HarmonyMethod prefix = new(typeof(RFArmyCanManagePatch), nameof(Prefix));
        harmony.Patch(original, prefix: prefix);
        return true;
    }

    public static bool Prefix(ref bool __result, ref TextObject disabledReason)
    {
        disabledReason = TextObject.GetEmpty();
        if (RFArmyCommandHelpers.IsPlayerBusy())
        {
            disabledReason = new TextObject("{=!}" + Hero.MainHero.Name + " is busy.");
            __result = false;
            return false;
        }

        if (Hero.MainHero.IsKingdomLeader)
        {
            __result = true;
            return false;
        }

        if (Clan.PlayerClan.IsUnderMercenaryService)
        {
            disabledReason = new TextObject("{=!}Cannot create or manage armies while at mercenary service.");
            __result = false;
            return false;
        }

        Army mainPartyArmy = MobileParty.MainParty?.Army;
        if (mainPartyArmy != null && !RFArmyCommandHelpers.IsSameParty(mainPartyArmy.LeaderParty, MobileParty.MainParty))
        {
            disabledReason = new TextObject("{=!}Cannot create an army while already a member of one.");
            __result = false;
            return false;
        }

        __result = true;
        return false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyEligibilityPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(DefaultArmyManagementCalculationModel),
            "CheckPartyEligibility",
            new[] { typeof(MobileParty), typeof(TextObject).MakeByRefType() });
    }

    private static bool Prefix(DefaultArmyManagementCalculationModel __instance, MobileParty party, ref TextObject explanation, ref bool __result)
    {
        MobileParty selectedLeaderParty = RFArmyManagementUIContext.Instance?.CurrentMainParty;
        Army selectedArmy = selectedLeaderParty?.Army;
        ArmyManagementCalculationModel calculationModel = Campaign.Current?.Models?.ArmyManagementCalculationModel;
        bool result = true;

        if (party == null)
        {
            result = false;
            explanation = new TextObject("{=f6vTzVar}Does not have a mobile party.");
        }
        else if (RFArmyCommandHelpers.IsPartyBusy(party))
        {
            result = false;
            explanation = new TextObject("{=!}Party Unavailable.");
        }
        else if (RFArmyCommandHelpers.IsPlayerBusy())
        {
            result = false;
            explanation = new TextObject("{=!}Player unable to perform this action.");
        }
        else
        {
            Hero factionLeader = Hero.MainHero.MapFaction?.Leader;
            if (RFArmyCommandHelpers.IsSameHero(party.LeaderHero, factionLeader) && (selectedLeaderParty != null || !Hero.MainHero.IsKingdomLeader))
            {
                result = false;
                explanation = new TextObject("{=ipLqVv1f}You cannot invite the ruler's party to your army.");
            }
            else
            {
                Army partyArmy = party.Army;
                if (partyArmy != null && !RFArmyCommandHelpers.IsSameArmy(partyArmy, selectedArmy))
                {
                    if (!Hero.MainHero.IsKingdomLeader)
                    {
                        result = false;
                        explanation = new TextObject("{=aROohsat}Already in another army.");
                    }
                    else if (selectedLeaderParty == null)
                    {
                        if (!RFArmyCommandHelpers.IsSameParty(partyArmy.LeaderParty, party))
                        {
                            result = false;
                            explanation = new TextObject("{=aROohsat}Already in another army as a member.");
                        }
                    }
                    else
                    {
                        result = false;
                        explanation = new TextObject("{=aROohsat}Already in another army.");
                    }
                }
                else if (partyArmy != null && RFArmyCommandHelpers.IsSameArmy(partyArmy, selectedArmy))
                {
                    result = false;
                    explanation = new TextObject("{=Vq8yavES}Already in army.");
                }

                if (result && calculationModel != null
                    && __instance.GetPartySizeScore(party) <= calculationModel.PlayerMobilePartySizeRatioToCallToArmy)
                {
                    result = false;
                    explanation = new TextObject("{=!}Party has less men than 40% of its party size limit.");
                }
            }
        }

        __result = result;
        return false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyManagementComparerPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        Type comparerType = AccessTools.Inner(typeof(ArmyManagementVM), "ManagementItemComparer")
            ?? AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement.ManagementItemComparer");

        MethodInfo directMethod = comparerType == null ? null : AccessTools.Method(comparerType, "Compare");
        if (directMethod != null)
        {
            return new[] { directMethod };
        }

        return typeof(ArmyManagementVM)
            .GetNestedTypes(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method =>
            {
                if (method.Name != "Compare")
                {
                    return false;
                }

                ParameterInfo[] parameters = method.GetParameters();
                return parameters.Length == 2
                    && parameters[0].ParameterType == typeof(ArmyManagementItemVM)
                    && parameters[1].ParameterType == typeof(ArmyManagementItemVM);
            })
            .Cast<MethodBase>();
    }

    private static bool Prefix(ArmyManagementItemVM x, ArmyManagementItemVM y, ref int __result)
    {
        MobileParty currentMainParty = RFArmyManagementUIContext.Instance?.CurrentMainParty;
        if (currentMainParty != null)
        {
            if (RFArmyCommandHelpers.IsSameParty(x?.Party, currentMainParty))
            {
                __result = -1;
                return false;
            }

            if (RFArmyCommandHelpers.IsSameParty(y?.Party, currentMainParty))
            {
                __result = 1;
                return false;
            }
        }

        __result = (y?.IsAlreadyWithPlayer ?? false).CompareTo(x?.IsAlreadyWithPlayer ?? false);
        return false;
    }
}

[HarmonyPatch]
[HarmonyPatchCategory("RFArmyCommand")]
internal static class RFArmyManagementVMPatches
{
    private static readonly ConditionalWeakTable<ArmyManagementVM, object> InitializedInstances = new();
    private static readonly FieldInfo InfluenceSpentForCohesionBoostingField = AccessTools.Field(typeof(ArmyManagementVM), "_influenceSpentForCohesionBoosting");
    private static readonly FieldInfo OnCloseField = AccessTools.Field(typeof(ArmyManagementVM), "_onClose");
    private static readonly FieldInfo MainPartyItemField = AccessTools.Field(typeof(ArmyManagementVM), "_mainPartyItem");
    private static readonly FieldInfo InitialInfluenceField = AccessTools.Field(typeof(ArmyManagementVM), "_initialInfluence");
    private static readonly FieldInfo BoostedCohesionField = AccessTools.Field(typeof(ArmyManagementVM), "_boostedCohesion");
    private static readonly FieldInfo PlayerHasArmyField = AccessTools.Field(typeof(ArmyManagementVM), "_playerHasArmy");
    private static readonly FieldInfo CurrentPartiesField = AccessTools.Field(typeof(ArmyManagementVM), "_currentParties");
    private static readonly FieldInfo PartiesToRemoveField = AccessTools.Field(typeof(ArmyManagementVM), "_partiesToRemove");
    private static readonly MethodInfo ApplyCohesionChangeMethod = AccessTools.Method(typeof(ArmyManagementVM), "ApplyCohesionChange");
    private static readonly MethodInfo OnFocusMethod = AccessTools.Method(typeof(ArmyManagementVM), "OnFocus", new[] { typeof(ArmyManagementItemVM) });
    private static readonly MethodInfo OnRefreshMethod = AccessTools.Method(typeof(ArmyManagementVM), "OnRefresh");

    private static T GetFieldValue<T>(FieldInfo field, object instance, T fallback = default)
    {
        if (field?.GetValue(instance) is T value)
        {
            return value;
        }

        return fallback;
    }

    private static void SetFieldValue<T>(FieldInfo field, object instance, T value)
    {
        field?.SetValue(instance, value);
    }

    private static Action<ArmyManagementItemVM> CreateItemCallback(ArmyManagementVM instance, MethodInfo method, Action<ArmyManagementItemVM> fallback)
    {
        if (instance == null || method == null)
        {
            return fallback;
        }

        try
        {
            return (Action<ArmyManagementItemVM>)Delegate.CreateDelegate(typeof(Action<ArmyManagementItemVM>), instance, method);
        }
        catch
        {
            return fallback;
        }
    }

    private static List<MobileParty> GetCurrentParties(ArmyManagementVM vm)
    {
        return CurrentPartiesField?.GetValue(vm) as List<MobileParty>;
    }

    private static void SetCurrentParties(ArmyManagementVM vm, List<MobileParty> parties)
    {
        CurrentPartiesField?.SetValue(vm, parties);
    }

    private static MBBindingList<ArmyManagementItemVM> GetPartiesToRemove(ArmyManagementVM vm)
    {
        if (PartiesToRemoveField?.GetValue(vm) is not MBBindingList<ArmyManagementItemVM> parties)
        {
            parties = new MBBindingList<ArmyManagementItemVM>();
            PartiesToRemoveField?.SetValue(vm, parties);
        }

        return parties;
    }

    private static ArmyManagementCalculationModel GetArmyManagementCalculationModel()
    {
        return Campaign.Current?.Models?.ArmyManagementCalculationModel;
    }

    private static void InvokeOnClose(ArmyManagementVM vm)
    {
        GetFieldValue<Action>(OnCloseField, vm)?.Invoke();
    }

    private static void SyncCurrentParties(ArmyManagementVM vm)
    {
        if (vm == null)
        {
            return;
        }

        List<MobileParty> currentParties = vm.PartiesInCart?
            .Select(item => item?.Party)
            .Where(party => party != null)
            .Distinct(RFArmyCommandHelpers.PartyIdentityComparer)
            .ToList()
            ?? new List<MobileParty>();

        SetCurrentParties(vm, currentParties);
    }

    internal static void EnsureInitialized(ArmyManagementVM instance)
    {
        if (instance == null)
        {
            return;
        }

        lock (InitializedInstances)
        {
            if (InitializedInstances.TryGetValue(instance, out _))
            {
                return;
            }

            if (InitializeArmyManagementViewModel(instance))
            {
                InitializedInstances.Add(instance, new object());
                RFLogger.Log("[RFArmyCommand] ArmyManagementVM initialized successfully.");
            }
            else
            {
                RFLogger.Log("[RFArmyCommand] ArmyManagementVM initialization returned false.");
            }
        }
    }

    internal static bool InitializeArmyManagementViewModel(ArmyManagementVM __instance)
    {
        ArmyManagementCalculationModel calculationModel = GetArmyManagementCalculationModel();
        if (__instance == null || calculationModel == null || Hero.MainHero == null)
        {
            return false;
        }

        Action<ArmyManagementItemVM> onAdd = item => OnAddToCart(__instance, item);
        Action<ArmyManagementItemVM> onRemove = item => OnRemove(__instance, item);
        Action<ArmyManagementItemVM> onFocus = CreateItemCallback(__instance, OnFocusMethod, _ => { });

        bool isKingdomLeader = Hero.MainHero.IsKingdomLeader;
        MobileParty selectedParty;
        if (!isKingdomLeader)
        {
            selectedParty = Hero.MainHero.PartyBelongedTo;
        }
        else
        {
            selectedParty = RFArmyCommandHelpers.GetPreferredSelectedArmy(RFArmyOverlayUIContext.Instance?.SelectedArmy)?.LeaderParty
                ?? Hero.MainHero.PartyBelongedTo;
        }

        if (selectedParty == null)
        {
            RFLogger.Log("[RFArmyCommand] InitializeArmyManagementViewModel aborted because selectedParty was null.");
            return false;
        }

        RFArmyManagementUIContext currentContext = RFArmyManagementUIContext.Instance;
        Army selectedPartyArmy = selectedParty.Army;
        bool mainPartyHasArmy = selectedPartyArmy != null && RFArmyCommandHelpers.IsSameParty(selectedPartyArmy.LeaderParty, selectedParty);

        if (currentContext != null)
        {
            currentContext.CurrentMainParty = selectedParty;
            mainPartyHasArmy = currentContext.MainPartyHasArmy;
            selectedPartyArmy = selectedParty.Army;
        }

        __instance.PartyList = new MBBindingList<ArmyManagementItemVM>();
        __instance.PartiesInCart = new MBBindingList<ArmyManagementItemVM>();
        PartiesToRemoveField?.SetValue(__instance, new MBBindingList<ArmyManagementItemVM>());
        SetCurrentParties(__instance, new List<MobileParty>());
        __instance.CohesionHint = new BasicTooltipViewModel();
        __instance.FoodHint = new HintViewModel();
        __instance.MoraleHint = new HintViewModel();
        __instance.BoostCohesionHint = new HintViewModel();
        __instance.DisbandArmyHint = new HintViewModel();
        __instance.DoneHint = new HintViewModel();
        __instance.TutorialNotification = new ElementNotificationVM();
        __instance.CanAffordInfluenceCost = true;

        __instance.PlayerHasArmy = selectedParty.IsMainParty && mainPartyHasArmy;

        ArmyManagementItemVM mainPartyItem = new(onAdd, onRemove, onFocus, selectedParty)
        {
            IsAlreadyWithPlayer = true,
            IsMainHero = selectedParty.IsMainParty,
            IsInCart = true,
            IsTransferDisabled = PlayerSiege.PlayerSiegeEvent != null
        };
        SetFieldValue(MainPartyItemField, __instance, mainPartyItem);

        mainPartyItem.Cost = mainPartyHasArmy
            ? 0
            : calculationModel.CalculatePartyInfluenceCost(Hero.MainHero.PartyBelongedTo, mainPartyItem.Party);

        __instance.PartiesInCart.Add(mainPartyItem);

        List<MobileParty> availableParties = MobileParty.All?.Where(party => party != null).ToList() ?? new List<MobileParty>();
        foreach (MobileParty party in availableParties)
        {
            if (party.LeaderHero != null && party.MapFaction == Hero.MainHero.MapFaction && !RFArmyCommandHelpers.IsSameHero(party.LeaderHero, selectedParty.LeaderHero) && !party.IsCaravan)
            {
                __instance.PartyList.Add(new ArmyManagementItemVM(onAdd, onRemove, onFocus, party)
                {
                    IsMainHero = false,
                    IsTransferDisabled = PlayerSiege.PlayerSiegeEvent != null
                });
            }
        }

        if (mainPartyHasArmy)
        {
            foreach (ArmyManagementItemVM item in __instance.PartyList)
            {
                if (RFArmyCommandHelpers.IsSameArmy(item.Party.Army, selectedPartyArmy))
                {
                    item.Cost = 0;
                    item.IsAlreadyWithPlayer = true;
                    item.IsInCart = true;
                    __instance.PartiesInCart.Add(item);
                }
                else
                {
                    item.Cost = calculationModel.CalculatePartyInfluenceCost(selectedParty, item.Party);
                }
            }
        }

        if (isKingdomLeader)
        {
            __instance.PartyList.Add(mainPartyItem);
        }

        Army mainPlayerArmy = MobileParty.MainParty?.Army;
        if (mainPlayerArmy != null && RFArmyCommandHelpers.IsSameArmy(mainPlayerArmy, selectedPartyArmy))
        {
            __instance.CohesionBoostCost = calculationModel.GetCohesionBoostInfluenceCost(mainPlayerArmy, 10);
        }

        SetFieldValue(InitialInfluenceField, __instance, Hero.MainHero.Clan.Influence);
        __instance.SortControllerVM = new ArmyManagementSortControllerVM(__instance.PartyList);
        __instance.PartiesInCart = GetOrderedPartiesInCart(__instance);
        SyncCurrentParties(__instance);
        __instance.SortControllerVM.CostState = 0;
        __instance.SortControllerVM.ExecuteSortByCost();
        RefreshArmyManagementState(__instance);
        RFLogger.Log($"[RFArmyCommand] InitializeArmyManagementViewModel complete | selectedParty={selectedParty.StringId ?? "none"} | hasArmy={mainPartyHasArmy} | partyList={__instance.PartyList?.Count ?? 0} | cart={__instance.PartiesInCart?.Count ?? 0}");
        return true;
    }

    [HarmonyPatch]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(ArmyManagementVM)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Cast<MethodBase>();
    }

    [HarmonyPostfix]
    private static void ConstructorPostfix(ArmyManagementVM __instance)
    {
        EnsureInitialized(__instance);
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "set_PlayerHasArmy")]
    [HarmonyPrefix]
    private static bool PlayerHasArmySetterPrefix(ArmyManagementVM __instance, bool value)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            return true;
        }

        MobileParty currentMainParty = context?.CurrentMainParty;
        value = currentMainParty?.IsMainParty == true && context?.MainPartyHasArmy == true;
        if (value != GetFieldValue(PlayerHasArmyField, __instance, false))
        {
            SetFieldValue(PlayerHasArmyField, __instance, value);
            __instance.OnPropertyChangedWithValue(value, "PlayerHasArmy");
        }

        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "RefreshValues")]
    [HarmonyPostfix]
    private static void RefreshValuesPostfix(ArmyManagementVM __instance)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context != null && !context.MainPartyHasArmy)
        {
            __instance.TitleText = "Army Creation";
        }
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "GetCanDisbandArmyWithReason")]
    [HarmonyPrefix]
    private static bool GetCanDisbandArmyWithReasonPrefix(ArmyManagementVM __instance, ref bool __result, ref TextObject disabledReason)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null || !context.MainPartyHasArmy)
        {
            disabledReason = new TextObject("{=iSZTOeYH}No army to disband.");
            __result = false;
            return false;
        }

        if (RFArmyCommandHelpers.IsPartyBusy(context.CurrentMainParty) || RFArmyCommandHelpers.IsPlayerBusy())
        {
            disabledReason = new TextObject("{=uipNpzVw}Cannot disband the army right now.");
            __result = false;
            return false;
        }

        disabledReason = TextObject.GetEmpty();
        __result = true;
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "OnAddToCart")]
    [HarmonyPrefix]
    private static bool OnAddToCartPrefix(ArmyManagementVM __instance, ArmyManagementItemVM armyItem)
    {
        OnAddToCart(__instance, armyItem);
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "OnRemove")]
    [HarmonyPrefix]
    private static bool OnRemovePrefix(ArmyManagementVM __instance, ArmyManagementItemVM armyItem)
    {
        OnRemove(__instance, armyItem);
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "ExecuteDone")]
    [HarmonyPrefix]
    private static bool ExecuteDonePrefix(ArmyManagementVM __instance)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (context == null)
        {
            InvokeOnClose(__instance);
            return false;
        }

        if (!__instance.CanAffordInfluenceCost)
        {
            return false;
        }

        if (__instance.NewCohesion > __instance.Cohesion)
        {
            try
            {
                ApplyCohesionChangeMethod?.Invoke(__instance, Array.Empty<object>());
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[RFArmyCommand] ApplyCohesionChange invoke failed: {ex}");
            }
        }

        MobileParty selectedLeaderParty = context.CurrentMainParty;
        if (selectedLeaderParty?.LeaderHero == null)
        {
            InvokeOnClose(__instance);
            return false;
        }

        Army selectedArmy = selectedLeaderParty?.Army;
        bool canManageKingdomArmy = MobileParty.MainParty?.MapFaction?.IsKingdomFaction == true;
        IEnumerable<MobileParty> partiesToJoin = Enumerable.Empty<MobileParty>();
        RFArmyCommandPlan desiredPlan = RFArmyCommandService.NormalizePlan(selectedLeaderParty, context.PendingPlan)
            ?? RFArmyCommandService.NormalizePlan(selectedLeaderParty, context.TargetSettlement, context.ArmyBehavior);

        if (canManageKingdomArmy && __instance.PartiesInCart.Count > 0)
        {
            partiesToJoin = __instance.PartiesInCart
                .Select(item => item.Party)
                .Where(party => party != null && !RFArmyCommandHelpers.IsSameParty(party, context.CurrentMainParty))
                .Distinct(RFArmyCommandHelpers.PartyIdentityComparer);

            selectedArmy = RFArmyCommandService.CreateOrUpdateArmy(
                selectedLeaderParty,
                partiesToJoin,
                desiredPlan);
            if (RFArmyOverlayUIContext.Instance != null)
            {
                RFArmyOverlayUIContext.Instance.SelectedArmy = selectedArmy;
            }
        }

        if (canManageKingdomArmy && __instance.PartiesInCart.Count > 0 && selectedArmy == null)
        {
            MBInformationManager.AddQuickInformation(new TextObject("{=rf_army_command_failed}Army command could not be applied right now."), 0, null);
            InvokeOnClose(__instance);
            return false;
        }

        int cohesionInfluenceSpent = GetFieldValue(InfluenceSpentForCohesionBoostingField, __instance, 0);
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -(__instance.TotalCost - cohesionInfluenceSpent));
        context.InfluenceSent = 0;

        MBBindingList<ArmyManagementItemVM> partiesToRemove = GetPartiesToRemove(__instance);
        if (__instance.PartiesInCart.Count == 1 && partiesToRemove.Count > 0)
        {
            CustomDisbandArmy(__instance);
            return false;
        }

        SyncCurrentParties(__instance);
        ApplyArmyOrders(selectedArmy ?? selectedLeaderParty.Army);
        context.PendingPlan = null;

        if (partiesToRemove.Count > 0 && selectedArmy != null)
        {
            RFArmyCommandService.RemovePartiesFromArmy(selectedArmy, partiesToRemove.Select(item => item.Party));
            partiesToRemove.Clear();
        }

        SyncCurrentParties(__instance);
        InvokeOnClose(__instance);
        CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "DisbandArmy")]
    [HarmonyPrefix]
    private static bool DisbandArmyPrefix(ArmyManagementVM __instance)
    {
        CustomDisbandArmy(__instance);
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "ExecuteReset")]
    [HarmonyPrefix]
    private static bool ExecuteResetPrefix(ArmyManagementVM __instance)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        MobileParty currentMainParty = context?.CurrentMainParty;
        if (currentMainParty != null)
        {
            ArmyManagementItemVM leaderItem = __instance.PartiesInCart.FirstOrDefault(item => RFArmyCommandHelpers.IsSameParty(item.Party, currentMainParty));
            if (leaderItem != null)
            {
                OnRemove(__instance, leaderItem);
            }
        }

        ArmyManagementItemVM mainPartyItem = GetFieldValue<ArmyManagementItemVM>(MainPartyItemField, __instance);
        if (mainPartyItem != null)
        {
            OnAddToCart(__instance, mainPartyItem);
        }
        __instance.NewCohesion = __instance.Cohesion;
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, GetFieldValue(InitialInfluenceField, __instance, 0f) - (Clan.PlayerClan.Influence + (context?.InfluenceSent ?? 0)));
        if (context != null)
        {
            context.InfluenceSent = 0;
            context.PendingPlan = null;
        }
        __instance.TotalCost = 0;
        SetFieldValue(BoostedCohesionField, __instance, 0);
        SetFieldValue(InfluenceSpentForCohesionBoostingField, __instance, 0);
        GetPartiesToRemove(__instance).Clear();
        SyncCurrentParties(__instance);
        RefreshArmyManagementState(__instance);
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "ExecuteCancel")]
    [HarmonyPrefix]
    private static bool ExecuteCancelPrefix(ArmyManagementVM __instance)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        ChangeClanInfluenceAction.Apply(Clan.PlayerClan, GetFieldValue(InitialInfluenceField, __instance, 0f) - (Clan.PlayerClan.Influence + (context?.InfluenceSent ?? 0)));
        if (context != null)
        {
            context.InfluenceSent = 0;
            context.PendingPlan = null;
        }
        InvokeOnClose(__instance);
        return false;
    }

    [HarmonyPatch(typeof(ArmyManagementVM), "OnFinalize")]
    [HarmonyPostfix]
    private static void OnFinalizePostfix()
    {
        if (RFArmyManagementUIContext.Instance?.CurrentArmyManagementVM != null)
        {
            InitializedInstances.Remove(RFArmyManagementUIContext.Instance.CurrentArmyManagementVM);
        }

        (RFArmyManagementUIContext.Instance?.CurrentArmyManagementVMMixIn as BaseViewModelMixin<ArmyManagementVM>)?.OnFinalize();
        RFArmyManagementUIContext.Instance?.UnregisterInstance();
    }

    private static void OnAddToCart(ArmyManagementVM vm, ArmyManagementItemVM armyItem)
    {
        if (vm == null || armyItem?.Party == null)
        {
            RefreshArmyManagementState(vm);
            return;
        }

        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        MBBindingList<ArmyManagementItemVM> partiesToRemove = GetPartiesToRemove(vm);
        if (vm.PartiesInCart.Contains(armyItem))
        {
            RefreshArmyManagementState(vm);
            return;
        }

        if (vm.PartiesInCart.Count == 0)
        {
            OnFirstPartyAdded(vm, armyItem);
        }
        else
        {
            vm.PartiesInCart.Add(armyItem);
            armyItem.IsInCart = true;
            Game.Current.EventManager.TriggerEvent(new PartyAddedToArmyByPlayerEvent(armyItem.Party));
            if (partiesToRemove.Contains(armyItem))
            {
                partiesToRemove.Remove(armyItem);
            }

            if (armyItem.IsAlreadyWithPlayer)
            {
                armyItem.CanJoinBackWithoutCost = false;
            }

            vm.TotalCost += armyItem.Cost;
        }

        SyncCurrentParties(vm);
        RefreshArmyManagementState(vm);
    }

    private static void OnRemove(ArmyManagementVM vm, ArmyManagementItemVM armyItem)
    {
        if (vm == null || armyItem?.Party == null)
        {
            RefreshArmyManagementState(vm);
            return;
        }

        MBBindingList<ArmyManagementItemVM> partiesToRemove = GetPartiesToRemove(vm);
        if (!vm.PartiesInCart.Contains(armyItem))
        {
            RefreshArmyManagementState(vm);
            return;
        }

        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (RFArmyCommandHelpers.IsSameParty(armyItem.Party, context?.CurrentMainParty))
        {
            OnArmyLeaderRemoved(vm);
        }
        else
        {
            vm.PartiesInCart.Remove(armyItem);
            armyItem.IsInCart = false;
            partiesToRemove.Add(armyItem);
            if (armyItem.IsAlreadyWithPlayer)
            {
                armyItem.CanJoinBackWithoutCost = true;
            }

            vm.TotalCost -= armyItem.Cost;
        }

        SyncCurrentParties(vm);
        RefreshArmyManagementState(vm);
    }

    private static void OnFirstPartyAdded(ArmyManagementVM vm, ArmyManagementItemVM armyItem)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        ArmyManagementCalculationModel calculationModel = GetArmyManagementCalculationModel();
        if (context == null || armyItem?.Party == null || calculationModel == null)
        {
            RefreshArmyManagementState(vm);
            return;
        }

        MobileParty selectedParty = armyItem.Party;
        Army selectedArmy = selectedParty.Army;
        context.CurrentMainParty = selectedParty;
        vm.PlayerHasArmy = context.MainPartyHasArmy && context.CurrentMainParty?.IsMainParty == true;

        if (context.MainPartyHasArmy)
        {
            foreach (ArmyManagementItemVM item in vm.PartyList)
            {
                if (RFArmyCommandHelpers.IsSameArmy(item.Party.Army, selectedArmy))
                {
                    item.Cost = 0;
                    item.IsAlreadyWithPlayer = true;
                    item.IsInCart = true;
                    item.CanJoinBackWithoutCost = false;
                    vm.PartiesInCart.Add(item);
                }
                else
                {
                    item.Cost = calculationModel.CalculatePartyInfluenceCost(selectedParty, item.Party);
                }

                item.UpdateEligibility();
            }
        }
        else
        {
            armyItem.IsAlreadyWithPlayer = true;
            armyItem.IsInCart = true;
            armyItem.CanJoinBackWithoutCost = false;
            vm.PartiesInCart.Add(armyItem);
            vm.TotalCost += armyItem.Cost;
            armyItem.UpdateEligibility();
            foreach (ArmyManagementItemVM item in vm.PartyList)
            {
                item.UpdateEligibility();
            }
        }

        vm.PartiesInCart = GetOrderedPartiesInCart(vm);
        SyncCurrentParties(vm);
        vm.SortControllerVM.CostState = 0;
        vm.SortControllerVM.ExecuteSortByCost();
        RefreshArmyManagementState(vm);
    }

    private static void OnArmyLeaderRemoved(ArmyManagementVM vm)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        ArmyManagementCalculationModel calculationModel = GetArmyManagementCalculationModel();
        if (context != null)
        {
            context.CurrentMainParty = null;
        }

        vm.PlayerHasArmy = false;
        MBBindingList<ArmyManagementItemVM> partiesToRemove = GetPartiesToRemove(vm);
        partiesToRemove.Clear();

        foreach (ArmyManagementItemVM item in vm.PartyList)
        {
            item.IsTransferDisabled = PlayerSiege.PlayerSiegeEvent != null;
            if (item.IsAlreadyWithPlayer || item.IsInCart)
            {
                item.IsAlreadyWithPlayer = false;
                item.CanJoinBackWithoutCost = false;
                if (item.IsInCart)
                {
                    item.IsInCart = false;
                    vm.PartiesInCart.Remove(item);
                }
            }

            item.Cost = RFArmyCommandHelpers.IsSameParty(item.Party.Army?.LeaderParty, item.Party)
                ? 0
                : calculationModel?.CalculatePartyInfluenceCost(Hero.MainHero.PartyBelongedTo, item.Party) ?? 0;
            item.UpdateEligibility();
        }

        SyncCurrentParties(vm);
        vm.TotalCost = GetFieldValue(InfluenceSpentForCohesionBoostingField, vm, 0);
        vm.SortControllerVM.CostState = 0;
        vm.SortControllerVM.ExecuteSortByCost();
        RefreshArmyManagementState(vm);
    }

    private static void CustomDisbandArmy(ArmyManagementVM vm)
    {
        MobileParty currentMainParty = RFArmyManagementUIContext.Instance?.CurrentMainParty;
        if (currentMainParty == null)
        {
            GetFieldValue<Action>(OnCloseField, vm)?.Invoke();
            return;
        }

        RFArmyCommandService.DisbandArmy(currentMainParty);

        GetPartiesToRemove(vm).Clear();
        if (vm?.PartiesInCart != null)
        {
            vm.PartiesInCart.Clear();
        }
        SyncCurrentParties(vm);
        vm.PlayerHasArmy = false;
        InvokeOnClose(vm);
        CampaignEventDispatcher.Instance.OnArmyOverlaySetDirty();
    }

    private static void ApplyArmyOrders(Army army)
    {
        RFArmyManagementUIContext context = RFArmyManagementUIContext.Instance;
        if (!RFArmyCommandHelpers.IsArmyInPlayerKingdom(army) || context == null)
        {
            return;
        }

        if (army?.LeaderParty?.IsMainParty == true)
        {
            return;
        }

        RFArmyCommandPlan plan = RFArmyCommandService.NormalizePlan(army.LeaderParty, context.PendingPlan)
            ?? RFArmyCommandService.NormalizePlan(army.LeaderParty, context.TargetSettlement, context.ArmyBehavior);
        if (plan == null)
        {
            return;
        }

        context.PendingPlan = null;
        context.TargetSettlement = plan.TargetSettlement;
        context.ArmyBehavior = plan.ArmyBehavior;
        RFArmyCommandService.ApplyPlan(army, plan, registerPlayerOverride: true);
    }

    private static MBBindingList<ArmyManagementItemVM> GetOrderedPartiesInCart(ArmyManagementVM vm)
    {
        MBBindingList<ArmyManagementItemVM> ordered = new();
        foreach (ArmyManagementItemVM item in vm.PartiesInCart
            .Where(item => item?.Party?.Party != null)
            .OrderByDescending(item => RFArmyCommandHelpers.IsSameParty(item.Party.Army?.LeaderParty, item.Party))
            .ThenByDescending(item => item.Party.Party.EstimatedStrength))
        {
            ordered.Add(item);
        }

        return ordered;
    }

    private static void RefreshArmyManagementState(ArmyManagementVM vm)
    {
        if (vm == null)
        {
            return;
        }

        try
        {
            OnRefreshMethod?.Invoke(vm, Array.Empty<object>());
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[RFArmyCommand] ArmyManagementVM.OnRefresh invoke failed: {ex}");
        }

        try
        {
            vm.RefreshValues();
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[RFArmyCommand] ArmyManagementVM.RefreshValues failed: {ex}");
        }
    }
}

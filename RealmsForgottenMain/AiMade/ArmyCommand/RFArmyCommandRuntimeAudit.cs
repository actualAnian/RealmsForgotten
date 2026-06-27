using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.AiMade.ArmyCommand;

internal static class RFArmyCommandRuntimeAudit
{
    private const string PatchCategoryName = "RFArmyCommand";

    internal static void LogStartupAudit(Assembly assembly)
    {
        try
        {
            RFLogger.Log("[RFArmyCommand] Runtime audit begin.");
            LogAssemblyFingerprint(assembly);
            LogConstructorPatchGuard(assembly);
            LogConstructor(
                "ArmyManagementVM..ctor(Action)",
                AccessTools.Constructor(typeof(ArmyManagementVM), new[] { typeof(Action) }));
            LogMethod("ArmyManagementVM.OnRefresh", AccessTools.Method(typeof(ArmyManagementVM), "OnRefresh"));
            LogMethod("ArmyManagementVM.RefreshValues", AccessTools.Method(typeof(ArmyManagementVM), "RefreshValues"));
            LogMethod("ArmyManagementVM.GetCanDisbandArmyWithReason", AccessTools.Method(typeof(ArmyManagementVM), "GetCanDisbandArmyWithReason"));
            LogMethod("ArmyManagementVM.OnAddToCart", AccessTools.Method(typeof(ArmyManagementVM), "OnAddToCart"));
            LogMethod("ArmyManagementVM.OnRemove", AccessTools.Method(typeof(ArmyManagementVM), "OnRemove"));
            LogMethod("ArmyManagementVM.ExecuteDone", AccessTools.Method(typeof(ArmyManagementVM), "ExecuteDone"));
            LogMethod("ArmyMenuOverlayVM.get_ArmyToUse", AccessTools.PropertyGetter(typeof(ArmyMenuOverlayVM), "ArmyToUse"));
            LogMethod("MapBarVM.GetIsGatherArmyVisible", AccessTools.Method(typeof(MapBarVM), "GetIsGatherArmyVisible"));
            LogMethod(
                "DefaultArmyManagementCalculationModel.CheckPartyEligibility",
                AccessTools.Method(
                    typeof(DefaultArmyManagementCalculationModel),
                    "CheckPartyEligibility",
                    new[] { typeof(MobileParty), typeof(TextObject).MakeByRefType() }));

            LogMethod(
                "CampaignUIHelper.GetCanManageCurrentArmyWithReason",
                RFArmyCanManagePatch.ResolveTargetMethod());

            string moduleRoot = GetModuleRoot();
            LogAsset(Path.Combine(moduleRoot, "SubModule.xml"));
            LogAsset(Path.Combine(moduleRoot, "GUI", "Prefabs", "Map", "RFArmyOverlayWindow.xml"));
            LogAsset(Path.Combine(moduleRoot, "GUI", "Prefabs", "Extensions", "RFArmyManagementWidgets.xml"));
            LogAsset(Path.Combine(moduleRoot, "GUI", "Brushes", "ArmyCommanderBrushes.xml"));

            int patchTypeCount = GetPatchTypes(assembly).Count;
            RFLogger.Log($"[RFArmyCommand] Runtime audit end. patchTypes={patchTypeCount}");
        }
        catch (Exception ex)
        {
            RFLogger.Log($"[RFArmyCommand] Runtime audit failed: {ex}");
        }
    }

    internal static bool ApplyCategorySafely(Harmony harmony, Assembly assembly)
    {
        List<Type> patchTypes = GetPatchTypes(assembly);
        if (patchTypes.Count == 0)
        {
            RFLogger.Log("[RFArmyCommand] Patch sweep found no categorized patch types.");
            return false;
        }

        RFLogger.Log($"[RFArmyCommand] Patch sweep begin. patchTypes={patchTypes.Count}");
        int successCount = 0;
        int failureCount = 0;

        foreach (Type patchType in patchTypes)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
                successCount++;
                RFLogger.Log($"[RFArmyCommand] Patched {patchType.FullName}");
            }
            catch (Exception ex)
            {
                failureCount++;
                RFLogger.Log($"[RFArmyCommand] Failed patching {patchType.FullName}: {ex}");
            }
        }

        RFLogger.Log($"[RFArmyCommand] Patch sweep finished. success={successCount} failed={failureCount}");
        return failureCount == 0 && successCount > 0;
    }

    private static List<Type> GetPatchTypes(Assembly assembly)
    {
        return GetAssemblyTypesSafe(assembly)
            .Where(type => string.Equals(type.Namespace, typeof(RFArmyCommandRuntimeAudit).Namespace, StringComparison.Ordinal))
            .Where(type => type.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == typeof(HarmonyPatch).FullName))
            .Where(type => type.CustomAttributes.Any(attribute =>
                attribute.AttributeType.FullName == typeof(HarmonyPatchCategory).FullName
                && attribute.ConstructorArguments.Count > 0
                && string.Equals(attribute.ConstructorArguments[0].Value as string, PatchCategoryName, StringComparison.Ordinal)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<Type> GetAssemblyTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            RFLogger.Log($"[RFArmyCommand] Runtime audit type load warning: loaded={ex.Types?.Count(type => type != null) ?? 0} loaderExceptions={ex.LoaderExceptions?.Length ?? 0}");
            return ex.Types.Where(type => type != null);
        }
    }

    private static string GetModuleRoot()
    {
        string assemblyDirectory = Path.GetDirectoryName(typeof(RFArmyCommandRuntimeAudit).Assembly.Location) ?? string.Empty;
        return Path.GetFullPath(Path.Combine(assemblyDirectory, "..", ".."));
    }

    private static void LogAssemblyFingerprint(Assembly assembly)
    {
        if (assembly == null)
        {
            RFLogger.Log("[RFArmyCommand] Assembly fingerprint missing | assembly=null");
            return;
        }

        string location = assembly.Location ?? string.Empty;
        string version = assembly.GetName().Version?.ToString() ?? "unknown";
        string lastWrite = File.Exists(location)
            ? File.GetLastWriteTimeUtc(location).ToString("O")
            : "missing";
        string hash = ComputeSha256(location) ?? "unavailable";

        RFLogger.Log($"[RFArmyCommand] Assembly fingerprint | path={location} | version={version} | lastWriteUtc={lastWrite} | sha256={hash}");
    }

    private static void LogConstructorPatchGuard(Assembly assembly)
    {
        Type patchType = assembly?.GetType("RealmsForgotten.AiMade.ArmyCommand.RFArmyManagementVMPatches");
        MethodInfo constructorPatchMethod = patchType?.GetMethod(
            "ConstructorPostfix",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        RFLogger.Log(constructorPatchMethod == null
            ? "[RFArmyCommand] Constructor patch guard FAILED | RFArmyManagementVMPatches.ConstructorPostfix missing"
            : "[RFArmyCommand] Constructor patch guard ok | RFArmyManagementVMPatches.ConstructorPostfix present");
    }

    private static string ComputeSha256(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            using FileStream stream = File.OpenRead(path);
            using SHA256 sha256 = SHA256.Create();
            return string.Concat(sha256.ComputeHash(stream).Select(b => b.ToString("X2")));
        }
        catch
        {
            return null;
        }
    }

    private static void LogAsset(string path)
    {
        RFLogger.Log($"[RFArmyCommand] Asset {(File.Exists(path) ? "ok" : "missing")} | {path}");
    }

    private static void LogMethod(string label, MethodBase method)
    {
        RFLogger.Log($"[RFArmyCommand] Method {(method == null ? "missing" : "ok")} | {label}");
    }

    private static void LogConstructor(string label, ConstructorInfo constructor)
    {
        RFLogger.Log($"[RFArmyCommand] Constructor {(constructor == null ? "missing" : "ok")} | {label}");
    }
}

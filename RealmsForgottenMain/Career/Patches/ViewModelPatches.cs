using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using RealmsForgotten.UI;

namespace RealmsForgotten.Career.Patches
{
    [HarmonyPatch]
    public static class ViewModelPatches
    {
        private const string Headposition = "HeadPosition";
        private const string Position = "Position";
        private const string DistanceToCamera = "DistanceToCamera";

        // Os prefixes abaixo so podem interceptar membros que a EXTENSAO RF possui.
        // A versao anterior curto-circuitava TODO binding de uma VM estendida, e
        // propriedades de mixin de outros mods (UIExtenderEx — ex.: o botao do
        // spellbook do SOTOR, IsSpellBookButtonVisible) vivem no cache
        // _propertiesAndMethods, que so o pipeline vanilla consulta → resolviam
        // null e a UI deles morria. Regra: e nosso → extensao; nao e → vanilla.
        private static readonly Dictionary<Type, HashSet<string>> _ownedProps = new();
        private static readonly Dictionary<Type, HashSet<string>> _ownedMethods = new();

        private static bool ExtensionOwnsProperty(IViewModelExtension ext, string name)
        {
            if (ext == null || string.IsNullOrEmpty(name)) return false;
            Type t = ext.GetType();
            if (!_ownedProps.TryGetValue(t, out var set))
            {
                set = new HashSet<string>(ext.GetProperties().Keys, StringComparer.Ordinal);
                _ownedProps[t] = set;
            }
            return set.Contains(name);
        }

        private static bool ExtensionOwnsMethod(IViewModelExtension ext, string name)
        {
            if (ext == null || string.IsNullOrEmpty(name)) return false;
            Type t = ext.GetType();
            if (!_ownedMethods.TryGetValue(t, out var set))
            {
                set = new HashSet<string>(ext.GetMethods().Keys, StringComparer.Ordinal);
                _ownedMethods[t] = set;
            }
            return set.Contains(name);
        }

        private static void LogPatchFailure(string stage, Exception ex, ViewModel vm = null)
        {
            try
            {
                global::RealmsForgotten.AiMade.RFLogger.Log(
                    $"[ViewModelPatches] {stage} failed | vm={vm?.GetType().FullName ?? "null"} | error={ex}");
            }
            catch
            {
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewModel), MethodType.Constructor)]
        public static void PatchVMConstructor(ViewModel __instance)
        {
            try
            {
                if (!__instance.HasExtensionType())
                {
                    return;
                }

                var VMExtensionType = __instance.GetExtensionType();
                if (VMExtensionType == null)
                {
                    return;
                }

                var exists = Traverse.Create(__instance).Field("_propertiesAndMethods").FieldExists();
                if (!exists)
                {
                    return;
                }

                if (Activator.CreateInstance(VMExtensionType, __instance) is not IViewModelExtension VMExtensionInstance)
                {
                    return;
                }

                var field = Traverse.Create(__instance).Field("_propertiesAndMethods").GetValue();
                var props = Traverse.Create(field).Property("Properties").GetValue() as Dictionary<string, PropertyInfo>;
                var methods = Traverse.Create(field).Property("Methods").GetValue() as Dictionary<string, MethodInfo>;
                if (props == null || methods == null)
                {
                    return;
                }

                foreach (var prop in VMExtensionInstance.GetProperties())
                {
                    props.AddItem(prop);
                }

                foreach (var method in VMExtensionInstance.GetMethods())
                {
                    methods.AddItem(method);
                }
            }
            catch (Exception ex)
            {
                LogPatchFailure(nameof(PatchVMConstructor), ex, __instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "OnFinalize")]
        public static void PatchVMDestructor(ViewModel __instance)
        {
            try
            {
                if (__instance.HasExtensionInstance())
                {
                    __instance.GetExtensionInstance().OnFinalize();
                }
            }
            catch (Exception ex)
            {
                LogPatchFailure(nameof(PatchVMDestructor), ex, __instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "GetViewModelAtPath", typeof(BindingPath))]
        public static bool PatchPathFinder(ViewModel __instance, BindingPath path, ref object __result)
        {
            try
            {
                if (__instance.HasExtensionInstance())
                {
                    var ext = __instance.GetExtensionInstance();
                    // So assumimos o caminho se o primeiro no do subpath for NOSSO;
                    // caminhos vanilla e de mixin de outros mods seguem no vanilla.
                    if (ExtensionOwnsProperty(ext, path?.SubPath?.FirstNode))
                    {
                        __result = ext.GetViewModelAtPath(path);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogPatchFailure(nameof(PatchPathFinder), ex, __instance);
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "GetPropertyValue", typeof(string))]
        public static bool PatchPropertyGetter(ViewModel __instance, string name, ref object __result)
        {
            try
            {
                if (__instance.HasExtensionInstance())
                {
                    var ext = __instance.GetExtensionInstance();
                    if (ExtensionOwnsProperty(ext, name))
                    {
                        __result = ext.GetPropertyValue(name);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogPatchFailure(nameof(PatchPropertyGetter), ex, __instance);
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "SetPropertyValue")]
        public static bool PatchPropertySetter(ViewModel __instance, string name, object value)
        {
            if (name == DistanceToCamera) return true;
            if (name == Position) return true;
            if (name == Headposition) return true;

            try
            {
                if (__instance.HasExtensionInstance())
                {
                    var ext = __instance.GetExtensionInstance();
                    if (ExtensionOwnsProperty(ext, name))
                    {
                        ext.SetPropertyValue(name, value);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogPatchFailure(nameof(PatchPropertySetter), ex, __instance);
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "ExecuteCommand")]
        public static bool PatchExecutor(ViewModel __instance, string commandName, object[] parameters)
        {
            try
            {
                if (__instance.HasExtensionInstance())
                {
                    var ext = __instance.GetExtensionInstance();
                    if (ExtensionOwnsMethod(ext, commandName))
                    {
                        ext.ExecuteCommand(commandName, parameters);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogPatchFailure(nameof(PatchExecutor), ex, __instance);
            }

            return true;
        }
    }
}

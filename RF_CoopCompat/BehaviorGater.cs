using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace RF_CoopCompat
{
    /// <summary>
    /// Aplica o mesmo padrao que o Coop usa nos behaviors vanilla, so que nos
    /// behaviors do RF: prefixo Harmony em RegisterEvents de cada
    /// CampaignBehavior, decidindo em runtime se o registro de eventos roda.
    /// SyncData nao passa por RegisterEvents, entao o save continua integro
    /// mesmo com o behavior silenciado.
    ///
    /// Tambem pode bloquear OnMissionBehaviorInitialize dos submodulos RF
    /// durante sessao coop, ja que as batalhas coop montam a propria lista de
    /// MissionBehaviors e logica de missao nao-sincronizada so causaria
    /// divergencia entre dono e fantoche.
    /// </summary>
    internal static class BehaviorGater
    {
        private static CompatConfig? _config;
        private static int _behaviorsPatched;
        private static int _submodulesPatched;

        public static void Install(Harmony harmony, CompatConfig config)
        {
            _config = config;

            var rfAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => config.TargetAssemblies.Contains(a.GetName().Name))
                .ToList();

            CompatLog.Info($"gater: scanning {rfAssemblies.Count} RF assemblies " +
                           $"({string.Join(", ", rfAssemblies.Select(a => a.GetName().Name))})");

            var patchedMethods = new HashSet<MethodBase>();
            foreach (var assembly in rfAssemblies)
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    try
                    {
                        if (typeof(CampaignBehaviorBase).IsAssignableFrom(type) && !type.IsAbstract)
                            PatchRegisterEvents(harmony, type, patchedMethods);

                        if (_config.DisableMissionLogic &&
                            typeof(MBSubModuleBase).IsAssignableFrom(type) && !type.IsAbstract)
                            PatchMissionInit(harmony, type, patchedMethods);
                    }
                    catch (Exception ex)
                    {
                        CompatLog.Info($"gater: failed on {type.FullName}: {ex.Message}");
                    }
                }
            }

            CompatLog.Info($"gater: {_behaviorsPatched} CampaignBehavior RegisterEvents gated, " +
                           $"{_submodulesPatched} SubModule OnMissionBehaviorInitialize gated");
        }

        private static void PatchRegisterEvents(Harmony harmony, Type type, HashSet<MethodBase> patchedMethods)
        {
            // pega a implementacao mais derivada declarada dentro dos assemblies RF
            var method = FindDeclared(type, "RegisterEvents", Type.EmptyTypes);
            if (method == null || method.IsAbstract) return;
            if (!patchedMethods.Add(method)) return;

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(BehaviorGater), nameof(RegisterEventsPrefix))
            {
                priority = Priority.First,
            });
            _behaviorsPatched++;
        }

        private static void PatchMissionInit(Harmony harmony, Type type, HashSet<MethodBase> patchedMethods)
        {
            var method = FindDeclared(type, "OnMissionBehaviorInitialize", new[] { typeof(Mission) });
            if (method == null || method.IsAbstract) return;
            if (!patchedMethods.Add(method)) return;

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(BehaviorGater), nameof(MissionInitPrefix))
            {
                priority = Priority.First,
            });
            _submodulesPatched++;
        }

        /// <summary>
        /// Sobe a hierarquia ate achar a implementacao declarada do metodo,
        /// parando antes de sair dos assemblies alvo (nunca patcheia o vanilla).
        /// </summary>
        private static MethodInfo? FindDeclared(Type type, string name, Type[] parameters)
        {
            var config = _config;
            for (var current = type; current != null; current = current.BaseType)
            {
                if (config != null && !config.TargetAssemblies.Contains(current.Assembly.GetName().Name))
                    return null;

                var method = current.GetMethod(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                    null, parameters, null);
                if (method != null) return method;
            }
            return null;
        }

        internal static bool RegisterEventsPrefix(CampaignBehaviorBase __instance)
        {
            if (!CoopSessionState.Active) return true;

            var config = _config;
            var policy = config?.PolicyFor(__instance.GetType()) ?? BehaviorPolicy.HostOnly;
            bool run;
            switch (policy)
            {
                case BehaviorPolicy.Everywhere:
                    run = true;
                    break;
                case BehaviorPolicy.Disabled:
                    run = false;
                    break;
                default:
                    run = CoopSessionState.IsServer;
                    break;
            }

            if (!run)
                CompatLog.Info($"gate: skipped RegisterEvents of {__instance.GetType().FullName} (policy={policy})");
            return run;
        }

        internal static bool MissionInitPrefix(MBSubModuleBase __instance)
        {
            if (!CoopSessionState.Active) return true;

            CompatLog.Info($"gate: skipped OnMissionBehaviorInitialize of {__instance.GetType().FullName}");
            return false;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // tipos que dependem de assemblies ausentes (ex.: NavalDLC desligado)
                return ex.Types.Where(t => t != null).Select(t => t!);
            }
        }
    }
}

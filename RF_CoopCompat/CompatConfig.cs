using System;
using System.Collections.Generic;
using System.IO;

namespace RF_CoopCompat
{
    internal enum BehaviorPolicy
    {
        HostOnly,
        Everywhere,
        Disabled,
    }

    /// <summary>
    /// Configuracao carregada de rf_coop_compat.cfg (na raiz do modulo).
    /// Formato de linha unica por diretiva; ver comentarios no proprio .cfg.
    /// </summary>
    internal sealed class CompatConfig
    {
        // Estabilidade primeiro: sem sessao configurada, nada do RF roda em coop.
        // O host do Coop nao tem MainHero, entao "host-only" nao e um padrao seguro.
        public BehaviorPolicy DefaultPolicy = BehaviorPolicy.Disabled;
        public bool DisableMissionLogic = true;

        // Neutraliza o bloqueio de DLC do Coop para permitir conectar com War
        // Sails ativo (o RF depende dele). Default true neste projeto.
        public bool AllowDlc = true;

        // padrao (prefixo de FullName ou nome simples) -> politica
        public readonly List<KeyValuePair<string, BehaviorPolicy>> Policies = new();
        public readonly List<string> UnpatchClientIds = new();
        public readonly List<string> UnpatchSessionIds = new();

        // assemblies do RF varridos pelo gater (nomes sem extensao)
        public readonly HashSet<string> TargetAssemblies = new(StringComparer.OrdinalIgnoreCase)
        {
            "RealmsForgotten",
            "RFCustomSettlements",
            "NecromancyAndSummoning",
            "RFSmithing",
            "RFReligions",
            "HuntableHerds",
            "RF_AIDialog",
            "RF_BattleAI",
            "RF_Ambush",
            "RF_Promoted",
            "RF_warsystem",
            "RF_Settlers",
            "RF_ResourceZones",
            "RF_LivingWorld",
            "RF_Enlistment",
            "HomesteadsReloaded",
            "SOTOR",
            "RF_DualWield",
            "RBM_RF",
            "RFMonsters",
        };

        public static CompatConfig Load(string moduleRoot)
        {
            var config = new CompatConfig();
            var path = Path.Combine(moduleRoot, "rf_coop_compat.cfg");
            if (!File.Exists(path))
            {
                CompatLog.Info($"config not found at {path}; using defaults");
                return config;
            }

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    switch (parts[0].ToLowerInvariant())
                    {
                        case "default" when parts.Length >= 2:
                            config.DefaultPolicy = ParsePolicy(parts[1]);
                            break;
                        case "policy" when parts.Length >= 3:
                            config.Policies.Add(new KeyValuePair<string, BehaviorPolicy>(parts[1], ParsePolicy(parts[2])));
                            break;
                        case "assembly" when parts.Length >= 2:
                            config.TargetAssemblies.Add(parts[1]);
                            break;
                        case "unpatch_client" when parts.Length >= 2:
                            config.UnpatchClientIds.Add(parts[1]);
                            break;
                        case "unpatch_session" when parts.Length >= 2:
                            config.UnpatchSessionIds.Add(parts[1]);
                            break;
                        case "set" when parts.Length >= 3:
                            if (parts[1].Equals("disable_mission_logic", StringComparison.OrdinalIgnoreCase))
                                config.DisableMissionLogic = bool.Parse(parts[2]);
                            else if (parts[1].Equals("allow_dlc", StringComparison.OrdinalIgnoreCase))
                                config.AllowDlc = bool.Parse(parts[2]);
                            break;
                        default:
                            CompatLog.Info($"config: unrecognized line '{line}'");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    CompatLog.Info($"config: failed to parse line '{line}': {ex.Message}");
                }
            }

            CompatLog.Info($"config loaded: default={config.DefaultPolicy}, policies={config.Policies.Count}, " +
                           $"unpatch_client={config.UnpatchClientIds.Count}, unpatch_session={config.UnpatchSessionIds.Count}, " +
                           $"disable_mission_logic={config.DisableMissionLogic}");
            return config;
        }

        public BehaviorPolicy PolicyFor(Type type)
        {
            var fullName = type.FullName ?? type.Name;
            foreach (var entry in Policies)
            {
                if (fullName.StartsWith(entry.Key, StringComparison.Ordinal) ||
                    type.Name.Equals(entry.Key, StringComparison.Ordinal))
                {
                    return entry.Value;
                }
            }
            return DefaultPolicy;
        }

        private static BehaviorPolicy ParsePolicy(string token)
        {
            switch (token.ToLowerInvariant())
            {
                case "hostonly": return BehaviorPolicy.HostOnly;
                case "everywhere": return BehaviorPolicy.Everywhere;
                case "disabled": return BehaviorPolicy.Disabled;
                default: throw new ArgumentException($"unknown policy '{token}'");
            }
        }
    }
}

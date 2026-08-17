using System;
using HarmonyLib;

namespace RF_CoopCompat
{
    /// <summary>
    /// Detecta inicio/fim de sessao coop sem referencia de compilacao ao Coop.
    /// O Coop so aplica seus patches Harmony quando uma sessao inicia
    /// (GameInterface.PatchAll dentro de StartAsServer/StartAsClient), entao um
    /// postfix ali e o sinal exato de "sessao coop ativa".
    /// </summary>
    internal static class CoopSessionHook
    {
        private static Harmony? _harmony;
        private static CompatConfig? _config;
        public static bool Installed { get; private set; }

        public static bool TryInstall(Harmony harmony, CompatConfig config)
        {
            if (Installed) return true;
            if (!CoopBridge.TryResolve()) return false;

            _harmony = harmony;
            _config = config;

            try
            {
                harmony.Patch(CoopBridge.PatchAllMethod,
                    postfix: new HarmonyMethod(typeof(CoopSessionHook), nameof(OnCoopPatchesApplied)));
                if (CoopBridge.UnpatchAllMethod != null)
                {
                    harmony.Patch(CoopBridge.UnpatchAllMethod,
                        postfix: new HarmonyMethod(typeof(CoopSessionHook), nameof(OnCoopPatchesRemoved)));
                }

                Installed = true;
                CompatLog.Info("session hook installed on GameInterface.PatchAll/UnpatchAll");
                return true;
            }
            catch (Exception ex)
            {
                CompatLog.Info($"session hook install failed: {ex}");
                return false;
            }
        }

        private static void OnCoopPatchesApplied()
        {
            try
            {
                // fallback do polling de CoopServices; idempotente.
                if (_harmony != null && _config != null)
                    CoopSessionState.BeginSession(CoopBridge.IsServer, _harmony, _config);
            }
            catch (Exception ex)
            {
                CompatLog.Info($"session start handling failed: {ex}");
            }
        }

        private static void OnCoopPatchesRemoved()
        {
            CoopSessionState.EndSession();
        }
    }
}

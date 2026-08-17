using System;
using HarmonyLib;

namespace RF_CoopCompat
{
    /// <summary>
    /// Remove, no inicio da sessao coop, patches Harmony do RF que sao
    /// perigosos dentro da sessao (config: unpatch_client / unpatch_session).
    /// A remocao vale ate o jogo reiniciar; voltar ao singleplayer depois de
    /// uma sessao coop exige restart de qualquer forma (o proprio Coop tambem
    /// remove patches globalmente ao encerrar).
    /// </summary>
    internal static class SessionUnpatcher
    {
        public static void Run(Harmony harmony, CompatConfig config, bool isServer)
        {
            foreach (var ownerId in config.UnpatchSessionIds)
                Unpatch(harmony, ownerId, "session");

            if (!isServer)
            {
                foreach (var ownerId in config.UnpatchClientIds)
                    Unpatch(harmony, ownerId, "client");
            }
        }

        private static void Unpatch(Harmony harmony, string ownerId, string scope)
        {
            try
            {
                harmony.UnpatchAll(ownerId);
                CompatLog.Info($"unpatch[{scope}]: removed all patches owned by '{ownerId}'");
            }
            catch (Exception ex)
            {
                CompatLog.Info($"unpatch[{scope}]: failed for '{ownerId}': {ex.Message}");
            }
        }
    }
}

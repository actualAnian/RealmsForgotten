using System;
using HarmonyLib;

namespace RF_CoopCompat
{
    /// <summary>
    /// Estado global da sessao coop. Fora de sessao, Active == false e todos os
    /// gates sao transparentes. BeginSession/EndSession sao idempotentes: podem
    /// ser chamados tanto pelo polling de servicos (CoopServices) quanto pelo
    /// hook em PatchAll (CoopSessionHook) sem efeito duplo.
    /// </summary>
    internal static class CoopSessionState
    {
        public static volatile bool Active;

        /// <summary>Snapshot do papel no momento em que a sessao iniciou.</summary>
        public static volatile bool IsServer;

        private static bool _started;
        private static readonly object Gate = new object();

        public static void BeginSession(bool isServer, Harmony harmony, CompatConfig config)
        {
            lock (Gate)
            {
                if (_started) return;
                _started = true;
                IsServer = isServer;
                Active = true;
                CompatLog.Info($"coop session STARTED, role={(isServer ? "server" : "client")}; RF gates active");

                try
                {
                    SessionUnpatcher.Run(harmony, config, isServer);
                }
                catch (Exception ex)
                {
                    CompatLog.Info($"session unpatch failed: {ex}");
                }
            }
        }

        public static void EndSession()
        {
            lock (Gate)
            {
                if (!_started) return;
                _started = false;
                Active = false;
                CompatLog.Info("coop session ENDED; restart o jogo antes de voltar ao singleplayer");
            }
        }
    }
}

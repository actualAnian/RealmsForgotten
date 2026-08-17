using System;

namespace RF_CoopWarsails
{
    /// <summary>
    /// Orquestrador de sessao: mantem o CoopNet vivo, e chamado a cada frame
    /// pelo SubModule, envia um heartbeat com contador crescente ~1x/s e
    /// reporta o contador que chega do outro lado (prova visivel de rede
    /// bidirecional entre os dois PCs).
    /// </summary>
    internal static class CoopController
    {
        private static CoopNet? _net;
        private static float _heartbeatTimer;
        private static int _localCounter;
        private static int _lastReportedRemote = -1;

        public static bool Active => _net != null;
        public static CoopLinkState State => _net?.State ?? CoopLinkState.Idle;
        public static bool Connected => _net?.State == CoopLinkState.Connected;

        /// <summary>Acesso ao transporte para o MissionBehavior de sync (marco 2).</summary>
        internal static CoopNet? Net => _net;

        public static void Host()
        {
            EnsureNet();
            _net!.StartHost();
        }

        public static void Join(string ip)
        {
            EnsureNet();
            _net!.StartJoin(ip);
        }

        public static void Stop()
        {
            if (_net == null) return;
            _net.Stop();
            _net = null;
            _localCounter = 0;
            _lastReportedRemote = -1;
            CoopLog.Screen("sessao encerrada");
        }

        public static void Tick(float dt)
        {
            if (_net == null) return;
            _net.Poll();

            if (_net.State != CoopLinkState.Connected) return;

            _heartbeatTimer += dt;
            if (_heartbeatTimer >= 1f)
            {
                _heartbeatTimer = 0f;
                _localCounter++;
                _net.SendHeartbeat(_localCounter, Environment.MachineName);
            }

            // reporta na tela quando o contador remoto avanca (a cada ~5 para nao spammar)
            if (_net.LastReceivedCounter != _lastReportedRemote &&
                _net.LastReceivedCounter % 5 == 0 &&
                _net.LastReceivedCounter >= 0)
            {
                _lastReportedRemote = _net.LastReceivedCounter;
                CoopLog.Screen($"recebendo de '{_net.RemoteName}': tick #{_net.LastReceivedCounter}");
            }
        }

        private static void EnsureNet()
        {
            if (_net != null) return;
            _net = new CoopNet();
        }
    }
}

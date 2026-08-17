using System;
using LiteNetLib;
using LiteNetLib.Utils;

namespace RF_CoopWarsails
{
    internal enum CoopRole { None, Host, Client }

    internal enum CoopLinkState { Idle, Listening, Connecting, Connected, Disconnected }

    /// <summary>
    /// Transporte P2P minimalista sobre LiteNetLib. Codigo proprio: abre um
    /// NetManager, aceita/estabelece uma unica conexao e troca dois tipos de
    /// pacote (handshake + heartbeat). Serve de fundacao para os marcos
    /// seguintes (missao compartilhada, dono/fantoche, etc.).
    ///
    /// Um "connection key" compartilhado impede que instancias nao relacionadas
    /// (ou de outra versao) se conectem por engano.
    /// </summary>
    internal sealed class CoopNet
    {
        public const int DefaultPort = 4300;
        private const string ConnectionKey = "RF_CoopWarsails_v0.1";

        private const byte PacketHandshake = 1;
        private const byte PacketHeartbeat = 2;
        private const byte PacketAgentState = 3;

        private readonly EventBasedNetListener _listener = new EventBasedNetListener();
        private readonly NetManager _manager;
        private NetPeer? _peer;

        public CoopRole Role { get; private set; } = CoopRole.None;
        public CoopLinkState State { get; private set; } = CoopLinkState.Idle;

        /// <summary>Ultimo contador recebido do outro lado (prova de dados fluindo).</summary>
        public int LastReceivedCounter { get; private set; } = -1;
        public string RemoteName { get; private set; } = "";

        // Ultimo estado de agente recebido do outro jogador (latest-wins).
        // Lido/escrito na thread do jogo (Poll roda em OnApplicationTick).
        public AgentStateSnapshot LatestRemoteAgentState { get; private set; }
        public bool HasRemoteAgentState { get; private set; }

        public event Action<CoopLinkState>? StateChanged;

        public CoopNet()
        {
            _manager = new NetManager(_listener)
            {
                UnconnectedMessagesEnabled = false,
                IPv6Enabled = false,
                DisconnectTimeout = 10000,
            };

            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkReceiveEvent += OnNetworkReceive;
        }

        public bool StartHost(int port = DefaultPort)
        {
            if (State != CoopLinkState.Idle && State != CoopLinkState.Disconnected) return false;
            if (!_manager.Start(port))
            {
                CoopLog.Screen($"falha ao abrir porta UDP {port}", 0xFFFF4040);
                return false;
            }
            Role = CoopRole.Host;
            SetState(CoopLinkState.Listening);
            CoopLog.Screen($"hospedando em UDP {port}; aguardando o outro jogador...");
            return true;
        }

        public bool StartJoin(string ip, int port = DefaultPort)
        {
            if (State != CoopLinkState.Idle && State != CoopLinkState.Disconnected) return false;
            if (!_manager.Start())
            {
                CoopLog.Screen("falha ao iniciar o cliente de rede", 0xFFFF4040);
                return false;
            }
            Role = CoopRole.Client;
            SetState(CoopLinkState.Connecting);
            CoopLog.Screen($"conectando a {ip}:{port}...");
            _peer = _manager.Connect(ip, port, ConnectionKey);
            return _peer != null;
        }

        /// <summary>Chamar todo frame do jogo.</summary>
        public void Poll() => _manager.PollEvents();

        public void SendHeartbeat(int counter, string localName)
        {
            if (_peer == null || State != CoopLinkState.Connected) return;
            var writer = new NetDataWriter();
            writer.Put(PacketHeartbeat);
            writer.Put(counter);
            writer.Put(localName);
            _peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>Envia o estado do agente local. Sequenced: descarta pacotes
        /// atrasados, ideal para movimento (o COOP usa Unreliable aqui).</summary>
        public void SendAgentState(in AgentStateSnapshot state)
        {
            if (_peer == null || State != CoopLinkState.Connected) return;
            var writer = new NetDataWriter();
            writer.Put(PacketAgentState);
            state.Write(writer);
            _peer.Send(writer, DeliveryMethod.Sequenced);
        }

        public void Stop()
        {
            try { _manager.Stop(); } catch { /* ignore */ }
            _peer = null;
            Role = CoopRole.None;
            SetState(CoopLinkState.Idle);
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {
            if (_peer != null) { request.Reject(); return; }
            request.AcceptIfKey(ConnectionKey);
        }

        private void OnPeerConnected(NetPeer peer)
        {
            _peer = peer;
            SetState(CoopLinkState.Connected);
            CoopLog.Screen($"CONECTADO a {peer.Address} (papel: {Role})");

            var writer = new NetDataWriter();
            writer.Put(PacketHandshake);
            writer.Put(Environment.MachineName);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            CoopLog.Screen($"desconectado ({info.Reason})", 0xFFFFAA00);
            _peer = null;
            SetState(CoopLinkState.Disconnected);
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            try
            {
                var type = reader.GetByte();
                switch (type)
                {
                    case PacketHandshake:
                        RemoteName = reader.GetString();
                        CoopLog.Screen($"handshake recebido de '{RemoteName}'");
                        break;
                    case PacketHeartbeat:
                        LastReceivedCounter = reader.GetInt();
                        RemoteName = reader.GetString();
                        break;
                    case PacketAgentState:
                        LatestRemoteAgentState = AgentStateSnapshot.Read(reader);
                        HasRemoteAgentState = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                CoopLog.File_($"erro ao ler pacote: {ex.Message}");
            }
            finally
            {
                reader.Recycle();
            }
        }

        private void SetState(CoopLinkState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}

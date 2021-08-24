using System;
using Network.Protocol;
using Version = Network.Protocol.Version;
using NLog;

namespace Network.Infrastructure
{
    public class ConnectionClientPacketHandlerAttribute : PacketHandlerAttribute
    {
        public ConnectionClientPacketHandlerAttribute(EClientConnectionState state, EPacket eType)
        {
            State = state;
            Type = eType;
        }
    }

    public class ConnectionClient : ConnectionBase
    {
        private readonly ConnectionClientSM m_ClientSM;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public event Action<ConnectionClient> OnConnected;

        public ConnectionClient(
            INetworkConnection network,
            IGameStatePersistence persistence) : base(network, persistence)
        {
            m_ClientSM = new ConnectionClientSM();

            #region State Machine Callbacks
            // Disconnect State
            m_ClientSM.DisconnectState.OnEntryFrom(m_ClientSM.DisconnectTrigger, closeConnection);

            // Join Requesting State
            m_ClientSM.JoinRequestingState.OnEntry(sendClientHello);
            #endregion

            // Packet Handler Registration
            Dispatcher.RegisterPacketHandler(receiveClientInfoRequest);
            Dispatcher.RegisterPacketHandler(receiveJoinRequestAccepted);
            Dispatcher.RegisterPacketHandler(receiveServerKeepAlive);

            Dispatcher.RegisterStateMachine(this, m_ClientSM);
        }

        public override Enum State => m_ClientSM.StateMachine.State;

        public Action<ConnectionClient> OnClientJoined { get; set; }

        public event Action<EDisconnectReason> OnDisconnected;

        ~ConnectionClient()
        {
            Dispatcher.UnregisterPacketHandlers(this);
        }

        public void Connect()
        {
            m_ClientSM.StateMachine.Fire(EClientConnectionTrigger.TryJoinServer);
        }

        public override void Disconnect(EDisconnectReason eReason)
        {
            if (!m_ClientSM.StateMachine.IsInState(EClientConnectionState.Disconnected))
            {
                m_ClientSM.StateMachine.Fire(
                    m_ClientSM.DisconnectTrigger,
                    eReason);
            }
        }

        private void closeConnection(EDisconnectReason eReason)
        {
            Network.Close(eReason);
            OnDisconnected?.Invoke(eReason);
        }

        #region ClientJoinRequesting
        private void sendClientHello()
        {
            Send(new Packet(EPacket.Client_Hello, new Client_Hello(Version.Number).Serialize()));
        }

        [ConnectionClientPacketHandler(EClientConnectionState.JoinRequesting, EPacket.Server_RequestClientInfo)]
        private void receiveClientInfoRequest(ConnectionBase connection, Packet packet)
        {
            Logger.Info("Client information was requested");
            Server_RequestClientInfo payload =
                Server_RequestClientInfo.Deserialize(new ByteReader(packet.Payload));
            sendClientInfo();
        }

        private void sendClientInfo()
        {
            var client_info_player = new Player("Unknown");
            Logger.Info("Telling the server my name is {}", client_info_player.Name);
            Send(
                new Packet(
                    EPacket.Client_Info,
                    new Client_Info(client_info_player).Serialize()));
        }

        [ConnectionClientPacketHandler(EClientConnectionState.JoinRequesting, EPacket.Server_JoinRequestAccepted)]
        private void receiveJoinRequestAccepted(ConnectionBase connection, Packet packet)
        {
            Logger.Info("My request to join the server was accepted");
            Server_JoinRequestAccepted payload =
                Server_JoinRequestAccepted.Deserialize(new ByteReader(packet.Payload));
            m_ClientSM.StateMachine.Fire(EClientConnectionTrigger.ServerAcceptedJoinRequest);

            OnConnected?.Invoke(this);
        }

        #endregion

        [ConnectionClientPacketHandler(EClientConnectionState.Connected, EPacket.KeepAlive)]
        private void receiveServerKeepAlive(ConnectionBase connection, Packet packet)
        {
            KeepAlive payload = KeepAlive.Deserialize(new ByteReader(packet.Payload));
            Send(new Packet(EPacket.KeepAlive, new KeepAlive(payload.m_iKeepAliveID).Serialize()));
        }
    }
}

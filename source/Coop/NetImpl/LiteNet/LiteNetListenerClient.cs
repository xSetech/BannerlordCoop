﻿using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using Network;
using Network.Infrastructure;
using NLog;

namespace Coop.NetImpl.LiteNet
{
    public class LiteNetListenerClient : INetEventListener
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly GameSession m_Session;

        public LiteNetListenerClient(GameSession session)
        {
            m_Session = session;
        }

        public void OnPeerConnected(NetPeer peer)
        {
            Logger.Info($"peer connected");
            LiteNetConnection network = new LiteNetConnection(peer);
            RailNetPeerWrapper persistence = new RailNetPeerWrapper(network);
            ConnectionClient connection = new ConnectionClient(
                network,
                persistence);
            m_Session.ConnectionCreated(connection);
            peer.Tag = connection;
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Info($"peer disconnected");
            if (m_Session.Connection != null)
            {
                m_Session.Disconnect(disconnectInfo.GetReason(false));
                peer.Tag = null;
            }
        }

        public void OnNetworkReceive(
            NetPeer peer,
            NetPacketReader reader,
            DeliveryMethod deliveryMethod)
        {
            if (reader.IsNull)
            {
                throw new InvalidNetworkPackageException($"Received empty package from ${peer}.");
            }

            peer.GetConnection()?.Receive(reader.GetRemainingBytesSegment());
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Logger.Error(
                "OnNetworkError({endPoint}, {socketError}).",
                endPoint.ToFriendlyString(),
                socketError);
            if (m_Session.Connection != null)
            {
                m_Session.Disconnect(EDisconnectReason.Unknown);
            }
        }

        public void OnNetworkReceiveUnconnected(
            IPEndPoint remoteEndPoint,
            NetPacketReader reader,
            UnconnectedMessageType messageType)
        {
            if (messageType == UnconnectedMessageType.Broadcast)
            {
                ArraySegment<byte> buffer = reader.GetRemainingBytesSegment();
                bool isDiscoveryPacket = Network.Protocol.Discovery.TryDeserialize(new ByteReader(buffer));
                if (isDiscoveryPacket)
                {
                    Logger.Info("Server discovered: {peer}", remoteEndPoint);
                    // TODO: Maybe connect to it if we are not already connected?
                }
            }
            else
            {
                Logger.Debug(
                    "Unknown broadcast: OnNetworkReceiveUnconnected({remoteEndPoint}, {reader}, {messageType}).",
                    remoteEndPoint,
                    reader,
                    messageType);
            }
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }
    }
}

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Common;
using Coop.Mod.Managers;
using Coop.Mod.Persistence;
using Coop.Mod.Persistence.RemoteAction;
using Coop.Mod.Serializers;
using Coop.NetImpl;
using Coop.NetImpl.LiteNet;
using CoopFramework;
using JetBrains.Annotations;
using Network;
using Network.Infrastructure;
using Network.Protocol;
using NLog;
using RailgunNet.Connection.Client;
using RailgunNet.Logic;
using Sync.Store;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using Logger = NLog.Logger;

namespace Coop.Mod
{
    class GameClientPacketHandlerAttribute : PacketHandlerAttribute
    {
        public GameClientPacketHandlerAttribute(ECoopClientState state, EPacket eType)
        {
            State = state;
            Type = eType;
        }
    }

    public class CoopClient : IUpdateable, IClientAccess
    {
        #region Private
        private const int MaxReconnectAttempts = 2;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Lazy<CoopClient> m_Instance =
            new Lazy<CoopClient>(() => new CoopClient(new ClientConfiguration()));
        private readonly CoopClientSM m_CoopClientSM;

        [NotNull] private readonly LiteNetManagerClient m_NetManager;
        private readonly UpdateableList m_Updateables = new UpdateableList();

        /// <summary>
        ///     Internal data storage for <see cref="SyncedObjectStore" />.
        /// </summary>
        private readonly Dictionary<ObjectId, object> m_SyncedObjects =
            new Dictionary<ObjectId, object>();

        private MBGameManager gameManager;

        private int m_ReconnectAttempts = MaxReconnectAttempts;
        private Hero m_Hero;
        private MBGUID m_HeroGUID;
        private ObjectId m_HeroId;
        #endregion
        public Action<PersistenceClient> OnPersistenceInitialized;

        public Action<RemoteStore> RemoteStoreCreated;

        public CoopClient(ClientConfiguration config)
        {
            Session = new GameSession(new GameData());
            Session.OnConnectionDestroyed += ConnectionDestroyed;
            m_NetManager = new LiteNetManagerClient(Session, config);
            m_Updateables.Add(m_NetManager);
            Events = new CoopEvents();
            m_CoopClientSM = new CoopClientSM();
            Synchronization = new CoopSyncClient(this);
            
            #region State Machine Callbacks
            m_CoopClientSM.CharacterCreationState.OnEntry(CreateCharacter);
            m_CoopClientSM.ReceivingWorldDataState.OnEntry(SendClientRequestInitialWorldData);
            m_CoopClientSM.LoadingState.OnEntry(SendGameLoading);
            m_CoopClientSM.PlayingState.OnEntry(SendGameLoaded);
            #endregion


            Init();
        }

        /// <summary>
        ///     Object store shared with the server if connected. Otherwise null.
        /// </summary>
        [CanBeNull]
        public RemoteStore SyncedObjectStore { get; private set; }

        [CanBeNull] public PersistenceClient Persistence { get; private set; }
        
        [NotNull] public SyncBuffered Synchronization { get; }

        [NotNull] public GameSession Session { get; }

        public static CoopClient Instance => m_Instance.Value;
        public CoopEvents Events { get; }

        public bool ClientConnected
        {
            get
            {
                if (Session.Connection == null)
                {
                    return false;
                }

                return Session.Connection.State.Equals(EClientConnectionState.Connected);
            }
        }

        public bool ClientPlaying
        {
            get
            {
                return m_CoopClientSM.State.Equals(ECoopClientState.Playing);
            }
        }

        public RemoteStore GetStore()
        {
            return SyncedObjectStore;
        }

        public RailClientRoom GetRoom()
        {
            return Persistence?.Room;
        }

        public void Update(TimeSpan frameTime)
        {
            m_Updateables.UpdateAll(frameTime);
        }
        public int Priority { get; } = UpdatePriority.MainLoop.Update;

        public string Connect(IPAddress ip, int iPort)
        {
            return m_NetManager.Connect(ip, iPort);
        }

        public void Disconnect()
        {
            m_NetManager.Disconnect(EDisconnectReason.ClientLeft);
        }

        private void Init()
        {
            Session.OnConnectionCreated += ConnectionCreated;
            if (Session.Connection != null)
            {
                ConnectionCreated(Session.Connection);
            }
        }

        private void TryInitPersistence()
        {
            ConnectionClient con = Session.Connection;
            if (con == null || !m_CoopClientSM.State.Equals(ECoopClientState.Playing)) return;

            if (Persistence == null)
            {
                Persistence = new PersistenceClient(new GameEnvironmentClient());
                m_Updateables.Add(Persistence);
                OnPersistenceInitialized?.Invoke(Persistence);
            }

            Persistence.SetConnection(con);
        }

        private void ConnectionCreated(ConnectionClient con)
        {
            if (con == null)
            {
                throw new ArgumentNullException(nameof(con));
            }

            Logger.Info($"Connection was created: {con} / (as server? {Coop.IsServer})");
            Session.Connection.OnConnected += ConnectionEstablished;
        }

        private void ConnectionEstablished(ConnectionClient con)
        {
            Logger.Info($"Establishing the connection: {con} / (as server? {Coop.IsServer})");
            if (m_CoopClientSM.State.Equals(ECoopClientState.MainManu))
            {
                if (Coop.IsServer)
                {
                    m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.CharacterExists);
                }
                else
                {
                    Session.Connection.Dispatcher.RegisterPacketHandler(ReceiveRequireCreateCharacter);
                    Session.Connection.Dispatcher.RegisterPacketHandler(ReceiveCharacterExists);

                    var playerId = new PlatformAPI().GetPlayerID().ToString();
                    Logger.Info($"Transfering my player id to the server: {playerId}");
                    Logger.Info("Requesting a hero on the server");

                    Session.Connection.Send(
                        new Packet(
                            EPacket.Client_RequestParty,
                            new Client_Request_Party(playerId).Serialize()));
                }

                SyncedObjectStore = new RemoteStore(m_SyncedObjects, con, new SerializableFactory());
                RemoteStoreCreated?.Invoke(SyncedObjectStore);

                #region events
                Session.Connection.OnDisconnected += ConnectionClosed;
                #endregion

                // Handler Registration
                Session.Connection.Dispatcher.RegisterPacketHandler(ReceiveInitialWorldData);
                Session.Connection.Dispatcher.RegisterPacketHandler(ReceiveSyncPacket);

                Session.Connection.Dispatcher.RegisterStateMachine(this, m_CoopClientSM);
            }
        }

        private void CreateCharacter()
        {
            Logger.Info("Entering character creation...");
            if (gameManager == null)
            {
                gameManager = new ClientCharacterCreatorManager();
                MBGameManager.StartNewGame(gameManager);

                ClientCharacterCreatorManager.OnGameLoadFinishedEvent += (object source, EventArgs e) =>
                {
                    if(e is HeroEventArgs args)
                    {
                        m_HeroId = args.HeroId;

                        CharacterCreationOver();
                        Logger.Info("Character created:  {} (id {})", args.PartyName, args.HeroId);
                    }
                    else
                    {
                        throw new Exception("EventArgs not of type HeroEventArgs");
                    }
                    
                };
            }
        }

        private void ConnectionClosed(EDisconnectReason eReason)
        {
            Persistence?.SetConnection(null);
            SyncedObjectStore = null;
        }

        private void ConnectionDestroyed(EDisconnectReason eReason)
        {
            switch (eReason)
            {
                case EDisconnectReason.Timeout:
                case EDisconnectReason.Unknown:
                    TryReconnect();
                    break;
            }
        }

        private void TryReconnect()
        {
            if (m_ReconnectAttempts > 0)
            {
                Logger.Info(
                    "Reconnect attempt [{currentAttempt}/{max}].",
                    m_ReconnectAttempts,
                    MaxReconnectAttempts);
                --m_ReconnectAttempts;
                m_NetManager.Reconnect();
            }
        }

        #region MainMenu
        [GameClientPacketHandler(ECoopClientState.MainManu, EPacket.Server_RequireCharacterCreation)]
        private void ReceiveRequireCreateCharacter(ConnectionBase connection, Packet packet)
        {
            Logger.Info("Received notification that character creation is required");
            m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.RequiresCharacterCreation);
        }

        [GameClientPacketHandler(ECoopClientState.MainManu, EPacket.Server_NotifyCharacterExists)]
        private void ReceiveCharacterExists(ConnectionBase connection, Packet packet)
        {
            Logger.Info("Received notification that a character exists ({}B payload)", packet.Length);
            m_HeroGUID = MBGUIDSerializer.Deserialize(new ByteReader(packet.Payload));
            Logger.Info("Character with GUID {} exists", m_HeroGUID);
            //m_Hero = (Hero)MBObjectManager.Instance.GetObject(m_HeroGUID);
            m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.CharacterExists);
        }
        #endregion

        #region ClientCharacterCreation

        public void CharacterCreationOver()
        {
            Logger.Info("Character creation is over");
            GetStore().OnObjectAcknowledged += (id, obj) =>
            {
                Logger.Info("Character hero id is {}", m_HeroId);
                if (id == m_HeroId)
                {
                    m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.CharacterCreated);
                    if (obj is Hero hero)
                    {
                        m_Hero = hero;
                        Logger.Info("Hero assigned: {}", hero.Name);

                    } else
                    {
                        Logger.Warn("Hero not assigned");
                    }
                }
            };
        }
        #endregion

        #region ClientAwaitingWorldData
        private void SendClientRequestInitialWorldData()
        {
            Logger.Info("Requesting/declining (is server? {}) initial world data", Coop.IsServer);
            if(Coop.IsServer)
            {
                Session.Connection.Send(
                new Packet(
                    EPacket.Client_DeclineWorldData,
                    new Client_DeclineWorldData().Serialize()));
                m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.WorldDataReceived);
                m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.GameLoaded);
            }
            else
            {
                Session.Connection.Send(
                new Packet(
                    EPacket.Client_RequestWorldData,
                    new Client_RequestWorldData().Serialize()));
            }
            
        }

        [GameClientPacketHandler(ECoopClientState.ReceivingWorldData, EPacket.Server_WorldData)]
        private void ReceiveInitialWorldData(ConnectionBase connection, Packet packet)
        {
            Logger.Info("Received initial world data packet: {packet_size}B", packet.Length);
            bool bSuccess = false;
            try
            {
                bSuccess = Session.World.Receive(packet.Payload);
            }
            catch (Exception e)
            {
                Logger.Error(
                    e,
                    "World data received from server could not be parsed . Disconnect {client}.",
                    this);
            }

            if (bSuccess)
            {
                Logger.Info("Hero GUID: {hero_guid}", m_HeroGUID);

                m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.WorldDataReceived);

                if(m_HeroGUID == new MBGUID(0))
                {
                    // Is m_Hero supposed to be null here? - Columbus
                    gameManager = new ClientManager(((GameData)Session.World).LoadResult, m_Hero);
                }
                else
                {
                    gameManager = new ClientManager(((GameData)Session.World).LoadResult, m_HeroGUID);
                }

                Logger.Info("Starting new game...");
                MBGameManager.StartNewGame(gameManager);
                Logger.Info("Started new game");

                ClientManager.OnPreLoadFinishedEvent += (source, e) => {
                    CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, SendPlayerPartyChanged);
                };

                ClientManager.OnPostLoadFinishedEvent += (source, e) => {
                    m_CoopClientSM.StateMachine.Fire(ECoopClientTrigger.GameLoaded);
                };

                Logger.Info("Initial world load complete.");
            }
            else
            {
                Logger.Error(
                    "World data received from server could not be parsed. Disconnect {client}.",
                    this);
                Session.Connection.Disconnect(EDisconnectReason.WorldDataTransferIssue);
            }
        }
        #endregion

        #region  ClientLoading
        private void SendGameLoading()
        {
            // TODO add loading and loaded messages
            //Session.Connection.Send(
            //    new Packet(
            //        EPacket.Client_Joined,
            //        new Client_GameLoading().Serialize()));
        }
        #endregion

        #region ClientPlaying
        public void SendGameLoaded()
        {
            Session.Connection.Send(
                new Packet(
                    EPacket.Client_Loaded,
                    new Client_Joined().Serialize()));
            TryInitPersistence();
        }

        private void SendPlayerPartyChanged(Hero hero, MobileParty party)
        {
            Session.Connection.Send(
                new Packet(
                    EPacket.Client_PartyChanged,
                    new MBGUIDSerializer(party.Id).Serialize()));
        }


        [GameClientPacketHandler(ECoopClientState.Playing, EPacket.Sync)]
        private void ReceiveSyncPacket(ConnectionBase connection, Packet packet)
        {
            try
            {
                Session.World.Receive(packet.Payload);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Sync data received from server could not be parsed. Ignored.");
            }
        }
        #endregion

        public override string ToString()
        {
            if (Session.Connection == null)
            {
                return "Client not connected.";
            }

            string sLeadingWhitespace = "       ";
            string sRet =
                $"{Session.Connection.Latency,-5}{Session.Connection.State,-30}{Session.Connection.Network}";
            sRet += Environment.NewLine + sLeadingWhitespace;
            if (Persistence != null)
            {
                IEnumerable<RailEntityBase> controlledEntity = Persistence.Room.LocalEntities;
                sRet += $"Controlling {controlledEntity.Count()} entities.";
                foreach (RailEntityBase entity in controlledEntity)
                {
                    sRet += Environment.NewLine + sLeadingWhitespace + entity;
                }
            }

            return sRet;
        }
    }

    
}
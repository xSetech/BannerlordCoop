using System;
using System.Linq;
using Coop.Mod.Patch;
using Coop.Mod.Persistence;
using Coop.Mod.Persistence.Party;
using Coop.Mod.Persistence.RPC;
using NLog;
using Sync;
using Sync.Store;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod
{

    public interface IGameEnvironmentServer : IEnvironmentServer
    {
        new FieldAccessGroup<MobileParty, MovementData> TargetPosition { get; }

        new bool CanChangeTimeControlMode { get; }

        new EventBroadcastingQueue EventQueue { get; }

        new MobileParty GetMobilePartyByIndex(int iPartyIndex);

        new SharedRemoteStore Store { get; }

        void LockTimeControlStopped();

        void UnlockTimeControl();
    }

    public class GameEnvironmentServer : IGameEnvironmentServer
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public FieldAccessGroup<MobileParty, MovementData> TargetPosition =>
            CampaignMapMovement.Movement;

        public bool CanChangeTimeControlMode => CoopServer.AreAllClientsPlaying;

        public EventBroadcastingQueue EventQueue => CoopServer.Persistence?.EventQueue;

        private readonly ICoopServer CoopServer;

        private readonly IGameLoopRunner GameLoopRunner;

        public GameEnvironmentServer(ICoopServer coopServer, IGameLoopRunner gameLoopRunner)
        {
            CoopServer = coopServer;
            GameLoopRunner = gameLoopRunner;
        }

        public MobileParty GetMobilePartyByIndex(int iPartyIndex)
        {
            MobileParty ret = null;
            GameLoopRunner.RunOnMainThread(
                () =>
                {
                    ret = MobileParty.All.SingleOrDefault(p => p.Party.Index == iPartyIndex);
                });
            return ret;
        }

        public SharedRemoteStore Store =>
            CoopServer.SyncedObjectStore ??
            throw new InvalidOperationException("Client not initialized.");

        public void LockTimeControlStopped()
        {
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            Campaign.Current.SetTimeControlModeLock(true);
        }

        public void UnlockTimeControl()
        {
            Campaign.Current.SetTimeControlModeLock(false);
        }
    }
}

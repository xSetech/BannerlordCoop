using Coop.Mod.Repository;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod
{

    public interface ICoop
    {
        bool IsServer { get; }
        bool IsClientConnected { get; }
        bool IsClientPlaying { get; }

        /// <summary>
        ///     The arbiter is the game instance with authority over all clients.
        /// </summary>
        bool IsArbiter { get; }

        bool DoSync();

        /// <summary>
        ///     Returns whether changes to an object should be synchronized.
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        bool DoSync(object instance);

        bool DoSync(MobileParty party);
    }

    public class Coop : ICoop
    {

        private readonly ICoopServer CoopServer;
        private readonly ICoopClient CoopClient;

        public Coop(
            ICoopServer coopServer,
            ICoopClient coopClient)
        {
            CoopServer = coopServer;
            CoopClient = coopClient;
        }

        public bool IsServer => CoopServer.Current != null;
        public bool IsClientConnected => CoopClient.ClientConnected;
        public bool IsClientPlaying => CoopClient.ClientPlaying;

        public bool IsArbiter =>
            IsServer && IsClientConnected; // The server currently runs in the hosts game session.

        public bool DoSync()
        {
            return IsClientConnected || IsServer;
        }

        public bool DoSync(object instance)
        {
            if (instance is MobileParty party)
            {
                return DoSync(party);
            }

            return IsArbiter;
        }

        public bool DoSync(MobileParty party)
        {
            bool isPlayerController = CoopClient.GameState.IsPlayerControlledParty(party);
            if (isPlayerController && party == MobileParty.MainParty)
            {
                return true;
            }

            if (IsArbiter && !isPlayerController)
            {
                return true;
            }

            return false;
        }
    }
}

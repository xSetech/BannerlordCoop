using System;
using System.Collections.Generic;
using System.Net;
using Coop.Mod.Persistence;
using Coop.Mod.Repository;
using Network.Infrastructure;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Coop.Mod.DebugUtil
{
    public interface ICLICommands
    {
        string DumpInfo(List<string> parameters);
        string ShowDebugUi(List<string> parameters);
        string StartServer(List<string> parameters);
        string ConnectTo(List<string> parameters);
        string Disconnect(List<string> parameters);
        string Help(List<string> parameters);
        string Record(List<string> parameters);
        string Play(List<string> parameters);
        string Stop(List<string> parameters);
        string DisableWarn(List<string> parameters);
        string Spawn(List<string> parameters);


    }

    public class CLICommands : ICLICommands
    {
        private const string sGroupName = "coop";
        private const string sTestGroupName = "test";

        private readonly IDebugUI DebugUI;

        private readonly IUpdateableRepository UpdateableRepository;
        private readonly ICoopClient CoopClient;
        private readonly ICoopServer CoopServer;
        private readonly IReplay Replay;

        public CLICommands(
            IUpdateableRepository updateableRepository,
            ICoopClient coopClient,
            ICoopServer coopServer,
            IDebugUI debugUI,
            IReplay replay)
        {
            UpdateableRepository = updateableRepository;
            CoopClient = coopClient;
            CoopServer = coopServer;
            DebugUI = debugUI;
            Replay = replay;
            UpdateableRepository.Add(DebugUI);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("info", sGroupName)]
        public string DumpInfo(List<string> parameters)
        {
            string sMessage = "";
            sMessage += CoopServer + Environment.NewLine;
            sMessage += Environment.NewLine + "*** Client ***" + Environment.NewLine;
            sMessage += CoopClient + Environment.NewLine;
            return sMessage;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("show_debug_ui", sGroupName)]
        public string ShowDebugUi(List<string> parameters)
        {

            DebugUI.Visible = true;
            return "";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("start_local_server", sGroupName)]
        public string StartServer(List<string> parameters)
        {
            if (CoopServer.StartServer() == null)
            {
                ServerConfiguration config = CoopServer.Current.ActiveConfig;
                CoopClient.Connect(config.NetworkConfiguration.LanAddress, config.NetworkConfiguration.LanPort);
                return CoopServer.ToString();
            }

            return null;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("connect_to", sGroupName)]
        public string ConnectTo(List<string> parameters)
        {
            if (parameters.Count != 2 ||
                !IPAddress.TryParse(parameters[0], out IPAddress ip) ||
                !int.TryParse(parameters[1], out int iPort))
            {
                return $"Usage: \"{sGroupName}.connect_to [IP] [Port]\"." +
                       Environment.NewLine +
                       $"\tExample: \"{sGroupName}.connect_to 127.0.0.1 4201\".";
            }

            CoopClient.Connect(ip, iPort);
            return "Client connection request sent.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("disconnect", sGroupName)]
        public string Disconnect(List<string> parameters)
        {
            CoopClient.Disconnect();
            return "Client disconnection request sent.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("help", sGroupName)]
        public string Help(List<string> parameters)
        {
            return "Coop commands:\n" +
                   "\tcoop.record <filename>\tStart record movements of all parties.\n" +
                   "\tcoop.play <filename>\tPlayback recorded movements of main hero party.\n" +
                   "\tcoop.stop\t\tStop record or playback.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("record", sGroupName)]
        public string Record(List<string> parameters)
        {
            if (parameters.Count < 1)
            {
                return Help(null);
            }

            return Replay.StartRecord(parameters[0]);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("play", sGroupName)]
        public string Play(List<string> parameters)
        {
            if (parameters.Count < 1)
            {
                return Help(null);
            }

            return Replay.Playback(parameters[0]);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("stop", sGroupName)]
        public string Stop(List<string> parameters)
        {
            return Replay.Stop();
        }

        [CommandLineFunctionality.CommandLineArgumentFunction(
            "disable_inconsistent_state_warnings",
            sGroupName)]
        public string DisableWarn(List<string> parameters)
        {
            string help =
                "Disable(1) or enable(0) to show warnings about inconsistent internal state\n" +
                "Usage:\n" +
                $"\t{sGroupName}.disable_inconsistent_state_warnings 1";
            if (parameters.Count < 1)
            {
                return help;
            }

            EntityManager entityManager = CoopServer.Persistence?.EntityManager;
            if (entityManager == null)
            {
                return "Server not started.";
            }

            if (parameters[0] == "1")
            {
                entityManager.SuppressInconsistentStateWarnings = true;
                return "Inconsistent state warnings disabled.";
            }

            if (parameters[0] == "0")
            {
                entityManager.SuppressInconsistentStateWarnings = false;
                return "Inconsistent state warnings enabled.";
            }

            return help;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_party", sTestGroupName)]
        public string Spawn(List<string> parameters)
        {
            MobileParty party = PartySpawnHelper.SpawnTestersNear(Campaign.Current.MainParty);
            return $"Spawned {party}.";
        }
    }
}

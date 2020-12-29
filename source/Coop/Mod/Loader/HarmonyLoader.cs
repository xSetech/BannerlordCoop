using Common;
using Coop.Lib.NoHarmony;
using Coop.Mod.Behaviour;
using Coop.Mod.DebugUtil;
using Coop.Mod.Patch;
using Coop.Mod.Repository;
using Coop.Mod.UI;
using HarmonyLib;
using Network.Infrastructure;
using NLog;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Coop.Mod.Loader
{

    public interface IHarmonyLoader
    {
        void NoHarmonyInit();
        void NoHarmonyLoad();

    }

    public class HarmonyLoader : NoHarmonyLoader, IHarmonyLoader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static readonly bool DEBUG = true;
        
        private bool m_IsFirstTick = true;

        private readonly ICoopServer CoopServer;
        private readonly ICoopClient CoopClient;
        private readonly ICLICommands CLICommands;
        private readonly IUpdateableRepository updateableRepository;
        private readonly ICoopConnectionUI CoopConnectionUI;

        public HarmonyLoader(
            IUpdateableRepository updateableRepository,
            ICoopClient coopClient,
            IGameLoopRunner gameLoopRunner,
            ICoopServer coopServer,
            ICLICommands cliCommands,
            ICoopConnectionUI coopConnectionUI)
        {
            this.updateableRepository = updateableRepository;
            CoopServer = coopServer;
            CLICommands = cliCommands;
            CoopClient = coopClient;
            CoopConnectionUI = coopConnectionUI;
            updateableRepository.Add(coopClient);
            updateableRepository.Add(gameLoopRunner);
        }

        public override void NoHarmonyInit()
        {
            initLogger();
        }

        public override void NoHarmonyLoad()
        {
            AddBehavior<InitServerBehaviour>();
            AddBehavior<GameLoadedBehaviour>();

            Harmony harmony = new Harmony("com.TaleWorlds.MountAndBlade.Bannerlord");
            IEnumerable<MethodInfo> patchInitializers =
                from t in Assembly.GetExecutingAssembly().GetTypes()
                from m in t.GetMethods()
                where m.IsDefined(typeof(PatchInitializerAttribute))
                select m;
            foreach (MethodInfo initializer in patchInitializers)
            {
                if (!initializer.IsStatic)
                {
                    throw new Exception("Invalid [PatchInitializer]. Has to be static.");
                }

                Logger.Info("Init patch {}", initializer.DeclaringType);
                initializer.Invoke(null, null);
            }

            // Skip startup splash screen
            if (DEBUG)
            {
                typeof(Module).GetField(
                                  "_splashScreenPlayed",
                                  BindingFlags.Instance | BindingFlags.NonPublic)
                              .SetValue(Module.CurrentModule, true);
            }

            // Apply all patches via harmony
            harmony.PatchAll();

            Module.CurrentModule.AddInitialStateOption(
                new InitialStateOption(
                    "CoOp Campaign",
                    new TextObject("Host Co-op Campaign"),
                    9990,
                    () =>
                    {
                        string[] array = Utilities.GetFullCommandLineString().Split(' ');

                        if (DEBUG)
                        {
                            foreach (string argument in array)
                            {
                                if (argument.ToLower() == "/server")
                                {
                                    //TODO add name to args
                                    CoopServer.StartGame("MP");
                                }
                                else if (argument.ToLower() == "/client")
                                {
                                    ServerConfiguration defaultConfiguration =
                                        new ServerConfiguration();
                                    CoopClient.Connect(
                                        defaultConfiguration.NetworkConfiguration.LanAddress,
                                        defaultConfiguration.NetworkConfiguration.LanPort
                                    );
                                }
                            }
                        }
                        else
                        {
                            InformationManager.DisplayMessage(
                                new InformationMessage("Hello World!"));
                            ScreenManager.PushScreen(
                                ViewCreatorManager.CreateScreenView<CoopLoadScreen>(
                                    new object[] { }));
                        }
                    },
                    false));

            Module.CurrentModule.AddInitialStateOption(new InitialStateOption(
              "Join Coop Game",
              new TextObject("Join Co-op Campaign"),
              9991,
              JoinWindow,
              false
            ));

        }

        protected override void OnSubModuleUnloaded()
        {
            CoopServer.ShutDownServer();
            base.OnSubModuleUnloaded();
        }

        protected override void OnApplicationTick(float dt)
        {
            if (m_IsFirstTick)
            {
                GameLoopRunner.Instance.SetGameLoopThread();
                m_IsFirstTick = false;
            }

            base.OnApplicationTick(dt);
            if (Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyDown(InputKey.Tilde))
            {
                CLICommands.ShowDebugUi(new List<string>());
                // DebugConsole.Toggle();
            }

            updateableRepository.UpdateAll(TimeSpan.FromSeconds(dt));
        }

        private void initLogger()
        {
            // NoHarmony
            Logging = true;

            // NLog
            Target.Register<MbLogTarget>("MbLog");
            Mod.Logging.Init(
                new Target[]
                {
                    new MbLogTarget
                    {
                        Layout = Layout.FromString("[${level:uppercase=true}] ${message}")
                    }
                });
        }

        internal void JoinWindow()
        {
            ScreenManager.PushScreen(ViewCreatorManager.CreateScreenView<CoopConnectionUI>(CoopConnectionUI));
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Coop.Mod.Config;
using Coop.Mod.DebugUtil;
using Coop.Mod.Loader;
using Coop.Mod.Persistence;
using Coop.Mod.Persistence.Party;
using Coop.Mod.Repository;
using Coop.Mod.UI;
using JetBrains.Annotations;

namespace Coop.Mod.Binding
{
    public class ContainerGenerator
    {

        public static IContainer Container { get; private set; }

        public static IContainer Generate()
        {
            var builder = new ContainerBuilder();

            RegisterTypes(builder);
            
            Container = builder.Build();

            return Container;
        }

        private static ICoopClient _CoopClient;
        
        [CanBeNull]
        public static ICoopClient CoopClient {
            get {
                if (_CoopClient == null)
                {
                    using (var scope = ContainerGenerator.Container.BeginLifetimeScope())
                    {
                        _CoopClient = scope.Resolve<ICoopClient>();
                    }
                }
                return _CoopClient;
            }
            private set
            {
                _CoopClient = value;
            }
        }

        private static ICoop _Coop;

        [CanBeNull]
        public static ICoop Coop
        {
            get
            {
                if (_Coop == null)
                {
                    using (var scope = ContainerGenerator.Container.BeginLifetimeScope())
                    {
                        _Coop = scope.Resolve<ICoop>();
                    }
                }
                return _Coop;
            }
            private set
            {
                _Coop = value;
            }
        }

        private static ICoopServer _CoopServer;

        [CanBeNull]
        public static ICoopServer CoopServer
        {
            get
            {
                if (_CoopServer == null)
                {
                    using (var scope = ContainerGenerator.Container.BeginLifetimeScope())
                    {
                        _CoopServer = scope.Resolve<ICoopServer>();
                    }
                }
                return _CoopServer;
            }
            private set
            {
                _CoopServer = value;
            }
        }

        private static void RegisterTypes(ContainerBuilder builder)
        {

            builder.RegisterType<CoopClient>().As<ICoopClient>();
            builder.RegisterType<GameLoopRunner>().As<IGameLoopRunner>();
            builder.RegisterType<ClientConfigurationCreator>().As<IClientConfigurationCreator>();
            builder.RegisterType<HarmonyLoader>().As<IHarmonyLoader>();
            builder.RegisterType<UpdateableRepository>().As<IUpdateableRepository>();
            builder.RegisterType<CLICommands>().As<ICLICommands>();
            builder.RegisterType<CoopServer>().As<ICoopServer>();
            builder.RegisterType<Replay>().As<IReplay>();
            builder.RegisterType<GameEnvironmentServer>().As<IGameEnvironmentServer>();
            builder.RegisterType<CoopConnectMenuVM>().As<ICoopConnectMenuVM>();
            builder.RegisterType<CoopConnectionUI>().As<ICoopConnectionUI>();
            builder.RegisterType<CoopLoadGameGauntletScreen>().As<ICoopLoadGameGauntletScreen>();
            builder.RegisterType<MobilePartyEntityClient>().As<IMobilePartyEntityClient>(); 

        }

    }
}

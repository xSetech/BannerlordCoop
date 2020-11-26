using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace Coop.Mod.Binding
{
    public class ContainerGenerator
    {

        private static IContainer Container { get; set; }

        public static IContainer Generate()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<CoopClient>().As<ICoopClient>();
            builder.RegisterType<GameLoopRunner>().As<IGameLoopRunner>();
            Container = builder.Build();
            return Container;
        }

    }
}

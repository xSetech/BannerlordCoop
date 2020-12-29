using Network.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coop.Mod.Config
{
    public interface IClientConfigurationCreator
    {
        ClientConfiguration Create();
    }

    public class ClientConfigurationCreator : IClientConfigurationCreator
    {
        public ClientConfiguration Create()
        {
            return new ClientConfiguration();
        }
    }
}

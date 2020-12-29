using System.Reflection;
using Coop.Mod.Binding;
using Coop.Mod.DebugUtil;
using Coop.Mod.Serializers;
using Network.Infrastructure;
using SandBox;
using SandBox.View.Map;
using Sync.Store;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem.Load;

namespace Coop.Mod.Managers
{
    public class ServerGameManager : CampaignGameManager
    {
        public ServerGameManager() : base() { }
        public ServerGameManager(LoadResult saveGameData) : base(saveGameData) { }

        ~ServerGameManager()
        {
            // TODO save all heros
        }

        public override void OnLoadFinished()
        {
            base.OnLoadFinished();
            if (ContainerGenerator.CoopServer.StartServer() == null)
            {
                ServerConfiguration config = ContainerGenerator.CoopServer.Current.ActiveConfig;
                ContainerGenerator.CoopClient.Connect(config.NetworkConfiguration.LanAddress, config.NetworkConfiguration.LanPort);
            }

            ContainerGenerator.CoopClient.RemoteStoreCreated += (remoteStore) => {
                remoteStore.OnObjectReceived += (objId, obj) =>
                {
                    if (obj is PlayerHeroSerializer serializedPlayerHero)
                    {
                        Hero hero = (Hero)serializedPlayerHero.Deserialize();
                    }

                };
            };
        }
    }
}

using Coop.Mod.Binding;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod.Behaviour
{
    public class GameLoadedBehaviour : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, GameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private static void GameLoaded(CampaignGameStarter gameStarter)
        {
            ContainerGenerator.CoopClient.Events.OnGameLoaded.Invoke();

            ContainerGenerator.CoopClient.GameState.AddPlayerControllerParty(MobileParty.MainParty);
            ContainerGenerator.CoopClient.Events.OnBeforePlayerPartySpawned.Invoke(MobileParty.MainParty);
        }
    }
}

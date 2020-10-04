using Sync;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod.Patch
{
    public static class Encounters
    {
        // 
        private static readonly MethodPatch patch = 
                new MethodPatch(typeof(Campaign))
                    .Intercept(nameof(Campaign.HandleSettlementEncounter), EMethodPatchFlag.DebounceCalls)
                    .Intercept(nameof(Campaign.HandlePartyEncounterEvent), EMethodPatchFlag.DebounceCalls)
            ;

        [PatchInitializer]
        public static void Init()
        {
            CoopClient.Instance.OnPersistenceInitialized += persistence =>
            {
                persistence.RpcSyncHandlers.Register(patch.Methods, CoopClient.Instance);
            };
        }
    }
}

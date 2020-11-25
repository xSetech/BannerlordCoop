using System;
using System.Linq;
using System.Reflection;
using Coop.Mod.Events;
using Sync;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod.Patch
{
    /// <summary>
    ///     Patch to replace the <see cref="TaleWorlds.CampaignSystem.Campaign.CampaignEventDispatcher"/>
    ///     with our wrapper <see cref="SyncedCampaignEventDispatcher"/>.
    /// </summary>
    public static class CampaignEventDispatcherPatch
    {
        private static readonly MethodPatch DispatcherPatch =
            new MethodPatch(typeof(CampaignEventDispatcher))
                .Intercept(nameof(CampaignEventDispatcher.OnSettlementEntered), EMethodPatchFlag.None, EPatchBehaviour.CallOriginalBaseOnDispatcherReturn)
                .Intercept(nameof(CampaignEventDispatcher.OnAfterSettlementEntered), EMethodPatchFlag.None, EPatchBehaviour.CallOriginalBaseOnDispatcherReturn);
        
        [PatchInitializer]
        public static void Init()
        {
            foreach (MethodAccess method in DispatcherPatch.Methods)
            {
                method.Condition = instance => SyncedCampaignEvents.ShouldBeSynchronized((CampaignEventDispatcher) instance, method.MemberInfo);
            }
            
            CoopClient.Instance.OnPersistenceInitialized += persistence => persistence.RpcSyncHandlers.Register(DispatcherPatch.Methods, CoopClient.Instance);
        }
    }
}

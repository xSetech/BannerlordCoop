using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod.Events
{
    public static class SyncedCampaignEvents
    {
        /// <summary>
        ///     Called to evaluate if a event dispatcher call should be distributed to all other
        ///     players.
        /// </summary>
        /// <param name="instance">Local instance of the dispatcher that the call was made on</param>
        /// <param name="method">Method called on instance</param>
        /// <returns>Should the call be synchronized?</returns>
        public static bool ShouldBeSynchronized(CampaignEventDispatcher instance, MethodInfo method)
        {
            if (!Coop.DoSync())
            {
                return false;
            }

            bool bIsHost = Coop.IsServer || Coop.IsArbiter;
            if (method.Name == nameof(CampaignEventDispatcher.OnSettlementEntered) ||
                method.Name == nameof(CampaignEventDispatcher.OnAfterSettlementEntered))
            {
                return bIsHost;
            }

            return false;
        }
    }
}

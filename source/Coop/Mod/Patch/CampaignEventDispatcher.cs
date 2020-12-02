using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coop.Mod.Events;
using HarmonyLib;
using Sync;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.Core;

namespace Coop.Mod.Patch
{
    /// <summary>
    ///     
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
        
        /// <summary>
        ///     IssuesCampaignBehavior.OnSettlementEntered inlines a call to CharacterObject.IsPlayerCharacter which makes
        ///     it impossible to patch the method. Since the unpatchable call can lead to a nullptr access for remote player
        ///     characters, we override the whole method.
        /// </summary>
        [HarmonyPatch(typeof(IssuesCampaignBehavior))]
        [HarmonyPatch("OnSettlementEntered")]
        class IssuesCampaignBehaviorPatch
        {
            static bool Prefix(IssuesCampaignBehavior __instance, ref MobileParty party, ref Settlement settlement, ref Hero hero)
            {
                if (!Coop.DoSync())
                {
                    return true;
                }

                if (party == null && hero == null)
                {
                    // Workaround: we are currently not synchronizing the spawn of new parties. So it might happen that a party is
                    // missing on a client. For now, we just ignore that and abort.
                    return false;
                }
                
                CharacterObject characterObject = party == null ? hero.CharacterObject : party.Leader;
                bool isPlayerCharacter = CoopClient.Instance.GameState.IsPlayerControlled(characterObject);
                // For AI characters the original works fine. For player characters mimic behavior as of Bannerlord 1.5.4 and do nothing.
                return !isPlayerCharacter;
            }
        }
    }
}

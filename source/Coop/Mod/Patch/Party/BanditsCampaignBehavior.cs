using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace Coop.Mod.Patch.Party
{
    /// <summary>
    /// Upon initial connection, we want to ensure that all existing bandits are immediately synced
    /// </summary>
    [HarmonyPatch(typeof(BanditsCampaignBehavior), "InitBanditParty")]
    internal class BanditsCampaignBehaviorPatch
    {
        public static EventHandler<MobileParty> OnBanditAdded;

        private static bool Prefix(BanditsCampaignBehavior __instance, ref MobileParty banditParty, ref Clan faction,
            ref Settlement homeSettlement)
        {
            if (!Coop.IsServer)
                return false;

            OnBanditAdded?.Invoke(__instance, banditParty);
            return true;
        }
    }

    /// <summary>
    /// Clients shouldn't spawn any bandits. The server will handle the bandits spawning in the Postfix
    /// </summary>
    [HarmonyPatch(typeof(BanditPartyComponent), "CreateBanditParty")]
    internal class BanditsCampaignSpawnPatch
    {
        private static bool Prefix()
        {
            return Coop.IsServer;
        }

        private static void Postfix(ref MobileParty __result)
        {
            if (!Coop.IsServer)
                return;

            CoopServer.Instance.SyncedObjectStore.Insert(__result);
            CoopServer.Instance.Persistence.MobilePartyEntityManager.AddToPendingParties(__result);
            
            __result.Party.Visuals.SetMapIconAsDirty();
        }
    }

    [HarmonyPatch(typeof(BanditsCampaignBehavior), "FillANewHideoutWithBandits")]
    internal class BanditsHideoutSpawnPatch
    {
        private static bool Prefix()
        {
            return Coop.IsServer;
        }
    }

    [HarmonyPatch(typeof(BanditsCampaignBehavior), "AddBossParty")]
    internal class BanditsCampaignAddBossPartyPatch
    {
        private static bool Prefix()
        {
            return Coop.IsServer;
        }
    }

    [HarmonyPatch(typeof(BanditsCampaignBehavior), "SpawnAPartyInFaction")]
    internal class BanditsCampaignSpawnAPartyInFactionPatch
    {
        private static bool Prefix()
        {
            return Coop.IsServer;
        }
    }
}
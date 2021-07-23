using CoopFramework;
using JetBrains.Annotations;
using Sync.Behaviour;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace Coop.Mod.Patch.World
{
    public class MobilePartySync : CoopManaged<MobilePartySync, MobileParty>
    {
        public MobilePartySync([NotNull] MobileParty instance) : base(instance) {}

        static MobilePartySync()
        {
            When(GameLoop)
                .Calls(Method("InitializeMobileParty"))
                .Broadcast(() => CoopClient.Instance.Synchronization)
                .DelegateTo(IsServer);

            AutoWrapAllInstances(c => new MobilePartySync(c));
        }
    }
    //public class BanditSync : CoopManaged<BanditSync, BanditsCampaignBehavior>
    //{
    //    public BanditSync([NotNull] BanditsCampaignBehavior instance) : base(instance)
    //    {
    //    }

    //    static BanditSync()
    //    {
    //        When(GameLoop)
    //            .Calls(Method("SpawnAPartyInFaction"))
    //            .Broadcast(() => CoopClient.Instance.Synchronization)
    //            .DelegateTo(IsServer);

    //        AutoWrapAllInstances(c => new BanditSync(c));
    //    }

    //    static ECallPropagation IsServer(IPendingMethodCall call)
    //    {
    //        return Coop.IsServer ? ECallPropagation.CallOriginal : ECallPropagation.Skip;
    //    }
    //}
}
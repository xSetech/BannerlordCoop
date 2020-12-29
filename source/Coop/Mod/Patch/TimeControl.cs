using System;
using System.Linq;
using Autofac;
using Coop.Mod.Binding;
using Coop.Mod.Persistence;
using HarmonyLib;
using Mono.Reflection;
using Sync;
using TaleWorlds.CampaignSystem;

namespace Coop.Mod.Patch
{
    public class TimeControl
    {
        private static readonly PropertyPatch TimeControlPatch =
            new PropertyPatch(typeof(Campaign)).InterceptSetter(nameof(Campaign.TimeControlMode));

        private static readonly PropertyPatch TimeControlLockPatch =
            new PropertyPatch(typeof(Campaign)).InterceptSetter(
                nameof(Campaign.TimeControlModeLock));

        private static readonly PropertyPatch IsMainPartyWaitingPatch =
            new PropertyPatch(typeof(Campaign), EPatchBehaviour.NeverCallOriginal).InterceptSetter(
                nameof(Campaign.IsMainPartyWaiting));

        public static FieldAccess<Campaign, CampaignTimeControlMode> TimeControlMode { get; } =
            new FieldAccess<Campaign, CampaignTimeControlMode>(
                AccessTools.Property(typeof(Campaign), nameof(Campaign.TimeControlMode))
                           .GetBackingField());

        public static FieldAccess<Campaign, bool> TimeControlModeLock { get; } =
            new FieldAccess<Campaign, bool>(
                AccessTools.Property(typeof(Campaign), nameof(Campaign.TimeControlModeLock))
                           .GetBackingField());

        private static ICoop CoopInstance;

        [PatchInitializer]
        public static void Init()
        {

            if (CoopInstance == null)
            {
                using (var scope = ContainerGenerator.Container.BeginLifetimeScope())
                {
                    CoopInstance = scope.Resolve<ICoop>();
                }
            }

            if (CoopInstance == null)
            {
                return;
            }

            FieldChangeBuffer.Intercept(TimeControlMode, TimeControlPatch.Setters, CoopInstance.DoSync);
            FieldChangeBuffer.Intercept(
                TimeControlModeLock,
                TimeControlLockPatch.Setters,
                CoopInstance.DoSync);

            MethodAccess mainPartyWaitingSetter = IsMainPartyWaitingPatch.Setters.First();
            mainPartyWaitingSetter.Condition = o => CoopInstance.DoSync();
            mainPartyWaitingSetter.SetGlobalHandler(SetIsMainPartyWaiting);
        }

        private static void SetIsMainPartyWaiting(object instance, object value)
        {


            if (ContainerGenerator.CoopClient == null)
            {
                return;
            }

            IEnvironmentClient env = ContainerGenerator.CoopClient.Persistence?.Environment;

            if (env == null) return;
            if (!(value is object[] args)) throw new ArgumentException();
            if (!(args[0] is bool isLocalMainPartyWaiting)) throw new ArgumentException();
            if (!(instance is Campaign campaign)) throw new ArgumentException();

            bool isEveryMainPartyWaiting = isLocalMainPartyWaiting;
            foreach (MobileParty party in env.PlayerControlledParties)
            {
                isEveryMainPartyWaiting = isEveryMainPartyWaiting && party.ComputeIsWaiting();
            }

            IsMainPartyWaitingPatch
                .Setters.First()
                .CallOriginal(instance, new object[] {isEveryMainPartyWaiting});
        }
    }
}

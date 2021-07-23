using CoopFramework;
using JetBrains.Annotations;
using Sync.Behaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace Coop.Mod.GameSync
{
    class MobilePartySync : CoopManaged<MobilePartySync, MobileParty>
    {
        static MobilePartySync()
        {
            When(GameLoop)
                .Calls(Method(typeof(MobileParty)
                    .GetMethod(
                        nameof(MobileParty.InitializeMobileParty), 
                        new Type[] // Second param is to specify which overload we're using of InitializeMobileParty
                        { 
                            typeof(PartyTemplateObject), 
                            typeof(Vec2), 
                            typeof(float), 
                            typeof(float), 
                            typeof(int) 
                        })
                    )
                )
                .Broadcast(() => CoopClient.Instance.Synchronization)
                .DelegateTo(IsServer);

            ApplyStaticPatches();
            AutoWrapAllInstances(c => new MobilePartySync(c));
        }

        public MobilePartySync([NotNull] MobileParty instance) : base(instance)
        {
        }

        private static ECallPropagation IsServer(IPendingMethodCall call)
        {
            return Coop.IsServer ? ECallPropagation.CallOriginal : ECallPropagation.Skip;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Coop.Mod
{
    public class CoopGameState
    {
        private readonly HashSet<MobileParty> m_PlayerParties;

        public CoopGameState()
        {
            m_PlayerParties = new HashSet<MobileParty>();
        }

        public bool IsGameLoaded => Campaign.Current != null;

        public void AddPlayerControllerParty(MobileParty party)
        {
            if (party == null)
            {
                throw new ArgumentNullException();
            }

            m_PlayerParties.Add(party);
        }

        public bool IsPlayerControlledParty(MobileParty party)
        {
            return m_PlayerParties.Contains(party);
        }
        
        public bool IsPlayerControlled(BasicCharacterObject character)
        {
            return !m_PlayerParties.Where(party => party.Leader == character).IsEmpty();
        }
    }
}

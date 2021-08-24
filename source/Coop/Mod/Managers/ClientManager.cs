using System;
using System.Linq;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem.Load;
using System.Reflection;
using TaleWorlds.CampaignSystem.Actions;
using JetBrains.Annotations;
using TaleWorlds.ObjectSystem;
using NLog;

namespace Coop.Mod.Managers
{
    public class ClientManager : CampaignGameManager
    {
        /// <summary>
        /// The clients hero as it was sent to the server. Note that the server may change some fields when introducing the hero to the campaign.
        /// </summary>
        [CanBeNull] private readonly Hero m_PlayerAsSerialized;
        [CanBeNull] private readonly MBGUID m_HeroGUID;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The clients hero as it exists in the server side campaign.
        /// </summary>
        [CanBeNull] Hero m_PlayerInCampaign;
        public ClientManager(LoadResult saveGameData, Hero playerAsSerialized) : base(saveGameData) 
        { 
            m_PlayerAsSerialized = playerAsSerialized;
        }

        public ClientManager(LoadResult saveGameData,  MBGUID heroGUID) : base(saveGameData)
        {
            m_HeroGUID = heroGUID;
        }

        public static event EventHandler OnPreLoadFinishedEvent;
        public static event EventHandler OnPostLoadFinishedEvent;
        public override void OnLoadFinished()
        {
            Logger.Info("Client load finished");
            OnPreLoadFinishedEvent?.Invoke(this, EventArgs.Empty);
            base.OnLoadFinished();
            OnPostLoadFinishedEvent?.Invoke(this, EventArgs.Empty);
            Logger.Info("Post client-load finished");
            try
            {

                if (m_PlayerAsSerialized != null)
                {
                    Logger.Info("No player for the client. Searching for the client's player party...");
                    Logger.Info("... player as serialized: {} of clan {}", m_PlayerAsSerialized.Name, m_PlayerAsSerialized.Clan);
                    MobileParty playerParty = MobileParty.All.AsParallel().SingleOrDefault(IsClientPlayersParty);

                    Logger.Info("Client's party: {} lead by {}", playerParty?.Name, playerParty?.LeaderHero?.Name);
                    m_PlayerInCampaign = playerParty.LeaderHero;

                    // Start player at training field
                    Logger.Info("Creating an encounter of the main party with the client's player party");
                    Settlement settlement = Settlement.Find("tutorial_training_field");
                    Campaign.Current.HandleSettlementEncounter(MobileParty.MainParty, settlement);
                    PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("training_field"), null, null, null);
                }
                else if (m_HeroGUID != null)
                {
                    Logger.Info("There's a player for the client with a guid of {guid}", m_HeroGUID);

                    m_PlayerInCampaign = (Hero)MBObjectManager.Instance.GetObject(m_HeroGUID);

                    // Switch current player party from host to client party
                    ChangePlayerCharacterAction.Apply(m_PlayerInCampaign);
                }
                else
                {
                    // Might need to adjust IsClientPlayersParty
                    throw new Exception("Transferred player party could not be found");
                }

                // Switch current player party from host to client party
                Logger.Info("Switching current player party from host to client party...");
                ChangePlayerCharacterAction.Apply(m_PlayerInCampaign);
            } catch (Exception e)
            {
                Logger.Error(e, "Failed to handle character encounter on client-side: ${stacktrace}");
                Logger.Info("Printing a trace -- \n{}", Environment.StackTrace);
                throw new Exception("uncaught on purpose - self destruct");
            }
        }

        public new void OnTick(float dt)
        {
            FieldInfo entityFieldInfo = typeof(GameManagerBase).GetField("_entitySystem", BindingFlags.Instance | BindingFlags.NonPublic);
            if(entityFieldInfo.GetValue(this) == null)
            {
                entityFieldInfo.SetValue(this, new EntitySystem<GameManagerComponent>());
            }
            base.OnTick(dt);
        }

        private bool IsClientPlayersParty(MobileParty candidate)
        {

            // This comparison is subject to change
            Hero candidateHero = candidate.LeaderHero;

            Logger.Info(
                "... candidate: '{}' lead by '{}' of clan '{}' of parents '{}' & '{}'",
                candidate.Name, candidateHero?.Name, candidateHero?.Clan.Name, candidateHero?.Father?.Name, candidateHero?.Mother?.Name
            );

            if (candidateHero == null)
            {
                return false;
            }

            // Hero itself is always sent
            if (candidateHero.Name.ToString() != m_PlayerAsSerialized.Name.ToString())
            {
                return false;
            }

            // Clan as well
            if (candidateHero.Clan.Name.ToString() != m_PlayerAsSerialized.Clan.Name.ToString())
            {
                return false;
            }

            // Parents are missing for now
            if (candidateHero.Father != null || candidateHero.Mother != null)
            {
                return false;
            }

            return true;
        }
    }
}

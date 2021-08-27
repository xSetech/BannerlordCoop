﻿using System;
using System.Threading;
using System.Linq;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem.Load;
using System.Reflection;
using TaleWorlds.Library;
using Sync.Store;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using NLog;

namespace Coop.Mod.Managers
{
    public class HeroEventArgs : EventArgs
    {

        public ObjectId HeroId { get; private set; }
        public string PartyName { get; private set; }
        public HeroEventArgs(string PartyName, ObjectId HeroId)
        {
            this.PartyName = PartyName;
            this.HeroId = HeroId;
        }
    }
    public class ClientCharacterCreatorManager : StoryModeGameManager
    {
        public ClientCharacterCreatorManager(LoadResult saveGameData) : base(saveGameData) { }

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

        public ClientCharacterCreatorManager()
        {
        }

        public delegate void OnLoadFinishedEventHandler(object source, EventArgs e);
        public static event OnLoadFinishedEventHandler OnCharacterCreationLoadFinishedEvent;
        public static event OnLoadFinishedEventHandler OnGameLoadFinishedEvent;

        public MobileParty ClientParty { get; private set; }
        public Hero ClientHero { get; private set; }
        public CharacterObject ClientCharacterObject { get; private set; }
        

        public override void OnLoadFinished()
        {
            Logger.Info("Character load finished...");
            base.OnLoadFinished();
            Logger.Info("Post character-load finished.");

            OnCharacterCreationLoadFinishedEvent?.Invoke(this, EventArgs.Empty);

#if DEBUG
            SkipCharacterCreation();
#endif

            Logger.Info("Placing character party at the training field");
            Settlement settlement = Settlement.Find("tutorial_training_field");
            MobileParty.MainParty.Position2D = settlement.Position2D;

            // Renamed the main hero for debugging - Columbus
            Hero.MainHero.Name = new TextObject("Vince");

            var mainHeroName = Hero.MainHero.Name.ToString();
            var mainPartyName = MobileParty.MainParty.Name.ToString();
            Logger.Info("Main party is '{}' (ignore?), main hero is '{}'", mainPartyName, mainHeroName);

            Logger.Info("Sending my hero to the server...");
            var syncd_hero_obj_id = CoopClient.Instance.SyncedObjectStore.Insert(Hero.MainHero);
            Logger.Info("Sent hero with object id:  {}", syncd_hero_obj_id);

            Logger.Info("Sending my hero's party to the server...");
            var syncd_party_obj_id = CoopClient.Instance.SyncedObjectStore.Insert(MobileParty.MainParty);
            Logger.Info("Sent party with object id:  {}", syncd_party_obj_id);

            OnGameLoadFinishedEvent?.Invoke(this, new HeroEventArgs(
                mainPartyName,
                syncd_hero_obj_id
            ));

            EndGame();
        }

        private void SkipCharacterCreation()
        {
            CharacterCreationState characterCreationState = GameStateManager.Current.ActiveState as CharacterCreationState;
            if (characterCreationState.CurrentStage is CharacterCreationCultureStage)
            {
                CultureObject culture = CharacterCreationContentBase.Instance.GetCultures().GetRandomElementInefficiently();
                CharacterCreationContentBase.Instance.SetSelectedCulture(culture, characterCreationState.CharacterCreation);
                characterCreationState.NextStage();
            }

            if (characterCreationState.CurrentStage is CharacterCreationFaceGeneratorStage)
            {
                ICharacterCreationStageListener listener = characterCreationState.CurrentStage.Listener;
                BodyGeneratorView bgv = (BodyGeneratorView)listener.GetType().GetField("_faceGeneratorView", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(listener);

                FaceGenVM facegen = bgv.DataSource;

                facegen.FaceProperties.Randomize();
                characterCreationState.NextStage();
            }

            if (characterCreationState.CurrentStage is CharacterCreationGenericStage)
            {
                for (int i = 0; i < characterCreationState.CharacterCreation.CharacterCreationMenuCount; i++)
                {
                    CharacterCreationOption characterCreationOption = characterCreationState.CharacterCreation.GetCurrentMenuOptions(i).FirstOrDefault((CharacterCreationOption o) => o.OnCondition == null || o.OnCondition());
                    bool flag4 = characterCreationOption != null;
                    if (flag4)
                    {
                        characterCreationState.CharacterCreation.RunConsequence(characterCreationOption, i, false);
                    }
                }
                characterCreationState.NextStage();
            }

            if (characterCreationState.CurrentStage is CharacterCreationReviewStage)
            {
                characterCreationState.NextStage();
            }

            characterCreationState = (GameStateManager.Current.ActiveState as CharacterCreationState);
        }
    }
}

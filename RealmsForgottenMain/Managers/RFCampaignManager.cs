using System;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem.Load;

namespace RealmsForgotten.Managers
{
    public class RFCampaignManager : MBGameManager
    {
        private readonly bool _loadingSavedGame;
        private LoadResult _loadedGameResult;
        private Game _loadedGame;

        public RFCampaignManager()
        {
            _loadingSavedGame = false;
        }

        public RFCampaignManager(LoadResult loadedGameResult)
        {
            _loadingSavedGame = true;
            _loadedGameResult = loadedGameResult;
        }

        public override void OnGameEnd(Game game)
        {
            MBDebug.SetErrorReportScene(null);
            base.OnGameEnd(game);
        }

        public override void OnGameInitializationFinished(Game game)
        {
            base.OnGameInitializationFinished(game);
        }

        protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
        {
            nextStep = GameManagerLoadingSteps.None;
            switch (gameManagerLoadingStep)
            {
                case GameManagerLoadingSteps.PreInitializeZerothStep:
                    nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
                    return;
                case GameManagerLoadingSteps.FirstInitializeFirstStep:
                    LoadModuleData(_loadingSavedGame);
                    nextStep = GameManagerLoadingSteps.WaitSecondStep;
                    return;
                case GameManagerLoadingSteps.WaitSecondStep:
                    {
                        if (!_loadingSavedGame)
                        {
                            StartNewGame();
                        }
                        nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
                        return;
                    }
                case GameManagerLoadingSteps.SecondInitializeThirdState:
                    {
                        MBGlobals.InitializeReferences();
                        if (!_loadingSavedGame)
                        {
                            MBDebug.Print("Initializing new game begin...", 0, Debug.DebugColor.White, 17592186044416UL);
                            Campaign campaign = new(CampaignGameMode.Campaign);
                            _loadedGame = Game.CreateGame(campaign, this);
                            campaign.SetLoadingParameters(Campaign.GameLoadingType.NewCampaign);
                            MBDebug.Print("Initializing new game end...", 0, Debug.DebugColor.White, 17592186044416UL);
                        }
                        else
                        {
                            MBDebug.Print("Initializing saved game begin...", 0, Debug.DebugColor.White, 17592186044416UL);
                            _loadedGame = Game.LoadSaveGame(_loadedGameResult, this);
                            ((Campaign)_loadedGame.GameType).SetLoadingParameters(Campaign.GameLoadingType.SavedCampaign);
                            _loadedGameResult = null;
                            Common.MemoryCleanupGC(false);
                            MBDebug.Print("Initializing saved game end...", 0, Debug.DebugColor.White, 17592186044416UL);
                        }
                        (_loadedGame ?? Game.Current)?.DoLoading();
                        nextStep = GameManagerLoadingSteps.PostInitializeFourthState;
                        return;
                    }
                case GameManagerLoadingSteps.PostInitializeFourthState:
                    {
                        Game activeGame = _loadedGame ?? Game.Current;
                        if (activeGame == null)
                        {
                            throw new InvalidOperationException("RFCampaignManager.PostInitializeFourthState reached with no active Game instance.");
                        }
                        bool submodulesLoaded = true;
                        foreach (MBSubModuleBase mbsubModuleBase in TaleWorlds.MountAndBlade.Module.CurrentModule.CollectSubModules())
                        {
                            submodulesLoaded = submodulesLoaded && mbsubModuleBase.DoLoading(activeGame);
                        }
                        nextStep = submodulesLoaded ? GameManagerLoadingSteps.FinishLoadingFifthStep : GameManagerLoadingSteps.PostInitializeFourthState;
                        return;
                    }
                case GameManagerLoadingSteps.FinishLoadingFifthStep:
                    {
                        Game activeGame = _loadedGame ?? Game.Current;
                        if (activeGame == null)
                        {
                            throw new InvalidOperationException("RFCampaignManager.FinishLoadingFifthStep reached with no active Game instance.");
                        }
                        nextStep = activeGame.DoLoading() ? GameManagerLoadingSteps.None : GameManagerLoadingSteps.FinishLoadingFifthStep;
                        return;
                    }
                default:
                    return;
            }
        }

        public override void OnLoadFinished()
        {
            if (!_loadingSavedGame)
            {
                MBDebug.Print("Switching to menu window...", 0, Debug.DebugColor.White, 17592186044416UL);


                VideoPlaybackState state = Game.Current.GameStateManager.CreateState<VideoPlaybackState>();
                string str = ModuleHelper.GetModuleFullPath(Globals.realmsForgottenAssembly.GetName().Name) + "Videos/CampaignIntro/";
                string subtitleFileBasePath = str + "RF_lore_intro_b";
                string videoPath = str + "RF_lore_intro_b.ivf";
                string audioPath = str + "RF_lore_intro_b.ogg";
                state.SetStartingParameters(videoPath, audioPath, subtitleFileBasePath);
                state.SetOnVideoFinisedDelegate(new Action(LaunchSandboxCharacterCreation));
                Game.Current.GameStateManager.CleanAndPushState(state);

            }
            else
            {
                Game.Current.GameStateManager.OnSavedGameLoadFinished();
                Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<MapState>(), 0);
                MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
                string text = mapState?.GameMenuId;
                if (!string.IsNullOrEmpty(text))
                {
                    PlayerEncounter playerEncounter = PlayerEncounter.Current;
                    playerEncounter?.OnLoad();
                    Campaign.Current.GameMenuManager.SetNextMenu(text);
                }
                PartyBase.MainParty.SetVisualAsDirty();
                Campaign.Current.CampaignInformationManager.OnGameLoaded();
                foreach (Settlement settlement in Settlement.All)
                {
                    settlement.Party.SetLevelMaskIsDirty();
                }
                CampaignEventDispatcher.Instance.OnGameLoadFinished();
            }
            IsLoaded = true;
        }

        private void LaunchSandboxCharacterCreation()
        {
            CharacterCreationState gameState = Game.Current.GameStateManager.CreateState<CharacterCreationState>();
            Game.Current.GameStateManager.CleanAndPushState(gameState, 0);
        }

        public override void OnAfterCampaignStart(Game game)
        {
        }

    }
}

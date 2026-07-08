protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
{
    nextStep = GameManagerLoadingSteps.None;
    switch (gameManagerLoadingStep)
    {
        case GameManagerLoadingSteps.PreInitializeZerothStep:
            nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
            return;
        case GameManagerLoadingSteps.FirstInitializeFirstStep:
            // This loads all XML data (settlements, clans, kingdoms, etc.)
            MBGameManager.LoadModuleData(_loadingSavedGame);
            nextStep = GameManagerLoadingSteps.WaitSecondStep;
            return;
        case GameManagerLoadingSteps.WaitSecondStep:
            {
                if (!_loadingSavedGame)
                {
                    // This starts the new game initialization (character templates, etc.)
                    MBGameManager.StartNewGame();
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
                    Game.CreateGame(campaign, this);
                    campaign.SetLoadingParameters(Campaign.GameLoadingType.NewCampaign);
                    MBDebug.Print("Initializing new game end...", 0, Debug.DebugColor.White, 17592186044416UL);
                }
                else
                {
                    MBDebug.Print("Initializing saved game begin...", 0, Debug.DebugColor.White, 17592186044416UL);
                    ((Campaign)Game.LoadSaveGame(_loadedGameResult, this).GameType).SetLoadingParameters(Campaign.GameLoadingType.SavedCampaign);
                    _loadedGameResult = null;
                    Common.MemoryCleanupGC(false);
                    MBDebug.Print("Initializing saved game end...", 0, Debug.DebugColor.White, 17592186044416UL);
                }
                Game.Current.DoLoading();
                nextStep = GameManagerLoadingSteps.PostInitializeFourthState;
                return;
            }
        case GameManagerLoadingSteps.PostInitializeFourthState:
            {
                bool submodulesLoaded = true;
                foreach (MBSubModuleBase mbsubModuleBase in TaleWorlds.MountAndBlade.Module.CurrentModule.CollectSubModules())
                {
                    submodulesLoaded = submodulesLoaded && mbsubModuleBase.DoLoading(Game.Current);
                }
                nextStep = submodulesLoaded ? GameManagerLoadingSteps.FinishLoadingFifthStep : GameManagerLoadingSteps.PostInitializeFourthState;
                return;
            }
        case GameManagerLoadingSteps.FinishLoadingFifthStep:
            nextStep = Game.Current.DoLoading() ? GameManagerLoadingSteps.None : GameManagerLoadingSteps.FinishLoadingFifthStep;
            return;
        default:
            return;
    }
}
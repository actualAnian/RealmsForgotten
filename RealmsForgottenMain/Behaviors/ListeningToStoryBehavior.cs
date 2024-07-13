using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using Bannerlord.Module1.States;

namespace Bannerlord.Module1
{
    public class ListeningToStoryBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject InitialText = new TextObject("{=storyteller_intro}As you passed by a small tavern, an old man stood on the outside and asked: 'If you are willing to throw me a coin, I will reward you with a story.'");
        private static readonly List<TextObject> StoryParts = new List<TextObject>
        {
            new TextObject("{=storyteller_story_2_part_1} ..."), // Add the full text here
            new TextObject("{=storyteller_story_2_part_2} ..."),
            new TextObject("{=storyteller_story_2_part_3} ..."),
            new TextObject("{=storyteller_story_2_part_4} ..."),
            new TextObject("{=storyteller_story_2_part_5} ..."),
            new TextObject("{=storyteller_story_2_part_6} ...")
        };

        private static readonly TextObject AcceptText = new TextObject("{=Accept}LISTEN");
        private static readonly TextObject DeclineText = new TextObject("{=Decline}IGNORE");

        private static GauntletLayer _gauntletLayer;
        private static GauntletMovie _gauntletMovie;
        private static YourPopupVM _popupVM;

        private CampaignTime _lastStoryTime;
        private CampaignTime _gameStartTime;
        private const int StoryCooldownDays = 30;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("last_story_time", ref _lastStoryTime);
            dataStore.SyncData("game_start_time", ref _gameStartTime);
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            _gameStartTime = CampaignTime.Now;
            _lastStoryTime = CampaignTime.Now; // Initial value to ensure the story doesn't trigger immediately
            Initialize();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void Initialize()
        {
            InformationManager.DisplayMessage(new InformationMessage("LISTENING TO STORY BEHAVIOR INITIALIZED SUCCESSFULLY.", Colors.Green));
        }

        private void OnHourlyTick()
        {
            if (CampaignTime.Now > _gameStartTime + CampaignTime.Days(30) &&
                (_lastStoryTime == null || CampaignTime.Now > _lastStoryTime + CampaignTime.Days(StoryCooldownDays)))
            {
                CreateInitialPopup();
            }
        }

        private void CreateInitialPopup()
        {
            InformationManager.ShowInquiry(new InquiryData(
                "A Mysterious Encounter",
                InitialText.ToString(),
                true,
                true,
                AcceptText.ToString(),
                DeclineText.ToString(),
                OnInitialAccept,
                OnDecline
            ));
        }

        private void OnInitialAccept()
        {
            _lastStoryTime = CampaignTime.Now;
            ShowStoryPart1();
        }

        private void OnDecline()
        {
            _lastStoryTime = CampaignTime.Now;
            InformationManager.DisplayMessage(new InformationMessage("YOU DECIDED TO IGNORE THE OLD MAN.", Colors.Red));
            DeletePopupVMLayer();
        }

        private void ShowStoryPart1()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[0].ToString(), "elvean_story_a", ShowStoryPart2);
        }

        private void ShowStoryPart2()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[1].ToString(), "elvean_story_b", ShowStoryPart3);
        }

        private void ShowStoryPart3()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[2].ToString(), "elvean_story_c", ShowStoryPart4);
        }

        private void ShowStoryPart4()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[3].ToString(), "elvean_story_d", ShowStoryPart5);
        }

        private void ShowStoryPart5()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[4].ToString(), "elvean_story_e", ShowStoryPart6);
        }

        private void ShowStoryPart6()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[5].ToString(), "elvean_story_f", EndStory);
        }

        private void EndStory()
        {
            InformationManager.DisplayMessage(new InformationMessage("YOU HAVE FINISHED LISTENING TO THE STORY.", Colors.Green));
            DeletePopupVMLayer();
        }

        private void ShowCustomPopup(string title, string smallText, string spriteName, Action onContinue)
        {
            if (_gauntletLayer == null)
            {
                _gauntletLayer = new GauntletLayer(1000, "GauntletLayer", false);
            }
            if (_popupVM == null)
            {
                _popupVM = new YourPopupVM(title, smallText, spriteName, onContinue, OnDecline);
            }
            else
            {
                _popupVM.UpdatePopup(title, smallText, spriteName, onContinue, OnDecline);
            }

            try
            {
                _gauntletMovie = (GauntletMovie)_gauntletLayer.LoadMovie("YourPopupXMLFileName", _popupVM);
            }
            catch (Exception e)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Error loading movie: {e.Message}", Colors.Red));
                return;
            }

            if (_gauntletMovie != null)
            {
                _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                ScreenManager.TopScreen.AddLayer(_gauntletLayer);
                _gauntletLayer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(_gauntletLayer);
                _popupVM.Refresh();
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("Failed to load the Gauntlet movie.", Colors.Red));
            }
        }

        public static void DeletePopupVMLayer()
        {
            if (_gauntletLayer != null)
            {
                _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                _gauntletLayer.IsFocusLayer = false;
                if (_gauntletMovie != null)
                {
                    _gauntletLayer.ReleaseMovie(_gauntletMovie);
                }
                ScreenManager.TopScreen.RemoveLayer(_gauntletLayer);
            }
            _gauntletLayer = null;
            _gauntletMovie = null;
            _popupVM = null;
        }
    }
}

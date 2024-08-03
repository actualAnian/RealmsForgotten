using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using Bannerlord.Module1.States;
using System; // Ensure this is the correct namespace for YourPopupVM

namespace Bannerlord.Module1.Stories
{
    public class Story2Behavior : CampaignBehaviorBase
    {
        private static readonly TextObject InitialText = new TextObject("{=story2_intro}You encounter a mysterious figure who says, 'Let me tell you a story that you won't forget.'");
        private static readonly List<TextObject> StoryParts = new List<TextObject>
        {
            new TextObject("{=story2_part1}Story 2 - Part 1: The beginning..."),
            new TextObject("{=story2_part2}Story 2 - Part 2: The middle..."),
            new TextObject("{=story2_part3}Story 2 - Part 3: The end...")
        };

        private static readonly TextObject AcceptText = new TextObject("{=Accept}LISTEN");
        private static readonly TextObject DeclineText = new TextObject("{=Decline}IGNORE");

        private static GauntletLayer _gauntletLayer;
        private static GauntletMovie _gauntletMovie;
        private static YourPopupVM _popupVM;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // No need to sync data
        }

        private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            Initialize();
        }

        private void Initialize()
        {
            InformationManager.DisplayMessage(new InformationMessage("STORY 2 BEHAVIOR INITIALIZED SUCCESSFULLY.", Colors.Green));
            CreateInitialPopup();
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
            ShowStoryPart1();
        }

        private void OnDecline()
        {
            InformationManager.DisplayMessage(new InformationMessage("YOU DECIDED TO IGNORE THE STORY.", Colors.Red));
            DeletePopupVMLayer();
        }

        private void ShowStoryPart1()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[0].ToString(), "image2", ShowStoryPart2);
        }

        private void ShowStoryPart2()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[1].ToString(), "image2", ShowStoryPart3);
        }

        private void ShowStoryPart3()
        {
            ShowCustomPopup("Listening to a Story", StoryParts[2].ToString(), "image2", EndStory);
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

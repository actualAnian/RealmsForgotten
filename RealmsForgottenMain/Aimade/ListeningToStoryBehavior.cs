using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;

// Correct namespace

namespace RealmsForgotten.AiMade
{
    public class ListeningToStoryBehavior : CampaignBehaviorBase
    {
        private static readonly TextObject InitialText = new TextObject("{=storyteller_intro}As you passed by a small tavern, an old man stood on the outside and asked: 'If you are willing to throw me a coin, I will reward you with a story.'");
        private static readonly List<TextObject> StoryParts = new List<TextObject>
    {
        new TextObject("{=storyteller_story_2_part_1}  Ah, a tale of love and tragedy, then. There was once an Elvish scout, whose duty was to oversee the early roads " +
            "  through the Elvish lands and the Realms of Men. In his many travels, one time he scouted a merchant's caravan that among its invaluable goods carried the most beautiful woman. " +
            "  The scout instantly fell in love with her, but that young lady happened to be the daughter of the caravan master."),
        new TextObject("{=storyteller_story_2_part_2}  Despite the tension between their cultures, both found a way to meet secretly under the cover of the ancient forests. Their love blossomed like a rare flower, beautiful yet fragile. " +
            "  The young woman's father, unaware of their secret meetings, planned to marry her off to a wealthy suitor from the southern realm."),
        new TextObject("{=storyteller_story_2_part_3}  As the caravan made their travels from north to south and back, their love matured, and they decided to escape together, to carve out a life away from the disapproving eyes of their kin. " +
            "  But fate, it seemed, had other plans. One fateful night, as the Elvish scout awaited his love at their secret grove, he was ambushed by the men sent by the father, and after a fierce fight ended up gravely wounded."),
        new TextObject("{=storyteller_story_2_part_4}  The young woman found him, her heart breaking at the sight of her beloved on the ground, blood leaving his wounds. She nursed him through the night, whispering words of love and hope. " +
            "  But as dawn approached, her father came into them, backed by another group of bodyguards."),
        new TextObject("{=storyteller_story_2_part_5}  Seeing the depth of their love, he hesitated, torn between his duty and his daughter’s happiness. But at the sight of her father's bodyguards, she grabbed the blade of the scout, and urged into them."),
        new TextObject("{=storyteller_story_2_part_6}  The aftermath was not less tragic, the young lady's body fell, lifeless, beside the one she tried to protect. " +
            "  It is said that a spring rose from where the earth consumed their bodies, and those who drink from such water will never grow old. " +
            "  Maybe you will find it on your travels, and drink from this precious fountain.")
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
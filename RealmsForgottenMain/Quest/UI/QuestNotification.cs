using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.Quest.UI
{
    public class QuestNotificationState : GameState
    {
        public int FontSize;
        public string Text;
        public Action OnLeaveVoid;
        public string SpriteID;
        public bool HaveImage;
        public QuestNotificationState(string text, int fontsize, Action onLeaveAction, bool haveImage, string spriteId)
        {
            Text = text;
            if (fontsize > 0 && fontsize <= 100)
                FontSize = fontsize;
            OnLeaveVoid = onLeaveAction;
            SpriteID = spriteId;
            HaveImage = haveImage;
        }
        public QuestNotificationState() { throw new ArgumentException(); }
    }

    [GameStateScreen(typeof(QuestNotificationState))]
    public class QuestNotificationScreen : ScreenBase, IGameStateListener
    {
        QuestNotificationState questNotificationState;
        public QuestNotificationScreen(QuestNotificationState questNotificationState)
        {
            this.questNotificationState = questNotificationState;
            this.questNotificationState.RegisterListener(this);
        }

        GauntletLayer _layer;
        QuestNotificationVm _dataSource;

        void IGameStateListener.OnActivate()
        {
            _layer = new GauntletLayer(1, "GauntletLayer", true);
            _dataSource = new QuestNotificationVm(questNotificationState);
            _layer.LoadMovie(questNotificationState.HaveImage ? "RFNotificationWithImage" : "RFNotification", _dataSource);
            _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("PartyHotKeyCategory"));
            _layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _layer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_layer);
            AddLayer(_layer);
        }

        void IGameStateListener.OnDeactivate()
        {
            _layer.InputRestrictions.ResetInputRestrictions();
            _layer.IsFocusLayer = false;
            RemoveLayer(_layer);
            ScreenManager.TryLoseFocus(_layer);
            _dataSource = null;
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (_layer.Input.IsKeyReleased(InputKey.Enter))
            {
                _dataSource.ExecuteDone();
            }
        }

        void IGameStateListener.OnFinalize() { }
        void IGameStateListener.OnInitialize() { }
    }

    public class QuestNotificationVm : ViewModel
    {
        private int FontSize = 60;
        private string CurrentText;
        private Action onLeaveAction;
        private string SpriteID;
        public QuestNotificationVm(QuestNotificationState questNotificationState)
        {
            CurrentText = questNotificationState.Text;
            FontSize = questNotificationState.FontSize;
            onLeaveAction = questNotificationState.OnLeaveVoid;
            SpriteID = questNotificationState.SpriteID;
        }

        [DataSourceProperty]
        public string DoneLabel => GameTexts.FindText("rf_leave", null).ToString();

        [DataSourceProperty]
        public string TitleLabel => GameTexts.FindText("rf_event", null).ToString();

        [DataSourceProperty]
        public string RFFontSize => FontSize.ToString();

        [DataSourceProperty]
        public string SpriteId => SpriteID;

        [DataSourceProperty]
        public string TextLabel => CurrentText;

        public void ExecuteDone()
        {
            GameStateManager.Current.PopState();
            if (onLeaveAction != null)
                onLeaveAction();
        }
    }




}

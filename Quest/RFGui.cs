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

namespace Quest
{
    public class RFNotificationState : GameState
    {
        public int fontSize;
        public string Text;
        public Action onLeaveVoid;
        public RFNotificationState(string text, int fontsize, Action onLeaveAction)
        {
            Text = text;
            if(fontsize > 0 && fontsize <= 100)
                fontSize = fontsize;
            onLeaveVoid = onLeaveAction;
        }
        public RFNotificationState() { throw new ArgumentException(); }
    }

    [GameStateScreen(typeof(RFNotificationState))]
    public class RFNotificationScreen : ScreenBase, IGameStateListener
    {
        RFNotificationState _RFNotificationState;
        public RFNotificationScreen(RFNotificationState rfNotificationState)
        {
            _RFNotificationState = rfNotificationState;
            _RFNotificationState.RegisterListener(this);
        }

        GauntletLayer _layer;
        RFNotificationVM _dataSource;

        void IGameStateListener.OnActivate()
        {
            _layer = new GauntletLayer(1, "GauntletLayer", true);
            _dataSource = new RFNotificationVM(_RFNotificationState);
            _layer.LoadMovie("RFNotification", _dataSource);
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

    public class RFNotificationVM : ViewModel
    {
        private int FontSize = 60;
        private string CurrentText;
        private Action onLeaveAction;
        public RFNotificationVM(RFNotificationState RFNotificationState)
        {
            CurrentText = RFNotificationState.Text;
            FontSize = RFNotificationState.fontSize;
            onLeaveAction = RFNotificationState.onLeaveVoid;
        }

        [DataSourceProperty]
        public string DoneLabel => GameTexts.FindText("rf_leave", null).ToString();

        [DataSourceProperty]
        public string TitleLabel => GameTexts.FindText("rf_event", null).ToString();

        [DataSourceProperty]
        public string RFFontSize => FontSize.ToString();
        [DataSourceProperty]
        public string TextLabel => CurrentText;
        
        public void ExecuteDone()
        {
            GameStateManager.Current.PopState();
            if(onLeaveAction != null)
                onLeaveAction();
        }
    }




}

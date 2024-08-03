using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.Module1.States
{
    public class YourPopupVM : ViewModel
    {
        private string _title;
        private string _smallText;
        private string _spriteName;
        private Action _onContinue;
        private Action _onDecline;

        public YourPopupVM(string title, string smallText, string spriteName, Action onContinue, Action onDecline)
        {
            this.Title = title;
            this.SmallText = smallText;
            this.SpriteName = spriteName;
            this._onContinue = onContinue;
            this._onDecline = onDecline;
        }

        public void Continue()
        {
            _onContinue?.Invoke();
        }

        public void Decline()
        {
            _onDecline?.Invoke();
        }

        public void Close()
        {
            ListeningToStoryBehavior.DeletePopupVMLayer();
        }

        public void Refresh()
        {
            base.OnPropertyChangedWithValue(_title, "PopupTitle");
            base.OnPropertyChangedWithValue(_smallText, "PopupSmallText");
            base.OnPropertyChangedWithValue(_spriteName, "SpriteName");
        }

        [DataSourceProperty]
        public string Title
        {
            get { return this._title; }
            set { this._title = value; base.OnPropertyChangedWithValue(value, "PopupTitle"); }
        }

        [DataSourceProperty]
        public string SmallText
        {
            get { return this._smallText; }
            set { this._smallText = value; base.OnPropertyChangedWithValue(value, "PopupSmallText"); }
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get { return this._spriteName; }
            set { this._spriteName = value; base.OnPropertyChangedWithValue(value, "SpriteName"); }
        }

        public void UpdatePopup(string title, string smallText, string spriteName, Action onContinue, Action onDecline)
        {
            this.Title = title;
            this.SmallText = smallText;
            this.SpriteName = spriteName;
            this._onContinue = onContinue;
            this._onDecline = onDecline;
            Refresh();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using static RealmsForgotten.Quest.SecondUpdate.SixthQuest;

namespace RealmsForgotten.Quest.SecondUpdate
{
    public class SixthQuestPopupVM : ViewModel
    {
        private string _title;
        private string _description;
        private string _spriteName;
        private Action _onContinue;
        private Action _onDecline;
        private string _buttonLabel;

        public SixthQuestPopupVM(string title, string description, string spriteName, Action onContinue, Action onDecline, string buttonLabel)
        {
            this.Title = title;
            this.Description = description;
            this.SpriteName = spriteName;
            this._onContinue = onContinue;
            this._onDecline = onDecline;
            this.ButtonLabel = buttonLabel;
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
            SixthQuestBehaviour.DeletePopupVMLayer();
        }

        public void Refresh()
        {
            base.OnPropertyChangedWithValue(_title, "PopupTitle");
            base.OnPropertyChangedWithValue(_description, "PopupDescription");
            base.OnPropertyChangedWithValue(_spriteName, "SpriteName");
            base.OnPropertyChangedWithValue(_buttonLabel, "ButtonLabel");
        }

        public string Title
        {
            get { return this._title; }
            set { this._title = value; base.OnPropertyChangedWithValue(value, "PopupTitle"); }
        }

        public string Description
        {
            get { return this._description; }
            set { this._description = value; base.OnPropertyChangedWithValue(value, "PopupDescription"); }
        }

        public string SpriteName
        {
            get { return this._spriteName; }
            set { this._spriteName = value; base.OnPropertyChangedWithValue(value, "SpriteName"); }
        }

        public string ButtonLabel
        {
            get { return this._buttonLabel; }
            set { this._buttonLabel = value; base.OnPropertyChangedWithValue(value, "ButtonLabel"); }
        }

        public void UpdatePopup(string title, string description, string spriteName, Action onContinue, Action onDecline, string buttonLabel)
        {
            this.Title = title;
            this.Description = description;
            this.SpriteName = spriteName;
            this._onContinue = onContinue;
            this._onDecline = onDecline;
            this.ButtonLabel = buttonLabel;
            Refresh();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten
{
    public class SpellStatusVM : ViewModel
    {
        public SpellStatusVM(string spellText, bool isVisible)
        {
            SpellText = spellText;
            Visible = isVisible;
        }
        public const string red = "#CC0000FF";

        private TextObject _spellTextObject;
        private string _spellText;
        private bool _visible;
        [DataSourceProperty]
        public string SpellText
        {
            get => _spellText;
            set
            {
                _spellText = value;
                OnPropertyChanged("SpellText");
            }
        }
        [DataSourceProperty]
        public bool Visible
        {
            get => _visible;
            
            set
            {
                _visible = value;
                OnPropertyChanged("Visible");
            }
        }
    }
}

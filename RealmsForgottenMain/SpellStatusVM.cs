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
        public SpellStatusVM(string spellText, bool isVisible, int height, int fontSize)
        {
            SpellText = spellText;
            Visible = isVisible;
            Height = height;
            FontSize = fontSize;
        }
        public const string red = "#CC0000FF";

        private TextObject _spellTextObject;
        private string _spellText;
        private bool _visible;
        private int _height;
        private int _fontSize;
        
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
        
        [DataSourceProperty]
        public int Height
        {
            get => _height;
            
            set
            {
                _height = value;
                OnPropertyChanged("Height");
            }
        }
        [DataSourceProperty]
        public int FontSize
        {
            get => _fontSize;
            
            set
            {
                _fontSize = value;
                OnPropertyChanged("FontSize");
            }
        }
    }
}

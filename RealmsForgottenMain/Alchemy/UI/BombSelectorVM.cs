using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Alchemy.UI
{
    public class BombSelectorVM : ViewModel
    {
        private MBBindingList<BombItemVM> _bombs;
        private bool _hasBombs;
        private string _titleText;
        private string _closeButtonText;
        private BombItemVM? _selectedItem;
        private bool _isVisible;

        public BombSelectorVM()
        {
            _bombs = new MBBindingList<BombItemVM>();
            _titleText = new TextObject("{=alch_bomb_sel_title}Select Alchemical Bomb").ToString();
            _closeButtonText = GameTexts.FindText("str_done").ToString();
        }
        public void AddBombItem(BombItemVM bombItem)
        {
            _bombs.Add(bombItem);
            HasBombs = _bombs.Count > 0;
        }
        public void DecrementBombAmount(BombItemVM bomb)
        {
            if (int.Parse(bomb.Amount) > 0)
            {
                bomb.Amount = (int.Parse(bomb.Amount) - 1).ToString();
                if (bomb.Amount == "0")
                {
                    _bombs.Remove(bomb);
                    HasBombs = _bombs.Count > 0;
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<BombItemVM> Bombs
        {
            get => _bombs;
            set
            {
                if (_bombs != value)
                {
                    _bombs = value;
                    OnPropertyChangedWithValue(value, nameof(Bombs));
                }
            }
        }

        [DataSourceProperty]
        public bool HasBombs
        {
            get => _hasBombs;
            set
            {
                if (_hasBombs != value)
                {
                    _hasBombs = value;
                    OnPropertyChangedWithValue(value, nameof(HasBombs));
                }
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (_titleText != value)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, nameof(TitleText));
                }
            }
        }

        [DataSourceProperty]
        public string CloseButtonText
        {
            get => _closeButtonText;
            set
            {
                if (_closeButtonText != value)
                {
                    _closeButtonText = value;
                    OnPropertyChangedWithValue(value, nameof(CloseButtonText));
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsVisible));
                }
            }
        }

        public BombItemVM? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                }
            }
        }
        public void OnBombItemSelected(BombItemVM item)
        {
            foreach (var bomb in _bombs)
                bomb.IsSelected = bomb == item;

            SelectedItem = item;
        }

        public void ExecuteClose()
        {
            IsVisible = false;
        }
    }
}

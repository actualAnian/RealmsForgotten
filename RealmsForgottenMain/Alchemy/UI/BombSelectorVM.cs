using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

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
            PopulateBombs();
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
                    PopulateBombs();
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

        private void PopulateBombs()
        {
            _bombs.Clear();

            var mainAgent = Agent.Main;
            if (mainAgent == null)
            {
                HasBombs = false;
                return;
            }

            var smokeBombItem = Game.Current.ObjectManager.GetObject<ItemObject>("grain");
            var fireBombItem = Game.Current.ObjectManager.GetObject<ItemObject>("grain");

            if (smokeBombItem != null)
                _bombs.Add(new BombItemVM(smokeBombItem.StringId, 2, OnBombItemSelected));

            if (fireBombItem != null)
                _bombs.Add(new BombItemVM(fireBombItem..StringId, 1, OnBombItemSelected));

            HasBombs = _bombs.Count > 0;
        }

        private void OnBombItemSelected(BombItemVM item)
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

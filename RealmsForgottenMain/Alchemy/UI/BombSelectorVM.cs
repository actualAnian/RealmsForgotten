using TaleWorlds.Library;

namespace RealmsForgotten.Alchemy.UI
{
    public class BombSelectorVM : ViewModel
    {
        private MBBindingList<BombItemVM> _bombs;
        private BombItemVM? _selectedItem;
        private bool _isVisible;

        public BombSelectorVM()
        {
            _bombs = new MBBindingList<BombItemVM>();
        }
        public void AddBombItem(BombItemVM bombItem)
        {
            _bombs.Add(bombItem);
        }
        public void DecrementBombAmount(BombItemVM bomb)
        {
            if (int.Parse(bomb.Amount) > 0)
            {
                bomb.Amount = (int.Parse(bomb.Amount) - 1).ToString();
                if (bomb.Amount == "0")
                {
                    _bombs.Remove(bomb);
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
            PlayerBombManager.Instance.SetBombToNewType(item.StringId);
        }

        public void ExecuteClose()
        {
            IsVisible = false;
        }
    }
}

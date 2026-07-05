using System;
using TaleWorlds.Library;

namespace RealmsForgotten.Alchemy.UI
{
    public class BombItemVM : ViewModel
    {
        private readonly Action<BombItemVM> _onSelected;
        private string _spriteName;
        private string _description;
        private bool _isAvailable = true;
        private bool _isSelected;
        private float _alpha = 1f;
        private Color _tint = new(0.384f, 0.278f, 0.149f, 1f);
        private string _amount;
        private readonly string _stringId;
        public string StringId => _stringId;
        public BombItemVM(string id, string description, string amount, string spriteName, Action<BombItemVM> onSelected)
        {
            _stringId = id;
            _description = description;
            _amount = amount;
            _spriteName = spriteName;
            _onSelected = onSelected;
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get => _spriteName;
            set
            {
                if (_spriteName != value)
                {
                    _spriteName = value;
                    OnPropertyChangedWithValue(value, nameof(SpriteName));
                }
            }
        }
        [DataSourceProperty]
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, nameof(Description));
                }
            }
        }

        [DataSourceProperty]
        public bool IsAvailable
        {
            get => _isAvailable;
            set
            {
                if (_isAvailable != value)
                {
                    _isAvailable = value;
                    OnPropertyChangedWithValue(value, nameof(IsAvailable));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, nameof(IsSelected));
                }
            }
        }

        [DataSourceProperty]
        public float Alpha
        {
            get => _alpha;
            set
            {
                if (Math.Abs(_alpha - value) > 0.001f)
                {
                    _alpha = value;
                    OnPropertyChangedWithValue(value, nameof(Alpha));
                }
            }
        }

        [DataSourceProperty]
        public Color Tint
        {
            get => _tint;
            set
            {
                if (_tint != value)
                {
                    _tint = value;
                    OnPropertyChangedWithValue(value, nameof(Tint));
                }
            }
        }

        [DataSourceProperty]
        public string Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    OnPropertyChangedWithValue(value, nameof(Amount));
                }
            }
        }
        public void ExecuteSelectBomb()
        {
            _onSelected(this);
        }

        public void OnSelected()
        {
            _onSelected(this);
        }
    }
}
﻿using System;
using TaleWorlds.Library;

namespace RealmsForgotten.Smithing.ViewModels
{
    public class ArmorTierFilterTypeVM : ViewModel
    {
        private readonly Action<ArmorPieceTierFlag> _onSelect;

        private bool _isSelected;

        private string _tierName;

        public ArmorTierFilterTypeVM(ArmorPieceTierFlag filterType, Action<ArmorPieceTierFlag> onSelect, string tierName)
        {
            FilterType = filterType;
            _onSelect = onSelect;
            TierName = tierName;
        }

        public ArmorPieceTierFlag FilterType { get; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        [DataSourceProperty]
        public string TierName
        {
            get
            {
                return _tierName;
            }
            set
            {
                if (value != _tierName)
                {
                    _tierName = value;
                    OnPropertyChangedWithValue(value, "TierName");
                }
            }
        }

        public void ExecuteSelectTier()
        {
            _onSelect?.Invoke(FilterType);
        }
    }
}
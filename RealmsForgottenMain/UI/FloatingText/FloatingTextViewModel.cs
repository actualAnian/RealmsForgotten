using System.Linq;
using TaleWorlds.Library;

public class FloatingTextVM : ViewModel
{
    private MBBindingList<FloatingTextItemVM> _allTexts;

    public FloatingTextVM()
    {
        _allTexts = new MBBindingList<FloatingTextItemVM>();
    }

    public void ClearAgents()
    {
        _allTexts.Clear();
    }

    public void AddTextItem(FloatingTextItemVM item)
    {
        _allTexts.Add(item);
    }

    public bool ContainsKey(int key)
    {
        return _allTexts.Any(item => item.Id == key); 
    }
    public void RemoveItem(FloatingTextItemVM item)
    {
        _allTexts.Remove(item);
    }

    [DataSourceProperty]
    public MBBindingList<FloatingTextItemVM> AllTexts
    {
        get => _allTexts;
        set
        {
            if (value != _allTexts)
            {
                _allTexts = value;
                OnPropertyChangedWithValue(value, nameof(AllTexts));
            }
        }
    }
}
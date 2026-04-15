using TaleWorlds.Library;

public class FloatingTextVM : ViewModel
{
    private MBBindingList<FloatingTextItemVM> _agents;

    public FloatingTextVM()
    {
        _agents = new MBBindingList<FloatingTextItemVM>();
    }

    public void ClearAgents()
    {
        _agents.Clear();
    }

    public void AddAgent(FloatingTextItemVM item)
    {
        _agents.Add(item);
    }

    [DataSourceProperty]
    public MBBindingList<FloatingTextItemVM> Agents
    {
        get => _agents;
        set
        {
            if (value != _agents)
            {
                _agents = value;
                OnPropertyChangedWithValue(value, nameof(Agents));
            }
        }
    }
}
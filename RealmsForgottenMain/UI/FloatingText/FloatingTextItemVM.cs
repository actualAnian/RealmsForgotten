using TaleWorlds.Library;

public class FloatingTextItemVM : ViewModel
{
    public int Id { get; private set; }
    private Vec2 _screenPosition;
    private string _text;
    private Color _color;
    private bool _isVisible;

    public FloatingTextItemVM(int id, Vec2 screenPosition, string text, Color color, bool isVisible)
    {
        Id = id;
        _screenPosition = screenPosition;
        _text = text;
        _color = color;
        _isVisible = isVisible;
    }

    [DataSourceProperty]
    public Vec2 ScreenPosition
    {
        get => _screenPosition;
        set
        {
            if (value != _screenPosition)
            {
                _screenPosition = value;
                OnPropertyChangedWithValue(value, nameof(ScreenPosition));
            }
        }
    }

    [DataSourceProperty]
    public string Text
    {
        get => _text;
        set
        {
            if (value != _text)
            {
                _text = value;
                OnPropertyChangedWithValue(value, nameof(Text));
            }
        }
    }
    [DataSourceProperty]
    public Color Color
    {
        get => _color;
        set
        {
            if (value != _color)
            {
                _color = value;
                OnPropertyChangedWithValue(value, nameof(Color));
            }
        }
    }
    [DataSourceProperty]
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value != _isVisible)
            {
                _isVisible = value;
                OnPropertyChangedWithValue(value, nameof(IsVisible));
            }
        }
    }
}
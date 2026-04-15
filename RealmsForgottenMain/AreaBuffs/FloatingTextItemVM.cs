using TaleWorlds.Library;

public class FloatingTextItemVM : ViewModel
{
    private Vec2 _screenPosition;
    private string _text;

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

}
using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.NameMarker;

namespace RF_AliveScenes.UI.Widgets;

/// <summary>
/// Um balao. Se posiciona pela coordenada de tela que a VM calcula e aparece com um
/// fade curto. Versao enxuta do widget do mod original (sem o estado de icone/tipo, que
/// nunca era usado, e sem a busca fragil pelo widget-pai na raiz da UI).
/// </summary>
public class RFAliveScenesBubbleWidget : ListPanel
{
    private Vec2 _position;
    private TextWidget _nameTextWidget;
    private bool _isEnemy;
    private Color _enemyColor;
    private float _transition;

    public MarkerRect Rect { get; private set; }

    public bool IsInScreenBoundaries { get; private set; }

    public RFAliveScenesBubbleWidget(UIContext context)
        : base(context)
    {
        Rect = new MarkerRect();
    }

    [DataSourceProperty]
    public Vec2 Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPropertyChanged(value, "Position");
            }
        }
    }

    [DataSourceProperty]
    public TextWidget NameTextWidget
    {
        get => _nameTextWidget;
        set
        {
            if (_nameTextWidget != value)
            {
                _nameTextWidget = value;
                OnPropertyChanged(value, "NameTextWidget");
                ApplyEnemyColor();
            }
        }
    }

    [DataSourceProperty]
    public bool IsEnemy
    {
        get => _isEnemy;
        set
        {
            if (_isEnemy != value)
            {
                _isEnemy = value;
                OnPropertyChanged(value, "IsEnemy");
                ApplyEnemyColor();
            }
        }
    }

    [Editor(false)]
    public Color EnemyColor
    {
        get => _enemyColor;
        set
        {
            if (value != _enemyColor)
            {
                _enemyColor = value;
                OnPropertyChanged(value, "EnemyColor");
                ApplyEnemyColor();
            }
        }
    }

    public void Update(float dt)
    {
        _transition = MathF.Clamp(dt * 12f, 0f, 1f);
        ApplyActionOnAllChildren(FadeIn);

        ScaledPositionXOffset = Position.x - Size.X / 2f;
        ScaledPositionYOffset = Position.y - Size.Y / 2f;

        UpdateRectangle();
    }

    public void UpdateRectangle()
    {
        Rect.Reset();
        Rect.UpdatePoints(ScaledPositionXOffset, ScaledPositionXOffset + Size.X,
            ScaledPositionYOffset, ScaledPositionYOffset + Size.Y);

        IsInScreenBoundaries =
            Rect.Left > -50f && Rect.Right < EventManager.PageSize.X + 50f &&
            Rect.Top > -50f && Rect.Bottom < EventManager.PageSize.Y + 50f;
    }

    private void ApplyActionOnAllChildren(Action<Widget> action)
    {
        action(this);
        for (int i = 0; i < ChildCount; i++)
        {
            Widget child = GetChild(i);
            action(child);
            for (int j = 0; j < child.ChildCount; j++)
            {
                action(child.GetChild(j));
            }
        }
    }

    private void FadeIn(Widget widget)
    {
        widget.AlphaFactor = Lerp(widget.AlphaFactor, 1f, _transition);
        widget.IsVisible = widget.AlphaFactor > 0.05f;
    }

    private static float Lerp(float start, float end, float delta)
        => Math.Abs(start - end) > float.Epsilon ? (end - start) * delta + start : end;

    private void ApplyEnemyColor()
    {
        if (IsEnemy && NameTextWidget != null)
        {
            NameTextWidget.Brush.GlobalColor = EnemyColor;
        }
    }
}

using System;
using RF_AliveScenes.Config;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AliveScenes.UI;

/// <summary>Um balao de fala preso a um agente.</summary>
public sealed class BubbleVM : ViewModel
{
    private Vec2 _screenPosition;
    private int _distance;
    private int _fontSize = 25;
    private bool _isEnemy;
    private readonly string _message;

    public Agent TargetAgent { get; }

    public BubbleVM(Agent agent, string message, bool isEnemy)
    {
        TargetAgent = agent;
        _message = message;
        _isEnemy = isEnemy;
        RefreshValues();
    }

    [DataSourceProperty]
    public string Name => _message;

    /// <summary>So aparece se o jogador estiver perto o bastante para "ouvir".</summary>
    public bool ShouldShow => PhysicalDistance() <= AliveScenesSettings.Instance.VisibleDistance;

    public Vec3 WorldPosition
    {
        get
        {
            if (TargetAgent == null || !TargetAgent.IsActive())
            {
                return Vec3.Zero;
            }

            try
            {
                Vec3 eye = TargetAgent.GetEyeGlobalPosition();
                Vec3 position = TargetAgent.Position;
                float z = eye.Z != 0f ? eye.Z : position.Z;
                return new Vec3(position.X, position.Y, z, -1f);
            }
            catch
            {
                return Vec3.Zero;
            }
        }
    }

    [DataSourceProperty]
    public Vec2 ScreenPosition
    {
        get => _screenPosition;
        set
        {
            if (value.x != _screenPosition.x || value.y != _screenPosition.y)
            {
                _screenPosition = value;
                OnPropertyChangedWithValue(value, "ScreenPosition");
            }
        }
    }

    [DataSourceProperty]
    public int Distance
    {
        get => _distance;
        set
        {
            if (value != _distance)
            {
                _distance = value;
                OnPropertyChangedWithValue(value, "Distance");
                RefreshFontSize();
            }
        }
    }

    [DataSourceProperty]
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (value != _fontSize)
            {
                _fontSize = value;
                OnPropertyChangedWithValue(value, "FontSize");
            }
        }
    }

    [DataSourceProperty]
    public bool IsEnemy
    {
        get => _isEnemy;
        set
        {
            if (value != _isEnemy)
            {
                _isEnemy = value;
                OnPropertyChangedWithValue(value, "IsEnemy");
            }
        }
    }

    private float PhysicalDistance()
    {
        if (TargetAgent == null || Mission.Current == null || Mission.Current.MainAgent == null)
        {
            return float.MaxValue;
        }
        return TargetAgent.Position.Distance(Mission.Current.MainAgent.Position);
    }

    private void RefreshFontSize()
    {
        float distance = PhysicalDistance();
        if (distance > 7f)
        {
            FontSize = Math.Max(18, 25 - (int)((distance - 7f) / 3f));
        }
        else
        {
            FontSize = 25;
        }
    }
}

using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal sealed class RFPoliticalLabelsVM : ViewModel
{
    private bool _isVisible;

    [DataSourceProperty]
    public MBBindingList<RFPoliticalKingdomLabelVM> Labels { get; } = new();

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

    internal void Rebuild(RFPoliticalTerritoryMap map)
    {
        Labels.Clear();
        foreach (RFPoliticalLabelAnchor anchor in RFPoliticalLabelLayout.Build(map))
        {
            if (anchor.OwnerIndex < 0 || anchor.OwnerIndex >= map.Factions.Count
                || map.Factions[anchor.OwnerIndex].Faction is not Kingdom kingdom)
            {
                continue;
            }

            Campaign.Current.MapSceneWrapper.GetTerrainHeightAndNormal(anchor.Position, out float height, out Vec3 normal);
            Labels.Add(new RFPoliticalKingdomLabelVM(
                kingdom.Name.ToString(),
                new Vec3(anchor.Position, height + 2f, -1f),
                anchor.CellCount));
        }
    }

    internal void UpdateScreenPositions(Camera camera)
    {
        List<ScreenLabelRectangle> accepted = new();
        foreach (RFPoliticalKingdomLabelVM label in Labels)
        {
            float x = 0f;
            float y = 0f;
            float w = 0f;
            MBWindowManager.WorldToScreenInsideUsableArea(camera, label.WorldPosition, ref x, ref y, ref w);
            ScreenLabelRectangle rectangle = new(x - label.Width * 0.5f, y - 28f, label.Width, 56f);
            bool onScreen = w > 0f && x >= 0f && x <= TaleWorlds.Engine.Screen.RealScreenResolutionWidth
                && y >= 0f && y <= TaleWorlds.Engine.Screen.RealScreenResolutionHeight;
            bool collides = false;
            foreach (ScreenLabelRectangle other in accepted)
            {
                if (rectangle.Intersects(other))
                {
                    collides = true;
                    break;
                }
            }

            label.X = x - label.Width * 0.5f;
            label.Y = y - 28f;
            label.IsVisible = onScreen && !collides;
            if (label.IsVisible)
            {
                accepted.Add(rectangle);
            }
        }
    }

    public override void OnFinalize()
    {
        foreach (RFPoliticalKingdomLabelVM label in Labels)
        {
            label.OnFinalize();
        }
        Labels.Clear();
        base.OnFinalize();
    }

    private readonly struct ScreenLabelRectangle
    {
        private readonly float _x;
        private readonly float _y;
        private readonly float _width;
        private readonly float _height;

        internal ScreenLabelRectangle(float x, float y, float width, float height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        internal bool Intersects(ScreenLabelRectangle other)
        {
            return _x < other._x + other._width && _x + _width > other._x
                && _y < other._y + other._height && _y + _height > other._y;
        }
    }
}

internal sealed class RFPoliticalKingdomLabelVM : ViewModel
{
    private float _x;
    private float _y;
    private bool _isVisible;

    internal Vec3 WorldPosition { get; }
    internal int TerritorySize { get; }

    [DataSourceProperty]
    public string Name { get; }

    [DataSourceProperty]
    public float Width { get; }

    [DataSourceProperty]
    public float X
    {
        get => _x;
        set
        {
            if (Math.Abs(value - _x) > 0.1f)
            {
                _x = value;
                OnPropertyChangedWithValue(value, nameof(X));
            }
        }
    }

    [DataSourceProperty]
    public float Y
    {
        get => _y;
        set
        {
            if (Math.Abs(value - _y) > 0.1f)
            {
                _y = value;
                OnPropertyChangedWithValue(value, nameof(Y));
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

    internal RFPoliticalKingdomLabelVM(string name, Vec3 worldPosition, int territorySize)
    {
        Name = name;
        Width = Math.Min(520f, Math.Max(180f, name.Length * 18f));
        WorldPosition = worldPosition;
        TerritorySize = territorySize;
    }
}

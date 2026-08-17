using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal sealed class RFPoliticalTerritoryFaction
{
    internal IFaction Faction { get; }
    internal uint Color { get; }

    internal RFPoliticalTerritoryFaction(IFaction faction, uint color)
    {
        Faction = faction;
        Color = color;
    }
}

internal sealed class RFPoliticalTerritoryMap
{
    private const int FrontierScanRadius = 6;
    private readonly Dictionary<IFaction, int> _factionIndices;
    private readonly Dictionary<string, int> _sharedEdges = new(StringComparer.Ordinal);
    private int _largestSharedBorder;

    internal Vec2 Minimum { get; }
    internal Vec2 Maximum { get; }
    internal int Columns { get; }
    internal int Rows { get; }
    internal float CellWidth { get; }
    internal float CellHeight { get; }
    internal int[] Owners { get; }
    internal bool[] LandCells { get; }
    internal float[] CornerHeights { get; }
    internal IReadOnlyList<RFPoliticalTerritoryFaction> Factions { get; }

    internal RFPoliticalTerritoryMap(
        Vec2 minimum,
        Vec2 maximum,
        int columns,
        int rows,
        int[] owners,
        bool[] landCells,
        float[] cornerHeights,
        IReadOnlyList<RFPoliticalTerritoryFaction> factions,
        Dictionary<IFaction, int> factionIndices)
    {
        Minimum = minimum;
        Maximum = maximum;
        Columns = columns;
        Rows = rows;
        Owners = owners;
        LandCells = landCells;
        CornerHeights = cornerHeights;
        Factions = factions;
        _factionIndices = factionIndices;
        CellWidth = (maximum.x - minimum.x) / columns;
        CellHeight = (maximum.y - minimum.y) / rows;
        BuildAdjacencyIndex();
    }

    internal int CellIndex(int column, int row) => row * Columns + column;
    internal int CornerIndex(int column, int row) => row * (Columns + 1) + column;

    internal Vec2 CornerPosition(int column, int row)
    {
        return new Vec2(Minimum.x + column * CellWidth, Minimum.y + row * CellHeight);
    }

    internal float GetAdjacency(Kingdom first, Kingdom second)
    {
        if (!TryGetFactionIndex(first, out int firstIndex) || !TryGetFactionIndex(second, out int secondIndex))
        {
            return 0f;
        }

        if (!_sharedEdges.TryGetValue(PairKey(firstIndex, secondIndex), out int shared) || shared <= 0)
        {
            return 0f;
        }

        float relativeLength = shared / (float)Math.Max(1, _largestSharedBorder);
        return Math.Min(1f, 0.35f + relativeLength * 0.65f);
    }

    internal float GetFrontierWeight(Kingdom owner, Settlement settlement, Kingdom? specificEnemy)
    {
        if (!TryGetFactionIndex(owner, out int ownerIndex))
        {
            return 0f;
        }

        int specificEnemyIndex = -1;
        if (specificEnemy != null && !TryGetFactionIndex(specificEnemy, out specificEnemyIndex))
        {
            return 0f;
        }

        Vec2 position = settlement.GetPosition2D;
        int centerColumn = Clamp((int)Math.Floor((position.x - Minimum.x) / CellWidth), 0, Columns - 1);
        int centerRow = Clamp((int)Math.Floor((position.y - Minimum.y) / CellHeight), 0, Rows - 1);
        float best = 0f;

        for (int y = -FrontierScanRadius; y <= FrontierScanRadius; y++)
        {
            int row = centerRow + y;
            if (row < 0 || row >= Rows)
            {
                continue;
            }

            for (int x = -FrontierScanRadius; x <= FrontierScanRadius; x++)
            {
                int column = centerColumn + x;
                if (column < 0 || column >= Columns)
                {
                    continue;
                }

                int cellOwner = Owners[CellIndex(column, row)];
                if (cellOwner < 0 || cellOwner == ownerIndex || !IsRelevantEnemy(owner, cellOwner, specificEnemyIndex))
                {
                    continue;
                }

                float distance = (float)Math.Sqrt(x * x + y * y);
                best = Math.Max(best, 1f - distance / (FrontierScanRadius + 1f));
            }
        }

        return Math.Max(0f, Math.Min(1f, best));
    }

    private bool IsRelevantEnemy(Kingdom owner, int candidateIndex, int specificEnemyIndex)
    {
        if (specificEnemyIndex >= 0)
        {
            return candidateIndex == specificEnemyIndex;
        }

        IFaction candidate = Factions[candidateIndex].Faction;
        return candidate is Kingdom candidateKingdom && owner.IsAtWarWith(candidateKingdom);
    }

    private bool TryGetFactionIndex(IFaction faction, out int index)
    {
        index = -1;
        return faction != null && _factionIndices.TryGetValue(faction, out index);
    }

    private void BuildAdjacencyIndex()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                int owner = Owners[CellIndex(column, row)];
                if (owner < 0)
                {
                    continue;
                }

                if (column + 1 < Columns)
                {
                    CountSharedEdge(owner, Owners[CellIndex(column + 1, row)]);
                }

                if (row + 1 < Rows)
                {
                    CountSharedEdge(owner, Owners[CellIndex(column, row + 1)]);
                }
            }
        }
    }

    private void CountSharedEdge(int first, int second)
    {
        if (first < 0 || second < 0 || first == second)
        {
            return;
        }

        string key = PairKey(first, second);
        int count = _sharedEdges.TryGetValue(key, out int existing) ? existing + 1 : 1;
        _sharedEdges[key] = count;
        _largestSharedBorder = Math.Max(_largestSharedBorder, count);
    }

    private static string PairKey(int first, int second)
    {
        return first < second ? $"{first}:{second}" : $"{second}:{first}";
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Max(minimum, Math.Min(maximum, value));
    }
}

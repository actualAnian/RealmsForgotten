using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal sealed class RFPoliticalTerritoryBuilder
{
    private const int TargetCellCount = 24000;
    private const int MinimumAxisCells = 56;
    private const int MaximumAxisCells = 240;

    private static readonly (int X, int Y)[] Neighbours =
    {
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),            (1, 0),
        (-1, 1),  (0, 1),  (1, 1)
    };

    private readonly IMapScene _mapScene;

    internal RFPoliticalTerritoryBuilder(IMapScene mapScene)
    {
        _mapScene = mapScene;
    }

    internal static int ComputeOwnershipFingerprint()
    {
        unchecked
        {
            int fingerprint = 17;
            foreach (Settlement settlement in Settlement.All)
            {
                if (!IsTerritorySettlement(settlement))
                {
                    continue;
                }

                Kingdom? kingdom = ResolveTerritoryKingdom(settlement);
                fingerprint = fingerprint * 31 + RuntimeHelpers.GetHashCode(settlement);
                fingerprint = fingerprint * 31 + (kingdom != null ? RuntimeHelpers.GetHashCode(kingdom) : 0);
                fingerprint = fingerprint * 31 + (int)(kingdom != null ? ResolvePrimaryKingdomColor(kingdom) : 0u);
            }

            return fingerprint;
        }
    }

    internal RFPoliticalTerritoryMap Build()
    {
        Vec2 minimum = default;
        Vec2 maximum = default;
        float maximumHeight = 0f;
        _mapScene.GetMapBorders(out minimum, out maximum, out maximumHeight);

        float width = Math.Max(1f, maximum.x - minimum.x);
        float height = Math.Max(1f, maximum.y - minimum.y);
        float targetCellSize = (float)Math.Sqrt(width * height / TargetCellCount);
        int columns = Clamp((int)Math.Round(width / targetCellSize), MinimumAxisCells, MaximumAxisCells);
        int rows = Clamp((int)Math.Round(height / targetCellSize), MinimumAxisCells, MaximumAxisCells);
        float cellWidth = width / columns;
        float cellHeight = height / rows;
        int cellCount = columns * rows;

        int[] owners = new int[cellCount];
        bool[] land = new bool[cellCount];
        TerrainType[] terrain = new TerrainType[cellCount];
        float[] costs = new float[cellCount];
        for (int index = 0; index < cellCount; index++)
        {
            owners[index] = -1;
            costs[index] = float.MaxValue;
        }

        SampleTerrain(minimum, columns, rows, cellWidth, cellHeight, land, terrain);
        float[] cornerHeights = SampleCornerHeights(minimum, columns, rows, cellWidth, cellHeight);

        List<RFPoliticalTerritoryFaction> factions = new();
        Dictionary<IFaction, int> factionIndices = new();
        List<TerritorySeed> seeds = new();
        BinaryMinHeap heap = new();

        foreach (Settlement settlement in Settlement.All)
        {
            if (!IsTerritorySettlement(settlement))
            {
                continue;
            }

            Kingdom? kingdom = ResolveTerritoryKingdom(settlement);
            if (kingdom == null)
            {
                continue;
            }

            if (!factionIndices.TryGetValue(kingdom, out int ownerIndex))
            {
                ownerIndex = factions.Count;
                factionIndices.Add(kingdom, ownerIndex);
                factions.Add(new RFPoliticalTerritoryFaction(kingdom, ResolvePrimaryKingdomColor(kingdom)));
            }

            Vec2 position = settlement.GetPosition2D;
            int sourceColumn = Clamp((int)((position.x - minimum.x) / cellWidth), 0, columns - 1);
            int sourceRow = Clamp((int)((position.y - minimum.y) / cellHeight), 0, rows - 1);
            int cellIndex = FindNearestLandCell(sourceColumn, sourceRow, columns, rows, land);
            if (cellIndex < 0)
            {
                continue;
            }

            seeds.Add(new TerritorySeed(cellIndex, ownerIndex));
            float seedCost = -GetSettlementInfluence(settlement);
            if (seedCost < costs[cellIndex])
            {
                costs[cellIndex] = seedCost;
                owners[cellIndex] = ownerIndex;
                heap.Push(new HeapNode(cellIndex, ownerIndex, seedCost));
            }
        }

        SpreadTerritories(columns, rows, cellWidth, cellHeight, land, terrain, owners, costs, heap);
        FillUnassignedLand(columns, rows, cellWidth, cellHeight, land, owners, seeds);

        return new RFPoliticalTerritoryMap(
            minimum,
            maximum,
            columns,
            rows,
            owners,
            land,
            cornerHeights,
            factions,
            factionIndices);
    }

    private void SampleTerrain(
        Vec2 minimum,
        int columns,
        int rows,
        float cellWidth,
        float cellHeight,
        bool[] land,
        TerrainType[] terrain)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                Vec2 center = new(
                    minimum.x + (column + 0.5f) * cellWidth,
                    minimum.y + (row + 0.5f) * cellHeight);
                CampaignVec2 campaignPosition = new(center, true);
                TerrainType terrainType = _mapScene.GetTerrainTypeAtPosition(campaignPosition);
                terrain[index] = terrainType;
                land[index] = IsLand(terrainType);
            }
        }
    }

    private float[] SampleCornerHeights(Vec2 minimum, int columns, int rows, float cellWidth, float cellHeight)
    {
        float[] heights = new float[(columns + 1) * (rows + 1)];
        for (int row = 0; row <= rows; row++)
        {
            for (int column = 0; column <= columns; column++)
            {
                Vec2 position = new(minimum.x + column * cellWidth, minimum.y + row * cellHeight);
                _mapScene.GetTerrainHeightAndNormal(position, out float height, out Vec3 normal);
                heights[row * (columns + 1) + column] = height;
            }
        }

        return heights;
    }

    private static void SpreadTerritories(
        int columns,
        int rows,
        float cellWidth,
        float cellHeight,
        bool[] land,
        TerrainType[] terrain,
        int[] owners,
        float[] costs,
        BinaryMinHeap heap)
    {
        while (heap.Count > 0)
        {
            HeapNode current = heap.Pop();
            if (current.OwnerIndex != owners[current.CellIndex] || current.Cost > costs[current.CellIndex] + 0.0001f)
            {
                continue;
            }

            int column = current.CellIndex % columns;
            int row = current.CellIndex / columns;
            foreach ((int offsetX, int offsetY) in Neighbours)
            {
                int nextColumn = column + offsetX;
                int nextRow = row + offsetY;
                if (nextColumn < 0 || nextColumn >= columns || nextRow < 0 || nextRow >= rows)
                {
                    continue;
                }

                int nextIndex = nextRow * columns + nextColumn;
                if (!land[nextIndex])
                {
                    continue;
                }

                if (offsetX != 0 && offsetY != 0)
                {
                    int horizontalIndex = row * columns + nextColumn;
                    int verticalIndex = nextRow * columns + column;
                    if (!land[horizontalIndex] && !land[verticalIndex])
                    {
                        continue;
                    }
                }

                float dx = offsetX * cellWidth;
                float dy = offsetY * cellHeight;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                float terrainCost = (GetTerrainCost(terrain[current.CellIndex]) + GetTerrainCost(terrain[nextIndex])) * 0.5f;
                float nextCost = current.Cost + distance * terrainCost;
                bool improves = nextCost + 0.0001f < costs[nextIndex];
                bool stableTie = Math.Abs(nextCost - costs[nextIndex]) <= 0.0001f
                    && (owners[nextIndex] < 0 || current.OwnerIndex < owners[nextIndex]);
                if (!improves && !stableTie)
                {
                    continue;
                }

                costs[nextIndex] = nextCost;
                owners[nextIndex] = current.OwnerIndex;
                heap.Push(new HeapNode(nextIndex, current.OwnerIndex, nextCost));
            }
        }
    }

    private static void FillUnassignedLand(
        int columns,
        int rows,
        float cellWidth,
        float cellHeight,
        bool[] land,
        int[] owners,
        IReadOnlyList<TerritorySeed> seeds)
    {
        if (seeds.Count == 0)
        {
            return;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                if (!land[index] || owners[index] >= 0)
                {
                    continue;
                }

                float bestDistanceSquared = float.MaxValue;
                int bestOwner = -1;
                foreach (TerritorySeed seed in seeds)
                {
                    int seedColumn = seed.CellIndex % columns;
                    int seedRow = seed.CellIndex / columns;
                    float dx = (column - seedColumn) * cellWidth;
                    float dy = (row - seedRow) * cellHeight;
                    float distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestOwner = seed.OwnerIndex;
                    }
                }

                owners[index] = bestOwner;
            }
        }
    }

    private static int FindNearestLandCell(int sourceColumn, int sourceRow, int columns, int rows, bool[] land)
    {
        int sourceIndex = sourceRow * columns + sourceColumn;
        if (land[sourceIndex])
        {
            return sourceIndex;
        }

        for (int radius = 1; radius <= 6; radius++)
        {
            int bestIndex = -1;
            int bestDistanceSquared = int.MaxValue;
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (Math.Abs(x) != radius && Math.Abs(y) != radius)
                    {
                        continue;
                    }

                    int column = sourceColumn + x;
                    int row = sourceRow + y;
                    if (column < 0 || column >= columns || row < 0 || row >= rows)
                    {
                        continue;
                    }

                    int index = row * columns + column;
                    int distanceSquared = x * x + y * y;
                    if (land[index] && distanceSquared < bestDistanceSquared)
                    {
                        bestIndex = index;
                        bestDistanceSquared = distanceSquared;
                    }
                }
            }

            if (bestIndex >= 0)
            {
                return bestIndex;
            }
        }

        return -1;
    }

    private static Kingdom? ResolveTerritoryKingdom(Settlement settlement)
    {
        return settlement.OwnerClan?.Kingdom;
    }

    private static uint ResolvePrimaryKingdomColor(Kingdom kingdom)
    {
        if (kingdom.PrimaryBannerColor != 0u && kingdom.PrimaryBannerColor != uint.MaxValue)
        {
            return kingdom.PrimaryBannerColor;
        }

        uint bannerColor = kingdom.Banner?.GetPrimaryColor() ?? 0u;
        return bannerColor != 0u && bannerColor != uint.MaxValue ? bannerColor : kingdom.Color;
    }

    private static bool IsTerritorySettlement(Settlement settlement)
    {
        return settlement.IsTown || settlement.IsCastle || settlement.IsVillage;
    }

    private static float GetSettlementInfluence(Settlement settlement)
    {
        return settlement.IsTown ? 16f : settlement.IsCastle ? 11f : 4f;
    }

    private static bool IsLand(TerrainType terrainType)
    {
        int value = (int)terrainType;
        return value != 8 && value != 10 && value != 18 && value != 19 && value != 24;
    }

    private static float GetTerrainCost(TerrainType terrainType)
    {
        return (int)terrainType switch
        {
            3 => 1.2f,
            4 => 1.18f,
            7 => 2.25f,
            11 or 22 => 1.35f,
            13 => 2f,
            15 => 1.55f,
            21 => 2.8f,
            23 => 2.5f,
            _ => 1f
        };
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        return Math.Max(minimum, Math.Min(maximum, value));
    }

    private readonly struct TerritorySeed
    {
        internal int CellIndex { get; }
        internal int OwnerIndex { get; }

        internal TerritorySeed(int cellIndex, int ownerIndex)
        {
            CellIndex = cellIndex;
            OwnerIndex = ownerIndex;
        }
    }

    private readonly struct HeapNode
    {
        internal int CellIndex { get; }
        internal int OwnerIndex { get; }
        internal float Cost { get; }

        internal HeapNode(int cellIndex, int ownerIndex, float cost)
        {
            CellIndex = cellIndex;
            OwnerIndex = ownerIndex;
            Cost = cost;
        }
    }

    private sealed class BinaryMinHeap
    {
        private readonly List<HeapNode> _items = new();
        internal int Count => _items.Count;

        internal void Push(HeapNode node)
        {
            _items.Add(node);
            int index = _items.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_items[parent].Cost <= node.Cost)
                {
                    break;
                }

                _items[index] = _items[parent];
                index = parent;
            }

            _items[index] = node;
        }

        internal HeapNode Pop()
        {
            HeapNode result = _items[0];
            int lastIndex = _items.Count - 1;
            HeapNode last = _items[lastIndex];
            _items.RemoveAt(lastIndex);
            if (_items.Count == 0)
            {
                return result;
            }

            int index = 0;
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= _items.Count)
                {
                    break;
                }

                int right = left + 1;
                int smaller = right < _items.Count && _items[right].Cost < _items[left].Cost ? right : left;
                if (_items[smaller].Cost >= last.Cost)
                {
                    break;
                }

                _items[index] = _items[smaller];
                index = smaller;
            }

            _items[index] = last;
            return result;
        }
    }
}

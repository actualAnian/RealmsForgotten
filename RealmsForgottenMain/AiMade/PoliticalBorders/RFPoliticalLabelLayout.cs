using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal sealed class RFPoliticalLabelAnchor
{
    internal int OwnerIndex { get; }
    internal int CellCount { get; }
    internal Vec2 Position { get; }

    internal RFPoliticalLabelAnchor(int ownerIndex, int cellCount, Vec2 position)
    {
        OwnerIndex = ownerIndex;
        CellCount = cellCount;
        Position = position;
    }
}

internal static class RFPoliticalLabelLayout
{
    private const int MinimumSecondaryComponentCells = 12;
    private const float SecondaryComponentRatio = 0.15f;
    private static readonly (int X, int Y)[] Neighbours = { (1, 0), (-1, 0), (0, 1), (0, -1) };

    internal static IReadOnlyList<RFPoliticalLabelAnchor> Build(RFPoliticalTerritoryMap map)
    {
        List<RFPoliticalLabelAnchor> result = new();
        bool[] visited = new bool[map.Owners.Length];
        Dictionary<int, List<List<int>>> componentsByOwner = new();

        for (int row = 0; row < map.Rows; row++)
        {
            for (int column = 0; column < map.Columns; column++)
            {
                int index = map.CellIndex(column, row);
                int owner = map.Owners[index];
                if (visited[index] || owner < 0 || !map.LandCells[index])
                {
                    continue;
                }

                List<int> component = FloodFill(map, column, row, owner, visited);
                if (!componentsByOwner.TryGetValue(owner, out List<List<int>>? components))
                {
                    components = new List<List<int>>();
                    componentsByOwner.Add(owner, components);
                }
                components.Add(component);
            }
        }

        foreach (KeyValuePair<int, List<List<int>>> pair in componentsByOwner)
        {
            pair.Value.Sort((left, right) => right.Count.CompareTo(left.Count));
            int largest = pair.Value[0].Count;
            foreach (List<int> component in pair.Value)
            {
                if (component != pair.Value[0]
                    && (component.Count < MinimumSecondaryComponentCells || component.Count < largest * SecondaryComponentRatio))
                {
                    continue;
                }

                result.Add(new RFPoliticalLabelAnchor(pair.Key, component.Count, FindInteriorAnchor(map, component, pair.Key)));
            }
        }

        result.Sort((left, right) => right.CellCount.CompareTo(left.CellCount));
        return result;
    }

    private static List<int> FloodFill(RFPoliticalTerritoryMap map, int startColumn, int startRow, int owner, bool[] visited)
    {
        List<int> cells = new();
        Queue<(int X, int Y)> queue = new();
        queue.Enqueue((startColumn, startRow));
        visited[map.CellIndex(startColumn, startRow)] = true;

        while (queue.Count > 0)
        {
            (int column, int row) = queue.Dequeue();
            cells.Add(map.CellIndex(column, row));
            foreach ((int offsetX, int offsetY) in Neighbours)
            {
                int nextColumn = column + offsetX;
                int nextRow = row + offsetY;
                if (nextColumn < 0 || nextColumn >= map.Columns || nextRow < 0 || nextRow >= map.Rows)
                {
                    continue;
                }

                int nextIndex = map.CellIndex(nextColumn, nextRow);
                if (!visited[nextIndex] && map.LandCells[nextIndex] && map.Owners[nextIndex] == owner)
                {
                    visited[nextIndex] = true;
                    queue.Enqueue((nextColumn, nextRow));
                }
            }
        }

        return cells;
    }

    private static Vec2 FindInteriorAnchor(RFPoliticalTerritoryMap map, List<int> component, int owner)
    {
        HashSet<int> componentSet = new(component);
        Queue<int> queue = new();
        Dictionary<int, int> distance = new();

        foreach (int index in component)
        {
            int column = index % map.Columns;
            int row = index / map.Columns;
            if (IsBoundaryCell(map, componentSet, owner, column, row))
            {
                distance[index] = 0;
                queue.Enqueue(index);
            }
        }

        int bestIndex = component[0];
        int bestDistance = -1;
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int currentDistance = distance[index];
            if (currentDistance > bestDistance)
            {
                bestDistance = currentDistance;
                bestIndex = index;
            }

            int column = index % map.Columns;
            int row = index / map.Columns;
            foreach ((int offsetX, int offsetY) in Neighbours)
            {
                int nextColumn = column + offsetX;
                int nextRow = row + offsetY;
                if (nextColumn < 0 || nextColumn >= map.Columns || nextRow < 0 || nextRow >= map.Rows)
                {
                    continue;
                }

                int nextIndex = map.CellIndex(nextColumn, nextRow);
                if (componentSet.Contains(nextIndex) && !distance.ContainsKey(nextIndex))
                {
                    distance[nextIndex] = currentDistance + 1;
                    queue.Enqueue(nextIndex);
                }
            }
        }

        int bestColumn = bestIndex % map.Columns;
        int bestRow = bestIndex / map.Columns;
        return new Vec2(
            map.Minimum.x + (bestColumn + 0.5f) * map.CellWidth,
            map.Minimum.y + (bestRow + 0.5f) * map.CellHeight);
    }

    private static bool IsBoundaryCell(RFPoliticalTerritoryMap map, HashSet<int> component, int owner, int column, int row)
    {
        foreach ((int offsetX, int offsetY) in Neighbours)
        {
            int nextColumn = column + offsetX;
            int nextRow = row + offsetY;
            if (nextColumn < 0 || nextColumn >= map.Columns || nextRow < 0 || nextRow >= map.Rows)
            {
                return true;
            }

            int nextIndex = map.CellIndex(nextColumn, nextRow);
            if (!component.Contains(nextIndex) || !map.LandCells[nextIndex] || map.Owners[nextIndex] != owner)
            {
                return true;
            }
        }
        return false;
    }
}

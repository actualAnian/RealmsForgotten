using System.Collections.Generic;
using TaleWorlds.Library;

namespace RealmsForgotten.Alchemy
{
    public class SpatialGrid<T>
    {
        private readonly float _cellSize;
        private readonly Dictionary<(int X, int Y), List<T>> _cells = new();

        public SpatialGrid(float cellSize)
        {
            _cellSize = cellSize;
        }

        public void Clear()
        {
            _cells.Clear();
        }

        private (int X, int Y) GetCell(Vec2 position)
        {
            return (MathF.Floor(position.X / _cellSize), MathF.Floor(position.Y / _cellSize));
        }

        public void Add(Vec2 position, T item)
        {
            var cell = GetCell(position);

            if (!_cells.TryGetValue(cell, out var list))
            {
                list = new List<T>();
                _cells[cell] = list;
            }

            list.Add(item);
        }

        //public IEnumerable<T> QueryCircle(Vec2 center, float radius)
        //{
        //    var minX = MathF.Floor((center.X - radius) / _cellSize);
        //    var maxX = MathF.Floor((center.X + radius) / _cellSize);
        //    var minY = MathF.Floor((center.Y - radius) / _cellSize);
        //    var maxY = MathF.Floor((center.Y + radius) / _cellSize);

        //    for (int x = minX; x <= maxX; x++)
        //    {
        //        for (int y = minY; y <= maxY; y++)
        //        {
        //            if (_cells.TryGetValue((x, y), out var list))
        //            {
        //                foreach (var item in list)
        //                    yield return item;
        //            }
        //        }
        //    }
        //}
        public IEnumerable<T> QueryBox(RFBoundingBox box)
        {
            for (int x = MathF.Floor(box.MinX); x <= box.MaxX; x++)
            {
                for (int y = MathF.Floor(box.MinY); y <= box.MaxY; y++)
                {
                    if (_cells.TryGetValue((x, y), out var list))
                    {
                        foreach (var item in list)
                            yield return item;
                    }
                }
            }
        }
    }
}

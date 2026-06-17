using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.UI.FloatingText
{
    public class FloatingTextManager
    {
        static FloatingTextManager? _instace;
        public static FloatingTextManager Instance { get { return _instace ??= new FloatingTextManager(); } }
        private readonly Dictionary<int, FloatingTextEntry> _entries = new();

        public int AddText(string text, Func<Vec3> positionProvider, Color color)
        {
            int nextKey = _entries.Count > 0 ? _entries.Last().Key + 1 : 0;
            _entries[nextKey] = new FloatingTextEntry(nextKey, text, positionProvider, color);
            return nextKey;
        }
        public void Remove(int id)
        {
            _entries.Remove(id);
        }

        internal bool Contains(int id)
        {
            return _entries.ContainsKey(id);
        }

        public IReadOnlyDictionary<int, FloatingTextEntry> Entries => _entries;
    }
    public record FloatingTextEntry(int Id, string Text, Func<Vec3> PositionProvider, Color Color) { }
}

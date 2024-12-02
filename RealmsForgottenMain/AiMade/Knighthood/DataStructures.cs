using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.Knighthood
{  
    public class BiDirectionalMap<TOne, TMany> where TOne : MBObjectBase where TMany : MBObjectBase
        {
            private readonly Dictionary<TMany, TOne> _manyToOne = new();
            private readonly Dictionary<TOne, HashSet<TMany>> _oneToMany = new();

            public bool Add(TOne one, TMany many)
            {
                if (!_oneToMany.ContainsKey(one)) _oneToMany[one] = new HashSet<TMany>();

                if (_manyToOne.ContainsKey(many))
                    throw new ArgumentException("The value has already been associated with another key.");

                _manyToOne[many] = one;
                return _oneToMany[one].Add(many);
            }

            public bool Add(TOne one)
            {
                if (_oneToMany.ContainsKey(one)) return false;

                _oneToMany[one] = new HashSet<TMany>();
                return true;
            }

            public IReadOnlyCollection<TMany>? GetMany(TOne one)
            {
                _ = _oneToMany.TryGetValue(one, out var manySet);
                return manySet;
            }

            public TOne? GetOne(TMany many)
            {
                _ = _manyToOne.TryGetValue(many, out var one);
                return one;
            }

            public bool Remove(TOne one, TMany many)
            {
                return _oneToMany.TryGetValue(one, out var manySet) && manySet.Remove(many) && _manyToOne.Remove(many);
            }

            public bool RemoveOne(TOne one)
            {
                if (!_oneToMany.TryGetValue(one, out var manySet)) return false;

                foreach (var many in manySet) _ = _manyToOne.Remove(many);

                return _oneToMany.Remove(one);
            }

            public bool RemoveMany(TMany many)
            {
                return _manyToOne.TryGetValue(many, out var one) &&
                       _oneToMany.TryGetValue(one, out var manySet) &&
                       _manyToOne.Remove(many) &&
                       manySet.Remove(many);
            }

            public bool ContainsOne(TOne one) { return _oneToMany.ContainsKey(one); }

            public bool ContainsMany(TMany many) { return _manyToOne.ContainsKey(many); }

            public void SyncData(IDataStore dataStore, string name)
            {
                if (dataStore.IsSaving)
                {
                    Dictionary<TMany, TOne> data = new();
                    foreach (var many in _manyToOne.Keys)
                    {
                        var one = GetOne(many);
                        if (one == null) continue;

                        data.Add(many, one);
                    }

                    if (dataStore.SyncData(name, ref data))
                        Logger.Trace($"Saved {name} successfully with {data.Count} entries");
                    else
                        Logger.Error($"Failed to save {name}");
                }
                else if (dataStore.IsLoading)
                {
                    Dictionary<TMany, TOne>? data = null;
                    if (dataStore.SyncData(name, ref data) && data != null)
                    {
                        _manyToOne.Clear();
                        _oneToMany.Clear();
                        foreach (var kv in data) _ = Add(kv.Value, kv.Key);
                        Logger.Trace($"Loaded {name} successfully with {data.Count} entries");
                    }
                    else
                    {
                        Logger.Error($"Failed to load {name}");
                    }
                }
            }
        }
    }

using psai.net;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MusicSounds
{
    public enum RFMusicTheme
    {
        WitchFight = 41,
        stealth = 42,
        action = 43
    }
    public static class RFMusicData
    {
        public static Dictionary<RFMusicType, List<int>> MusicTypeToIDsMap = new()
        {
            [RFMusicType.StandardBattle] = new() 
            { 
                (int)MusicTheme.BattleMedium, 
                (int)MusicTheme.BattleSmall, 
                (int)MusicTheme.BattlePaganA,
                (int)MusicTheme.BattlePaganB,
                (int)RFMusicTheme.action
            },
            [RFMusicType.Stealth] = new() 
            {
                //(int)RFMusicTheme.WitchFight
                (int)MusicTheme.StealthA,
                (int)RFMusicTheme.stealth
               

            }
        };
    }
    public enum RFMusicType
    {
        StandardBattle,
        Stealth
    }

    public class RFMusicManager
    {
        private static readonly Lazy<RFMusicManager> _instance = new(() => new RFMusicManager());
        public static RFMusicManager Instance => _instance.Value;

        private readonly object _sync = new();
        private readonly List<MusicRequest> _requests = new();
        private static readonly Random _random = new();

        private RFMusicManager() { }
        private sealed class MusicRequest
        {
            public int? TrackId { get; }
            public RFMusicType? Type { get; }
            public int Weight { get; }

            public MusicRequest(int trackId, int weight)
            {
                TrackId = trackId;
                Type = null;
                Weight = weight;
            }

            public MusicRequest(RFMusicType type, int weight)
            {
                TrackId = null;
                Type = type;
                Weight = weight;
            }
        }
        public void AddRequest(RFMusicType type, int weight)
        {
            if (weight <= 0) return;
            lock (_sync)
            {
                _requests.Add(new MusicRequest(type, weight));
            }
        }

        public void AddRequest(int trackId, int weight)
        {
            if (weight <= 0) return;
            lock (_sync)
            {
                _requests.Add(new MusicRequest(trackId, weight));
            }
        }
        public void ClearRequests()
        {
            lock (_sync)
            {
                _requests.Clear();
            }
        }
        public bool PlayMusic()
        {
            bool value = PlayHighestWeight();
            _requests.Clear();
            return value;
        }
        private bool PlayHighestWeight()
        {
            MusicRequest? chosen = null;
            lock (_sync)
            {
                if (_requests.Count == 0) return false;

                int maxWeight = _requests.Max(r => r.Weight);
                var top = _requests.Where(r => r.Weight == maxWeight).ToList();
                chosen = top.Count == 1 ? top[0] : top[_random.Next(top.Count)];
                _requests.Clear();
            }
            if (chosen == null) return false;
            int trackToPlay = 0;
            if (chosen.TrackId.HasValue)
                trackToPlay = chosen.TrackId.Value;
            else
            {
                RFMusicData.MusicTypeToIDsMap.TryGetValue(chosen.Type!.Value, out var ids);
                if (ids != null && ids.Count > 0)
                {
                    trackToPlay = ids[_random.Next(ids.Count)];
                }
            }
            PsaiCore.Instance.TriggerMusicTheme(trackToPlay, 1);
            return true;
        }
        public void StopMusicWithFadeout(float fadeoutTime = 3f)
        {
            PsaiCore.Instance.StopMusic(true, fadeoutTime);
        }
    }
}

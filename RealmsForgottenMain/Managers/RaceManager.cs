using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace RealmsForgotten.Managers;

public class RaceManager
{
    static RaceManager? _instance;
    public static RaceManager Instance
    {
        get
        {
            _instance ??= new RaceManager();
            return _instance;
        }
    }
    private readonly Dictionary<int, string> _idToName;
    private readonly Dictionary<string, int> _nameToId;

    public RaceManager()
    {
        (_idToName, _nameToId) = InitializeRaceMappings();
    }

    private (Dictionary<int, string>, Dictionary<string, int>) InitializeRaceMappings()
    {
        var idToName = new Dictionary<int, string>();
        var nameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var raceNames = TaleWorlds.Core.FaceGen.GetRaceNames();
            if (raceNames != null)
            {
                for (var i = 0; i < raceNames.Length; i++)
                {
                    var raceName = raceNames[i];
                    idToName[i] = raceName;
                    nameToId[raceName] = i;
                }
            }
            else
            {
                InformationManager.AddSystemNotification("FaceGen.GetRaceNames() returned null, using fallback mapping");
                idToName[0] = "human";
                nameToId["human"] = 0;
            }
        }
        catch (Exception ex)
        {
            InformationManager.AddSystemNotification($"Error initializing race mappings: {ex.Message}");
            idToName[0] = "human";
            nameToId["human"] = 0;
        }
        return (idToName, nameToId);
    }
    public List<int> GetAllRaceIds()
    {
        return new List<int>(_idToName.Keys);
    }
    public List<string> GetAllRaceNames()
    {
        return new List<string>(_idToName.Values);
    }
    public bool IsValidRaceName(string name)
    {
        return !string.IsNullOrEmpty(name) && _nameToId.ContainsKey(name);
    }
    public bool IsValidRaceId(int id)
    {
        return _idToName.ContainsKey(id);
    }
    public int GetRaceIdFromName(string name)
    {
        if (!_nameToId.TryGetValue(name, out var id))
            return 0;
        return id;
    }

    public string GetRaceNameFromId(int id)
    {
        if (_idToName.TryGetValue(id, out var name)) return name;
        return _idToName.TryGetValue(0, out var value) ? value : "human";
    }
}
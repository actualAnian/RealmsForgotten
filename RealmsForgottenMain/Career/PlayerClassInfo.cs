using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career
{
    public class PlayerClassInfo
    {
        [SaveableField(0)] public string CareerID = string.Empty;
        [SaveableField(1)] public List<string> CareerChoices = new();
        [SaveableField(2)] public bool IsAbilityActive = false;
        [SaveableField(3)] public bool IsAbilityUpgraded = false;
    }
}
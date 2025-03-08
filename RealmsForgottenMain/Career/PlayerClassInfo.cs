using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Career
{
    public class PlayerClassInfo
    {
        [SaveableField(0)] public string CareerID = string.Empty;
        [SaveableField(1)] public List<string> CareerChoices = new();
    }
}
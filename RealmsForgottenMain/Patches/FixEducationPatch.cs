using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace RealmsForgotten.Patches;

[HarmonyPatch(typeof(EducationCampaignBehavior), "CreateStage2")]
public static class FixEducationPatch
{
    public static void Prefix(Hero child)
    {
        Attributes.All.Remove(RFAttribute.Discipline);
    }
    
    public static void Postfix(Hero child)
    {
        Attributes.All.Add(RFAttribute.Discipline);
    }
}
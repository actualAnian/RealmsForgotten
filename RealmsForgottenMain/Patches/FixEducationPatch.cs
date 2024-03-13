using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MonoMod.Utils;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.Education;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace RealmsForgotten.Patches;

[HarmonyPatch(typeof(EducationCampaignBehavior), "CreateStage2")]
public static class FixEducationPatch
{
    public static void Prefix(Hero child)
    {
        Attributes.All.Remove(RFAttributes.Discipline);
    }
    
    public static void Postfix(Hero child)
    {
        Attributes.All.Add(RFAttributes.Discipline);
    }
}
[HarmonyPatch(typeof(EducationGainedPropertiesVM), "PopulateInitialValues")]
public static class FixGetItemFromAttribute
{
    public static void Postfix(Dictionary<CharacterAttribute, Tuple<int, int>> ____affectedAttributesMap,
        Dictionary<SkillObject, Tuple<int, int>> ____affectedSkillFocusMap,
        Dictionary<SkillObject, Tuple<int, int>> ____affectedSkillValueMap)
    {
        ____affectedAttributesMap.Remove(RFAttributes.Discipline);
        var toRemove1 = ____affectedSkillFocusMap.Where(x => x.Key.StringId == "faith" ||
                                                             x.Key.StringId == "arcane" ||
                                                             x.Key.StringId == "alchemy").Select(x=>x.Key);

        for (int i = ____affectedSkillFocusMap.Count - 1; i >= 0; i--)
        {
            if (____affectedSkillFocusMap.ElementAt(i).Key.StringId == "arcane" ||
                ____affectedSkillFocusMap.ElementAt(i).Key.StringId == "alchemy" ||
                ____affectedSkillFocusMap.ElementAt(i).Key.StringId == "faith") ;
                ____affectedSkillFocusMap.Remove(____affectedSkillFocusMap.ElementAt(i).Key);
        }
        
        for (int i = ____affectedSkillValueMap.Count - 1; i >= 0; i--)
        {
            if (____affectedSkillValueMap.ElementAt(i).Key.StringId == "arcane" ||
                ____affectedSkillValueMap.ElementAt(i).Key.StringId == "alchemy" ||
                ____affectedSkillValueMap.ElementAt(i).Key.StringId == "faith") ;
            ____affectedSkillValueMap.Remove(____affectedSkillValueMap.ElementAt(i).Key);
        }

    }
}

[HarmonyPatch(typeof(Skills), "All", MethodType.Getter)]
public static class FixPickAll
{
    public static void Postfix(MBReadOnlyList<SkillObject> __result)
    {
        string callerClass = new StackFrame(2).GetType().Name;
        if (callerClass.Contains("Education"))
        {
            __result.RemoveAll(x => x.StringId == "faith" ||
                                    x.StringId == "arcane" ||
                                    x.StringId == "alchemy");
        }
    }
}

[HarmonyPatch(typeof(Attributes), "All", MethodType.Getter)]
public static class FixPickAll2
{
    public static void Postfix(MBReadOnlyList<CharacterAttribute> __result)
    {
        bool result = false;
        for (int i = 0; i < 5; i++)
        {
            if (new StackFrame(i).GetMethod().GetType().Name.Contains("Education") ||
                new StackFrame(i).GetMethod().GetRealDeclaringType().Name.Contains("Education"))
                result = true;
        }
        if (result)
        {
            __result.Remove(RFAttributes.Discipline);
        }
    }
}
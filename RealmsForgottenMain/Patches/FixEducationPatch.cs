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

[HarmonyPatch(typeof(Skills), "All", MethodType.Getter)]
public static class FixPickAll
{
    public static void Postfix(MBReadOnlyList<SkillObject> __result)
    {
        bool result = false;
        for (int i = 0; i < 5; i++)
        {
            if (new StackFrame(i).GetMethod()?.GetType()?.Name?.Contains("Education") == true ||
                new StackFrame(i).GetMethod()?.GetRealDeclaringType()?.Name?.Contains("Education") == true)
            {
                result = true;
            }
        }
        if (result)
        {
            __result.Remove(RFSkills.Faith);
            __result.Remove(RFSkills.Alchemy);
            __result.Remove(RFSkills.Arcane);
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
            if (new StackFrame(i).GetMethod()?.GetType()?.Name?.Contains("Education") == true ||
                new StackFrame(i).GetMethod()?.GetRealDeclaringType()?.Name?.Contains("Education") == true)
                result = true;
        }
        if (result)
        {
            __result.Remove(RFAttributes.Discipline);
        }
    }
}
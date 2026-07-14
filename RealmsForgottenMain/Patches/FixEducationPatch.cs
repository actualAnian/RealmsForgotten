using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
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
    public static void Postfix(ref MBReadOnlyList<SkillObject> __result)
    {
        bool result = false;
        for (int i = 0; i < 5; i++)
        {
            MethodBase method = new StackFrame(i).GetMethod();
            if (method?.DeclaringType?.Name?.Contains("Education") == true ||
                method?.ReflectedType?.Name?.Contains("Education") == true)
            {
                result = true;
            }
        }
        if (result)
        {
            // Return a FILTERED COPY — Skills.All hands back the game's live
            // registered-objects list; Remove() on it would delete Faith/
            // Alchemy/Arcane from every consumer for the rest of the session.
            __result = new MBReadOnlyList<SkillObject>(__result.Where(s =>
                s != RFSkills.Faith && s != RFSkills.Alchemy && s != RFSkills.Arcane));
        }
    }
}

[HarmonyPatch(typeof(Attributes), "All", MethodType.Getter)]
public static class FixPickAll2
{
    public static void Postfix(ref MBReadOnlyList<CharacterAttribute> __result)
    {
        bool result = false;
        for (int i = 0; i < 5; i++)
        {
            MethodBase method = new StackFrame(i).GetMethod();
            if (method?.DeclaringType?.Name?.Contains("Education") == true ||
                method?.ReflectedType?.Name?.Contains("Education") == true)
                result = true;
        }
        if (result)
        {
            // Filtered copy — never mutate the live Attributes.All list.
            __result = new MBReadOnlyList<CharacterAttribute>(__result.Where(a =>
                a != RFAttributes.Discipline && a != RFAttributes.Seafaring));
        }
    }
}

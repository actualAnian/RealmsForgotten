using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches;

[HarmonyPatch(typeof(Skills), "All", MethodType.Getter)]
public static class FixPickAll
{
    public static void Postfix(MBReadOnlyList<SkillObject> __result)
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
            MethodBase method = new StackFrame(i).GetMethod();
            if (method?.DeclaringType?.Name?.Contains("Education") == true ||
                method?.ReflectedType?.Name?.Contains("Education") == true)
                result = true;
        }
        if (result)
        {
            __result.Remove(RFAttributes.Discipline);
        }
    }
}

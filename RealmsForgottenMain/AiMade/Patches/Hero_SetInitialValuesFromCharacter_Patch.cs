using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;


namespace RealmsForgotten.AiMade.Patches
{
    /// <summary>
    /// Finalizer patch for HeroCreator.CreateNewHero(CharacterObject template, int age).
    /// Swallows NullReferenceException so the game doesn't crash.
    /// </summary>
    [HarmonyPatch]
    public static class HeroCreator_CreateNewHero_Finalizer
    {
        static Type TargetType()
        {
            // same approach
            var asm = typeof(Hero).Assembly;
            var heroCreatorType = asm.GetType("TaleWorlds.CampaignSystem.HeroCreator");
            if (heroCreatorType == null)
                throw new Exception("HeroCreator type not found in TaleWorlds.CampaignSystem.");

            return heroCreatorType;
        }

        static MethodBase TargetMethod()
        {
            var t = TargetType();
            var methods = AccessTools.GetDeclaredMethods(t)
                .Where(m => m.Name == "CreateNewHero")
                .ToList();

            // We'll match the 2-parameter version: (CharacterObject, int)
            var method = methods.FirstOrDefault(m =>
            {
                var p = m.GetParameters();
                return p.Length == 2
                    && p[0].ParameterType == typeof(CharacterObject)
                    && p[1].ParameterType == typeof(int);
            });
            if (method == null)
                throw new Exception("CreateNewHero(CharacterObject,int) not found in HeroCreator.");

            return method;
        }

        [HarmonyFinalizer]
        static Exception Finalizer(
            Exception __exception,
            CharacterObject template,
            int age,
            ref Hero __result
        )
        {
            if (__exception == null)
                return null;

            if (!(__exception is NullReferenceException))
                return __exception;

            InformationManager.DisplayMessage(new InformationMessage(
                "[RF] Warning: NullRef in CreateNewHero. Possibly settlement or template was null. Aborting creation."
            ));

            __result = null; // no hero
            return null;      // swallow crash
        }
    }
}
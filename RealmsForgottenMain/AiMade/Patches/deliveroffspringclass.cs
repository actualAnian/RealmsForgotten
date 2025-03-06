using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(HeroCreator), "DeliverOffSpring")]
    internal class DeliverOffspringPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Hero mother, Hero father, ref (int, int) __state)
        {
            __state.Item1 = ((BasicCharacterObject)mother.CharacterObject).Race;
            __state.Item2 = ((BasicCharacterObject)father.CharacterObject).Race;
            ((BasicCharacterObject)mother.CharacterObject).Race = 0;
            ((BasicCharacterObject)father.CharacterObject).Race = 0;
        }

        [HarmonyPostfix]
        private static void Postfix(Hero mother, Hero father, Hero __result, ref (int, int) __state)
        {
            ((BasicCharacterObject)mother.CharacterObject).Race = __state.Item1;
            ((BasicCharacterObject)father.CharacterObject).Race = __state.Item2;
            CharacterRacialMix.CreateForNewborn(__result);
        }
    }
}
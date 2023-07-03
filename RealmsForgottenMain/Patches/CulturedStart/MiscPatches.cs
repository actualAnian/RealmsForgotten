using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;

//using StoryMode.CharacterCreationContent;
using RealmsForgotten.Managers;
using RealmsForgotten.Utility.Utility;

namespace RealmsForgotten.Patches.CulturedStart
{
    public class MiscPatches
    {
        //private static readonly AccessTools.StructFieldRef<BodyProperties, StaticBodyProperties> StaticBodyProps = AccessTools.StructFieldRefAccess<BodyProperties, StaticBodyProperties>("_staticBodyProperties");

        [HarmonyPatch(typeof(SandboxCharacterCreationContent), "OnCultureSelected")]
        public class SandboxCharacterCreationContentRefreshPropsAndClothing
        {
            public static void Postfix()
            {
                CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
                playerCharacter.UpdatePlayerCharacterBodyProperties(Helper.GenerateCultureBodyProperties(playerCharacter.Culture.StringId), playerCharacter.Race, playerCharacter.IsFemale);
            }
        }

        // This class does not contain any actual patches. Copied from original version so I left it in.
        [HarmonyPatch]
        public class CSPatchCharacterCreationInitialized
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(SandboxCharacterCreationContent), "OnInitialized", null, null);
                yield return AccessTools.Method(typeof(StoryModeCharacterCreationContent), "OnInitialized", null, null);
                yield return AccessTools.Method(typeof(RFCharacterCreationContent), "OnInitialized", null, null);
                yield break;
            }
        }

        [HarmonyPatch]
        public class CSPatchCharacterCreationFinalized
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(SandboxCharacterCreationContent), "OnCharacterCreationFinalized", null, null);
                yield return AccessTools.Method(typeof(StoryModeCharacterCreationContent), "OnCharacterCreationFinalized", null, null);
                yield return AccessTools.Method(typeof(RFCharacterCreationContent), "OnCharacterCreationFinalized", null, null);
                yield break;
            }

            public static void Postfix() => CulturedStartAction.Apply(CulturedStartManager.Current.StoryOption, CulturedStartManager.Current.LocationOption);
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using RealmsForgotten.Managers;
using RealmsForgotten.Utility;
using Helpers;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Settlements;
using System.Linq;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace RealmsForgotten.Patches.CulturedStart
{
#pragma warning disable IDE0051 // Remove unused private members
    public class MiscPatches
    {
        [HarmonyPatch(typeof(FactionHelper), "GenerateClanNameforPlayer")]
        internal class FactionHelperPatches
        {
            public static void Postfix(ref TextObject __result)
            {
                CultureObject playerCulture = Hero.MainHero.Culture;
                var newSettlement = from settlement in Settlement.All where settlement.StringId == "town_V1" select settlement;
                if (playerCulture.StringId == "vlandia")
                    __result = NameGenerator.Current.GenerateClanName(playerCulture, newSettlement.ElementAt(0));
            }
        }
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
                yield return AccessTools.Method("TaleWorlds.CampaignSystem.CharacterCreationContent.SandboxCharacterCreationContent:OnInitialized", null, null);
                yield return AccessTools.Method("StoryMode.CharacterCreationContent.StoryModeCharacterCreationContent:OnInitialized", null, null);
                yield return AccessTools.Method("RealmsForgotten.RFCharacterCreationContent:OnInitialized", null, null);
                yield break;
            }
        }
        [HarmonyPatch]
        public class CSPatchCharacterCreationFinalized
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method("TaleWorlds.CampaignSystem.CharacterCreationContent.SandboxCharacterCreationContent:OnCharacterCreationFinalized", null, null);
                yield return AccessTools.Method("StoryMode.CharacterCreationContent.StoryModeCharacterCreationContent:OnCharacterCreationFinalized", null, null);
                yield return AccessTools.Method("RealmsForgotten.RFCharacterCreationContent:OnCharacterCreationFinalized", null, null);
                yield break;
            }
            public static void Postfix() => CulturedStartAction.Apply(CulturedStartManager.Current.StoryOption, CulturedStartManager.Current.LocationOption);
        }
        [HarmonyPatch(typeof(FaceGenVM), "Refresh")]
        public class FaceGenVMRefreshPatch
        {
            public static void Postfix(FaceGenVM __instance)
            {
                if (__instance.RaceSelector != null && __instance.RaceSelector.ItemList != null)
                {
                    if (__instance.RaceSelector.ItemList.Count() == Globals.PlayerSelectableRaces.Count) return;
                    var realRaceList = from item in __instance.RaceSelector.ItemList where Globals.PlayerSelectableRaces.Contains(item.StringItem) select item;
                    TaleWorlds.Library.MBBindingList<SelectorItemVM> list = new();
                    foreach ( var item in realRaceList ) { list.Add(item); }
                    __instance.RaceSelector.ItemList = list;
                }

            }
        }
    }
#pragma warning restore IDE0051 // Remove unused private members
}

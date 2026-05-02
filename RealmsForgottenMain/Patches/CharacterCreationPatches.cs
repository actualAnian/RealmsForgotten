using HarmonyLib;
using TaleWorlds.CampaignSystem;
using Helpers;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Settlements;
using System.Linq;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace RealmsForgotten.Patches
{
#pragma warning disable IDE0051 // Remove unused private members
    public class CharacterCreationPatches
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

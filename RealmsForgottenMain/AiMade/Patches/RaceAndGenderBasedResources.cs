using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using RealmsForgotten.AiMade.Patches;

namespace RealmsForgotten.AiMade.Patches
{
    public static class FaceGenPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FaceGenVM), "UpdateRaceAndGenderBasedResources")]
        public static void ReplaceImages(FaceGenVM __instance)
        {
            int selectedRace = __instance.RaceSelector == null ? 0 : __instance.RaceSelector.SelectedIndex;

            foreach (var item in __instance.BeardTypes)
            {
                string name = FaceGenHelper.GetBeardName(item.Index, selectedRace, __instance.SelectedGender);
                if (!string.IsNullOrEmpty(name))
                {
                    item.ImagePath = "FaceGen\\Beard\\" + name;
                }
            }
        }
    }
}
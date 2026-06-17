using HarmonyLib;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TaleWorlds.Core;

namespace RealmsForgotten.AiMade.Patches
{
    [HarmonyPatch(typeof(FaceGenVM), "UpdateRaceAndGenderBasedResources")]
    public static class FaceGenPatch
    {
        private static int defaultBeardsNum = 19;
        static void Postfix(FaceGenVM __instance)
        {
            int selectedRace = __instance.RaceSelector == null ? 0 : __instance.RaceSelector.SelectedIndex;

            //if (selectedRace == 0) defaultBeardsNum = __instance.BeardTypes.Count; // count number of default beards

            var names = FaceGen.GetRaceNames();
            if (names[selectedRace] != "dwarf") return;
            for (int i = defaultBeardsNum; i < __instance.BeardTypes.Count; i++) 
            {
                var item = __instance.BeardTypes[i];
                string? name = FaceGenHelper.GetBeardName(item.Index, selectedRace, __instance.SelectedGender);
                if (!string.IsNullOrEmpty(name))
                {
                    item.ImagePath = "FaceGen\\Beard\\" + name;
                }
            }
        }
    }
}
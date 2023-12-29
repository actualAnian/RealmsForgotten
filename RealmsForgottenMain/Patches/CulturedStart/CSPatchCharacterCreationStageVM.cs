using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

namespace RealmsForgotten.Patches.CulturedStart.Patches.CulturedStart
{
    [HarmonyPatch(typeof(CharacterCreationGenericStageVM), "RefreshSelectedOptions")]
    public class CSPatchCharacterCreationStageVM
    {
        private static CharacterCreationGenericStageVM? _characterCreationGenericStageVM;
        public static void Postfix(CharacterCreationGenericStageVM __instance) => _characterCreationGenericStageVM = __instance;

        // OnNextStage does not seem to be called. Copied from original version, so I left it in.
        //public static void OnNextStage() => _characterCreationGenericStageVM.OnNextStage();

    }
}

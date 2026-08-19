using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Patches
{
    /// <summary>
    /// Cena com "forceatmo" no nome mantém a atmosfera AUTORADA no editor, em vez da
    /// atmosfera que a campanha impõe pela estação/clima do mapa.
    ///
    /// Portado do AtmospherePersistence do LOTRAOM (que credita o TOW_Core do The Old
    /// Realms). Uso: renomear a cena para conter "forceatmo" (ex.:
    /// rf_temple_forceatmo) — nenhum outro registro é preciso.
    ///
    /// O original patchava a interop ScriptingInterfaceOfIMBMission.InitializeMission;
    /// aqui o alvo é o MissionState.CreateMission gerenciado (verificado no 1.4.8:
    /// CreateMission(MissionInitializerRecord, bool)), por onde todo record passa antes
    /// de chegar à engine — mesmo efeito, sem depender de classe interop gerada.
    /// </summary>
    [HarmonyPatch(typeof(MissionState), "CreateMission")]
    internal static class AtmospherePersistencePatch
    {
        private const string ForceAtmosphereKey = "forceatmo";

        private static void Prefix(ref MissionInitializerRecord rec)
        {
            if (rec.SceneName != null && rec.SceneName.Contains(ForceAtmosphereKey))
            {
                // Sem estes dois, a engine sobrepõe a atmosfera da campanha à da cena.
                rec.PlayingInCampaignMode = false;
                rec.AtmosphereOnCampaign = default;
            }
        }
    }
}

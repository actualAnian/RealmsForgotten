using HarmonyLib;
using TaleWorlds.Library;

namespace RealmsForgotten.Patches
{
    // CTD 2026-08-26 (20:33): HeroCreator.DeliverOffSpring abre com
    // Debug.SilentAssert(mother.Race == father.Race) — no RF, casamento inter-racial
    // e LEGAL e o RFHeroCreationModel ja gera o filho corretamente. Mas o assert
    // "silencioso" com o watchdog anexado nao e silencioso: congela o jogo (~60s)
    // capturando stacks, dispara o CrashUploader (relatorio de 14MB) e o processo
    // morre em seguida. Stack do crash: PregnancyCampaignBehavior.DailyTickHero ->
    // CheckOffspringToDeliver -> DeliverOffSpring -> assert (HeroCreator.cs:272).
    //
    // Correcao CIRURGICA: prefix em Debug.SilentAssert que engole SO a chamada vinda
    // de DeliverOffSpring (identificada pelo callerMethod que o proprio vanilla passa
    // via [CallerMemberName]). Qualquer outro SilentAssert do jogo segue intocado.
    [HarmonyPatch(typeof(Debug), nameof(Debug.SilentAssert))]
    public static class OffspringAssertSilencerPatch
    {
        private static bool Prefix(bool condition, string callerMethod)
        {
            if (!condition && callerMethod == "DeliverOffSpring")
            {
                Debug.Print("[RF] SilentAssert de racas em DeliverOffSpring suprimido (casamento inter-racial e suportado pelo RFHeroCreationModel).");
                return false;
            }
            return true;
        }
    }
}

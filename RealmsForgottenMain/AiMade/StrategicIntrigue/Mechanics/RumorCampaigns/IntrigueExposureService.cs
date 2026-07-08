using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.RumorCampaigns;

public static class IntrigueExposureService
{
    public static bool IsExposed(ClanIntrigueState state, IntrigueOperation operation)
    {
        float exposureScore = 0f;
        exposureScore += operation.Risk * 0.55f;
        exposureScore += state.Suspicion * 0.45f;
        exposureScore -= state.Infiltration * 0.20f;
        exposureScore -= state.TrustToPlayer * 0.10f;
        exposureScore = MBMath.ClampFloat(exposureScore, 0f, 100f);

        return MBRandom.RandomFloat * 100f < exposureScore;
    }
}

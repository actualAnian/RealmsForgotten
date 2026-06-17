using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.MissionEffects.EffectDurationTypes
{
    public class EndOnKillDuration : EffectDurationBase
    {
        int _amountToKill;

        public EndOnKillDuration(int amountToKill)
        {
            _amountToKill = amountToKill;
        }

        public override void OnAgentKilled(Agent removedAgent)
        {
            _amountToKill--;
            if (_amountToKill <= 0)
                IsExpired = true;
        }
    }
}
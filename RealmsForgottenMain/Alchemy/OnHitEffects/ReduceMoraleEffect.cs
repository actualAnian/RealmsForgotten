using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Alchemy.OnHitEffects
{
    internal class ReduceMoraleEffect : IOnHitEffect
    {
        readonly float _reduceMoraleBy;
        public ReduceMoraleEffect(float reduceMoraleBy)
        {
            _reduceMoraleBy = reduceMoraleBy;
        }

        public void OnHit(Agent agent)
        {
            agent.ChangeMorale(_reduceMoraleBy);
        }
    }
}

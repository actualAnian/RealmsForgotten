using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects.Alchemy.Bombs
{
    internal class FireBomb : IAlchemicalBomb
    {
        public float Size { get; set; } = 6f;
        public float Duration { get; set; } = 10f;
        public string ParticleId { get; set; } = "alchemical_mist2";

        public void OnEntered(Agent agent)
        {

        }

        public void OnLeft(Agent agent)
        {
        }

        public void OnProjectileEntered(Mission.Missile missile)
        {
        }

        public void OnProjectileLeft(Mission.Missile missile)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds.Extensions {
    public static class MissionExtensions {
        public static Vec3 GetTrueRandomPositionAroundPoint(this Mission mission, Vec3 position, float minDistance, float maxDistance, bool nearFirst = false, int tries = 100) {
            Vec3 randomPos = mission.GetRandomPositionAroundPoint(position, minDistance, maxDistance, nearFirst);
            while (randomPos == position && tries > 0) {
                tries--;
                randomPos = mission.GetRandomPositionAroundPoint(position, minDistance, maxDistance, nearFirst);
            }
            return randomPos;
        }
    }
}

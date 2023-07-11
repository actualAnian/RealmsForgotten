using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace HuntableHerds.Extensions {
    public static class AgentExtensions {
        public static bool CanSeeOtherAgent(this Agent agent, Agent otherAgent, float angleMax = 1.5f, float distance = 30f) {
            if (agent == null || otherAgent == null)
                return false;

            if ((agent.Position - otherAgent.Position).Length < distance) {
                Vec3 eyeGlobalPosition = otherAgent.GetChestGlobalPosition();
                Vec3 eyeGlobalPosition2 = agent.GetEyeGlobalPosition();
                if (MathF.Abs(Vec3.AngleBetweenTwoVectors(otherAgent.Position - agent.Position, agent.LookDirection)) < angleMax) {
                    float num;
                    return !Mission.Current.Scene.RayCastForClosestEntityOrTerrain(eyeGlobalPosition2, eyeGlobalPosition, out num, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
                }
            }
            return false;
        }
    }
}

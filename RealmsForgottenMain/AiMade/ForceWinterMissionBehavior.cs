using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public class ForceWinterMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            Vec2 position = Mission.Current.PlayerTeam?.GeneralAgent?.Position.AsVec2 ?? new Vec2(0f, 0f);

            if (IsInAlwaysWinterRegion(position))
            {
                // Log confirmation — here’s where you'd hook weather visuals
                InformationManager.DisplayMessage(new InformationMessage($"❄ Winter visuals active at {position}"));
            }
        }

        private bool IsInAlwaysWinterRegion(Vec2 pos)
        {
            return
                InBox(pos, 100f, 200f, 1385f, 1600f) ||
                InBox(pos, 280f, 300f, 1330f, 1600f) ||
                InBox(pos, 540f, 550f, 1110f, 1600f) ||
                InBox(pos, 770f, 780f, 860f, 1600f) ||
                InBox(pos, 1060f, 1075f, 860f, 1600f) ||
                InBox(pos, 1640f, 1660f, 1010f, 1600f) ||
                InBox(pos, 1340f, 1360f, 1010f, 1600f);
        }

        private bool InBox(Vec2 pos, float minX, float maxX, float minY, float maxY)
        {
            return pos.X >= minX && pos.X <= maxX && pos.Y >= minY && pos.Y <= maxY;
        }
    }
}
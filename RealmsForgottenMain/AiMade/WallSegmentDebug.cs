using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade
{
    public class WallSegmentDebug : ScriptComponentBehavior
    {
        protected override void OnInit()
        {
            base.OnInit();
            RunDebugCheck();
        }

        private void RunDebugCheck()
        {
            WeakGameEntity gameEntity = base.GameEntity.GetChildren().FirstOrDefault(ce => ce.HasTag("solid_child"));
            WeakGameEntity gameEntity2 = base.GameEntity.GetChildren().FirstOrDefault(ce => ce.HasTag("broken_child"));

            if (gameEntity2 == null)
            {
                MBDebug.ShowWarning("⚠️ 'broken_child' GameEntity not found under wall root!");
                return;
            }

            var children = gameEntity2.GetChildren();
            if (children == null || !children.Any())
            {
                MBDebug.ShowWarning("⚠️ 'broken_child' has no children!");
                return;
            }

            int strategicAreaCount = 0;
            foreach (var child in children)
            {
                if (child.HasScriptOfType<StrategicArea>())
                {
                    strategicAreaCount++;
                }
                else
                {
                    MBDebug.ShowWarning($"Child '{child.Name}' is missing StrategicArea script.");
                }
            }

            if (strategicAreaCount == 0)
            {
                MBDebug.ShowWarning("❌ No StrategicArea found on any children of 'broken_child'!");
            }
            else
            {
                MBDebug.Print($"✅ Found {strategicAreaCount} StrategicArea script(s) under 'broken_child'.");
            }
        }
    }
}
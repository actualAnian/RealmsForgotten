using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

public class FloatingTextMissionView : MissionView
{
    private GauntletLayer _layer;
    private FloatingTextVM _vm;

    public override void OnMissionScreenInitialize()
    {
        base.OnMissionScreenInitialize();

        _vm = new FloatingTextVM();

        _layer = new GauntletLayer("test", 100);
        _layer.LoadMovie("FloatingAgentText", _vm);

        MissionScreen.AddLayer(_layer);
    }

    public override void OnMissionScreenFinalize()
    {
        base.OnMissionScreenFinalize();

        if (_layer != null)
        {
            MissionScreen.RemoveLayer(_layer);
            _layer = null;
        }
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (Mission == null) return;

        _vm.ClearAgents();

        foreach (var agent in Mission.Agents)
        {
            if (agent?.AgentVisuals == null) continue;
            if (agent == Agent.Main || agent == Agent.Main.MountAgent) continue;
            float a = 0f;
            float b = 0f;
            float num = 0f;
            MBWindowManager.WorldToScreen(MissionScreen.CombatCamera, agent.GetChestGlobalPosition() + new Vec3(0, 0, 2), ref a, ref b, ref num);
            //MBWindowManager.WorldToScreenInsideUsableArea(base.MissionScreen.CombatCamera, agent.GetChestGlobalPosition(), ref a, ref b, ref num);
            if (num < 0) continue;
            Vec2 worldPos = new Vec2(a, b);
            //worldPos = new Vec2(0, 0);
            _vm.AddAgent(new FloatingTextItemVM
            {
                ScreenPosition = worldPos,
                Text = ""
            });
        }
    }
}
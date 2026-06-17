using RealmsForgotten.UI.FloatingText;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;

[DefaultView]
public class FloatingTextMissionView : MissionView
{
    private GauntletLayer ?_layer;
    private FloatingTextVM? _vm;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        _vm = new FloatingTextVM();
        _layer = new GauntletLayer("floatingText", 101);
        _layer.LoadMovie("FloatingAgentText", _vm);
        MissionScreen.AddLayer(_layer);

        _vm.ClearAgents();
        foreach (var entry in FloatingTextManager.Instance.Entries)
        {
            var pos = TryGetScreenPos(entry.Value.PositionProvider(), out bool isVisible);
            _vm.AddTextItem(new FloatingTextItemVM(entry.Key, pos, entry.Value.Text, entry.Value.Color, false));
        }
    }
    public override void OnMissionTick(float dt)
    {
        bool canSeeTexts = ShouldSeeTexts();
        if (Mission == null || _vm == null) return;
        foreach (var entry in FloatingTextManager.Instance.Entries)
        {
            if (!_vm.ContainsKey(entry.Key))
            {
                var pos = TryGetScreenPos(entry.Value.PositionProvider(), out bool isVisible);
                _vm.AddTextItem(new FloatingTextItemVM(entry.Key, pos, entry.Value.Text, entry.Value.Color, isVisible && canSeeTexts));
            }
        }
        for (int i = _vm.AllTexts.Count - 1; i >= 0; i--)
        {
            FloatingTextItemVM? item = _vm.AllTexts[i];
            if (!FloatingTextManager.Instance.Contains(item.Id))
            {
                _vm.RemoveItem(item);
            }
        }
        foreach (var entry in FloatingTextManager.Instance.Entries)
        {
            var vm = _vm.AllTexts.FirstOrDefault(x => x.Id == entry.Key);
            vm.ScreenPosition = TryGetScreenPos(entry.Value.PositionProvider(), out bool isVisible);
            vm.IsVisible = isVisible && canSeeTexts;
        }
    }

    private bool ShouldSeeTexts() => Mission.InputManager.IsGameKeyDown(5);

    public Vec2 TryGetScreenPos(Vec3 position, out bool isVisible)
    {
        var a = 0f;
        var b = 0f;
        var distance = 0f;
        MBWindowManager.WorldToScreen(MissionScreen.CombatCamera, position, ref a, ref b, ref distance);
        isVisible = distance > 0;
        return new Vec2(a, b);
    }
}
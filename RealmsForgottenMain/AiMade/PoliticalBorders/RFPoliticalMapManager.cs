using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ScreenSystem;

namespace RealmsForgotten.AiMade.PoliticalBorders;

internal static class RFPoliticalMapManager
{
    private static RFPoliticalMapRenderer? _renderer;

    internal static void Tick(float dt)
    {
        MapScreen? screen = Campaign.Current != null ? ScreenManager.TopScreen as MapScreen : null;
        if (screen == null)
        {
            Dispose();
            return;
        }

        if (_renderer == null || _renderer.Screen != screen)
        {
            Dispose();
            _renderer = new RFPoliticalMapRenderer(screen);
        }

        _renderer.Tick(dt);
    }

    internal static void Dispose()
    {
        _renderer?.Dispose();
        _renderer = null;
    }
}

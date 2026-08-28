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
            // ⚠️ CAUSA DAS TRANSICOES DE 15s (capturada pelo RFHitchDetector,
            // 2026-08-26): enciclopedia/character/conversa COBREM o mapa (TopScreen
            // deixa de ser MapScreen) e o codigo antigo dava Dispose() aqui — jogando
            // fora toda a geometria do mapa politico — para reconstruir ~9,5s
            // (BuildFillMeshes, triangulo a triangulo) A CADA fechamento de tela.
            // Mapa COBERTO nao e mapa MORTO: a geometria fica pronta e o retorno e
            // instantaneo. So descartamos quando o MapScreen do renderer realmente
            // morreu (saiu da campanha / tela de mapa trocada).
            if (_renderer != null && (Campaign.Current == null || MapScreen.Instance != _renderer.Screen))
            {
                Dispose();
            }
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

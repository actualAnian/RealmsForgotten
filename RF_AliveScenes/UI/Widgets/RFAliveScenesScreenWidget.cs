using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace RF_AliveScenes.UI.Widgets;

/// <summary>
/// Container dos baloes: mantem a lista viva e empurra na horizontal os que se
/// sobrepoem, para duas falas proximas nao virarem uma sopa de letras.
///
/// Correcao em relacao ao original: ao trocar o container, o handler antigo agora e
/// removido (la fazia "+=" duas vezes e vazava assinatura).
/// </summary>
public class RFAliveScenesScreenWidget : Widget
{
    private readonly List<RFAliveScenesBubbleWidget> _bubbles = new();
    private Widget _bubblesContainer;

    public RFAliveScenesScreenWidget(UIContext context)
        : base(context)
    {
    }

    [Editor(false)]
    public Widget BubblesContainer
    {
        get => _bubblesContainer;
        set
        {
            if (value == _bubblesContainer)
            {
                return;
            }

            if (_bubblesContainer != null)
            {
                _bubblesContainer.EventFire -= OnContainerEvent;
            }

            _bubblesContainer = value;

            if (_bubblesContainer != null)
            {
                _bubblesContainer.EventFire += OnContainerEvent;
            }

            OnPropertyChanged(value, "BubblesContainer");
        }
    }

    protected override void OnLateUpdate(float dt)
    {
        base.OnLateUpdate(dt);

        if (_bubbles.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _bubbles.Count; i++)
        {
            _bubbles[i].Update(dt);
        }

        _bubbles.Sort((a, b) => a.Rect.Left.CompareTo(b.Rect.Left));

        for (int i = 0; i < _bubbles.Count; i++)
        {
            for (int j = i + 1; j < _bubbles.Count; j++)
            {
                if (_bubbles[j].Rect.Left - _bubbles[i].Rect.Left > _bubbles[i].Rect.Width)
                {
                    break;
                }
                if (_bubbles[i].Rect.IsOverlapping(_bubbles[j].Rect))
                {
                    _bubbles[j].ScaledPositionXOffset += _bubbles[i].Rect.Right - _bubbles[j].Rect.Left;
                    _bubbles[j].UpdateRectangle();
                }
            }
        }
    }

    private void OnContainerEvent(Widget widget, string eventName, object[] args)
    {
        if (args == null || args.Length != 1)
        {
            return;
        }

        if (args[0] is not RFAliveScenesBubbleWidget bubble)
        {
            return;
        }

        if (eventName == "ItemAdd")
        {
            _bubbles.Add(bubble);
        }
        else if (eventName == "ItemRemove")
        {
            _bubbles.Remove(bubble);
        }
    }
}

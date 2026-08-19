using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AliveScenes.UI;

/// <summary>Lista de baloes ativos e sua posicao na tela.</summary>
public sealed class BubbleLayerVM : ViewModel
{
    private static readonly Vec3 HeadOffset = new Vec3(0f, 0f, 0.35f, -1f);
    private static readonly Vec2 OffScreen = new Vec2(-100f, -100f);

    private readonly Camera _camera;
    private MBBindingList<BubbleVM> _bubbles;

    public BubbleLayerVM(Camera camera)
    {
        _camera = camera;
        Bubbles = new MBBindingList<BubbleVM>();
    }

    [DataSourceProperty]
    public MBBindingList<BubbleVM> Bubbles
    {
        get => _bubbles;
        set
        {
            if (value != _bubbles)
            {
                _bubbles = value;
                OnPropertyChangedWithValue(value, "Bubbles");
            }
        }
    }

    public void Add(Agent agent, string message, bool isEnemy)
    {
        if (agent == null || string.IsNullOrEmpty(message))
        {
            return;
        }

        // Um agente fala uma coisa de cada vez: qualquer balao anterior dele sai antes de
        // entrar o novo. Tambem blinda contra evento assinado em duplicidade (baloes
        // duplicados vistos no teste de 2026-08-18).
        Remove(agent);
        Bubbles.Add(new BubbleVM(agent, message, isEnemy));
    }

    /// <summary>
    /// Remove o balao do agente. FirstOrDefault de proposito: o original usava
    /// SingleOrDefault e lancava se o mesmo agente tivesse dois baloes vivos.
    /// </summary>
    public void Remove(Agent agent)
    {
        BubbleVM bubble = Bubbles.FirstOrDefault(b => b.TargetAgent == agent);
        if (bubble != null)
        {
            Bubbles.Remove(bubble);
        }
    }

    public void Tick(float dt)
    {
        if (_camera == null)
        {
            return;
        }

        for (int i = Bubbles.Count - 1; i >= 0; i--)
        {
            BubbleVM bubble = Bubbles[i];

            if (bubble.TargetAgent == null || !bubble.TargetAgent.IsActive())
            {
                Bubbles.RemoveAt(i);
                continue;
            }

            float screenX = -100f;
            float screenY = -100f;
            float depth = 0f;

            Vec3 world = bubble.WorldPosition;
            bubble.Distance = (int)(world - _camera.Position).Length;

            MBWindowManager.WorldToScreenInsideUsableArea(_camera, world + HeadOffset, ref screenX, ref screenY, ref depth);

            if (depth > 0f && bubble.ShouldShow)
            {
                bubble.ScreenPosition = new Vec2(screenX, screenY);
            }
            else
            {
                bubble.ScreenPosition = OffScreen;
                bubble.Distance = -1;
            }
        }
    }

    public override void RefreshValues()
    {
        base.RefreshValues();
        Bubbles.ApplyActionOnAllItems(b => b.RefreshValues());
    }

    public override void OnFinalize()
    {
        Bubbles.ApplyActionOnAllItems(b => b.OnFinalize());
        Bubbles.Clear();
        base.OnFinalize();
    }
}

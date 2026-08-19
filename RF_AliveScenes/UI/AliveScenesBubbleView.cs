using System;
using RF_AliveScenes.Missions;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace RF_AliveScenes.UI;

/// <summary>
/// Camada Gauntlet dos baloes. Registrada em runtime pelo SubModule
/// (MissionScreen.AddMissionView), sem nenhum patch Harmony — o mod original precisava
/// de nove postfixes em SandBoxMissionViews mais um patch por reflexao para o naval.
/// </summary>
public sealed class AliveScenesBubbleView : MissionView
{
    private GauntletLayer _layer;
    private BubbleLayerVM _dataSource;
    private AliveScenesMissionLogic _logic;

    public override void OnMissionScreenInitialize()
    {
        base.OnMissionScreenInitialize();
        try
        {
            _dataSource = new BubbleLayerVM(MissionScreen.CombatCamera);
            _layer = new GauntletLayer("RFAliveScenesBubble", 1, false);
            _layer.LoadMovie("RFAliveScenesBubble", _dataSource);
            MissionScreen.AddLayer(_layer);

            _logic = Mission.GetMissionBehavior<AliveScenesMissionLogic>();
            if (_logic != null)
            {
                // -= antes do +=: se a engine reinicializar a tela da missao, a assinatura
                // nao duplica (causa provavel dos baloes em dobro no teste de 2026-08-18).
                _logic.AgentSpoke -= OnAgentSpoke;
                _logic.AgentStoppedSpeaking -= OnAgentStoppedSpeaking;
                _logic.AgentSpoke += OnAgentSpoke;
                _logic.AgentStoppedSpeaking += OnAgentStoppedSpeaking;
            }
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao montar a camada de baloes: " + e.Message);
        }
    }

    public override void OnMissionScreenFinalize()
    {
        try
        {
            if (_logic != null)
            {
                _logic.AgentSpoke -= OnAgentSpoke;
                _logic.AgentStoppedSpeaking -= OnAgentStoppedSpeaking;
                _logic = null;
            }

            if (_layer != null)
            {
                MissionScreen?.RemoveLayer(_layer);
                _layer = null;
            }

            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
                _dataSource = null;
            }
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao desmontar a camada de baloes: " + e.Message);
        }

        base.OnMissionScreenFinalize();
    }

    public override void OnMissionScreenTick(float dt)
    {
        base.OnMissionScreenTick(dt);
        _dataSource?.Tick(dt);
    }

    public override void OnPhotoModeActivated()
    {
        base.OnPhotoModeActivated();
        if (_layer != null)
        {
            _layer.UIContext.ContextAlpha = 0f;
        }
    }

    public override void OnPhotoModeDeactivated()
    {
        base.OnPhotoModeDeactivated();
        if (_layer != null)
        {
            _layer.UIContext.ContextAlpha = 1f;
        }
    }

    private void OnAgentSpoke(Agent agent, string message, bool isEnemy) => _dataSource?.Add(agent, message, isEnemy);

    private void OnAgentStoppedSpeaking(Agent agent) => _dataSource?.Remove(agent);
}

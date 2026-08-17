using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_CoopWarsails
{
    /// <summary>
    /// Marco 2: sincronizacao de movimento numa missao compartilhada.
    ///
    /// Anexado automaticamente a qualquer missao (ver SubModule). Quando a
    /// sessao P2P esta conectada, a cada frame envia o estado do agente local
    /// (Agent.Main) e aplica o estado recebido a um agente "fantoche" que
    /// representa o outro jogador - spawnado uma unica vez na primeira
    /// atualizacao. Assim os dois se veem andar na mesma cena.
    ///
    /// O spawn e a escolha de estado sao adaptados do CoopArenaController /
    /// AgentData do BannerlordCoop (com permissao; ver THIRD_PARTY_NOTICES.md).
    /// </summary>
    internal sealed class CoopMissionSync : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private Agent? _puppet;
        private bool _spawnAttempted;
        private float _sendTimer;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            var net = CoopController.Net;
            if (net == null || net.State != CoopLinkState.Connected) return;

            // 1) enviar o proprio estado (throttle leve: ~30 Hz)
            _sendTimer += dt;
            if (_sendTimer >= 0.033f && Agent.Main != null && Agent.Main.IsActive())
            {
                _sendTimer = 0f;
                net.SendAgentState(AgentStateSnapshot.Capture(Agent.Main));
            }

            // 2) aplicar o estado do outro jogador ao fantoche
            if (!net.HasRemoteAgentState) return;
            var remote = net.LatestRemoteAgentState;

            if (_puppet == null)
            {
                if (!_spawnAttempted) TrySpawnPuppet(remote);
                return;
            }

            if (_puppet.IsActive())
                remote.Apply(_puppet);
        }

        private void TrySpawnPuppet(AgentStateSnapshot state)
        {
            _spawnAttempted = true;
            try
            {
                if (Mission.Current == null) return;

                // Marco 2: o fantoche usa o personagem do jogador local como
                // aparencia provisoria. Sincronizar o personagem real do outro
                // lado vem num marco seguinte (handshake de aparencia).
                CharacterObject? character = CharacterObject.PlayerCharacter;
                if (character == null)
                {
                    CoopLog.File_("puppet spawn: sem PlayerCharacter; missao sem contexto de campanha?");
                    return;
                }

                Team team = Mission.Current.PlayerTeam ?? Mission.Current.AttackerTeam;
                if (team == null)
                {
                    CoopLog.File_("puppet spawn: missao sem teams; aguardando...");
                    _spawnAttempted = false; // tenta de novo no proximo tick
                    return;
                }

                var build = new AgentBuildData(character)
                    .Team(team)
                    .InitialPosition(state.Position)
                    .InitialDirection(Vec2.Forward)
                    .NoHorses(true)
                    .Equipment(character.Equipment)
                    .TroopOrigin(new SimpleAgentOrigin(character, -1, null, default))
                    .Controller(AgentControllerType.None); // dirigido por pacotes, sem IA

                _puppet = Mission.Current.SpawnAgent(build);
                _puppet.FadeIn();
                CoopLog.Screen($"fantoche do outro jogador spawnado ({_puppet.Name})");
            }
            catch (Exception ex)
            {
                CoopLog.File_($"puppet spawn falhou: {ex}");
            }
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            base.OnAgentDeleted(affectedAgent);
            if (affectedAgent == _puppet)
            {
                _puppet = null;
                _spawnAttempted = false;
            }
        }
    }
}

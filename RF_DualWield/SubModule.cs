using System;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace RF_DualWield
{
    /// <summary>
    /// Ponto de entrada do modulo. Faz duas coisas e mais nada:
    ///   - aplica o unico patch Harmony (colisao da mao esquerda);
    ///   - registra o DualWieldMissionBehavior em cada missao.
    ///
    /// Sem hotkeys, sem DisplayMessage, sem componentes de agente extras, sem alteracao
    /// de dano/moral/knockdown global.
    /// </summary>
    public sealed class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "com.realmsforgotten.rfdualwield";

        private bool _patched;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            // PatchAll pode lancar se um alvo mudar numa atualizacao do jogo. Se lancar aqui,
            // o carregamento do modulo falha e derruba o startup -- entao contem-se a excecao,
            // loga-se, e o modulo segue sem o patch (dual wield sem dano na offhand, mas o jogo vive).
            try
            {
                new Harmony(HarmonyId).PatchAll(typeof(SubModule).Assembly);
                _patched = true;
            }
            catch (Exception ex)
            {
                _patched = false;
                DualWieldLog.Warn("falha ao aplicar o patch de colisao: " + ex.Message +
                                  ". O modulo segue carregado, mas o golpe da mao esquerda nao vai registrar.");
            }
        }

        /// <summary>
        /// Registrar AQUI, nao em OnMissionBehaviorInitialize. Em Mission.AfterStart() do vanilla a
        /// ordem e: OnBeforeMissionBehaviorInitialize -> foreach behavior.OnBehaviorInitialize() ->
        /// OnMissionBehaviorInitialize. Um behavior adicionado no ultimo hook nunca recebe
        /// OnBehaviorInitialize -- foi o bug da rodada 1 (a validacao nunca rodava, IsSystemHealthy
        /// ficava false e o guard removia a arma de offhand silenciosamente).
        /// </summary>
        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);

            if (mission == null)
            {
                return;
            }

            if (mission.GetMissionBehavior<DualWieldMissionBehavior>() != null)
            {
                return;
            }

            if (!_patched)
            {
                // Sem o patch de colisao o dual wield nao funciona como arma; ainda assim
                // registramos o behavior, porque ele e quem impede o CTD de animacao.
                DualWieldLog.Warn("registrando o behavior sem o patch de colisao (modo apenas-seguranca).");
            }

            mission.AddMissionBehavior(new DualWieldMissionBehavior());
        }
    }
}

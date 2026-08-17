using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RF_CoopWarsails
{
    /// <summary>
    /// RF Coop Warsails - marco 1: transporte P2P proprio.
    ///
    /// Adiciona duas opcoes no menu principal ("RF Coop: Host" / "RF Coop: Join").
    /// Host abre uma porta UDP e espera; Join pede um IP e conecta. Uma vez
    /// conectados, os dois lados trocam um heartbeat com contador crescente,
    /// visivel no canto da tela - prova de que a rede bidirecional funciona
    /// entre dois PCs, sem nenhum codigo do BannerlordCoop.
    ///
    /// Marcos seguintes constroem sobre esta base: colocar ambos numa cena
    /// compartilhada, depois dono/fantoche de agentes, depois batalha.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private InitialStateOption? _hostOption;
        private InitialStateOption? _joinOption;
        private InitialStateOption? _stopOption;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                var moduleRoot = TaleWorlds.ModuleManager.ModuleHelper.GetModuleFullPath("RF_CoopWarsails");
                CoopLog.Initialize(moduleRoot);
                CoopLog.File_("OnSubModuleLoad");
            }
            catch (Exception ex)
            {
                CoopLog.File_($"OnSubModuleLoad failed: {ex}");
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_hostOption != null) return; // registra uma unica vez

            _hostOption = new InitialStateOption(
                "RF_Coop_Host",
                new TextObject("RF Coop: Hospedar"),
                9701,
                OnHostSelected,
                () => (false, new TextObject("")));

            _joinOption = new InitialStateOption(
                "RF_Coop_Join",
                new TextObject("RF Coop: Conectar"),
                9702,
                OnJoinSelected,
                () => (false, new TextObject("")));

            _stopOption = new InitialStateOption(
                "RF_Coop_Stop",
                new TextObject("RF Coop: Encerrar sessao"),
                9703,
                OnStopSelected,
                () => (false, new TextObject("")));

            Module.CurrentModule.AddInitialStateOption(_hostOption);
            Module.CurrentModule.AddInitialStateOption(_joinOption);
            Module.CurrentModule.AddInitialStateOption(_stopOption);
            CoopLog.File_("menu options registered");
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            CoopController.Tick(dt);
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            // anexa o sync a toda missao; ele so age quando a sessao P2P conecta
            mission.AddMissionBehavior(new CoopMissionSync());
            CoopLog.File_("CoopMissionSync anexado a missao");
        }

        private static void OnHostSelected()
        {
            CoopController.Host();
        }

        private static void OnJoinSelected()
        {
            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Conectar ao host",
                "IP do host (ex.: 192.168.0.10 ou 127.0.0.1 para testar no mesmo PC):",
                true,  // isAffirmativeOptionShown
                true,  // isNegativeOptionShown
                "Conectar",
                "Cancelar",
                ip => { if (!string.IsNullOrWhiteSpace(ip)) CoopController.Join(ip.Trim()); },
                () => { }));
        }

        private static void OnStopSelected()
        {
            CoopController.Stop();
        }
    }
}

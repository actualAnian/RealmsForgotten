using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace RF_CoopCompat
{
    /// <summary>
    /// Camada de compatibilidade Realms Forgotten x Bannerlord Coop.
    ///
    /// Nao altera nada do RF nem do Coop em disco: em runtime, quando uma
    /// sessao coop inicia, silencia os CampaignBehaviors do RF conforme a
    /// politica configurada (padrao: rodam so no host autoritativo), bloqueia
    /// a injecao de MissionLogic do RF em batalhas coop e remove patches
    /// Harmony do RF marcados como perigosos. Em singleplayer normal (ou sem o
    /// Coop instalado) o modulo fica dormente.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        private const string HarmonyId = "rf.coopcompat";
        private const int HookRetryFrames = 300;   // ~5s por tentativa a 60fps
        private const int HookMaxAttempts = 120;   // desiste depois de ~10min

        private Harmony? _harmony;
        private CompatConfig? _config;
        private bool _coopModuleActive;
        private bool _gatesInstalled;
        private int _framesSinceRetry;
        private int _hookAttempts;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                var moduleRoot = TaleWorlds.ModuleManager.ModuleHelper.GetModuleFullPath("RF_CoopCompat");
                CompatLog.Initialize(moduleRoot);
                _config = CompatConfig.Load(moduleRoot);
                _harmony = new Harmony(HarmonyId);
            }
            catch (Exception ex)
            {
                CompatLog.Info($"OnSubModuleLoad failed: {ex}");
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_gatesInstalled || _harmony == null || _config == null) return;

            _coopModuleActive = DetectCoopModule();
            if (!_coopModuleActive)
            {
                CompatLog.Info("coop module not active; RF_CoopCompat dormant");
                return;
            }

            // Destrava a conexao com War Sails ativo (o RF depende dele).
            // Feito cedo, antes de qualquer tentativa de conexao coop.
            if (_config.AllowDlc)
                DlcBlockNeutralizer.Install(_harmony);

            try
            {
                BehaviorGater.Install(_harmony, _config);
                _gatesInstalled = true;
            }
            catch (Exception ex)
            {
                CompatLog.Info($"gater install failed: {ex}");
            }

            CoopSessionHook.TryInstall(_harmony, _config);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            if (!_coopModuleActive || _harmony == null || _config == null) return;

            // Deteccao primaria (padrao Hex): polling do container DI do Coop.
            // Sobe/desce a sessao e resolve broker/network/mapper para o sync.
            CoopServices.Poll(_harmony, _config);

            // Fallback: hook em PatchAll, caso o ContainerProvider mude entre versoes.
            if (!CoopSessionHook.Installed && _hookAttempts < HookMaxAttempts && ++_framesSinceRetry >= HookRetryFrames)
            {
                _framesSinceRetry = 0;
                _hookAttempts++;
                if (CoopSessionHook.TryInstall(_harmony, _config))
                    CompatLog.Info($"session hook installed after {_hookAttempts} retries");
                else if (_hookAttempts == HookMaxAttempts)
                    CompatLog.Info("giving up on session hook install; usando polling de CoopServices");
            }
        }

        private static bool DetectCoopModule()
        {
            try
            {
                var names = TaleWorlds.Engine.Utilities.GetModulesNames();
                if (names != null && names.Any(n => n == "Coop" || n == "CoopNightly"))
                    return true;
            }
            catch (Exception ex)
            {
                CompatLog.Info($"module name detection failed ({ex.Message}); falling back to assembly scan");
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "GameInterface" || a.GetName().Name == "Coop");
        }
    }
}

using RBM_RF.Patches;
using TaleWorlds.MountAndBlade;

namespace RBM_RF;

// [RBM_RF] Ate hoje este modulo era so DADOS (o XSLT que corrige material_type das pecas magicas
// quando o RBM esta ativo). Ganhou uma DLL por um motivo unico: hospedar o crash fix do
// RBMAI.StanceLogic. Ele vive AQUI, e nao no RealmsForgotten, porque este e o modulo que declara
// DependedModule RBM — quem joga sem RBM nao carrega nada disto.
//
// Regra para o futuro: patch que fala com o RBM entra neste projeto. Nada de logica de jogo do RF
// aqui.
public class SubModule : MBSubModuleBase
{
	// RBMAI.dll e carregada sob demanda pela RBM.dll e nao aparece no SubModule.xml do RBM, entao
	// no OnSubModuleLoad o tipo ainda nao existe. O inicio da missao e o primeiro momento em que
	// StanceLogic ja foi registrada; EnsureApplied e idempotente e tenta de novo se necessario.
	public override void OnMissionBehaviorInitialize(Mission mission)
	{
		base.OnMissionBehaviorInitialize(mission);
		RBMStanceLogicMountStaggerCrashFixPatch.EnsureApplied();
	}
}

using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RBM_RF.Patches;

// [RBM_RF] Crash fix do RBM sob 1.4.8 — cavalo que morre em movimento derruba o jogo.
//
// ORIGEM: patch do amigo do autor (mod LT_EE1259_Core13). Portado para CA porque o lugar certo
// dele e este modulo: RBM_RF e quem declara DependedModule RBM, entao a correcao vive junto da
// compatibilidade e nao suja o RealmsForgotten (que roda sem RBM).
//
// DIAGNOSTICO CONFIRMADO no binario instalado (ilspycmd em Modules/RBM/bin/.../RBMAI.dll,
// RBMAI.StanceLogic.OnAgentRemoved):
//
//   if (!Mission.Current.IsFieldBattle || affectedAgent == null || affectedAgent.IsHuman) return;
//   if (velocidade >= 5f) {
//       coleta vizinhos com (CurrentMortalityState != Invulnerable && !HasMount)
//       foreach -> SetActionChannel(0, act_stagger_backward_3, ...)
//   }
//   base.OnAgentRemoved(...)   // MissionBehavior — VAZIO
//
// A ARMADILHA: `HasMount` responde "este agente esta montado em algo?". Um CAVALO nunca esta
// montado em outro cavalo, logo `!HasMount` e verdadeiro para TODO cavalo, inclusive os que estao
// sendo cavalgados. O filtro queria excluir cavaleiros e acabou incluindo justamente os cavalos,
// que recebem `act_stagger_backward_3` — animacao que o action set `as_horse` NAO possui
// ("as_horse does not contain act_stagger_backward_3" no rgl_log). Sem clipe, o indice de animacao
// fica -1 e a busca nativa le lixo de heap: AccessViolation (0xC0000005) em TaleWorlds.Native.dll,
// intermitente, ~20s dentro de batalhas de cavalaria.
//
// (E a MESMA familia de bug do nosso RF_DualWield: acao sem clipe no action set -> indice -1 ->
// crash nativo. La era as_human_warrior sem act_dual_ready_thrust_1h.)
//
// CORRECAO: prefixo que reimplementa o metodo com um filtro `IsHuman` a mais. Substituir o
// original inteiro e seguro porque o unico resto dele e o `base.OnAgentRemoved`, que e vazio
// (verificado no decompilado). Os 13 argumentos do SetActionChannel sao copiados IDENTICOS aos do
// RBM, para o stagger dos humanos continuar exatamente igual ao original.
//
// APLICACAO TARDIA (nao PatchAll): RBMAI.dll NAO e declarada no SubModule.xml do RBM — ela e
// carregada sob demanda pela RBM.dll, entao o tipo nao existe durante o nosso OnSubModuleLoad.
// Por isso o EnsureApplied() e chamado no inicio de cada missao e tenta de novo se ainda nao achou.
//
// MANUTENCAO: como substituimos o metodo, uma versao futura do RBM que mude essa logica seria
// silenciosamente descartada. A linha "patch aplicado" no rgl_log existe para isso — se o RBM
// atualizar, reconferir o decompilado antes de confiar.
internal static class RBMStanceLogicMountStaggerCrashFixPatch
{
	private const float MinDyingMountSpeed = 5f;

	private static bool _resolved;
	private static bool _retryLogged;
	private static int _blockedTotal;

	public static void EnsureApplied()
	{
		if (_resolved)
		{
			return;
		}

		Type stanceLogicType = AccessTools.TypeByName("RBMAI.StanceLogic");
		if (stanceLogicType == null)
		{
			// RBM desligado no launcher, ou RBMAI.dll ainda nao carregada: tenta na proxima missao.
			if (!_retryLogged)
			{
				_retryLogged = true;
				Debug.Print("[RBM_RF] RBMAI.StanceLogic ainda nao carregada; tentando na proxima missao (normal se o RBM estiver desligado).");
			}
			return;
		}

		MethodInfo original = AccessTools.Method(stanceLogicType, "OnAgentRemoved");
		if (original == null)
		{
			// Metodo ausente nao aparece depois: para de tentar.
			_resolved = true;
			Debug.Print("[RBM_RF] AVISO: RBMAI.StanceLogic.OnAgentRemoved nao encontrado — patch NAO aplicado. Se o RBM mudou de versao, reconferir o decompilado.");
			return;
		}

		try
		{
			new Harmony("rf.rbm_rf.stancelogic_mount_stagger").Patch(
				original,
				prefix: new HarmonyMethod(typeof(RBMStanceLogicMountStaggerCrashFixPatch), nameof(Prefix)));
			_resolved = true;
			Debug.Print("[RBM_RF] patch aplicado em RBMAI.StanceLogic.OnAgentRemoved (crash de cavalo morrendo em movimento).");
		}
		catch (Exception e)
		{
			_resolved = true;
			Debug.Print("[RBM_RF] ERRO ao aplicar o patch em RBMAI.StanceLogic.OnAgentRemoved: " + e.GetType().Name + ": " + e.Message);
		}
	}

	// O parametro TEM de se chamar affectedAgent: e o nome no original (conferido no decompilado)
	// e o Harmony liga os argumentos por nome.
	private static bool Prefix(Agent affectedAgent)
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || !mission.IsFieldBattle || affectedAgent == null || affectedAgent.IsHuman)
			{
				return false;
			}

			if (affectedAgent.MovementVelocity.Length < MinDyingMountSpeed)
			{
				return false;
			}

			Vec2 searchPosition = affectedAgent.Position.AsVec2 + affectedAgent.GetMovementDirection().Normalized();
			int blockedThisDeath = 0;

			AgentProximityMap.ProximityMapSearchStruct search =
				AgentProximityMap.BeginSearch(mission, searchPosition, 0f, extendRangeByBiggestAgentCollisionPadding: true);
			while (search.LastFoundAgent != null)
			{
				Agent nearby = search.LastFoundAgent;
				if (nearby.CurrentMortalityState != Agent.MortalityState.Invulnerable && !nearby.HasMount)
				{
					if (nearby.IsHuman)
					{
						// Mesmos 13 argumentos do RBM original — stagger humano inalterado.
						nearby.SetActionChannel(0, in ActionIndexCache.act_stagger_backward_3, false, (AnimFlags)0,
							0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					}
					else
					{
						// Exatamente o caminho que crashava: sem clipe em as_horse -> indice -1.
						blockedThisDeath++;
					}
				}
				AgentProximityMap.FindNext(mission, ref search);
			}

			if (blockedThisDeath > 0)
			{
				_blockedTotal += blockedThisDeath;
				Debug.Print("[RBM_RF] crash evitado: act_stagger_backward_3 bloqueado em " + blockedThisDeath +
					" agente(s) nao-humano(s) perto de montaria morrendo (total na sessao: " + _blockedTotal + ").");
			}
		}
		catch (Exception e)
		{
			// Nunca deixar a cosmetica de stagger derrubar a missao.
			Debug.Print("[RBM_RF] excecao no prefixo de OnAgentRemoved: " + e.GetType().Name + ": " + e.Message);
		}

		return false;
	}
}

using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RF_DualWield
{
    /// <summary>
    /// Resolucao e validacao dos ActionIndexCache do dual wield.
    ///
    /// Porque isto existe: na 1.4.8 uma action declarada em action_types.xml mas SEM binding
    /// de animacao no action_set do agente faz MBActionSet.GetAnimationIndexOfAction devolver -1.
    /// O sistema de animacao nativo indexa com esse -1 e derruba o processo -- CTD sem excecao
    /// gerenciada (nada aparece no Visual Studio). Foi exatamente o crash do modulo anterior;
    /// evidencia no rgl_log:
    ///     "as_human_warrior does not contain act_dual_quick_release_thrust_1h"
    ///     "5013 -1 6218"
    ///
    /// Politica: NUNCA deixar o jogo chegar a esse ponto. Validamos tudo uma vez por
    /// action_set e, se faltar um unico binding, o dual wield e desligado para aquele agente.
    /// </summary>
    internal static class DualWieldActions
    {
        /// <summary>Action set base dos humanos: onde action_sets.xml deste modulo mescla os bindings.</summary>
        internal const string HumanWarriorActionSetName = "as_human_warrior";

        /// <summary>item_usage_set da arma de offhand (o gatilho do dual wield).</summary>
        internal const string OffhandItemUsage = "dual_shield";

        /// <summary>item_usage_set da arma da mao principal.</summary>
        internal const string MainHandItemUsage = "dual_shield_thrust";

        // ActionIndexCache e struct na 1.4.8 (era classe na 1.1/1.2). Guardamos por valor,
        // nunca comparamos com null, e passamos sempre por 'in' para as APIs do engine.
        private static ActionIndexCache[]? _actions;
        private static bool _resolveAttempted;
        private static bool _resolveOk;
        private static string _firstUnresolvedName = string.Empty;

        // Cache de veredito por action_set (chave = nome do set; MBActionSet.Index e internal).
        // Validar 82 actions por agente sairia caro; por action_set roda uma vez e serve para todos.
        private static readonly Dictionary<string, bool> ActionSetVerdicts = new Dictionary<string, bool>();

        internal static void Reset()
        {
            _actions = null;
            _resolveAttempted = false;
            _resolveOk = false;
            _firstUnresolvedName = string.Empty;
            ActionSetVerdicts.Clear();
        }

        /// <summary>
        /// Resolve os 82 ActionIndexCache uma unica vez. Falha se qualquer nome nao existir
        /// na tabela de action_types carregada (indice invalido == act_none).
        /// </summary>
        internal static bool TryResolveActions(out string firstUnresolved)
        {
            if (_resolveAttempted)
            {
                firstUnresolved = _firstUnresolvedName;
                return _resolveOk;
            }

            _resolveAttempted = true;

            string[] names = DualWieldActionTable.ActionNames;
            if (names.Length == 0)
            {
                _firstUnresolvedName = "(tabela vazia)";
                firstUnresolved = _firstUnresolvedName;
                return false;
            }

            var resolved = new ActionIndexCache[names.Length];
            int noneIndex = ActionIndexCache.act_none.Index;

            for (int i = 0; i < names.Length; i++)
            {
                ActionIndexCache action = ActionIndexCache.Create(names[i]);
                if (action.Index < 0 || action.Index == noneIndex)
                {
                    _firstUnresolvedName = names[i];
                    firstUnresolved = _firstUnresolvedName;
                    return false;
                }

                resolved[i] = action;
            }

            _actions = resolved;
            _resolveOk = true;
            firstUnresolved = string.Empty;
            return true;
        }

        /// <summary>
        /// Confere se TODAS as actions do dual wield tem clipe de animacao no action_set dado.
        /// Este e o guard que substitui o CTD: se devolver false, nao se aplica nada ao agente.
        /// </summary>
        internal static bool ActionSetSupportsDualWield(MBActionSet actionSet, out string firstMissing)
        {
            firstMissing = string.Empty;

            if (!actionSet.IsValid)
            {
                firstMissing = "(action set invalido)";
                return false;
            }

            ActionIndexCache[]? actions = _actions;
            if (!_resolveOk || actions == null)
            {
                firstMissing = "(actions nao resolvidas)";
                return false;
            }

            string key = actionSet.GetName() ?? string.Empty;

            bool cached;
            if (ActionSetVerdicts.TryGetValue(key, out cached))
            {
                if (!cached)
                {
                    firstMissing = "(reprovado em validacao anterior)";
                }

                return cached;
            }

            for (int i = 0; i < actions.Length; i++)
            {
                if (!MBActionSet.CheckActionAnimationClipExists(actionSet, in actions[i]))
                {
                    firstMissing = DualWieldActionTable.ActionNames[i] + Diagnose(actionSet, in actions[i]);
                    ActionSetVerdicts[key] = false;
                    return false;
                }
            }

            ActionSetVerdicts[key] = true;
            return true;
        }

        /// <summary>
        /// Separa as DUAS causas possiveis de CheckActionAnimationClipExists devolver false, que
        /// antes ficavam indistinguiveis no log e custaram um teste inteiro do autor:
        ///
        ///   (A) BINDING AUSENTE -- o action_set nao tem &lt;action type="..." animation="..."/&gt;.
        ///       GetActionAnimationName devolve vazio. Problema de XML: action_sets.xml deste
        ///       modulo nao mesclou em as_human_warrior (ou a action set do agente nao herda dele).
        ///
        ///   (B) CLIPE AUSENTE -- o binding existe e aponta um nome de animacao, mas o engine nao
        ///       tem o clipe carregado, entao GetAnimationIndexOfAction devolve -1. Problema de
        ///       ASSET: o *_anm.tpac de Assets so guarda metadata; o payload esta no
        ///       RuntimeDataCache/&lt;guid&gt;.rdc. Sem RuntimeDataCache o engine nem loga
        ///       "Loading packages $BASE/Modules/RF_DualWield/Assets..." e nenhum clipe existe.
        ///
        /// Foi (B) no 2o teste (rgl_log_23844): merge do XML estava correto o tempo todo.
        /// </summary>
        private static string Diagnose(MBActionSet actionSet, in ActionIndexCache action)
        {
            string animation;
            try
            {
                animation = MBActionSet.GetActionAnimationName(actionSet, in action) ?? string.Empty;
            }
            catch
            {
                animation = string.Empty;
            }

            if (string.IsNullOrEmpty(animation))
            {
                return " [causa: BINDING AUSENTE - o action_set nao tem binding de animacao para esta " +
                       "action; conferir o merge de RF_DualWield/ModuleData/action_sets.xml em '" +
                       HumanWarriorActionSetName + "']";
            }

            int animationIndex;
            try
            {
                animationIndex = MBActionSet.GetAnimationIndexOfAction(actionSet, in action);
            }
            catch
            {
                animationIndex = -1;
            }

            if (animationIndex < 0)
            {
                return " [causa: CLIPE AUSENTE - binding existe e aponta a animacao '" + animation +
                       "', mas o engine nao carregou o clipe (indice " + animationIndex +
                       "); conferir RF_DualWield/Assets/" + animation +
                       "_anm.tpac e o .rdc correspondente em RF_DualWield/RuntimeDataCache]";
            }

            return " [causa desconhecida: binding aponta '" + animation + "' e o indice de animacao e " +
                   animationIndex + ", mas CheckActionAnimationClipExists devolveu false]";
        }
    }
}

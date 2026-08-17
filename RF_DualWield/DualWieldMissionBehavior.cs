using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_DualWield
{
    /// <summary>
    /// Unico comportamento de missao do modulo. Ele nao "concede" o dual wield: quem faz o dual
    /// wield funcionar sao os assets (action_types, action_sets, item_usage_sets, movement_sets) e a
    /// flag <c>ItemFlags.HeldInOffHand</c> no item de offhand. O papel desta classe e:
    ///
    ///   1. validar, uma vez por missao, que TODAS as actions do dual wield tem clipe de animacao;
    ///   2. impedir o CTD quando nao tem (removendo a arma de offhand do agente), SEMPRE com log;
    ///   3. como rede de seguranca tardia, forcar o wield da offhand se o caminho de dados do
    ///      vanilla nao a tiver empunhado.
    ///
    /// ORDEM DOS HOOKS (o que quebrou a rodada 1) -- de Mission.AfterStart() no vanilla decompilado:
    ///     1. subModule.OnBeforeMissionBehaviorInitialize(mission)
    ///     2. foreach behavior -> OnBehaviorInitialize()      &lt;-- roda AQUI
    ///     3. subModule.OnMissionBehaviorInitialize(mission)  &lt;-- registrar aqui e TARDE DEMAIS
    ///     4. foreach behavior -> EarlyStart()
    ///     5. foreach behavior -> AfterStart()
    /// Registrar o behavior no passo 3 faz o passo 2 nunca chamar o nosso OnBehaviorInitialize.
    /// Por isso o SubModule registra no passo 1, e a validacao ainda por cima e preguicosa
    /// (EnsureValidated) para nao depender de ordem de hook nenhuma.
    ///
    /// ORDEM DO SPAWN (o que quebrou o wield na rodada 1) -- de Mission.SpawnTroop():
    ///     BuildAgent(agent)  ->  EquipItemsFromSpawnEquipment  ->  OnAgentBuild(agent)   [nos]
    ///     ... e SO DEPOIS ...  agent.WieldInitialWeapons()
    /// Ou seja: em OnAgentBuild o wield ainda NAO aconteceu. Mexer no equipamento aqui sabota o
    /// wield que vem depois -- e forcar o wield aqui seria sobrescrito. Dai a lista de pendentes,
    /// processada um tick depois, quando WieldInitialWeapons ja rodou.
    /// </summary>
    public sealed class DualWieldMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        /// <summary>
        /// True somente quando a validacao da missao atual passou. Consumido pelo patch de colisao
        /// para nao alterar registro de golpe quando o dual wield esta desligado.
        /// </summary>
        internal static bool IsSystemHealthy { get; private set; }

        private bool _validated;
        private bool _loggedStrip;
        private bool _loggedForcedWield;

        // Agentes que passaram a validacao em OnAgentBuild e precisam de uma conferida de wield
        // no tick seguinte (quando WieldInitialWeapons do vanilla ja rodou).
        private readonly List<Agent> _pendingWieldCheck = new List<Agent>();

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            EnsureValidated();
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            EnsureValidated();
        }

        /// <summary>
        /// Validacao preguicosa e idempotente: roda no primeiro hook que chegar, seja
        /// OnBehaviorInitialize, EarlyStart ou o primeiro OnAgentBuild.
        /// </summary>
        private void EnsureValidated()
        {
            if (_validated)
            {
                return;
            }

            _validated = true;
            IsSystemHealthy = false;
            DualWieldActions.Reset();

            string unresolved;
            if (!DualWieldActions.TryResolveActions(out unresolved))
            {
                DualWieldLog.Warn(
                    "dual wield DESLIGADO nesta missao: a action '" + unresolved +
                    "' nao existe na tabela de action_types carregada. " +
                    "Confira RF_DualWield/ModuleData/action_types.xml e a linha soln_action_types em project.mbproj.");
                return;
            }

            MBActionSet humanWarrior = MBActionSet.GetActionSet(DualWieldActions.HumanWarriorActionSetName);
            if (!humanWarrior.IsValid)
            {
                DualWieldLog.Warn(
                    "dual wield DESLIGADO nesta missao: action set '" +
                    DualWieldActions.HumanWarriorActionSetName + "' nao resolveu.");
                return;
            }

            string missing;
            if (!DualWieldActions.ActionSetSupportsDualWield(humanWarrior, out missing))
            {
                DualWieldLog.Warn(
                    "dual wield DESLIGADO nesta missao: '" + DualWieldActions.HumanWarriorActionSetName +
                    "' nao tem clipe de animacao para '" + missing + "'. " +
                    "Isto e a causa do CTD nativo: sem clipe, o engine usa indice de animacao -1. " +
                    "Regere RF_DualWield/ModuleData/action_sets.xml com scripts/gen_action_sets.ps1.");
                return;
            }

            IsSystemHealthy = true;
            DualWieldLog.Info(
                "validacao OK (" + DualWieldActionTable.ActionNames.Length + " actions com clipe em '" +
                DualWieldActions.HumanWarriorActionSetName + "'). Dual wield ativo.");
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            // Guards antes de tocar em qualquer coisa do agente.
            if (agent == null || !agent.IsHuman || agent.IsMount)
            {
                return;
            }

            if (!agent.IsActive())
            {
                return;
            }

            MissionEquipment equipment = agent.Equipment;
            if (equipment == null)
            {
                return;
            }

            EquipmentIndex offhandSlot = FindOffhandSlot(equipment);
            if (offhandSlot == EquipmentIndex.None)
            {
                return;
            }

            EnsureValidated();

            if (IsSystemHealthy)
            {
                string missing;
                if (DualWieldActions.ActionSetSupportsDualWield(agent.ActionSet, out missing))
                {
                    // Action set do agente esta completo: NAO mexer no equipamento. Deixar o
                    // WieldInitialWeapons do vanilla (que roda depois deste hook) fazer o wield,
                    // e so conferir no tick seguinte.
                    _pendingWieldCheck.Add(agent);
                    return;
                }

                if (!_loggedStrip)
                {
                    _loggedStrip = true;
                    DualWieldLog.Warn(
                        "action set '" + SafeActionSetName(agent) + "' nao tem clipe para '" + missing +
                        "'. Removendo a arma de offhand desses agentes para evitar CTD. " +
                        "(Action sets raciais do RF_Races nao herdam as_human_warrior.)");
                }
            }
            else if (!_loggedStrip)
            {
                _loggedStrip = true;
                DualWieldLog.Warn(
                    "sistema invalidado nesta missao (ver aviso anterior). Removendo a arma de offhand " +
                    "dos agentes por seguranca.");
            }

            agent.RemoveEquippedWeapon(offhandSlot);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_pendingWieldCheck.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _pendingWieldCheck.Count; i++)
            {
                TryForceOffhandWield(_pendingWieldCheck[i]);
            }

            // Lista de uso unico: um tick depois do build ja e depois do WieldInitialWeapons.
            _pendingWieldCheck.Clear();
        }

        /// <summary>
        /// Rede de seguranca tardia. No vanilla, <c>Equipment.GetInitialWeaponIndicesToEquip</c>
        /// escolhe para a offhand qualquer item com <c>ItemFlags.HeldInOffHand</c> e chama
        /// <c>TryToWieldWeaponInSlot</c> -- portanto o caminho de dados deveria bastar. Se por algum
        /// motivo nao bastou, forcamos aqui uma unica vez, e logamos, para o autor saber por qual
        /// caminho a arma foi para a mao esquerda.
        /// </summary>
        private void TryForceOffhandWield(Agent agent)
        {
            if (agent == null || !agent.IsActive())
            {
                return;
            }

            MissionEquipment equipment = agent.Equipment;
            if (equipment == null)
            {
                return;
            }

            EquipmentIndex offhandSlot = FindOffhandSlot(equipment);
            if (offhandSlot == EquipmentIndex.None)
            {
                return;
            }

            if (agent.GetOffhandWieldedItemIndex() == offhandSlot)
            {
                // Caminho de dados do vanilla funcionou. Nada a fazer.
                return;
            }

            agent.TryToWieldWeaponInSlot(offhandSlot, Agent.WeaponWieldActionType.Instant, false);

            if (!_loggedForcedWield)
            {
                _loggedForcedWield = true;
                bool ok = agent.GetOffhandWieldedItemIndex() == offhandSlot;
                DualWieldLog.Info(
                    "wield da offhand forcado por codigo (o caminho de dados HeldInOffHand nao empunhou). " +
                    "Resultado: " + (ok ? "empunhada" : "AINDA NAO empunhada - checar ItemFlags/item_usage do item de offhand") + ".");
            }
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            base.OnAgentDeleted(affectedAgent);

            if (_pendingWieldCheck.Count > 0 && affectedAgent != null)
            {
                _pendingWieldCheck.Remove(affectedAgent);
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            IsSystemHealthy = false;
            _validated = false;
            _loggedStrip = false;
            _loggedForcedWield = false;
            _pendingWieldCheck.Clear();
            DualWieldActions.Reset();
        }

        private static EquipmentIndex FindOffhandSlot(MissionEquipment equipment)
        {
            for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot;
                 slot < EquipmentIndex.NumAllWeaponSlots;
                 slot++)
            {
                MissionWeapon weapon = equipment[slot];
                if (weapon.IsEmpty)
                {
                    continue;
                }

                WeaponComponentData usageItem = weapon.CurrentUsageItem;
                if (usageItem == null)
                {
                    continue;
                }

                if (usageItem.ItemUsage == DualWieldActions.OffhandItemUsage)
                {
                    return slot;
                }
            }

            return EquipmentIndex.None;
        }

        private static string SafeActionSetName(Agent agent)
        {
            MBActionSet set = agent.ActionSet;
            return set.IsValid ? set.GetName() : "(invalido)";
        }
    }
}

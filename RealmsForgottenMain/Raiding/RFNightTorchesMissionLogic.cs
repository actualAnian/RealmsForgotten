using System;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// TOCHAS NOTURNAS (2026-08-27) — porte do "Raise your Torch" para dentro do RF.
    ///
    /// Depois das 20h e antes das 4h, o mundo passa a carregar fogo:
    ///   - NA BATALHA, uma fracao das tropas de cada classe leva tocha acesa, e a
    ///     linha de batalha noturna deixa de ser uma mancha escura;
    ///   - NA ALDEIA E NA CIDADE, moradores andam pelas ruas com tocha na mao, e
    ///     voce tambem recebe a sua ao entrar.
    ///
    /// A tocha usada e a <see cref="RFArsonMissionLogic.CarriedTorchId"/>, cujo XML
    /// carrega as flags de off-hand (HeldInOffHand + HasToBeHeldUp +
    /// ForceAttachOffHandPrimaryItemBone + DropOnWeaponChange=false). Sao elas — e
    /// nao codigo — que fazem a tocha ficar erguida na mao esquerda sem atrapalhar
    /// a arma da direita. O prefab da tocha vanilla ja traz chama e luz.
    ///
    /// A distribuicao acontece no OnAgentBuild, um contador por classe: a cada N
    /// soldados daquela classe, um sai com tocha. Contador, nao sorteio — assim a
    /// proporcao e exata e nao ha aglomerado de fogo num canto so.
    /// </summary>
    public class RFNightTorchesMissionLogic : MissionLogic
    {
        private const float NightStartHour = 20f;
        private const float NightEndHour = 4f;

        /// <summary>
        /// Acima disto o motor ja esta no limite e nao ganhamos nada empilhando
        /// luzes e particulas (o "Raise your Torch" corta na mesma faixa).
        /// </summary>
        private const int AgentBudget = 1400;

        private enum SceneKind
        {
            Irrelevant,
            Battle,
            Settlement
        }

        private SceneKind _kind = SceneKind.Irrelevant;
        private ItemObject _torch;
        private readonly int[] _counters = new int[5];
        private int _civilianCounter;
        private int _torchesLit;

        public static bool IsNight(Mission mission)
        {
            try
            {
                float hour = mission?.Scene?.TimeOfDay ?? 12f;
                return hour > NightStartHour || hour < NightEndHour;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void AfterStart()
        {
            base.AfterStart();

            if (!RFRaidConfig.NightTorchesEnabled || !IsNight(Mission))
            {
                return;
            }

            _torch = MBObjectManager.Instance.GetObject<ItemObject>(RFArsonMissionLogic.CarriedTorchId);
            if (_torch == null)
            {
                Debug.Print("[RF_NightTorches] item '" + RFArsonMissionLogic.CarriedTorchId + "' nao carregou do XML.");
                return;
            }

            bool isSettlementScene = Mission.HasMissionBehavior<VillageMissionController>()
                                     || Mission.HasMissionBehavior<TownCenterMissionController>();

            if (isSettlementScene)
            {
                _kind = SceneKind.Settlement;
            }
            else if (Mission.CombatType == Mission.MissionCombatType.Combat && !Mission.IsFriendlyMission)
            {
                _kind = SceneKind.Battle;
            }

            if (_kind == SceneKind.Settlement)
            {
                // Cena de assentamento: os moradores ja estao em cena quando esta
                // logica sobe, entao a varredura inicial e necessaria (o
                // OnAgentBuild so pega quem nascer depois).
                LightExistingAgents();
            }
        }

        protected override void OnEndMission()
        {
            if (_torchesLit > 0)
            {
                Debug.Print($"[RF_NightTorches] {_torchesLit} tocha(s) acesa(s) nesta cena ({_kind}).");
            }
            base.OnEndMission();
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (_kind == SceneKind.Irrelevant || _torch == null || agent == null || !agent.IsHuman)
            {
                return;
            }
            if (Mission.AllAgents.Count >= AgentBudget)
            {
                return;
            }

            if (_kind == SceneKind.Settlement)
            {
                ConsiderSettlementAgent(agent);
            }
            else
            {
                ConsiderSoldier(agent);
            }
        }

        // ------------------------------------------------------------------
        //  batalha
        // ------------------------------------------------------------------

        /// <summary>
        /// Uma tocha a cada N soldados DAQUELA classe. Herois e o jogador ficam de
        /// fora: o jogador recebe a dele so em cena de assentamento, onde a tocha
        /// e conveniencia e nao estorvo no meio de uma carga.
        /// </summary>
        private void ConsiderSoldier(Agent agent)
        {
            if (agent.IsMainAgent || agent.Character == null || agent.Character.IsHero)
            {
                return;
            }

            int group = agent.Character.DefaultFormationGroup;
            if (group < 0 || group > 4)
            {
                group = 4;
            }

            int ratio = RatioForGroup(group);
            if (ratio <= 0)
            {
                return;
            }

            _counters[group]++;
            if (_counters[group] < ratio)
            {
                return;
            }
            _counters[group] = 0;

            GiveTorch(agent);
        }

        private static int RatioForGroup(int group)
        {
            switch (group)
            {
                case 0: // infantaria
                case 1: // arqueiros
                    return RFRaidConfig.NightTorchRatioFoot;
                case 2: // cavalaria
                case 3: // cavalaria montada de tiro
                    return RFRaidConfig.NightTorchRatioMounted;
                default:
                    return RFRaidConfig.NightTorchRatioFoot;
            }
        }

        // ------------------------------------------------------------------
        //  aldeia e cidade
        // ------------------------------------------------------------------

        private void LightExistingAgents()
        {
            foreach (Agent agent in Mission.Agents)
            {
                ConsiderSettlementAgent(agent);
            }
        }

        /// <summary>
        /// O jogador sempre recebe a sua; os moradores, um a cada N. Guardas e
        /// milicianos entram na conta dos moradores — um vigia noturno com tocha e
        /// exatamente o que se espera de uma rua as duas da manha.
        /// </summary>
        private void ConsiderSettlementAgent(Agent agent)
        {
            if (agent == null || !agent.IsHuman || !agent.IsActive())
            {
                return;
            }

            if (agent.IsMainAgent)
            {
                if (RFRaidConfig.NightTorchForPlayer)
                {
                    GiveTorch(agent);
                }
                return;
            }

            int ratio = RFRaidConfig.NightTorchRatioCivilian;
            if (ratio <= 0)
            {
                return;
            }

            _civilianCounter++;
            if (_civilianCounter < ratio)
            {
                return;
            }
            _civilianCounter = 0;

            GiveTorch(agent);
        }

        // ------------------------------------------------------------------

        private void GiveTorch(Agent agent)
        {
            try
            {
                if (!agent.Equipment[EquipmentIndex.ExtraWeaponSlot].IsEmpty)
                {
                    return;
                }
                MissionWeapon weapon = new MissionWeapon(_torch, null, null);
                agent.EquipWeaponWithNewEntity(EquipmentIndex.ExtraWeaponSlot, ref weapon);
                // Sem o wield a tocha fica equipada e invisivel — o passo que o
                // "Raise your Torch" nos ensinou.
                agent.TryToWieldWeaponInSlot(EquipmentIndex.ExtraWeaponSlot, Agent.WeaponWieldActionType.WithAnimation, false);
                _torchesLit++;
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_NightTorches] falha ao dar tocha: " + ex.Message);
            }
        }
    }
}

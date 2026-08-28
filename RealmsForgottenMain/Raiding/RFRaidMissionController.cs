using System;
using System.Collections.Generic;
using SandBox.Objects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// SAQUE A PE (porte do More Raiding, 2026-08-27).
    ///
    /// Voce entra na aldeia ou cidade como visitante e o saque comeca no momento
    /// em que voce ENCOSTA A MAO num morador — nao ha botao, nao ha menu: o
    /// primeiro golpe e a declaracao. A partir dai os moradores reagem um a um
    /// (ver <see cref="RFVillagerFightOrFlight"/>), as portas trancam, suas tropas
    /// entram na cena com voce e a milicia vem no relogio.
    ///
    /// O controller so e anexado a cenas de assentamento pacificas
    /// (<see cref="RFRaidCampaignBehavior"/> cuida disso); em batalha, cerco ou
    /// esconderijo ele nunca existe.
    /// </summary>
    public class RFRaidMissionController : MissionLogic
    {
        private const float ReactionInterval = 0.5f;
        private const float AlertRadiusStart = 40f;
        private const float AlertRadiusMax = 220f;
        private const float AlertRadiusGrowthPerSecond = 6f;
        private const float VictoryHoldSeconds = 6f;
        private const int DefencelessAgeCap = 20;

        private List<GameEntity> _exits = new List<GameEntity>();
        private readonly Dictionary<Agent, RFVillagerFightOrFlight> _reacting = new Dictionary<Agent, RFVillagerFightOrFlight>();
        private readonly List<Agent> _defenders = new List<Agent>();
        private readonly List<Agent> _finishedReacting = new List<Agent>();

        private GameEntity _defenderSpawn;
        private RFArsonMissionLogic _arson;

        /// <summary>Sistema de incendio desta missao (resolvido sob demanda).</summary>
        private RFArsonMissionLogic Arson =>
            _arson ?? (_arson = Mission?.GetMissionBehavior<RFArsonMissionLogic>());

        private MBList<Agent> _nearbyCache;
        private bool _isVillage;
        private bool _raidStarted;
        private bool _defendersSpawned;
        private bool _raidWon;
        private float _defenderCountdown;
        private readonly List<KeyValuePair<Agent, RFVillagerFightOrFlight>> _reactingSnapshot
            = new List<KeyValuePair<Agent, RFVillagerFightOrFlight>>();
        private float _reactionTimer;
        private float _alertRadius = AlertRadiusStart;
        private float _victoryTimer;
        private float _playerDownTimer = -1f;
        private int _defencelessKilled;

        public override void EarlyStart()
        {
            RFRaidState.ResetSceneFlags();

            Settlement settlement = PlayerEncounter.EncounterSettlement ?? Settlement.CurrentSettlement;
            _isVillage = settlement != null && settlement.IsVillage;

            _defenderSpawn = Mission.Scene.FindEntityWithTag("defender_infantry_reinforcement");
            Mission.Scene.GetAllEntitiesWithScriptComponent<PassageUsePoint>(ref _exits);

            // Na cidade o reforco entra pela passagem do salao do lorde / centro:
            // e de la que a guarnicao sairia de verdade.
            if (!_isVillage)
            {
                foreach (GameEntity exit in _exits)
                {
                    PassageUsePoint passage = exit?.GetFirstScriptOfType<PassageUsePoint>();
                    string toLocation = passage?.ToLocation?.StringId;
                    if (toLocation == null)
                    {
                        continue;
                    }
                    if (toLocation.Contains("lordshall")
                        || (Mission.Scene.IsAtmosphereIndoor && toLocation.Contains("center")))
                    {
                        _defenderSpawn = exit;
                        break;
                    }
                }
            }

            _defenderCountdown = _isVillage
                ? RFRaidConfig.MilitiaResponseVillage
                : RFRaidConfig.MilitiaResponseTown;

            RFRaidState.RaidSceneReady = true;
        }

        protected override void OnEndMission()
        {
            RFRaidState.ResetSceneFlags();
            _reacting.Clear();
            _defenders.Clear();
            base.OnEndMission();
        }

        // ------------------------------------------------------------------
        //  o gatilho: o primeiro golpe
        // ------------------------------------------------------------------

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon,
            in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (!RFRaidConfig.SceneRaidsEnabled || _raidStarted || Mission == null)
            {
                return;
            }
            if (affectorAgent == null || affectedAgent == null || !affectedAgent.IsHuman || !affectorAgent.IsHuman)
            {
                return;
            }
            if (!affectorAgent.IsPlayerControlled || affectorWeapon.IsEmpty)
            {
                return;
            }
            // Bater em quem ja e inimigo (uma briga de rua, um duelo) nao e saque.
            if (affectedAgent.Team == Mission.PlayerEnemyTeam)
            {
                return;
            }

            StartRaid();
        }

        private void StartRaid()
        {
            _raidStarted = true;
            RFRaidState.RaidInProgress = true;

            MakeCiviliansHostile();

            // A CHAVE do F1: as ordens visuais (F1-F8) vem do VisualOrderFactory,
            // cujo provider so esta disponivel quando !Mission.IsFriendlyMission.
            // Cena de vila nasce amigavel, entao o factory devolvia ZERO ordens e
            // as teclas morriam em silencio (a tecla 1 funcionava porque selecao
            // de formacao nao passa pelo factory). O saque tornou a missao
            // hostil de fato — declara-lo.
            Mission.IsFriendlyMission = false;

            Mission.SetMissionMode(MissionMode.Battle, false);
            Mission.PlayerTeam.SetPlayerRole(true, false);

            TextObject warning = _isVillage
                ? new TextObject("{=rf_raid_militia_soon}The village militia will be on you in {TIME} seconds!")
                : new TextObject("{=rf_raid_garrison_soon}The garrison will be on the streets in {TIME} seconds!");
            warning.SetTextVariable("TIME", (int)_defenderCountdown);
            MBInformationManager.AddQuickInformation(warning, 0, null, null, "");

            SpawnPlayerTroops();
            WirePlayerCommand();
            // NAO equipar o jogador com tochas: qualquer arremessavel guardado
            // com o prefab da tocha queima pendurado na cintura (a chama e do
            // prefab, nao ha como apagar so no coldre). Incendiar e trabalho
            // das tropas; o jogador comanda.
            Arson?.LightTheRaidersTorches();
        }

        /// <summary>
        /// Liga o F1 as tropas spawnadas. O OrderController do time nasce no
        /// inicio da missao, quando IsPlayerGeneral ainda e false — entao ele
        /// nunca marcou as formacoes como do jogador (formation.PlayerOwner), e
        /// IsFormationSelectable devolve "no troops under your command". Aqui
        /// refazemos o que o construtor teria feito se o saque ja existisse.
        /// </summary>
        private void WirePlayerCommand()
        {
            try
            {
                Team team = Mission.PlayerTeam;
                Agent player = Mission.MainAgent;
                if (team == null || player == null)
                {
                    return;
                }

                OrderController orders = team.PlayerOrderController;
                if (orders != null)
                {
                    orders.Owner = player;
                }
                foreach (Formation formation in team.FormationsIncludingEmpty)
                {
                    formation.PlayerOwner = player;
                }
                orders?.SelectAllFormations();

                // Ordem inicial: CARGA. Formacoes de jogador-general sem ordem
                // ficam paradas em posicao ("andam 3 passos e param") — o saque
                // precisa comecar em movimento. O F1 deixa o jogador retomar o
                // controle fino a qualquer momento.
                orders?.SetOrder(OrderType.Charge);
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Raid] falha ao ligar o comando de tropas: " + ex.Message);
            }
        }

        /// <summary>Todo civil vira inimigo — e a partir daqui que existe "lado".</summary>
        private void MakeCiviliansHostile()
        {
            foreach (Agent agent in Mission.Agents)
            {
                if (!agent.IsHuman || agent.IsPlayerControlled)
                {
                    continue;
                }
                if (agent.Team == Mission.PlayerTeam || agent.Team == Mission.PlayerAllyTeam)
                {
                    continue;
                }
                agent.SetTeam(Mission.PlayerEnemyTeam, false);
            }
            Mission.SetMissionCombatType(Mission.MissionCombatType.Combat);
        }

        // ------------------------------------------------------------------
        //  suas tropas entram na cena
        // ------------------------------------------------------------------

        private void SpawnPlayerTroops()
        {
            Agent player = Mission.MainAgent;
            if (player == null)
            {
                return;
            }

            // Interior aperta o numero: nao cabe um exercito dentro de uma casa.
            int allowance = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) + (Mission.Scene.IsAtmosphereIndoor ? 5 : 20);
            allowance = Math.Min(allowance, RFRaidConfig.MaxTroopsInScene);

            TroopRoster roster = PartyBase.MainParty?.MemberRoster;
            if (roster == null || allowance <= 0)
            {
                return;
            }

            List<CharacterObject> pool = FlattenHealthy(roster, allowance, includeHeroes: false);
            Vec3 origin = player.Position;
            int index = 0;

            foreach (CharacterObject character in pool)
            {
                if (character == null || character.IsPlayerCharacter)
                {
                    continue;
                }
                try
                {
                    Vec3 spot = origin + new Vec3(MBRandom.RandomFloatRanged(-4f, 4f), MBRandom.RandomFloatRanged(-4f, 4f), 0f, -1f);
                    Mission.SpawnTroop(
                        new PartyAgentOrigin(PartyBase.MainParty, character),
                        isPlayerSide: true,
                        hasFormation: true,
                        spawnWithHorse: false,
                        isReinforcement: false,
                        formationTroopCount: pool.Count,
                        formationTroopIndex: index++,
                        isAlarmed: true,
                        wieldInitialWeapons: true,
                        initialPosition: spot,
                        initialDirection: player.LookDirection.AsVec2);
                }
                catch (Exception ex)
                {
                    Debug.Print("[RF_Raid] falha ao spawnar tropa do jogador: " + ex.Message);
                }
            }
        }

        /// <summary>Achata o roster em personagens saudaveis, ate <paramref name="max"/>.</summary>
        private static List<CharacterObject> FlattenHealthy(TroopRoster roster, int max, bool includeHeroes)
        {
            List<CharacterObject> result = new List<CharacterObject>();
            foreach (TroopRosterElement element in roster.GetTroopRoster())
            {
                CharacterObject character = element.Character;
                if (character == null || (!includeHeroes && character.IsHero))
                {
                    continue;
                }
                int healthy = element.Number - element.WoundedNumber;
                for (int i = 0; i < healthy && result.Count < max; i++)
                {
                    result.Add(character);
                }
                if (result.Count >= max)
                {
                    break;
                }
            }
            return result;
        }

        // ------------------------------------------------------------------
        //  tick
        // ------------------------------------------------------------------

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_raidWon)
            {
                TickVictory(dt);
                return;
            }
            if (!_raidStarted)
            {
                // Entrada armada ("Storm the village"): o saque foi declarado no
                // mapa, entao comeca sozinho. Esperamos Agent.Main nascer porque
                // StartRaid entrega tochas e spawna tropas — sem o jogador na
                // cena nao ha em que mao colocar nada.
                if (Agent.Main != null && RFRaidState.ConsumeArmedEntry())
                {
                    StartRaid();
                }
                return;
            }

            _alertRadius = Math.Min(AlertRadiusMax, _alertRadius + AlertRadiusGrowthPerSecond * dt);

            _reactionTimer -= dt;
            if (_reactionTimer <= 0f)
            {
                _reactionTimer = ReactionInterval;
                TriggerNearbyReactions();
            }

            TickReactions(dt);
            TickPlayerDown(dt);
            Arson?.TickTroopArson(dt);

            if (!_defendersSpawned)
            {
                _defenderCountdown -= dt;
                if (_defenderCountdown <= 0f)
                {
                    SpawnDefenders();
                }
            }
            else if (AreDefendersBeaten())
            {
                _raidWon = true;
                _victoryTimer = VictoryHoldSeconds;
                CompleteRaid();
            }
        }

        /// <summary>Quem esta dentro do raio do alarme passa a reagir.</summary>
        private void TriggerNearbyReactions()
        {
            Agent player = Mission.MainAgent;
            if (player == null)
            {
                return;
            }
            if (_nearbyCache == null)
            {
                _nearbyCache = new MBList<Agent>();
            }

            _nearbyCache = Mission.GetNearbyEnemyAgents(player.Position.AsVec2, _alertRadius, Mission.PlayerTeam, _nearbyCache);
            foreach (Agent agent in _nearbyCache)
            {
                if (agent == null || !agent.IsHuman || agent.IsPlayerControlled
                    || agent.Team == Mission.PlayerTeam || _reacting.ContainsKey(agent)
                    || _defenders.Contains(agent))
                {
                    continue;
                }

                if (agent.Age < DefencelessAgeCap && !RFRaidConfig.GrimConsequencesEnabled)
                {
                    // Sem as consequencias sombrias, os indefesos apenas fogem e
                    // continuam INVULNERAVEIS — o vanilla ja os protege e nos nao
                    // tiramos essa protecao.
                    _reacting.Add(agent, new RFVillagerFightOrFlight(agent, _exits));
                    continue;
                }

                agent.SetMortalityState(Agent.MortalityState.Mortal);
                _reacting.Add(agent, new RFVillagerFightOrFlight(agent, _exits));
                agent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
                agent.DisableScriptedCombatMovement();
                agent.ClearTargetFrame();
                agent.DisableScriptedMovement();
                agent.ForceAiBehaviorSelection();
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
        }

        private void TickReactions(float dt)
        {
            _finishedReacting.Clear();
            // Snapshot obrigatorio: Tick pode chamar FadeOut, que remove o agente
            // SINCRONAMENTE -> OnAgentRemoved -> _reacting.Remove -> dicionario
            // mutado no meio do foreach (crash de 2026-08-28).
            _reactingSnapshot.Clear();
            foreach (KeyValuePair<Agent, RFVillagerFightOrFlight> pair in _reacting)
            {
                _reactingSnapshot.Add(pair);
            }
            foreach (KeyValuePair<Agent, RFVillagerFightOrFlight> pair in _reactingSnapshot)
            {
                if (!_reacting.ContainsKey(pair.Key))
                {
                    continue; // ja saiu durante o tick de outro
                }
                if (!pair.Value.Tick(dt))
                {
                    _finishedReacting.Add(pair.Key);
                }
            }
            foreach (Agent agent in _finishedReacting)
            {
                _reacting.Remove(agent);
            }
        }

        // ------------------------------------------------------------------
        //  a resposta armada
        // ------------------------------------------------------------------

        private void SpawnDefenders()
        {
            _defendersSpawned = true;

            Settlement settlement = PlayerEncounter.EncounterSettlement ?? Settlement.CurrentSettlement;
            TroopRoster roster = GetDefenderRoster(settlement, out PartyBase defenderParty);
            if (roster == null || defenderParty == null)
            {
                MBInformationManager.AddQuickInformation(
                    new TextObject("{=rf_raid_nodefenders}No one is left to defend this place."), 0, null, null, "");
                return;
            }

            int count = Math.Min(roster.TotalHealthyCount, _isVillage ? 20 : 30);
            List<CharacterObject> pool = FlattenHealthy(roster, count, includeHeroes: false);

            Vec3 spawnPoint = _defenderSpawn != null
                ? _defenderSpawn.GlobalPosition
                : (Mission.MainAgent?.Position ?? Vec3.Zero);

            int index = 0;
            foreach (CharacterObject character in pool)
            {
                try
                {
                    Vec3 spot = spawnPoint + new Vec3(MBRandom.RandomFloatRanged(-5f, 5f), MBRandom.RandomFloatRanged(-5f, 5f), 0f, -1f);
                    Agent defender = Mission.SpawnTroop(
                        new PartyAgentOrigin(defenderParty, character),
                        isPlayerSide: false,
                        hasFormation: true,
                        spawnWithHorse: false,
                        isReinforcement: true,
                        formationTroopCount: pool.Count,
                        formationTroopIndex: index++,
                        isAlarmed: true,
                        wieldInitialWeapons: true,
                        initialPosition: spot,
                        initialDirection: Vec2.Forward);
                    if (defender != null)
                    {
                        defender.SetWatchState(Agent.WatchState.Alarmed);
                        _defenders.Add(defender);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("[RF_Raid] falha ao spawnar defensor: " + ex.Message);
                }
            }

            MBInformationManager.AddQuickInformation(
                _isVillage
                    ? new TextObject("{=rf_raid_militia_here}The militia has arrived!")
                    : new TextObject("{=rf_raid_garrison_here}The garrison is on the street!"),
                0, null, null, "");
        }

        private static TroopRoster GetDefenderRoster(Settlement settlement, out PartyBase defenderParty)
        {
            defenderParty = null;
            if (settlement == null)
            {
                return null;
            }

            MobileParty militia = settlement.MilitiaPartyComponent?.MobileParty;
            if (militia != null && militia.MemberRoster.TotalHealthyCount > 0)
            {
                defenderParty = militia.Party;
                return militia.MemberRoster;
            }

            MobileParty garrison = settlement.Town?.GarrisonParty;
            if (garrison != null && garrison.MemberRoster.TotalHealthyCount > 0)
            {
                defenderParty = garrison.Party;
                return garrison.MemberRoster;
            }

            return null;
        }

        private bool AreDefendersBeaten()
        {
            foreach (Agent defender in _defenders)
            {
                if (defender != null && defender.IsActive() && defender.Health > 0f)
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------
        //  desfecho
        // ------------------------------------------------------------------

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (!_raidStarted || affectedAgent == null)
            {
                return;
            }
            _reacting.Remove(affectedAgent);

            if (RFRaidConfig.GrimConsequencesEnabled
                && agentState == AgentState.Killed
                && affectorAgent != null
                && affectorAgent.IsPlayerControlled
                && affectedAgent.Age < DefencelessAgeCap)
            {
                _defencelessKilled++;
            }
        }

        /// <summary>
        /// Cair no meio do saque nao e "game over": depois de alguns segundos no
        /// chao voce e arrastado para uma cela — o resto e com o
        /// RFRaidCampaignBehavior (dias servidos, fuga por Roguery). Com a opcao
        /// desligada, a missao apenas termina como o jogo faria.
        /// </summary>
        private void TickPlayerDown(float dt)
        {
            if (Agent.Main != null && Agent.Main.IsActive())
            {
                _playerDownTimer = -1f;
                return;
            }

            if (_playerDownTimer < 0f)
            {
                _playerDownTimer = 5f;
                return;
            }

            _playerDownTimer -= dt;
            if (_playerDownTimer > 0f)
            {
                return;
            }
            _playerDownTimer = -1f;
            RFRaidState.RaidInProgress = false;

            if (RFRaidConfig.HostageCaptureEnabled)
            {
                RFRaidCampaignBehavior.TakePlayerCaptive();
            }
            Mission.EndMission();
        }

        private void TickVictory(float dt)
        {
            _victoryTimer -= dt;
            if (_victoryTimer <= 0f)
            {
                RFRaidState.RaidInProgress = false;
                Mission.EndMission();
            }
        }

        /// <summary>Saque concluido: espolio, relacao e — se ligado — o peso na consciencia.</summary>
        private void CompleteRaid()
        {
            RFRaidState.RaidInProgress = false;

            Settlement settlement = PlayerEncounter.EncounterSettlement ?? Settlement.CurrentSettlement;
            if (settlement == null)
            {
                return;
            }

            try
            {
                TakeSpoils(settlement);
                ApplyRaidConsequences(settlement);
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Raid] falha ao fechar o saque: " + ex.Message);
            }

            MBInformationManager.AddQuickInformation(
                new TextObject("{=rf_raid_done}The place is yours. Take what you came for."), 0, null, null, "");
        }

        /// <summary>Comida do estoque do assentamento + ouro escalado por Roguery.</summary>
        private void TakeSpoils(Settlement settlement)
        {
            int roguery = Math.Max(1, Hero.MainHero.GetSkillValue(DefaultSkills.Roguery));
            ItemRoster loot = new ItemRoster();
            int budget = _isVillage ? MBRandom.RandomInt(12, 40) : MBRandom.RandomInt(24, 100);

            foreach (ItemRosterElement element in settlement.ItemRoster)
            {
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || !item.IsFood || element.Amount <= 0)
                {
                    continue;
                }
                int available = Math.Min(element.Amount, 8);
                int taken = MBRandom.RandomInt(1, available + 1) * (_isVillage ? 4 : 8);
                taken = Math.Min(taken, element.Amount);
                if (taken <= 0)
                {
                    continue;
                }
                loot.AddToCounts(element.EquipmentElement, taken);
                budget -= taken;
                if (budget <= 0)
                {
                    break;
                }
            }

            foreach (ItemRosterElement element in loot)
            {
                settlement.ItemRoster.AddToCounts(element.EquipmentElement, -element.Amount);
                PartyBase.MainParty.ItemRoster.AddToCounts(element.EquipmentElement, element.Amount);
            }

            if (loot.Count > 0)
            {
                CampaignEventDispatcher.Instance.OnItemsLooted(MobileParty.MainParty, loot);
            }

            float multiplier = roguery / 25f;
            int gold = _isVillage
                ? (int)(MBRandom.RandomInt(200, 800) * multiplier)
                : (int)(MBRandom.RandomInt(1000, 4000) * multiplier);
            if (gold > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, false);
            }
        }

        private void ApplyRaidConsequences(Settlement settlement)
        {
            Hero owner = settlement.OwnerClan?.Leader;
            if (owner != null && owner != Hero.MainHero)
            {
                ChangeRelationAction.ApplyPlayerRelation(owner, _isVillage ? -8 : -15, affectRelatives: true, showQuickNotification: true);
            }

            if (settlement.Village != null)
            {
                settlement.Village.Hearth = Math.Max(10f, settlement.Village.Hearth * 0.9f);
            }

            if (!RFRaidConfig.GrimConsequencesEnabled || _defencelessKilled <= 0)
            {
                return;
            }

            // Bloco sombrio (desligado por padrao): matar quem nao podia se
            // defender pesa no traco de Misericordia.
            int mercy = Hero.MainHero.GetTraitLevel(DefaultTraits.Mercy);
            if (mercy > DefaultTraits.Mercy.MinValue)
            {
                Hero.MainHero.SetTraitLevel(DefaultTraits.Mercy, mercy - 1);
                MBInformationManager.AddQuickInformation(
                    new TextObject("{=rf_raid_cruel}What you did here will be remembered."), 0, null, null, "");
            }
            _defencelessKilled = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using RealmsForgotten.RFEffects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.Raiding
{
    /// <summary>
    /// INCENDIO (2026-08-27, pedido do autor): tochas arremessadas pegam fogo onde
    /// caem, e o fogo cresce, se espalha pela construcao e queima quem chegar perto.
    ///
    /// Duas maos acendem a aldeia:
    ///   - VOCE joga a tocha (missil de verdade, com fisica) e o fogo nasce no ponto
    ///     exato do impacto contra a construcao;
    ///   - SEUS SOLDADOS tocam fogo sozinhos no que estiver por perto enquanto o
    ///     saque corre, para a aldeia arder inteira sem voce ter que mirar em cada
    ///     casa.
    ///
    /// PARTICULAS — leia antes de trocar qualquer nome daqui:
    /// a licao de 2026-08-26 e que "existe no XML" NAO significa "renderiza neste
    /// contexto" (o psys_game_burning_agent existe e nao aparece em agente). Por
    /// isso a primeira escolha e a particula que o autor JA VIU renderizar como
    /// efeito de mundo na cena do monasterio, e ha uma cadeia de reserva verificada
    /// no runtime. Nao substitua sem um teste A/B em jogo.
    /// </summary>
    public class RFArsonMissionLogic : MissionLogic
    {
        public const string ThrowingTorchId = "rf_throwing_torch";
        /// <summary>Tocha empunhada na mao esquerda (flags de off-hand no XML).</summary>
        public const string CarriedTorchId = "rf_carried_torch";
        private const EquipmentIndex TorchSlot = EquipmentIndex.ExtraWeaponSlot;

        /// <summary>Comprovada como efeito de MUNDO (cena do monasterio anorita).</summary>
        private static readonly string[] FireParticleCandidates =
        {
            "battleground_fire_smoke_square",
            "outdoor_siege_fire",
            "fire_ground"
        };

        private static readonly string[] SmokeParticleCandidates =
        {
            "outdoor_smoke_large",
            "outdoor_fire_smoke_large_smoke",
            "battleground_smoke_far"
        };

        // Teto de fogos simultaneos: 51 particulas de fogo+fumaca na cena do
        // monasterio ja pesaram no FPS (medicao 2026-08-26). Uma aldeia ardendo
        // nao precisa de mais que isto para parecer perdida.
        private const int MaxSimultaneousFires = 14;
        private const float FireLifetimeSeconds = 150f;
        private const float GrowthDelaySeconds = 7f;
        private const float SpreadIntervalSeconds = 14f;
        private const float SpreadRadius = 6f;
        private const float BurnRadius = 3.4f;
        private const float BurnHeightTolerance = 4f;
        private const float AgentScanInterval = 0.5f;
        private const float TroopArsonInterval = 6f;
        private const float TroopArsonRadius = 7f;
        private const int TorchesGrantedOnRaid = 5;
        /// <summary>Um em cada N soldados carrega tocha acesa durante o saque.</summary>
        private const int TorchBearerRatio = 3;
        private const float FireLightRadius = 9f;
        private const float FireLightIntensity = 26f;

        private sealed class BuildingFire
        {
            public Vec3 Position;
            public readonly List<GameEntity> Carriers = new List<GameEntity>();
            public float Age;
            public float NextSpread = SpreadIntervalSeconds;
            public bool Grown;
        }

        /// <summary>Arremesso no ar: se o impacto nao vier, o fogo acontece no ponto mirado.</summary>
        private struct PendingThrow
        {
            public Vec3 Landing;
            public float Timeout;
            public int MissileIndex;
        }

        /// <summary>
        /// CASA EM CHAMAS (redesign 2026-08-28). O fogo pertence a CONSTRUCAO,
        /// nao ao ponto de impacto: emissores presos as paredes e ao telhado da
        /// entity da casa, em estagios (parede -> telhado+fumaca -> tomada).
        /// As casas sao descobertas lendo a cena: entities cujo nome contem
        /// house/hut/hovel/cottage (convencao consistente em todas as culturas
        /// vanilla, conferida nos .xscene) com volume de construcao de verdade.
        /// </summary>
        private sealed class HouseFire
        {
            public GameEntity Entity;
            public Vec3 BoxMin;
            public Vec3 BoxMax;
            public float Age;
            public bool RoofStage;
            public bool FullStage;
            public bool Expired;
            public float NextSpread = SpreadIntervalSeconds;
            public readonly List<GameEntity> Carriers = new List<GameEntity>();
        }

        private const int MaxBurningHouses = 8;
        private const float HouseFireLifetime = 220f;
        private const float RoofStageSeconds = 8f;
        private const float FullStageSeconds = 22f;
        private const float TorchThrowRange = 14f;

        private readonly List<GameEntity> _houses = new List<GameEntity>();
        private readonly List<HouseFire> _houseFires = new List<HouseFire>();
        private bool _housesScanned;

        /// <summary>
        /// ESQUADRAO INCENDIARIO (redesign 2026-08-28). Portador de tocha nao
        /// participa da carga: recebe movimento roteirizado ate a casa
        /// reivindicada, arremessa ao chegar perto, e SO ENTAO vira combatente
        /// comum. Resolve o anda-e-para (portadores presos na carga, re-mirando
        /// civis em fuga a cada tick) e garante que as tochas voam de fato.
        /// </summary>
        private sealed class ArsonDuty
        {
            public Agent Bearer;
            public GameEntity House;
        }

        private readonly List<Agent> _bearers = new List<Agent>();
        private readonly List<ArsonDuty> _duties = new List<ArsonDuty>();
        private readonly HashSet<GameEntity> _claimedHouses = new HashSet<GameEntity>();
        private float _squadTimer;
        private const float SquadTickSeconds = 1.5f;
        private const float DutySearchRadius = 60f;
        private const float DutyThrowRadius = 8f;

        private readonly List<BuildingFire> _fires = new List<BuildingFire>();
        private readonly List<BuildingFire> _expired = new List<BuildingFire>();
        private readonly HashSet<int> _torchMissiles = new HashSet<int>();
        private readonly List<PendingThrow> _pendingThrows = new List<PendingThrow>();
        private float _agentScanTimer;
        private float _troopArsonTimer = TroopArsonInterval;
        private string _fireParticle;
        private string _smokeParticle;
        private bool _particlesResolved;

        /// <summary>Quantas construcoes o jogador incendiou nesta missao.</summary>
        public int FiresStarted { get; private set; }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_fires.Count == 0 && _houseFires.Count == 0)
            {
                return;
            }

            TickFires(dt);
            TickHouseFires(dt);

            _agentScanTimer -= dt;
            if (_agentScanTimer <= 0f)
            {
                _agentScanTimer = AgentScanInterval;
                BurnAgentsNearFires();
                BurnAgentsNearHouses();
                RFIgnition.CleanupExpired();
            }
        }

        protected override void OnEndMission()
        {
            foreach (BuildingFire fire in _fires)
            {
                RemoveFire(fire);
            }
            _fires.Clear();
            foreach (HouseFire houseFire in _houseFires)
            {
                foreach (GameEntity carrier in houseFire.Carriers)
                {
                    try
                    {
                        if (carrier != null)
                        {
                            carrier.Remove(0);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                houseFire.Carriers.Clear();
            }
            _houseFires.Clear();
            base.OnEndMission();
        }

        // ------------------------------------------------------------------
        //  a tocha arremessada
        // ------------------------------------------------------------------

        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            base.OnMissileHit(attacker, victim, isCanceled, collisionData);

            if (isCanceled || attacker == null || !RFRaidConfig.ArsonEnabled)
            {
                return;
            }
            // So tocha incendeia — sem este filtro, QUALQUER flecha perdida
            // punha gente em chamas (fogo amigo relatado em 2026-08-28).
            bool troopTorch = ConsumeTorchMissile(collisionData.AffectorWeaponSlotOrMissileIndex);
            if (!troopTorch && !IsThrowingTorch(attacker))
            {
                return;
            }
            // O missil reportou impacto: a rede de seguranca daquele arremesso
            // nao deve mais disparar, senao nasce um SEGUNDO fogo no ponto
            // projetado — que fica a frente do arremessador, no meio da tropa.
            CancelPendingThrow(collisionData.AffectorWeaponSlotOrMissileIndex);

            // Bateu numa pessoa: quem pega fogo e ela, nao a parede atras. Mas
            // aliado do arremessador nao acende — tropa em bloco arremessando
            // por cima do ombro nao pode fritar a propria linha de frente.
            if (collisionData.IsColliderAgent)
            {
                if (victim != null && victim.Team != attacker.Team)
                {
                    RFIgnition.Ignite(victim, 6f);
                }
                return;
            }
            if (!collisionData.EntityExists)
            {
                return;
            }

            if (IgniteAt(collisionData.CollisionGlobalPosition) && attacker.IsPlayerControlled)
            {
                FiresStarted++;
            }
        }

        /// <summary>A arma na mao do arremessador e a nossa tocha?</summary>
        private static bool IsThrowingTorch(Agent agent)
        {
            try
            {
                EquipmentIndex index = agent.GetPrimaryWieldedItemIndex();
                if (index == EquipmentIndex.None)
                {
                    return false;
                }
                ItemObject item = agent.Equipment[index].Item;
                return item != null && item.StringId == ThrowingTorchId;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ------------------------------------------------------------------
        //  o fogo
        // ------------------------------------------------------------------

        private void ResolveParticles()
        {
            if (_particlesResolved)
            {
                return;
            }
            _particlesResolved = true;
            _fireParticle = FirstAvailable(FireParticleCandidates);
            _smokeParticle = FirstAvailable(SmokeParticleCandidates);
            Debug.Print($"[RF_Arson] particulas resolvidas: fogo='{_fireParticle ?? "NENHUMA"}' fumaca='{_smokeParticle ?? "nenhuma"}'");
        }

        /// <summary>Regra do canario: so usamos o que o runtime confirma existir.</summary>
        private static string FirstAvailable(string[] candidates)
        {
            foreach (string name in candidates)
            {
                try
                {
                    if (ParticleSystemManager.GetRuntimeIdByName(name) != -1)
                    {
                        return name;
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        internal bool StartFire(Vec3 position)
        {
            ResolveParticles();
            if (_fireParticle == null || _fires.Count >= MaxSimultaneousFires)
            {
                return false;
            }
            // Nao empilhar fogo em cima de fogo: fica feio e custa FPS.
            foreach (BuildingFire existing in _fires)
            {
                if (existing.Position.Distance(position) < 3f)
                {
                    return false;
                }
            }

            BuildingFire fire = new BuildingFire { Position = position };
            if (!AttachParticle(fire, _fireParticle, position))
            {
                return false;
            }
            _fires.Add(fire);
            return true;
        }

        private bool AttachParticle(BuildingFire fire, string particleName, Vec3 position)
        {
            try
            {
                Scene scene = Mission.Scene;
                if (scene == null || particleName == null)
                {
                    return false;
                }

                GameEntity carrier = GameEntity.CreateEmpty(scene);
                MatrixFrame frame = new MatrixFrame(Mat3.Identity, position);
                carrier.SetFrame(ref frame);

                MatrixFrame local = new MatrixFrame(Mat3.Identity, default(Vec3));
                ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleName, carrier, ref local);
                if (particle == null)
                {
                    carrier.Remove(0);
                    return false;
                }
                // Fogo que nao ilumina nao convence: ponto de luz alaranjado com
                // tremulacao, a receita que o "Raise your Torch" usa nas tochas.
                // So no PRIMEIRO carrier de cada foco — uma luz por incendio.
                if (fire.Carriers.Count == 0)
                {
                    try
                    {
                        Light light = Light.CreatePointLight(FireLightRadius);
                        light.Intensity = FireLightIntensity;
                        light.LightColor = new Vec3(1f, 0.55f, 0.2f, -1f);
                        light.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, 1.2f, -1f));
                        light.SetLightFlicker(0.35f, 0.12f);
                        carrier.AddLight(light);
                    }
                    catch (Exception)
                    {
                    }
                }

                fire.Carriers.Add(carrier);
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Arson] falha ao acender: " + ex.Message);
                return false;
            }
        }

        private void TickFires(float dt)
        {
            _expired.Clear();

            // StartFire adiciona em _fires; enumerar a lista viva enquanto o
            // alastramento acontece derruba o enumerador (InvalidOperation,
            // crash de 2026-08-28 no "Storm the village"). Os spreads deste
            // tick nascem DEPOIS do loop.
            List<Vec3> spreads = null;

            foreach (BuildingFire fire in _fires)
            {
                fire.Age += dt;

                // Cresce: a coluna de fumaca so aparece quando a casa ja esta perdida.
                if (!fire.Grown && fire.Age >= GrowthDelaySeconds)
                {
                    fire.Grown = true;
                    if (_smokeParticle != null)
                    {
                        AttachParticle(fire, _smokeParticle, fire.Position + new Vec3(0f, 0f, 2.2f, -1f));
                    }
                }

                // Se alastra pela construcao enquanto houver folga de fogos.
                fire.NextSpread -= dt;
                if (fire.NextSpread <= 0f)
                {
                    fire.NextSpread = SpreadIntervalSeconds;
                    if (fire.Grown && _fires.Count < MaxSimultaneousFires)
                    {
                        Vec3 offset = new Vec3(
                            MBRandom.RandomFloatRanged(-SpreadRadius, SpreadRadius),
                            MBRandom.RandomFloatRanged(-SpreadRadius, SpreadRadius),
                            0f, -1f);
                        (spreads ?? (spreads = new List<Vec3>())).Add(fire.Position + offset);
                    }
                }

                if (fire.Age >= FireLifetimeSeconds)
                {
                    _expired.Add(fire);
                }
            }

            foreach (BuildingFire fire in _expired)
            {
                RemoveFire(fire);
                _fires.Remove(fire);
            }

            if (spreads != null)
            {
                foreach (Vec3 pos in spreads)
                {
                    if (_fires.Count >= MaxSimultaneousFires)
                    {
                        break;
                    }
                    StartFire(pos);
                }
            }
        }

        private static void RemoveFire(BuildingFire fire)
        {
            foreach (GameEntity carrier in fire.Carriers)
            {
                if (carrier == null)
                {
                    continue;
                }
                try
                {
                    carrier.RemoveAllParticleSystems();
                    carrier.Remove(0);
                }
                catch (Exception)
                {
                }
            }
            fire.Carriers.Clear();
        }

        /// <summary>Quem anda dentro das chamas queima — reusa o pipeline do RFIgnition.</summary>
        private void BurnAgentsNearFires()
        {
            foreach (Agent agent in Mission.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman)
                {
                    continue;
                }
                Vec3 position = agent.Position;
                foreach (BuildingFire fire in _fires)
                {
                    // Fogo recem-ateado nao queima: so a fogueira crescida (apos
                    // GrowthDelaySeconds) machuca. Sem esta graca, um fogo que
                    // nasce junto da formacao acendia a tropa inteira no susto.
                    if (!fire.Grown)
                    {
                        continue;
                    }
                    float dx = position.x - fire.Position.x;
                    float dy = position.y - fire.Position.y;
                    if (dx * dx + dy * dy > BurnRadius * BurnRadius)
                    {
                        continue;
                    }
                    if (Math.Abs(position.z - fire.Position.z) > BurnHeightTolerance)
                    {
                        continue;
                    }
                    RFIgnition.Ignite(agent, 4f);
                    break;
                }
            }
        }

        // ------------------------------------------------------------------
        //  seus soldados tambem incendeiam
        // ------------------------------------------------------------------

        /// <summary>
        /// O soldado saca uma tocha, ARREMESSA de verdade (missil com fisica, arco
        /// visivel) e o fogo nasce onde ela cai — o mesmo caminho do arremesso do
        /// jogador, nao um fogo que aparece do nada. Guardamos o indice do missil
        /// para reconhece-lo no impacto, porque o soldado nao esta "empunhando" a
        /// tocha no sentido que o OnMissileHit enxerga.
        /// </summary>
        internal void TickTroopArson(float dt)
        {
            if (!RFRaidConfig.ArsonEnabled || !RFRaidConfig.TroopArsonEnabled)
            {
                return;
            }

            TickPendingThrows(dt);

            _squadTimer -= dt;
            if (_squadTimer > 0f)
            {
                return;
            }
            _squadTimer = SquadTickSeconds;

            EnsureHousesScanned();
            if (_houses.Count == 0)
            {
                return;
            }

            AssignDuties();
            TickDuties();
        }

        /// <summary>Cada portador ocioso reivindica a casa livre mais proxima.</summary>
        private void AssignDuties()
        {
            for (int i = _bearers.Count - 1; i >= 0; i--)
            {
                Agent bearer = _bearers[i];
                if (bearer == null || !bearer.IsActive() || !HasCarriedTorch(bearer))
                {
                    _bearers.RemoveAt(i);
                    continue;
                }
                if (HasDuty(bearer))
                {
                    continue;
                }

                GameEntity best = null;
                float bestDist = DutySearchRadius;
                Vec3 pos = bearer.Position;
                foreach (GameEntity house in _houses)
                {
                    if (_claimedHouses.Contains(house) || IsHouseBurning(house))
                    {
                        continue;
                    }
                    float dist = pos.AsVec2.Distance(ClosestWallPoint(house, pos).AsVec2);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = house;
                    }
                }
                if (best != null)
                {
                    _claimedHouses.Add(best);
                    _duties.Add(new ArsonDuty { Bearer = bearer, House = best });
                }
            }
        }

        private bool HasDuty(Agent bearer)
        {
            foreach (ArsonDuty duty in _duties)
            {
                if (duty.Bearer == bearer)
                {
                    return true;
                }
            }
            return false;
        }

        private void TickDuties()
        {
            for (int i = _duties.Count - 1; i >= 0; i--)
            {
                ArsonDuty duty = _duties[i];
                Agent bearer = duty.Bearer;

                if (bearer == null || !bearer.IsActive() || !HasCarriedTorch(bearer)
                    || IsHouseBurning(duty.House))
                {
                    ReleaseDuty(i, disableMovement: bearer != null && bearer.IsActive());
                    continue;
                }

                // Inimigo em cima dele: solta o roteiro, deixa lutar. A missao
                // fica reivindicada; ele volta a ela quando a briga acabar.
                if (bearer.GetTargetAgent() != null)
                {
                    try
                    {
                        bearer.DisableScriptedMovement();
                    }
                    catch (Exception)
                    {
                    }
                    continue;
                }

                Vec3 wall = ClosestWallPoint(duty.House, bearer.Position);
                float dist = bearer.Position.AsVec2.Distance(wall.AsVec2);

                if (dist <= DutyThrowRadius)
                {
                    try
                    {
                        bearer.DisableScriptedMovement();
                    }
                    catch (Exception)
                    {
                    }
                    ThrowTorch(bearer, wall);
                    ReleaseDuty(i, disableMovement: false);
                    continue;
                }

                // A caminho: ponto 2.5m antes da parede, no lado do portador.
                try
                {
                    Vec2 back = (bearer.Position.AsVec2 - wall.AsVec2);
                    if (back.LengthSquared > 0.01f)
                    {
                        back.Normalize();
                    }
                    Vec2 stand2 = wall.AsVec2 + back * 2.5f;
                    Vec3 stand = new Vec3(stand2.x, stand2.y, bearer.Position.z, -1f);
                    WorldPosition wp = new WorldPosition(Mission.Scene, stand);
                    bearer.SetScriptedPosition(ref wp, false, Agent.AIScriptedFrameFlags.GoToPosition);
                }
                catch (Exception ex)
                {
                    Debug.Print("[RF_Arson] roteiro do incendiario falhou: " + ex.Message);
                    ReleaseDuty(i, disableMovement: false);
                }
            }
        }

        private void ReleaseDuty(int index, bool disableMovement)
        {
            ArsonDuty duty = _duties[index];
            _claimedHouses.Remove(duty.House);
            if (disableMovement)
            {
                try
                {
                    duty.Bearer.DisableScriptedMovement();
                }
                catch (Exception)
                {
                }
            }
            _duties.RemoveAt(index);
        }

        // ------------------------------------------------------------------
        //  as casas
        // ------------------------------------------------------------------

        private void EnsureHousesScanned()
        {
            if (_housesScanned)
            {
                return;
            }
            _housesScanned = true;
            try
            {
                List<GameEntity> all = new List<GameEntity>();
                Mission.Scene.GetEntities(ref all);
                foreach (GameEntity entity in all)
                {
                    if (entity == null || !LooksLikeHouse(entity.Name))
                    {
                        continue;
                    }
                    Vec3 min = entity.GlobalBoxMin;
                    Vec3 max = entity.GlobalBoxMax;
                    float footprint = (max.x - min.x) * (max.y - min.y);
                    float height = max.z - min.z;
                    // Volume de construcao de verdade: nem um adereco com "hut"
                    // no nome, nem o terreno inteiro.
                    if (footprint < 9f || footprint > 900f || height < 2.5f || height > 30f)
                    {
                        continue;
                    }
                    _houses.Add(entity);
                }
                Debug.Print("[RF_Arson] " + _houses.Count + " construcoes incendiaveis descobertas na cena.");
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Arson] varredura de casas falhou: " + ex.Message);
            }
        }

        private static bool LooksLikeHouse(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            string n = name.ToLowerInvariant();
            // Vocabulario levantado dos .xscene de TODAS as vilas vanilla
            // (2026-08-28): imperio/aserai/battania/sturgia moram em *house*/
            // hut_*/pict_houses; khuzait mora em TENT (khuzait_tent_a/b) — foi
            // a ausencia de "tent" que fez a khuzait_village_c dar zero casas.
            if (n.Contains("interior"))
            {
                return false; // o recheio da tenda, nao a tenda
            }
            return n.Contains("house") || n.Contains("hut_") || n.Contains("_hut")
                || n.Contains("hovel") || n.Contains("cottage") || n.Contains("tent")
                || n.Contains("barn") || n.Contains("stable") || n.Contains("mill_a")
                || n.Contains("windmill");
        }

        private bool IsHouseBurning(GameEntity house)
        {
            foreach (HouseFire fire in _houseFires)
            {
                if (!fire.Expired && fire.Entity == house)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Ponto na superficie lateral da casa mais proximo de quem olha.</summary>
        private static Vec3 ClosestWallPoint(GameEntity house, Vec3 from)
        {
            Vec3 min = house.GlobalBoxMin;
            Vec3 max = house.GlobalBoxMax;
            float x = Math.Max(min.x, Math.Min(max.x, from.x));
            float y = Math.Max(min.y, Math.Min(max.y, from.y));
            return new Vec3(x, y, min.z + Math.Min(2.2f, (max.z - min.z) * 0.45f), -1f);
        }

        /// <summary>
        /// Ponto de fogo pedido: se ha casa por perto, ELA pega fogo (emissores
        /// presos as paredes/telhado); senao, fogo de chao pequeno como antes.
        /// </summary>
        internal bool IgniteAt(Vec3 point)
        {
            EnsureHousesScanned();

            GameEntity best = null;
            float bestDist = 6f;
            foreach (GameEntity house in _houses)
            {
                if (IsHouseBurning(house))
                {
                    continue;
                }
                Vec3 min = house.GlobalBoxMin;
                Vec3 max = house.GlobalBoxMax;
                float dx = Math.Max(0f, Math.Max(min.x - point.x, point.x - max.x));
                float dy = Math.Max(0f, Math.Max(min.y - point.y, point.y - max.y));
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = house;
                }
            }

            if (best != null)
            {
                return IgniteHouse(best, point);
            }
            return StartFire(point);
        }

        private bool IgniteHouse(GameEntity house, Vec3 sparkPoint)
        {
            ResolveParticles();
            if (_fireParticle == null)
            {
                return false;
            }
            int burning = 0;
            foreach (HouseFire f in _houseFires)
            {
                if (!f.Expired)
                {
                    burning++;
                }
            }
            if (burning >= MaxBurningHouses)
            {
                return false;
            }

            HouseFire fire = new HouseFire
            {
                Entity = house,
                BoxMin = house.GlobalBoxMin,
                BoxMax = house.GlobalBoxMax,
            };

            // Estagio 1: o fogo nasce NA PAREDE onde a tocha bateu — dois
            // emissores presos a face, um no ponto do impacto, outro deslocado.
            Vec3 wall = ClosestWallPoint(house, sparkPoint);
            AttachHouseParticle(fire, _fireParticle, wall);
            Vec3 along = fire.BoxMax - fire.BoxMin;
            bool xLonger = Math.Abs(along.x) >= Math.Abs(along.y);
            Vec3 second = wall + (xLonger ? new Vec3(2.2f, 0f, 0.6f, -1f) : new Vec3(0f, 2.2f, 0.6f, -1f));
            second.x = Math.Max(fire.BoxMin.x, Math.Min(fire.BoxMax.x, second.x));
            second.y = Math.Max(fire.BoxMin.y, Math.Min(fire.BoxMax.y, second.y));
            AttachHouseParticle(fire, _fireParticle, second);

            _houseFires.Add(fire);
            Debug.Print("[RF_Arson] casa em chamas: " + house.Name);
            return true;
        }

        private void AttachHouseParticle(HouseFire fire, string particleName, Vec3 position)
        {
            try
            {
                GameEntity carrier = GameEntity.CreateEmpty(Mission.Scene);
                MatrixFrame frame = new MatrixFrame(Mat3.Identity, position);
                carrier.SetFrame(ref frame);
                MatrixFrame local = new MatrixFrame(Mat3.Identity, default(Vec3));
                ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleName, carrier, ref local);
                if (particle == null)
                {
                    carrier.Remove(0);
                    return;
                }
                if (fire.Carriers.Count == 0)
                {
                    try
                    {
                        Light light = Light.CreatePointLight(FireLightRadius * 1.6f);
                        light.Intensity = FireLightIntensity;
                        light.LightColor = new Vec3(1f, 0.55f, 0.2f, -1f);
                        light.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0f, 2.0f, -1f));
                        light.SetLightFlicker(0.35f, 0.12f);
                        carrier.AddLight(light);
                    }
                    catch (Exception)
                    {
                    }
                }
                fire.Carriers.Add(carrier);
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Arson] emissor de casa falhou: " + ex.Message);
            }
        }

        private void TickHouseFires(float dt)
        {
            for (int i = _houseFires.Count - 1; i >= 0; i--)
            {
                HouseFire fire = _houseFires[i];
                if (fire.Expired)
                {
                    continue;
                }
                fire.Age += dt;

                // Estagio 2: o telhado pega e a coluna de fumaca sobe.
                if (!fire.RoofStage && fire.Age >= RoofStageSeconds)
                {
                    fire.RoofStage = true;
                    Vec3 roof = new Vec3(
                        (fire.BoxMin.x + fire.BoxMax.x) * 0.5f,
                        (fire.BoxMin.y + fire.BoxMax.y) * 0.5f,
                        fire.BoxMax.z - 0.4f, -1f);
                    AttachHouseParticle(fire, _fireParticle, roof);
                    if (_smokeParticle != null)
                    {
                        AttachHouseParticle(fire, _smokeParticle, roof + new Vec3(0f, 0f, 1.2f, -1f));
                    }
                }

                // Estagio 3: a casa esta tomada — fogo tambem na outra face.
                if (!fire.FullStage && fire.Age >= FullStageSeconds)
                {
                    fire.FullStage = true;
                    Vec3 center = (fire.BoxMin + fire.BoxMax) * 0.5f;
                    Vec3 far = new Vec3(
                        fire.BoxMin.x + (fire.BoxMax.x - fire.BoxMin.x) * 0.8f,
                        fire.BoxMin.y + (fire.BoxMax.y - fire.BoxMin.y) * 0.8f,
                        fire.BoxMin.z + 1.5f, -1f);
                    AttachHouseParticle(fire, _fireParticle, far);
                }

                // A casa tomada contagia a vizinha mais proxima.
                if (fire.RoofStage)
                {
                    fire.NextSpread -= dt;
                    if (fire.NextSpread <= 0f)
                    {
                        fire.NextSpread = SpreadIntervalSeconds;
                        SpreadFromHouse(fire);
                    }
                }

                if (fire.Age >= HouseFireLifetime)
                {
                    fire.Expired = true;
                    foreach (GameEntity carrier in fire.Carriers)
                    {
                        try
                        {
                            if (carrier != null)
                            {
                                carrier.Remove(0);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    fire.Carriers.Clear();
                }
            }
        }

        private void SpreadFromHouse(HouseFire source)
        {
            Vec3 center = (source.BoxMin + source.BoxMax) * 0.5f;
            GameEntity best = null;
            float bestDist = 14f;
            foreach (GameEntity house in _houses)
            {
                if (house == source.Entity || IsHouseBurning(house))
                {
                    continue;
                }
                float dist = center.AsVec2.Distance(((house.GlobalBoxMin + house.GlobalBoxMax) * 0.5f).AsVec2);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = house;
                }
            }
            if (best != null)
            {
                IgniteHouse(best, center);
            }
        }

        /// <summary>Quem encosta numa casa em chamas (apos o telhado pegar) queima.</summary>
        private void BurnAgentsNearHouses()
        {
            foreach (Agent agent in Mission.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman)
                {
                    continue;
                }
                Vec3 pos = agent.Position;
                foreach (HouseFire fire in _houseFires)
                {
                    if (fire.Expired || !fire.RoofStage)
                    {
                        continue;
                    }
                    float dx = Math.Max(0f, Math.Max(fire.BoxMin.x - pos.x, pos.x - fire.BoxMax.x));
                    float dy = Math.Max(0f, Math.Max(fire.BoxMin.y - pos.y, pos.y - fire.BoxMax.y));
                    if (dx * dx + dy * dy > 2.0f * 2.0f)
                    {
                        continue;
                    }
                    if (pos.z > fire.BoxMax.z || pos.z < fire.BoxMin.z - 2f)
                    {
                        continue;
                    }
                    RFIgnition.Ignite(agent, 4f);
                    break;
                }
            }
        }

        private bool IsNearExistingFire(Vec3 point, float radius)
        {
            foreach (BuildingFire fire in _fires)
            {
                float dx = point.x - fire.Position.x;
                float dy = point.y - fire.Position.y;
                if (dx * dx + dy * dy < radius * radius)
                {
                    return true;
                }
            }
            return false;
        }


        private static bool HasCarriedTorch(Agent agent)
        {
            try
            {
                ItemObject item = agent.Equipment[TorchSlot].Item;
                return item != null && item.StringId == CarriedTorchId;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Acende as tochas do bando quando o saque comeca: um em cada
        /// <see cref="TorchBearerRatio"/> soldados passa a carregar uma tocha ACESA
        /// na mao esquerda (as flags de off-hand do item deixam ele lutar normalmente
        /// com a direita). Tecnica do "Raise your Torch": o item so precisa ser
        /// equipado e empunhado — o prefab da tocha traz chama e luz de graca.
        /// </summary>
        internal void LightTheRaidersTorches()
        {
            if (!RFRaidConfig.ArsonEnabled || !RFRaidConfig.TroopArsonEnabled)
            {
                return;
            }

            ItemObject torch = MBObjectManager.Instance.GetObject<ItemObject>(CarriedTorchId);
            if (torch == null)
            {
                Debug.Print("[RF_Arson] item '" + CarriedTorchId + "' nao carregou do XML — os soldados ficam sem tocha na mao.");
                return;
            }

            int counter = 0;
            int lit = 0;
            foreach (Agent agent in Mission.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman
                    || agent.IsPlayerControlled || agent.Team != Mission.PlayerTeam)
                {
                    continue;
                }
                counter++;
                if (counter % TorchBearerRatio != 0)
                {
                    continue;
                }
                if (GiveCarriedTorch(agent, torch))
                {
                    lit++;
                    _bearers.Add(agent);
                }
            }
            Debug.Print($"[RF_Arson] {lit} tocha(s) acesa(s) entre os saqueadores.");
        }

        private static bool GiveCarriedTorch(Agent agent, ItemObject torch)
        {
            try
            {
                if (!agent.Equipment[TorchSlot].IsEmpty)
                {
                    return false;
                }
                MissionWeapon weapon = new MissionWeapon(torch, null, null);
                agent.EquipWeaponWithNewEntity(TorchSlot, ref weapon);
                // Sem o wield a tocha fica equipada mas invisivel na mao.
                agent.TryToWieldWeaponInSlot(TorchSlot, Agent.WeaponWieldActionType.WithAnimation, false);

                // CRUCIAL: re-empunhar a arma principal. A tocha empunhada
                // sozinha virava a "arma" do soldado (classe banner, 6 de dano)
                // e a IA, sem arma de verdade, hesitava — o anda-e-para
                // relatado em 2026-08-28. As flags do item (HeldInOffHand +
                // DropOnWeaponChange=false) existem exatamente para a tocha
                // sobreviver na mao esquerda quando a direita empunha a arma.
                for (int slot = 0; slot < 4; slot++)
                {
                    MissionWeapon candidate = agent.Equipment[(EquipmentIndex)slot];
                    if (candidate.IsEmpty || candidate.Item?.PrimaryWeapon == null)
                    {
                        continue;
                    }
                    WeaponComponentData data = candidate.Item.PrimaryWeapon;
                    if (data.IsShield || !data.IsMeleeWeapon)
                    {
                        continue;
                    }
                    agent.TryToWieldWeaponInSlot((EquipmentIndex)slot,
                        Agent.WeaponWieldActionType.WithAnimation, false);
                    break;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Arson] falha ao acender tocha de soldado: " + ex.Message);
                return false;
            }
        }

        private void ThrowTorch(Agent thrower, Vec3 targetPoint)
        {
            try
            {
                ItemObject torch = MBObjectManager.Instance.GetObject<ItemObject>(ThrowingTorchId);
                if (torch == null)
                {
                    return;
                }

                Vec3 origin = thrower.GetEyeGlobalPosition();
                // Mira no alvo REAL (achado pelo leque de raios), com leve arco.
                Vec3 direction = (targetPoint - origin).NormalizedCopy();
                direction = (direction + new Vec3(0f, 0f, 0.35f, -1f)).NormalizedCopy();
                Vec3 landing = targetPoint;

                // A tocha arremessada e a que ele carregava: some da mao dele.
                if (HasCarriedTorch(thrower))
                {
                    try
                    {
                        thrower.RemoveEquippedWeapon(TorchSlot);
                    }
                    catch (Exception)
                    {
                    }
                }

                PlayThrowAnimation(thrower);

                MissionWeapon weapon = new MissionWeapon(torch, null, null, 1);
                Mission.Missile missile = Mission.AddCustomMissile(
                    thrower, weapon, origin, direction, Mat3.Identity,
                    baseSpeed: 12f, speed: 12f, addRigidBody: true, missionObjectToIgnore: null);

                if (missile != null)
                {
                    _torchMissiles.Add(missile.Index);
                }
                // Rede de seguranca: se o missil sumir sem reportar impacto (caiu
                // fora da borda, entrou na agua), o fogo acontece assim mesmo.
                _pendingThrows.Add(new PendingThrow { Landing = landing, Timeout = 5f, MissileIndex = missile?.Index ?? -1 });
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Arson] arremesso de tropa falhou: " + ex.Message);
            }
        }

        /// <summary>
        /// Anima o arremesso SO se o esqueleto daquele agente tiver o clipe — as
        /// racas do RF nem sempre tem, e tocar acao inexistente e CTD garantido
        /// (mesmo idioma do HuntableHerds e dos tableaus).
        /// </summary>
        private static void PlayThrowAnimation(Agent agent)
        {
            try
            {
                ActionIndexCache action = ActionIndexCache.Create("act_release_heavy_thrown");
                if (action.Index < 0)
                {
                    return;
                }
                MBActionSet actionSet = agent.ActionSet;
                if (!actionSet.IsValid || !MBActionSet.CheckActionAnimationClipExists(actionSet, action))
                {
                    return;
                }
                agent.SetActionChannel(1, action, ignorePriority: false, 0UL);
            }
            catch (Exception)
            {
            }
        }

        private void CancelPendingThrow(int missileIndex)
        {
            for (int i = _pendingThrows.Count - 1; i >= 0; i--)
            {
                if (_pendingThrows[i].MissileIndex == missileIndex)
                {
                    _pendingThrows.RemoveAt(i);
                }
            }
        }

        private void TickPendingThrows(float dt)
        {
            for (int i = _pendingThrows.Count - 1; i >= 0; i--)
            {
                PendingThrow pending = _pendingThrows[i];
                pending.Timeout -= dt;
                if (pending.Timeout > 0f)
                {
                    _pendingThrows[i] = pending;
                    continue;
                }
                _pendingThrows.RemoveAt(i);
                _torchMissiles.Remove(pending.MissileIndex);
                IgniteAt(pending.Landing);
            }
        }

        /// <summary>Consome o registro do missil quando ele finalmente bate.</summary>
        private bool ConsumeTorchMissile(int missileIndex)
        {
            if (!_torchMissiles.Remove(missileIndex))
            {
                return false;
            }
            for (int i = _pendingThrows.Count - 1; i >= 0; i--)
            {
                if (_pendingThrows[i].MissileIndex == missileIndex)
                {
                    _pendingThrows.RemoveAt(i);
                }
            }
            return true;
        }

        // ------------------------------------------------------------------
        //  as tochas na sua mao
        // ------------------------------------------------------------------

        /// <summary>Entrega tochas de arremesso ao jogador quando o saque comeca.</summary>
        internal static void GrantTorchesToPlayer()
        {
            try
            {
                Agent player = Agent.Main;
                if (player == null || !RFRaidConfig.ArsonEnabled)
                {
                    return;
                }

                ItemObject torch = MBObjectManager.Instance.GetObject<ItemObject>(ThrowingTorchId);
                if (torch == null)
                {
                    Debug.Print("[RF_Arson] item '" + ThrowingTorchId + "' nao existe — a tocha de arremesso nao foi carregada do XML.");
                    return;
                }

                EquipmentIndex slot = FindFreeWeaponSlot(player);
                if (slot == EquipmentIndex.None)
                {
                    return;
                }

                MissionWeapon weapon = new MissionWeapon(torch, null, null, (short)TorchesGrantedOnRaid);
                player.EquipWeaponWithNewEntity(slot, ref weapon);

                MBInformationManager.AddQuickInformation(
                    new TextObject("{=rf_arson_torches}You carry torches. Throw them at the roofs."), 0, null, null, "");
            }
            catch (Exception ex)
            {
                Debug.Print("[RF_Arson] falha ao entregar tochas: " + ex.Message);
            }
        }

        private static EquipmentIndex FindFreeWeaponSlot(Agent agent)
        {
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                if (agent.Equipment[i].IsEmpty)
                {
                    return i;
                }
            }
            return EquipmentIndex.None;
        }
    }
}

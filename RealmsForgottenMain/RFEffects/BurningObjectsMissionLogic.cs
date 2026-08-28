using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    /// <summary>
    /// Objetos em chamas da cena (fogueiras, braseiros...) ferem e INCENDEIAM quem
    /// chega perto — NPC e jogador igualmente (pedido do autor, 2026-08-26).
    ///
    /// Como funciona: no inicio da missao, uma unica varredura de todas as entidades da
    /// cena coleta as fontes de fogo por nome (lista de palavras-chave de prefabs de
    /// fogo). Depois, a cada meio segundo, agentes dentro do raio de uma fonte sao
    /// acesos via RFIgnition (particula de vitima + dano por segundo do
    /// MagicEffectsBehavior — o mesmo pipeline dos fire swords). Ficar dentro do fogo
    /// renova a queima; sair deixa o resto da duracao consumir sozinho.
    /// </summary>
    public sealed class BurningObjectsMissionLogic : MissionLogic
    {
        private const float ScanIntervalSeconds = 0.5f;
        private const float IgniteDurationSeconds = 4f;

        private readonly struct FireSource
        {
            public readonly Vec3 Position;
            public readonly float RadiusSquared;
            public readonly float HeightTolerance;

            public FireSource(Vec3 position, float radius, float heightTolerance)
            {
                Position = position;
                RadiusSquared = radius * radius;
                HeightTolerance = heightTolerance;
            }
        }

        // Calibrado com o dump real da cena do monastério anorita (2026-08-26):
        // as chamas de lá são entidades-FILHAS proprias — torch_flame_base (128x),
        // flame_loop, torch_outdoors_a_burning, torch_candle_b_fire, candle_flame.
        // Tochas queimam com raio CURTO (tem que quase encostar) e altura apertada
        // (tocha de parede nao assa quem passa embaixo); fogueiras grandes, raio cheio.
        private static readonly string[] TorchFlameKeywords =
        {
            "torch_flame", "torch_fire", "torch_candle", "torch_outdoors", "candle_flame", "flame_loop"
        };
        private const float TorchRadius = 0.9f;
        private const float TorchHeightTolerance = 2.0f;

        private static readonly string[] BigFireKeywords =
        {
            "campfire", "brazier", "bonfire", "fire_pit", "firepit", "fireplace", "burning"
        };
        private const float BigFireRadius = 1.35f;
        private const float BigFireHeightTolerance = 2.2f;

        private readonly List<FireSource> _fireSources = new();
        private float _scanTimer;
        private float _diagTimer;
        private bool _sourcesCollected;
        private int _taggedSourceCount;
        private static readonly bool _devDiag = System.IO.File.Exists(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "rf_dev_diag.flag"));

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        protected override void OnEndMission()
        {
            RFIgnition.Reset();
            base.OnEndMission();
        }

        // Coleta ADIADA ~2s para dentro da missao: no AfterStart os frames globais das
        // entidades-filhas de prefab ainda nao estao computados e GlobalPosition sai
        // errado (medicao 2026-08-26: "fonte mais proxima a 21m" num corredor cheio de
        // tochas).
        private float _collectDelay = 2f;

        private void TryCollectDeferred(float dt)
        {
            if (_sourcesCollected)
            {
                return;
            }
            _collectDelay -= dt;
            if (_collectDelay > 0f)
            {
                return;
            }
            try
            {
                CollectFireSources();
            }
            catch (Exception ex)
            {
                _sourcesCollected = true;
                Debug.Print("[RF_BurningObjects] coleta de fontes falhou: " + ex.Message);
            }
        }

        private void CollectFireSources()
        {
            _fireSources.Clear();
            _sourcesCollected = true;
            Scene scene = Mission.Scene;
            if (scene == null)
            {
                return;
            }

            // Os fogos de cena muitas vezes vivem como ENTIDADES-FILHAS de um prefab
            // (a raiz se chama "monastery_ruin_x", a chama e um filho "fire_big") —
            // por isso a varredura recursa na hierarquia inteira.
            List<GameEntity> roots = new();
            scene.GetEntities(ref roots);
            int visited = 0;
            System.Collections.Generic.Dictionary<string, int>? nameDump = CreateNameDumpIfDevFlag();

            foreach (GameEntity root in roots)
            {
                CollectRecursive(root, ref visited, nameDump);
            }

            if (nameDump != null)
            {
                WriteNameDump(nameDump, visited);
            }

            WriteSourceDumpIfDev();

            // GATE DE CENA SOCIAL (bug da taverna em chamas, 2026-08-26): em visita de
            // settlement (taverna, cidade, vila, keep) os NPCs sao autorados SENTADOS
            // em cima de velas e lareiras — o vanilla nunca quis que esse fogo
            // queimasse. 1ª tentativa usou Mission.CombatType, mas o log provou que a
            // taverna fica no DEFAULT Combat (nenhuma missao seta isso) e o gate nao
            // segurou. O sinal confiavel e CampaignMission.Current.Location: != null
            // exatamente nas missoes de location de settlement, null em batalha/cerco/
            // hideout/cena custom de quest (mesmo idioma do RFSettlementTorchMission-
            // Behavior). Cena de location so participa se o autor optou marcando
            // entidades com rf_fire (monasterio anorita: 51 tags).
            bool settlementLocation = TaleWorlds.CampaignSystem.CampaignMission.Current?.Location != null;
            if (settlementLocation && _taggedSourceCount == 0)
            {
                Debug.Print($"[RF_BurningObjects] cena social (location='{TaleWorlds.CampaignSystem.CampaignMission.Current.Location.StringId}', 0 tags rf_fire) — {_fireSources.Count} fonte(s) descartada(s), sistema inativo aqui.");
                _fireSources.Clear();
                return;
            }

            Debug.Print($"[RF_BurningObjects] {_fireSources.Count} fonte(s) de fogo na cena (de {visited} entidades visitadas, {_taggedSourceCount} com tag rf_fire).");
        }

        private void CollectRecursive(GameEntity entity, ref int visited, Dictionary<string, int>? nameDump)
        {
            if (entity == null)
            {
                return;
            }

            visited++;

            // TAG DE AUTOR: chamas colocadas no editor como particle effect solto viram
            // "empty_object" (75x na cena do monastério — indistinguíveis por nome).
            // O autor marca essas entidades com a tag `rf_fire` no editor e elas viram
            // zona de queima (raio pelo bbox; partícula sem caixa cai no piso 1,35m).
            if (entity.HasTag("rf_fire"))
            {
                AddFireSource(entity, BigFireRadius, BigFireHeightTolerance);
                _taggedSourceCount++;
            }

            string name = entity.Name;
            if (!string.IsNullOrEmpty(name))
            {
                string lower = name.ToLowerInvariant();
                if (nameDump != null)
                {
                    nameDump.TryGetValue(lower, out int count);
                    nameDump[lower] = count + 1;
                }

                // Tochas ANTES das fogueiras: "torch_outdoors_a_burning" contem
                // "burning" mas e tocha — raio-piso curto.
                bool matched = entity.HasTag("rf_fire");
                for (int k = 0; k < TorchFlameKeywords.Length; k++)
                {
                    if (lower.Contains(TorchFlameKeywords[k]))
                    {
                        AddFireSource(entity, TorchRadius, TorchHeightTolerance);
                        matched = true;
                        break;
                    }
                }
                if (!matched)
                {
                    for (int k = 0; k < BigFireKeywords.Length; k++)
                    {
                        if (lower.Contains(BigFireKeywords[k]))
                        {
                            AddFireSource(entity, BigFireRadius, BigFireHeightTolerance);
                            break;
                        }
                    }
                }
            }

            int childCount = entity.ChildCount;
            for (int i = 0; i < childCount; i++)
            {
                CollectRecursive(entity.GetChild(i), ref visited, nameDump);
            }
        }

        /// <summary>
        /// O raio de queima vem do TAMANHO REAL da chama (bounding box global da
        /// entidade — pedido do autor 2026-08-26): fogueira grande queima de longe,
        /// vela so encostando. O raio por categoria vira apenas PISO/fallback para
        /// entidades de particula sem caixa utilizavel. Centro = centro da caixa
        /// (nao a origem da entidade, que em chama de tocha fica no suporte).
        /// </summary>
        private void AddFireSource(GameEntity entity, float floorRadius, float floorHeightTolerance)
        {
            Vec3 position = entity.GlobalPosition;
            float radius = floorRadius;
            float heightTolerance = floorHeightTolerance;
            try
            {
                Vec3 boxMin = entity.GlobalBoxMin;
                Vec3 boxMax = entity.GlobalBoxMax;
                float extentX = boxMax.x - boxMin.x;
                float extentY = boxMax.y - boxMin.y;
                float extentZ = boxMax.z - boxMin.z;
                // Caixa degenerada (particula pura) ou absurda (prefab gigante): fica no piso.
                float halfHorizontal = Math.Max(extentX, extentY) * 0.5f;
                if (halfHorizontal > 0.05f && halfHorizontal < 6f)
                {
                    radius = Math.Max(floorRadius, halfHorizontal * 1.15f);
                    heightTolerance = Math.Max(floorHeightTolerance, extentZ * 0.75f + 0.5f);
                    position = (boxMin + boxMax) * 0.5f;
                }
            }
            catch
            {
            }

            _fireSources.Add(new FireSource(position, radius, heightTolerance));

            if (_devDiag)
            {
                _sourceDump.Add($"{entity.Name} pos=({position.x:0.0},{position.y:0.0},{position.z:0.0}) r={radius:0.00} ht={heightTolerance:0.00}");
            }
        }

        private readonly List<string> _sourceDump = new();

        private void WriteSourceDumpIfDev()
        {
            if (!_devDiag || _sourceDump.Count == 0)
            {
                return;
            }
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord", "Logs");
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllLines(System.IO.Path.Combine(dir, "RF_FireSources.log"), _sourceDump);
            }
            catch
            {
            }
        }

        /// <summary>Dump de nomes DEV-ONLY (arquivo-flag rf_dev_diag.flag): lista todos
        /// os nomes de entidade da cena p/ calibrar as keywords de fogo com evidencia.</summary>
        private static Dictionary<string, int>? CreateNameDumpIfDevFlag()
        {
            string flag = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord", "rf_dev_diag.flag");
            return System.IO.File.Exists(flag) ? new Dictionary<string, int>() : null;
        }

        private static void WriteNameDump(Dictionary<string, int> names, int visited)
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Mount and Blade II Bannerlord", "Logs");
                System.IO.Directory.CreateDirectory(dir);
                var lines = new List<string> { $"=== {DateTime.Now:HH:mm:ss} | {visited} entidades, {names.Count} nomes unicos ===" };
                foreach (var pair in names)
                {
                    lines.Add($"{pair.Value,5}x {pair.Key}");
                }
                lines.Sort(1, lines.Count - 1, StringComparer.Ordinal);
                System.IO.File.WriteAllLines(System.IO.Path.Combine(dir, "RF_SceneEntities.log"), lines);
            }
            catch
            {
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            TryCollectDeferred(dt);
            if (!_sourcesCollected || _fireSources.Count == 0 || Mission.Mode == MissionMode.Conversation || Mission.Mode == MissionMode.Barter)
            {
                return;
            }

            _scanTimer += dt;
            if (_scanTimer < ScanIntervalSeconds)
            {
                return;
            }
            _scanTimer = 0f;

            // Visuais de queima expirados/mortos saem aqui (o dano expira sozinho no
            // FireTick da casa; o visual e nosso — ver RFIgnition).
            RFIgnition.CleanupExpired();

            // Diagnostico dev-only: a cada ~2s, distancia do jogador a fonte mais
            // proxima — prova se as posicoes coletadas fazem sentido.
            _diagTimer += ScanIntervalSeconds;
            if (_devDiag && _diagTimer >= 2f && Agent.Main != null)
            {
                _diagTimer = 0f;
                Vec3 p = Agent.Main.Position;
                float bestD = float.MaxValue;
                float bestDz = 0f;
                Vec3 bestPos = Vec3.Zero;
                for (int i = 0; i < _fireSources.Count; i++)
                {
                    float ddx = p.x - _fireSources[i].Position.x;
                    float ddy = p.y - _fireSources[i].Position.y;
                    float d = ddx * ddx + ddy * ddy;
                    if (d < bestD)
                    {
                        bestD = d;
                        bestDz = p.z - _fireSources[i].Position.z;
                        bestPos = _fireSources[i].Position;
                    }
                }
                Debug.Print($"[RF_BurningObjects] player=({p.x:0.0},{p.y:0.0},{p.z:0.0}) fonteMaisProxima=({bestPos.x:0.0},{bestPos.y:0.0},{bestPos.z:0.0}) dh={Math.Sqrt(bestD):0.00} dz={bestDz:0.00}");
            }

            foreach (Agent agent in Mission.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman)
                {
                    continue;
                }

                Vec3 position = agent.Position;
                for (int i = 0; i < _fireSources.Count; i++)
                {
                    FireSource source = _fireSources[i];
                    float dx = position.x - source.Position.x;
                    float dy = position.y - source.Position.y;
                    if (dx * dx + dy * dy > source.RadiusSquared)
                    {
                        continue;
                    }
                    if (Math.Abs(position.z - source.Position.z) > source.HeightTolerance)
                    {
                        continue;
                    }

                    RFIgnition.Ignite(agent, IgniteDurationSeconds);
                    break;
                }
            }
        }
    }
}

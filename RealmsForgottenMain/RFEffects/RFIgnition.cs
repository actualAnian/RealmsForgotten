using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFEffects
{
    /// <summary>
    /// Ignicao de agentes (NPC/jogador). DANO: pipeline da casa (AgentEffectData
    /// "Fire" + FireTick). VISUAL: o UNICO tijolo comprovado na tela do autor —
    /// "fire_ground" anexado no padrao exato do TOWParticleSystem.ApplyParticleToAgentBone
    /// (entidade vazia filha do AgentVisuals + componente no osso) — replicado aqui em
    /// 4 OSSOS espalhados (pelvis/perna/peito/braco) com rastreamento PROPRIO de cada
    /// carrier (o helper da casa em multi-osso vaza: devolve so a ultima entidade).
    /// psys_game_burning_agent NAO renderiza neste contexto — nao usar (2026-08-26).
    /// </summary>
    public static class RFIgnition
    {
        public const string FireEffectId = "Fire";
        public const string VictimFireParticle = "fire_ground";

        /// <summary>Ossos do array do TOW ({0,1,2,3,5,6,7,9,12,13,15,17,22,24}) —
        /// subconjunto espalhado pelo corpo.</summary>
        private static readonly sbyte[] BurnBones = { 0, 3, 9, 13 };

        private sealed class BurnVisual
        {
            public readonly List<GameEntity> Carriers = new List<GameEntity>();
        }

        private static readonly Dictionary<Agent, BurnVisual> _visuals = new();

        /// <summary>Poe o agente em chamas por <paramref name="durationSeconds"/>.
        /// Se ja esta queimando, estende o timer e garante o visual.</summary>
        public static void Ignite(Agent victim, float durationSeconds)
        {
            MagicEffectsBehavior behavior = MagicEffectsBehavior.Instance;
            if (behavior == null || victim == null || !victim.IsActive() || !victim.IsHuman)
            {
                return;
            }

            var effects = behavior.AgentsUnderEffect;
            bool hasEffect = false;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Agent == victim && effects[i].Effect == FireEffectId)
                {
                    AgentEffectData refreshed = effects[i];
                    refreshed.Timer = new Timer(Time.ApplicationTime, durationSeconds, false);
                    effects[i] = refreshed;
                    hasEffect = true;
                    break;
                }
            }

            if (!hasEffect)
            {
                // Entity null: o RemoveEffect da casa nao toca no visual; a limpeza e
                // nossa (CleanupExpired), na ordem certa e por carrier.
                effects.Add(new AgentEffectData(victim, FireEffectId, new Timer(Time.ApplicationTime, durationSeconds, false), null));
                behavior.BurningEffectStopwatch[victim.Index] = new Timer(Time.ApplicationTime, 2f);
            }

            if (!_visuals.ContainsKey(victim))
            {
                BurnVisual visual = AttachBurningVisual(victim);
                if (visual != null && visual.Carriers.Count > 0)
                {
                    _visuals[victim] = visual;
                }
            }
        }

        /// <summary>Remove visuais de quem nao esta mais sob "Fire" (ou morreu).
        /// Chamado pelo BurningObjectsMissionLogic a cada varredura.</summary>
        public static void CleanupExpired()
        {
            if (_visuals.Count == 0)
            {
                return;
            }

            MagicEffectsBehavior behavior = MagicEffectsBehavior.Instance;
            List<Agent> done = null;
            foreach (var pair in _visuals)
            {
                Agent agent = pair.Key;
                bool burning = false;
                if (behavior != null && agent != null && agent.IsActive())
                {
                    var effects = behavior.AgentsUnderEffect;
                    for (int i = 0; i < effects.Count; i++)
                    {
                        if (effects[i].Agent == agent && effects[i].Effect == FireEffectId)
                        {
                            burning = true;
                            break;
                        }
                    }
                }
                if (!burning)
                {
                    (done ??= new List<Agent>()).Add(agent);
                }
            }

            if (done == null)
            {
                return;
            }
            foreach (Agent agent in done)
            {
                DetachBurningVisual(agent, _visuals[agent]);
                _visuals.Remove(agent);
            }
        }

        /// <summary>Fim de missao: descarta tudo.</summary>
        public static void Reset()
        {
            foreach (var pair in _visuals)
            {
                DetachBurningVisual(pair.Key, pair.Value);
            }
            _visuals.Clear();
        }

        /// <summary>Copia fiel do ApplyParticleToAgentBone da casa (o caminho que o
        /// autor VIU funcionar na tocha), por osso, com cada carrier rastreado.</summary>
        private static BurnVisual AttachBurningVisual(Agent agent)
        {
            try
            {
                Skeleton skeleton = agent.AgentVisuals?.GetSkeleton();
                Scene scene = Mission.Current?.Scene;
                if (skeleton == null || scene == null)
                {
                    return null;
                }
                if (ParticleSystemManager.GetRuntimeIdByName(VictimFireParticle) == -1)
                {
                    Debug.Print("[RF_Ignition] particula '" + VictimFireParticle + "' inexistente no runtime.");
                    return null;
                }

                BurnVisual visual = new BurnVisual();
                int boneCount = skeleton.GetBoneCount();
                foreach (sbyte bone in BurnBones)
                {
                    if (bone >= boneCount)
                    {
                        continue;
                    }

                    GameEntity carrier = GameEntity.CreateEmpty(scene);
                    MatrixFrame localFrame = new MatrixFrame(Mat3.Identity, default(Vec3));
                    ParticleSystem particle = ParticleSystem.CreateParticleSystemAttachedToEntity(VictimFireParticle, carrier, ref localFrame);
                    if (particle == null)
                    {
                        carrier.Remove(0);
                        continue;
                    }
                    agent.AgentVisuals.AddChildEntity(carrier);
                    skeleton.AddComponentToBone(bone, particle);
                    visual.Carriers.Add(carrier);
                }
                return visual;
            }
            catch (System.Exception ex)
            {
                Debug.Print("[RF_Ignition] AttachBurningVisual falhou: " + ex.Message);
                return null;
            }
        }

        private static void DetachBurningVisual(Agent agent, BurnVisual visual)
        {
            if (visual == null)
            {
                return;
            }
            foreach (GameEntity carrier in visual.Carriers)
            {
                if (carrier == null)
                {
                    continue;
                }
                try
                {
                    carrier.RemoveAllParticleSystems();
                    if (agent?.AgentVisuals != null)
                    {
                        agent.AgentVisuals.RemoveChildEntity(carrier, 0);
                    }
                    else
                    {
                        carrier.Remove(0);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Print("[RF_Ignition] DetachBurningVisual falhou: " + ex.Message);
                }
            }
            visual.Carriers.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.HumanAIComponent;

namespace RealmsForgotten.AiMade.Infect
{
    public class InfectionMissionBehavior : MissionLogic
    {
        private readonly Queue<(Agent dead, Agent killer)> _queue = new Queue<(Agent, Agent)>();
        private static readonly HashSet<string> InfectingIds = new HashSet<string>
        {
            "deformed_villager_bandit",
            "deformed_villager_raider",
            "deformed_villager_chief",
            "deformed_villager_boss",
            "ghorlag_the_unraveler"
        };

        // Probabilidade de infecção por morte válida
        private const float InfectionChance = 0.35f;

        public static InfectionMissionBehavior EnsureOn(Mission mission)
        {
            var beh = mission.GetMissionBehavior<InfectionMissionBehavior>();
            if (beh == null)
            {
                beh = new InfectionMissionBehavior();
                mission.AddMissionBehavior(beh);
            }
            return beh;
        }

        public void Enqueue(Agent dead, Agent killer)
        {
            _queue.Enqueue((dead, killer));
        }

        public override void OnMissionTick(float dt)
        {
            // Só processa em missões de batalha. Em Duel/Stealth a AI de formação geralmente não dirige combate aberto.
            if (Mission == null || Mission.Mode != MissionMode.Battle)
                return;

            // Processa no máximo alguns por tick para evitar burst
            int budget = 8;
            while (budget-- > 0 && _queue.Count > 0)
            {
                var (dead, killer) = _queue.Dequeue();
                TrySpawnInfected(dead, killer);
            }
        }

        private void TrySpawnInfected(Agent deadAgent, Agent killerAgent)
        {
            try
            {
                // Sanidade básica
                if (deadAgent == null || killerAgent == null) return;
                if (!deadAgent.IsHuman || deadAgent.IsHero) return;
                if (killerAgent.Character == null) return;
                if (!InfectingIds.Contains(killerAgent.Character.StringId)) return;

                // Chance de infecção
                if (MBRandom.RandomFloat > InfectionChance) return;

                // Precisa de times válidos e TeamAI (ambiente de batalha)
                Team spawnTeam = killerAgent.Team;
                if (spawnTeam == null || spawnTeam.TeamAI == null) return;

                // Escolher um template (usamos o próprio killer ou um fallback)
                CharacterObject template = killerAgent.Character as CharacterObject
                                           ?? CharacterObject.All.FirstOrDefault(c => c.StringId == "deformed_villager_bandit");
                if (template == null) return;

                // Origem simples
                var origin = new SimpleAgentOrigin(template);

                // Lado do player? (param bool isPlayerSide da API)
                bool isPlayerSide = (Mission.PlayerTeam != null && spawnTeam == Mission.PlayerTeam);

                // Posição inicial próxima do morto
                var pos = deadAgent.Position + new Vec3(MBRandom.RandomFloatRanged(-0.8f, 0.8f), MBRandom.RandomFloatRanged(-0.8f, 0.8f), 0f);
                var dir = (killerAgent.Position - deadAgent.Position).AsVec2;
                if (dir.LengthSquared < 0.001f) dir = Vec2.Forward;

                // Usamos Infantaria por padrão; o Mission.SpawnTroop com hasFormation:true + formationIndex
                // vai anexar o agente à formação e a FormationAI faz o resto.
                // Assinatura de SpawnTroop (parâmetros relevantes) confirmada no engine:
                // ... (IAgentOriginBase, bool isPlayerSide, bool hasFormation, bool spawnWithHorse, bool isReinforcement,
                //     int formationTroopCount, int formationTroopIndex, bool isAlarmed, bool wieldInitialWeapons,
                //     bool forceDismounted, Vec3? initialPosition, Vec2? initialDirection, string specialActionSetSuffix,
                //     ItemObject bannerItem, FormationClass formationIndex, bool useTroopClassForSpawn)  :contentReference[oaicite:2]{index=2}
                Agent spawned = Mission.SpawnTroop(
                    origin,
                    isPlayerSide,
                    hasFormation: true,
                    spawnWithHorse: false,
                    isReinforcement: true,
                    formationTroopCount: 1,
                    formationTroopIndex: 0,
                    isAlarmed: true,
                    wieldInitialWeapons: true,
                    initialPosition: pos,
                    initialDirection: dir,
                    specialActionSetSuffix: null,
                    bannerItem: null,
                    formationIndex: FormationClass.Infantry,
                    useTroopClassForSpawn: false
                );

                if (spawned != null)
                {
                    // Realça visual (debug)
                    spawned.AgentVisuals?.SetContourColor(Colors.Green.ToUnsignedInteger(), true);

                    // Agora a formação do time cuida das ordens (FormationAI já empurra CurrentOrder para a formação)
                    // Ver como o engine aplica ordens/AI de formação. :contentReference[oaicite:3]{index=3}
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[Infect] Spawn failed: {ex.Message}", Colors.Red));
            }
        }
    }
}
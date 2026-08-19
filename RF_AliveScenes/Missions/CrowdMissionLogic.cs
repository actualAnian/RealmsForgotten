using System;
using System.Collections.Generic;
using System.Linq;
using RF_AliveScenes.Config;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AliveScenes.Missions;

/// <summary>
/// Enche a cena de cidade com figurantes extras clonados dos proprios moradores.
///
/// Reescrito em relacao ao mod original, que:
///   - inseria tropas na lista viva Culture.NotableTemplates a cada clone (contaminava o
///     spawn de notaveis da campanha inteira ate reiniciar o jogo);
///   - nao tinha teto de agentes, so multiplicador 3-5.
/// Aqui o clone e sempre do MESMO CharacterObject do morador (seguro com RF_Races), a
/// aparencia varia pelo BodyPropertyRange do proprio personagem e ha teto duro por cena.
/// </summary>
public sealed class CrowdMissionLogic : MissionLogic
{
    private struct PendingSpawn
    {
        public CharacterObject Character;
        public MatrixFrame Frame;
        public Team Team;
        public float SpawnAt;
    }

    private readonly Dictionary<Agent, Vec3> _walkTargets = new();
    private readonly List<Vec3> _walkPoints = new();
    private readonly List<PendingSpawn> _pending = new();

    private float _clock;
    private bool _disabled;
    private bool _populated;
    private int _spawned;

    public override void EarlyStart()
    {
        base.EarlyStart();
        try
        {
            bool blockedLocation =
                Mission.HasMissionBehavior<TournamentBehavior>() ||
                IsAtLocation("arena") ||
                IsAtLocation("tavern") ||
                IsAtLocation("port");

            _disabled = blockedLocation || Mission.SceneName.Contains("arena_");

            if (!_disabled)
            {
                TaleWorlds.MountAndBlade.FaceGen.CreateInstance();
            }
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Multidao desligada nesta cena: " + e.Message);
            _disabled = true;
        }
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (_disabled || Mission.MainAgent == null)
        {
            return;
        }

        try
        {
            if (Mission.Scene != null && Mission.Scene.IsAtmosphereIndoor)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        _clock += dt;

        if (!_populated)
        {
            Populate();
            _populated = true;
        }

        FlushPendingSpawns();
        KeepWalking();
    }

    private void Populate()
    {
        AliveScenesSettings settings = AliveScenesSettings.Instance;
        CultureObject culture = Settlement.CurrentSettlement?.Culture;
        if (culture == null)
        {
            _disabled = true;
            return;
        }

        int budget = settings.CrowdMaxExtraAgents;

        foreach (Agent agent in Mission.Agents.ToList())
        {
            if (budget <= 0)
            {
                break;
            }
            if (!agent.IsHuman || agent.Character == null || agent.Character.IsHero)
            {
                continue;
            }

            CharacterObject character = agent.Character as CharacterObject;
            if (character == null || (character != culture.Townsman && character != culture.Townswoman))
            {
                continue;
            }

            _walkPoints.Add(agent.Position);

            int clones = MBRandom.RandomInt(settings.CrowdMultiplicationMin, settings.CrowdMultiplicationMax + 1);
            for (int i = 0; i < clones && budget > 0; i++)
            {
                budget--;
                _pending.Add(new PendingSpawn
                {
                    Character = character,
                    Frame = agent.Frame,
                    Team = agent.Team,
                    SpawnAt = _clock + MBRandom.RandomFloatRanged(0f, 6f)
                });
            }
        }

        if (_walkPoints.Count == 0)
        {
            _disabled = true;
        }
    }

    private void FlushPendingSpawns()
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            PendingSpawn pending = _pending[i];
            if (pending.SpawnAt > _clock)
            {
                continue;
            }

            _pending.RemoveAt(i);

            Agent agent = SpawnExtra(pending);
            if (agent == null)
            {
                continue;
            }

            _spawned++;
            SendTo(agent, RandomWalkPoint());
        }
    }

    private void KeepWalking()
    {
        if (_walkTargets.Count == 0)
        {
            return;
        }

        List<Agent> arrived = null;
        foreach (KeyValuePair<Agent, Vec3> pair in _walkTargets)
        {
            if (!pair.Key.IsActive())
            {
                (arrived ??= new List<Agent>()).Add(pair.Key);
                continue;
            }
            if (pair.Key.Position.Distance(pair.Value) < 0.8f)
            {
                (arrived ??= new List<Agent>()).Add(pair.Key);
            }
        }

        if (arrived == null)
        {
            return;
        }

        foreach (Agent agent in arrived)
        {
            _walkTargets.Remove(agent);
            if (agent.IsActive())
            {
                SendTo(agent, RandomWalkPoint());
            }
        }
    }

    private Vec3 RandomWalkPoint() => _walkPoints[MBRandom.RandomInt(_walkPoints.Count)];

    private void SendTo(Agent agent, Vec3 target)
    {
        try
        {
            WorldPosition position = new WorldPosition(Mission.Scene, target);
            // DoNotRun (16): o mesmo flag do mod original. Sem ele os figurantes CORREM
            // entre os pontos em vez de caminhar (visto no teste de 2026-08-18).
            agent.SetScriptedPosition(ref position, true, Agent.AIScriptedFrameFlags.DoNotRun);
            _walkTargets[agent] = target;
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao mandar figurante andar: " + e.Message);
        }
    }

    private Agent SpawnExtra(PendingSpawn pending)
    {
        try
        {
            CharacterObject character = pending.Character;

            MatrixFrame frame = new MatrixFrame(pending.Frame.rotation, pending.Frame.origin);
            frame.Strafe(MBRandom.RandomFloatRanged(-1.5f, 1.5f));
            frame.origin.z = Mission.Scene.GetGroundHeightAtPosition(frame.origin);

            Vec2 direction = frame.rotation.f.AsVec2.Normalized();

            Equipment equipment = character.CivilianEquipments != null && character.CivilianEquipments.Any()
                ? character.CivilianEquipments.GetRandomElementInefficiently()
                : character.Equipment;

            AgentBuildData buildData = new AgentBuildData(new AgentData(character))
                .Team(pending.Team)
                .InitialPosition(in frame.origin)
                .InitialDirection(in direction)
                .NoHorses(true)
                .Equipment(equipment)
                .Age(MBRandom.RandomInt(20, 70));

            // Rosto/corpo variados SEM tocar em nenhuma lista compartilhada da cultura:
            // tudo vem do proprio CharacterObject clonado, o que tambem mantem a raca
            // correta quando o RF_Races esta ativo.
            try
            {
                BodyProperties body = BodyProperties.GetRandomBodyProperties(
                    character.Race,
                    character.IsFemale,
                    character.GetBodyPropertiesMin(false),
                    character.GetBodyPropertiesMax(false),
                    equipment != null ? (int)equipment.HairCoverType : 0,
                    MBRandom.RandomInt(1, int.MaxValue),
                    character.BodyPropertyRange.HairTags,
                    character.BodyPropertyRange.BeardTags,
                    character.BodyPropertyRange.TattooTags,
                    0f);
                buildData.BodyProperties(body);
            }
            catch (Exception e)
            {
                Debug.Print("[RF_AliveScenes] BodyProperties padrao para o figurante: " + e.Message);
            }

            return Mission.SpawnAgent(buildData, false);
        }
        catch (Exception e)
        {
            Debug.Print("[RF_AliveScenes] Falha ao criar figurante: " + e.Message);
            return null;
        }
    }

    private static bool IsAtLocation(string locationId)
    {
        try
        {
            if (CampaignMission.Current == null || LocationComplex.Current == null)
            {
                return false;
            }
            return CampaignMission.Current.Location == LocationComplex.Current.GetLocationWithId(locationId);
        }
        catch
        {
            return false;
        }
    }
}

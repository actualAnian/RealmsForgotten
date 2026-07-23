using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Reinforcements
{
    /// <summary>
    /// RF-owned replacement for the retired ADODReinforcementsSystem.
    ///
    /// Design: when a player field battle starts, nearby map parties notice it.
    /// Whether they march in is a personality decision — war stance against
    /// BOTH sides (three-faction aware), family ties, relation with the player,
    /// and the lord's traits weighed against the current balance of power.
    /// Joiners "travel" (a timer proportional to map distance) and then stream
    /// onto the field from their own side's deployment area, horn first.
    ///
    /// Mechanics deliberately kept from the old system because they are the
    /// correct campaign-layer integration: the arriving party is added to the
    /// map event side (so casualties/loot/XP flow through the battle result)
    /// and its troops are spawned manually via Mission.SpawnTroop — the vanilla
    /// spawn logic freezes its ready-troop pool at mission start and will never
    /// supply late-joining parties itself. Deliberately dropped: the reflection
    /// hack on MissionAgentSpawnLogic._numSpawnedTroops (it skewed vanilla's
    /// reinforcement budget, usually for the wrong side) and every off-main-
    /// thread spawn path.
    /// </summary>
    public sealed class RFReinforcementsMissionLogic : MissionLogic
    {
        private const int DefenderSideIndex = 0;
        private const int AttackerSideIndex = 1;

        private sealed class PendingReinforcement
        {
            public MobileParty Party;
            public bool JoinsPlayerSide;
            public MissionTimer ArrivalTimer;
        }

        private readonly List<PendingReinforcement> _pending = new();
        private readonly Queue<IAgentOriginBase>[] _spawnQueues = { new Queue<IAgentOriginBase>(), new Queue<IAgentOriginBase>() };
        private readonly List<PendingReinforcement> _arrivedThisTick = new();
        private MissionTimer _spawnPumpTimer;
        private bool _scanned;
        private bool _stopped;

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);
            if (oldMissionMode != MissionMode.Deployment || _scanned || !RFReinforcementsConfig.Enabled)
            {
                return;
            }
            _scanned = true;

            Mission mission = Mission.Current;
            MapEvent playerEvent = MapEvent.PlayerMapEvent;
            if (mission?.PlayerTeam == null || playerEvent == null || Campaign.Current?.MainParty == null)
            {
                return;
            }

            BattleSideEnum playerSide = playerEvent.PlayerSide;
            BattleSideEnum enemySide = playerSide.GetOppositeSide();
            IFaction playerFaction = playerEvent.GetLeaderParty(playerSide)?.MapFaction;
            PartyBase enemyLeader = playerEvent.GetLeaderParty(enemySide);
            IFaction enemyFaction = enemyLeader?.MapFaction;
            bool banditFight = enemyLeader?.MobileParty != null && enemyLeader.MobileParty.IsBandit;
            if (playerFaction == null)
            {
                return;
            }

            playerEvent.RecalculateStrengthOfSides();
            float playerStrength = playerEvent.StrengthOfSide[(int)playerSide];
            float enemyStrength = playerEvent.StrengthOfSide[(int)enemySide];

            Vec2 playerPosition = Campaign.Current.MainParty.GetPosition2D;

            foreach (MobileParty party in MobileParty.AllLordParties)
            {
                if (!IsEligible(party, playerEvent, playerPosition))
                {
                    continue;
                }

                IFaction otherFaction = party.MapFaction;
                if (otherFaction == null)
                {
                    continue;
                }

                bool warWithPlayer = otherFaction != playerFaction && otherFaction.IsAtWarWith(playerFaction);
                bool warWithEnemy = enemyFaction != null && otherFaction != enemyFaction && otherFaction.IsAtWarWith(enemyFaction);

                if (warWithPlayer && warWithEnemy)
                {
                    // Hostile to everyone on the field — watches from the hills.
                    continue;
                }

                if (warWithPlayer)
                {
                    if (banditFight)
                    {
                        continue; // enemy lords stay out of the player's bandit fights
                    }
                    if (ComputeEnemyJoinScore(party, playerStrength, enemyStrength) >= RFReinforcementsConfig.JoinThreshold)
                    {
                        QueueArrival(party, joinsPlayerSide: false, playerPosition);
                    }
                }
                else if (warWithEnemy || HasPersonalTie(party))
                {
                    if (ComputeAllyJoinScore(party, playerStrength, enemyStrength) >= RFReinforcementsConfig.JoinThreshold)
                    {
                        QueueArrival(party, joinsPlayerSide: true, playerPosition);
                    }
                }
            }

            if (banditFight)
            {
                foreach (MobileParty bandit in MobileParty.AllBanditParties)
                {
                    if (!IsEligible(bandit, playerEvent, playerPosition) || bandit.IsEngaging
                        || (bandit.CurrentSettlement != null && bandit.CurrentSettlement.IsHideout))
                    {
                        continue;
                    }
                    // Brethren pile in unless the fight is already hopeless.
                    float share = (enemyStrength + bandit.Party.EstimatedStrength)
                                  / (playerStrength + enemyStrength + bandit.Party.EstimatedStrength);
                    if (share >= 0.2f)
                    {
                        QueueArrival(bandit, joinsPlayerSide: false, playerPosition, bandit: true);
                    }
                }
            }

            if (_pending.Count > 0)
            {
                _spawnPumpTimer = new MissionTimer(RFReinforcementsConfig.SpawnPumpIntervalSeconds);
                RFReinforcementsConfig.Debug($"[RFReinforcements] {_pending.Count} part(y/ies) marching to the battle.");
            }
        }

        private static bool IsEligible(MobileParty party, MapEvent playerEvent, Vec2 playerPosition)
        {
            if (party?.Party == null
                || party.IsMainParty
                || party.MapEvent != null
                || party.IsGarrison
                || party.CurrentSettlement != null
                || party.BesiegerCamp != null
                || party.MemberRoster == null
                || party.MemberRoster.TotalHealthyCount <= 0
                || playerEvent.InvolvedParties.Contains(party.Party)
                || party.MemberRoster.Contains(Hero.MainHero.CharacterObject))
            {
                return false;
            }
            // Attached army members act through their leader, never alone.
            if (party.Army != null && party.Army.LeaderParty != party && party.Army.DoesLeaderPartyAndAttachedPartiesContain(party))
            {
                return false;
            }
            return playerPosition.Distance(party.GetPosition2D) <= RFReinforcementsConfig.RadiusMapUnits;
        }

        private static bool HasPersonalTie(MobileParty party)
        {
            Hero leader = party.LeaderHero;
            if (leader == null)
            {
                return false;
            }
            Hero main = Hero.MainHero;
            if (leader == main.Father || leader == main.Mother || leader == main.Spouse
                || (main.Siblings != null && main.Siblings.Contains(leader))
                || leader.Father == main || leader.Mother == main)
            {
                return true;
            }
            if (leader.Clan != null && leader.Clan == main.Clan)
            {
                return true;
            }
            if (leader.MapFaction != null && leader.MapFaction == main.MapFaction)
            {
                return true;
            }
            return leader.GetRelationWithPlayer() >= 10f;
        }

        private static int PersonalTieScore(Hero leader)
        {
            Hero main = Hero.MainHero;
            if (leader == null)
            {
                return 0;
            }
            if (leader == main.Father || leader == main.Mother || leader == main.Spouse
                || (main.Siblings != null && main.Siblings.Contains(leader))
                || leader.Father == main || leader.Mother == main)
            {
                return 40;
            }
            if (leader.Clan != null && leader.Clan == main.Clan)
            {
                return 30;
            }
            if (leader.MapFaction != null && leader.MapFaction == main.MapFaction)
            {
                return 25;
            }
            return leader.GetRelationWithPlayer() >= 10f ? 15 : 0;
        }

        private static int ComputeAllyJoinScore(MobileParty party, float playerStrength, float enemyStrength)
        {
            Hero leader = party.LeaderHero;
            int score = PersonalTieScore(leader);

            if (leader != null)
            {
                score += (int)(MBMath.ClampFloat(leader.GetRelationWithPlayer(), -50f, 50f) * 0.4f);

                float playerShare = playerStrength / Math.Max(1f, playerStrength + enemyStrength);
                if (playerShare < 0.45f)
                {
                    if (leader.GetTraitLevel(DefaultTraits.Mercy) > 0) score += 15;
                    if (leader.GetTraitLevel(DefaultTraits.Valor) > 0) score += 10;
                    if (leader.GetTraitLevel(DefaultTraits.Calculating) > 0) score -= 10;
                }
                else if (playerShare > 0.6f && leader.GetTraitLevel(DefaultTraits.Calculating) > 0)
                {
                    score += 10; // bandwagoning is a calculating man's virtue
                }
            }

            score += MBRandom.RandomInt(-10, 11);
            return score;
        }

        private static int ComputeEnemyJoinScore(MobileParty party, float playerStrength, float enemyStrength)
        {
            // Being at war is motivation enough — the question is nerve.
            int score = 30;
            float strength = party.Army != null && party.Army.LeaderParty == party
                ? party.Army.EstimatedStrength
                : party.Party.EstimatedStrength;
            float shareAfterJoin = (enemyStrength + strength)
                                   / Math.Max(1f, playerStrength + enemyStrength + strength);

            Hero leader = party.LeaderHero;
            int valor = leader?.GetTraitLevel(DefaultTraits.Valor) ?? 0;
            int calculating = leader?.GetTraitLevel(DefaultTraits.Calculating) ?? 0;

            if (shareAfterJoin < 0.35f && valor <= 0)
            {
                score -= 20; // joining a rout takes courage they lack
            }
            if (valor < 0 && shareAfterJoin < 0.5f)
            {
                score -= 20;
            }
            if (calculating > 0 && shareAfterJoin > 0.5f)
            {
                score += 10;
            }

            score += MBRandom.RandomInt(-10, 11);
            return score;
        }

        private void QueueArrival(MobileParty party, bool joinsPlayerSide, Vec2 playerPosition, bool bandit = false)
        {
            float distance = playerPosition.Distance(party.GetPosition2D);
            float secondsPerUnit = bandit
                ? RFReinforcementsConfig.BanditSecondsPerMapUnit
                : RFReinforcementsConfig.SecondsPerMapUnit;
            float travelSeconds = Math.Max(15f, (distance - 3f) * secondsPerUnit);
            _pending.Add(new PendingReinforcement
            {
                Party = party,
                JoinsPlayerSide = joinsPlayerSide,
                ArrivalTimer = new MissionTimer(travelSeconds),
            });
            RFReinforcementsConfig.Debug($"[RFReinforcements] {party.Name} will arrive in {travelSeconds:F0}s ({(joinsPlayerSide ? "ally" : "enemy")}).");
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_stopped || (_pending.Count == 0 && _spawnQueues[0].Count == 0 && _spawnQueues[1].Count == 0))
            {
                return;
            }

            foreach (PendingReinforcement pending in _pending)
            {
                if (pending.ArrivalTimer.Check(false))
                {
                    _arrivedThisTick.Add(pending);
                }
            }
            if (_arrivedThisTick.Count > 0)
            {
                foreach (PendingReinforcement arrived in _arrivedThisTick)
                {
                    _pending.Remove(arrived);
                    Arrive(arrived);
                }
                _arrivedThisTick.Clear();
            }

            if (_spawnPumpTimer != null && _spawnPumpTimer.Check(true))
            {
                PumpSpawns(DefenderSideIndex);
                PumpSpawns(AttackerSideIndex);
            }
        }

        private void Arrive(PendingReinforcement arrival)
        {
            MobileParty party = arrival.Party;
            MapEvent playerEvent = MapEvent.PlayerMapEvent;
            if (playerEvent == null || party?.Party == null || party.MapEvent != null
                || party.MemberRoster == null || party.MemberRoster.TotalHealthyCount <= 0)
            {
                return; // the world moved on while they marched
            }

            BattleSideEnum joinSide = arrival.JoinsPlayerSide
                ? playerEvent.PlayerSide
                : playerEvent.PlayerSide.GetOppositeSide();
            MapEventSide eventSide = playerEvent.GetMapEventSide(joinSide);
            if (eventSide == null)
            {
                return;
            }

            List<MobileParty> joiningParties = new() { party };
            if (party.Army != null && party.Army.LeaderParty == party)
            {
                foreach (MobileParty attached in party.Army.Parties)
                {
                    if (attached != party
                        && party.Army.DoesLeaderPartyAndAttachedPartiesContain(attached)
                        && attached.MapEvent == null
                        && !attached.IsMainParty
                        && !playerEvent.InvolvedParties.Contains(attached.Party)
                        && !attached.MemberRoster.Contains(Hero.MainHero.CharacterObject))
                    {
                        joiningParties.Add(attached);
                    }
                }
            }

            int sideIndex = (int)joinSide == (int)BattleSideEnum.Defender ? DefenderSideIndex : AttackerSideIndex;
            foreach (MobileParty joiner in joiningParties)
            {
                joiner.MapEventSide = eventSide;
                joiner.Position = Campaign.Current.MainParty.Position;
                foreach (TroopRosterElement element in joiner.MemberRoster.GetTroopRoster())
                {
                    int healthy = element.Number - element.WoundedNumber;
                    for (int i = 0; i < healthy; i++)
                    {
                        _spawnQueues[sideIndex].Enqueue(new PartyAgentOrigin(joiner.Party, element.Character, -1, default, false));
                    }
                }
            }
            playerEvent.RecalculateStrengthOfSides();

            SoundEvent.PlaySound2D(arrival.JoinsPlayerSide
                ? "event:/alerts/horns/reinforcements"
                : "event:/alerts/horns/attack");
            TextObject message = new(arrival.JoinsPlayerSide
                ? "{=rf_reinf_ally}{PARTY} arrives to reinforce your side!"
                : "{=rf_reinf_enemy}{PARTY} arrives to reinforce the enemy!");
            message.SetTextVariable("PARTY", party.Name);
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(),
                arrival.JoinsPlayerSide ? Colors.Green : Colors.Red));
            RFReinforcementsChronicle.TryAddEvent($"{party.Name} joined a battle near {Hero.MainHero.Name}'s position as {(arrival.JoinsPlayerSide ? "an ally" : "an enemy")}.");
        }

        private void PumpSpawns(int sideIndex)
        {
            Mission mission = Mission.Current;
            Queue<IAgentOriginBase> queue = _spawnQueues[sideIndex];
            if (mission == null || queue.Count == 0 || mission.Agents.Count >= RFReinforcementsConfig.MaxMissionAgents)
            {
                return;
            }

            bool isPlayerSide = MapEvent.PlayerMapEvent != null
                && (int)MapEvent.PlayerMapEvent.PlayerSide == sideIndex;
            bool hasAllyTeam = sideIndex == DefenderSideIndex
                ? mission.DefenderAllyTeam != null
                : mission.AttackerAllyTeam != null;
            Team sideTeam = (sideIndex == DefenderSideIndex ? mission.DefenderTeam : mission.AttackerTeam) ?? mission.PlayerTeam;
            if (sideTeam == null)
            {
                return;
            }

            int burst = RFReinforcementsConfig.SpawnBurst;
            while (burst-- > 0 && queue.Count > 0 && mission.Agents.Count < RFReinforcementsConfig.MaxMissionAgents)
            {
                IAgentOriginBase origin = queue.Dequeue();
                try
                {
                    Vec2 spawnPosition = mission.GetFormationSpawnPosition(sideTeam, origin.Troop.DefaultFormationClass);
                    mission.SpawnTroop(
                        origin,
                        isPlayerSide,
                        hasAllyTeam,
                        true, true,
                        0,
                        origin.Troop.DefaultFormationGroup,
                        true, false,
                        spawnPosition.ToVec3(0f),
                        spawnPosition,
                        null,
                        null,
                        FormationClass.NumberOfAllFormations,
                        false);
                }
                catch (Exception e)
                {
                    RFReinforcementsConfig.Debug($"[RFReinforcements] spawn failed: {e.Message}");
                }
            }
        }

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            _stopped = true;
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            _stopped = true;
        }
    }

    /// <summary>
    /// Tunables with an optional plain-text override, following the
    /// promoted_config.txt precedent: drop rf_reinforcements_config.txt in the
    /// RealmsForgotten module root with lines like "Enabled=false" or
    /// "RadiusMapUnits=30" to change behavior without recompiling.
    /// </summary>
    public static class RFReinforcementsConfig
    {
        public static bool Enabled = true;
        public static float RadiusMapUnits = 20f;
        public static float SecondsPerMapUnit = 23f;
        public static float BanditSecondsPerMapUnit = 16f;
        public static int JoinThreshold = 25;
        public static int MaxMissionAgents = 1400;
        public static int SpawnBurst = 15;
        public static float SpawnPumpIntervalSeconds = 1f;
        public static bool DebugMessages = false;

        private static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;
            try
            {
                string path = System.IO.Path.Combine(ModuleHelper.GetModuleFullPath("RealmsForgotten"), "rf_reinforcements_config.txt");
                if (!File.Exists(path))
                {
                    return;
                }
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    int eq = line.IndexOf('=');
                    if (line.Length == 0 || line.StartsWith("#") || eq <= 0)
                    {
                        continue;
                    }
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case nameof(Enabled): bool.TryParse(value, out Enabled); break;
                        case nameof(RadiusMapUnits): float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out RadiusMapUnits); break;
                        case nameof(SecondsPerMapUnit): float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out SecondsPerMapUnit); break;
                        case nameof(BanditSecondsPerMapUnit): float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out BanditSecondsPerMapUnit); break;
                        case nameof(JoinThreshold): int.TryParse(value, out JoinThreshold); break;
                        case nameof(MaxMissionAgents): int.TryParse(value, out MaxMissionAgents); break;
                        case nameof(SpawnBurst): int.TryParse(value, out SpawnBurst); break;
                        case nameof(SpawnPumpIntervalSeconds): float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out SpawnPumpIntervalSeconds); break;
                        case nameof(DebugMessages): bool.TryParse(value, out DebugMessages); break;
                    }
                }
            }
            catch
            {
                // config is optional; defaults stand
            }
        }

        public static void Debug(string message)
        {
            if (DebugMessages)
            {
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
            }
        }
    }

    /// <summary>
    /// Reflection bridge to RF_AIDialog's WorldHistoryStore (no compile-time
    /// reference between the projects) — same pattern as HomesteadChronicle.
    /// </summary>
    internal static class RFReinforcementsChronicle
    {
        private static bool _resolved;
        private static PropertyInfo _instanceProperty;
        private static MethodInfo _addEventMethod;

        public static void TryAddEvent(string text)
        {
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    Type store = Type.GetType("RF_AIDialog.WorldHistoryStore, RF_AIDialog");
                    _instanceProperty = store?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _addEventMethod = store?.GetMethod("AddEvent", new[] { typeof(string), typeof(int) });
                }
                object instance = _instanceProperty?.GetValue(null);
                if (instance != null && _addEventMethod != null)
                {
                    _addEventMethod.Invoke(instance, new object[] { text, 1 });
                }
            }
            catch
            {
                // chronicle is flavor, never a failure point
            }
        }
    }
}

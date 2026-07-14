using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.InputSystem;
using RealmsForgotten.HuntableHerds.AgentComponents;
using RealmsForgotten.HuntableHerds.Models;
using TaleWorlds.Engine;
using RealmsForgotten.HuntableHerds.Extensions;
using Helpers;

namespace RealmsForgotten.HuntableHerds
{
    public class HerdMissionLogic : TaleWorlds.MountAndBlade.MissionLogic {
        private Dictionary<Agent, HerdAgentComponent> animals = new();

        // Hold Left Alt to track the herd: glowing contour on every animal,
        // visible through terrain/trees (same engine highlight battles use).
        // Orange = alive prey, green = carcass ready to loot (Q).
        private bool _trackingActive;
        private const uint TrackAliveColor = 0xFFFF8C1A;
        private const uint TrackDeadColor = 0xFF66FF66;

        private bool isRandomScene = true;
        private List<Vec3> playerSpawnPositions = new();
        private List<Vec3> animalSpawnPositions = new();

        public HerdMissionLogic(bool isRandomScene) {
            this.isRandomScene = isRandomScene;
        }

        public override void AfterStart() {
            if (!isRandomScene) {
                foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_player")) {
                    MatrixFrame globalFrame = entity.GetGlobalFrame();
                    playerSpawnPositions.Add(globalFrame.origin);
                }

                foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_herdanimal")) {
                    MatrixFrame globalFrame = entity.GetGlobalFrame();
                    animalSpawnPositions.Add(globalFrame.origin);
                }
            }
            SpawnPlayer();
            SubModule.PrintDebugMessage("Press Q nearby slain animals to skin and loot them!");
            SubModule.PrintDebugMessage("Hold Left Alt to track the herd (orange = alive, green = ready to loot).");
        }

        public override void OnMissionTick(float dt) {
            if (Agent.Main == null)
                return;

            UpdateHuntTracking();

            if (Input.IsKeyPressed(InputKey.Q))
                LootArea(10f);

            if (animals.Count >= HerdBuildData.CurrentHerdBuildData.TotalAmountInHerd)
                return;

            Vec3 position = isRandomScene ? Mission.Current.GetTrueRandomPositionAroundPoint(Agent.Main.Position, 20f, 500f) : GetRandomSpawnPosition(animalSpawnPositions);
            SpawnAnimalToHunt(position);
        }

        public override void OnAgentDeleted(Agent affectedAgent) {
            // Prune deleted agents so LootArea/OnMissionTick never dereference a
            // freed native Agent (invalid native access on long hunts) and the
            // herd-size count stays accurate. Deletion happens after the corpse
            // is cleaned up, so looting of slain-but-present animals is unaffected.
            if (affectedAgent != null)
                animals.Remove(affectedAgent);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow) {
            // Animal died while tracking was held: refresh its contour to the
            // "ready to loot" color.
            if (_trackingActive && affectedAgent != null && animals.ContainsKey(affectedAgent))
                SetTrackContour(affectedAgent, true);
        }

        private void UpdateHuntTracking() {
            bool track = Input.IsKeyDown(InputKey.LeftAlt);
            if (track == _trackingActive)
                return;

            _trackingActive = track;
            foreach (KeyValuePair<Agent, HerdAgentComponent> pair in animals)
                SetTrackContour(pair.Key, track);
        }

        private static void SetTrackContour(Agent agent, bool on) {
            try {
                var entity = agent?.AgentVisuals?.GetEntity();
                if (entity == null)
                    return;
                uint color = agent.IsActive() ? TrackAliveColor : TrackDeadColor;
                entity.SetContourColor(on ? color : (uint?)null, true);
            }
            catch {
                // A despawning agent's visuals can vanish between the null check
                // and the native call — losing one outline is fine.
            }
        }

        private void LootArea(float maxDistance) {
            ItemRoster fullItemRoster = new ItemRoster();
            List<HerdAgentComponent> huntableAgentsLooted = new();

            foreach (KeyValuePair<Agent, HerdAgentComponent> pair in animals) {
                if (pair.Key.IsActive() || pair.Value.GetItemDrops().IsEmpty() || pair.Key.Position.Distance(Agent.Main.Position) > maxDistance)
                    continue;
                fullItemRoster.Add(pair.Value.GetItemDrops());
                huntableAgentsLooted.Add(pair.Value);
            }

            if (huntableAgentsLooted.Count == 0) {
                SubModule.PrintDebugMessage("There's nothing to loot nearby...");
                return;
            }

            if (Settings.Instance.CrouchNeededEnabled && !Agent.Main.CrouchMode) {
                SubModule.PrintDebugMessage("You should crouch down (default: Z) to field dress and gather loot.");
                return;
            }

            InventoryScreenHelper.OpenScreenAsReceiveItems(fullItemRoster, new TextObject("Loot"), () => {
                foreach (HerdAgentComponent component in huntableAgentsLooted)
                    component.ClearItemDrops();
            });
        }

        private Agent SpawnPlayer() {
            MatrixFrame matrixFrame = MatrixFrame.Identity;
            CharacterObject playerCharacter = CharacterObject.PlayerCharacter;
            Vec3 centerPos = matrixFrame.origin;
            Mission.Scene.GetNavMeshCenterPosition(0, ref centerPos);
            Vec3 playerSpawnPos = isRandomScene ? Mission.Current.GetTrueRandomPositionAroundPoint(centerPos, 20f, 200f) : GetRandomSpawnPosition(playerSpawnPositions);
            AgentBuildData agentBuildData = new AgentBuildData(playerCharacter).Team(base.Mission.PlayerTeam).InitialPosition(playerSpawnPos);

            Vec2 vec = matrixFrame.rotation.f.AsVec2;
            vec = vec.Normalized();

            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).CivilianEquipment(false).NoHorses(false).NoWeapons(false).ClothingColor1(base.Mission.PlayerTeam.Color).ClothingColor2(base.Mission.PlayerTeam.Color2).TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default(UniqueTroopDescriptor), false)).MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed())).Controller(AgentControllerType.Player);
            Hero heroObject = playerCharacter.HeroObject;

            if (((heroObject != null) ? heroObject.ClanBanner : null) != null) {
                agentBuildData2.Banner(playerCharacter.HeroObject.ClanBanner);
            }

            Agent agent = base.Mission.SpawnAgent(agentBuildData2);

            for (int i = 0; i < 3; i++) {
                Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), true);
            }

            return agent;
        }

        private void SpawnAnimalToHunt(Vec3 position) {
            MatrixFrame frame = MatrixFrame.Identity;

            ItemObject spawnObject = Game.Current.ObjectManager.GetObject<ItemObject>(HerdBuildData.CurrentHerdBuildData.SpawnId);
            ItemRosterElement rosterElement = new ItemRosterElement(spawnObject);
            Vec2 initialDirection = frame.rotation.f.AsVec2;
            Agent agent = base.Mission.SpawnMonster(rosterElement, default(ItemRosterElement), in position, in initialDirection);

            HerdAgentComponent huntAgentComponent = HerdBuildData.CurrentHerdBuildData.IsPassive ? new PassiveHerdAgentComponent(agent) : new AggressiveHerdAgentComponent(agent);

            agent.AddComponent(huntAgentComponent);

            animals.Add(agent, huntAgentComponent);

            // Animals that spawn while the player is already holding the track
            // key get their outline immediately.
            if (_trackingActive)
                SetTrackContour(agent, true);

            for (int i = 0; i < 3; i++) {
                agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
            }
        }

        private Vec3 GetRandomSpawnPosition(List<Vec3> spawnPositions) {
            if (spawnPositions.Count == 0) {
                SubModule.PrintDebugMessage("spawn points aren't set up properly in this scene for hunting!!!");
                Vec3 playerSpawnFallback = Mission.Current.Scene.FindEntityWithName("sp_player").GlobalPosition;
                return Mission.Current.GetTrueRandomPositionAroundPoint(playerSpawnFallback, 20, 500, false);
            }
            int randomIndex = MBRandom.RandomInt(0, spawnPositions.Count);
            return spawnPositions[randomIndex];
        }
    }
}

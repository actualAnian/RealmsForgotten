using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.InputSystem;
using RealmsForgotten.HuntableHerds.AgentComponents;
using RealmsForgotten.HuntableHerds.Models;
using TaleWorlds.Engine;
using RealmsForgotten.HuntableHerds.Extensions;
using Helpers;

namespace RealmsForgotten.HuntableHerds
{
    public class HerdMissionLogic : TaleWorlds.MountAndBlade.MissionLogic, IRFLootableMission
    {
        private class PendingCarcass
        {
            public Agent Agent;
            public float Timer;
            public PendingCarcass(Agent agent, float timer)
            {
                Agent = agent;
                Timer = timer;
            }
        }

        private readonly Dictionary<Agent, HerdAgentComponent> animals = new();

        /// <summary>Carcasses the player can walk up to and loot individually.</summary>
        public Dictionary<Agent, Vec3> LootableAgents { get; } = new();

        /// <summary>Corpses waiting for the ragdoll to settle before they become lootable.</summary>
        private readonly List<PendingCarcass> _pendingCarcasses = new();

        private readonly bool isRandomScene = true;

        /// <summary>The herd of THIS hunt, captured when the mission was created.</summary>
        private readonly HerdBuildData _herd;

        private readonly List<Vec3> playerSpawnPositions = new();
        private readonly List<Vec3> animalSpawnPositions = new();

        public HerdMissionLogic(bool isRandomScene, HerdBuildData? herd = null)
        {
            this.isRandomScene = isRandomScene;
            _herd = herd ?? HerdBuildData.CurrentHerdBuildData ?? HerdBuildData.PickRandom(null)!;
        }

        public override void AfterStart()
        {
            try
            {
                if (!isRandomScene)
                {
                    foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_player"))
                    {
                        MatrixFrame globalFrame = entity.GetGlobalFrame();
                        playerSpawnPositions.Add(globalFrame.origin);
                    }

                    foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag("spawnpoint_herdanimal"))
                    {
                        MatrixFrame globalFrame = entity.GetGlobalFrame();
                        animalSpawnPositions.Add(globalFrame.origin);
                    }
                }
                SpawnPlayer();
                SubModule.PrintDebugMessage("Look at a slain animal and press the interaction key to skin it. Press Q to skin everything nearby.");
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: AfterStart failed ({e.Message})", 255, 0, 0);
            }
        }

        public override void OnMissionTick(float dt)
        {
            try
            {
                if (Agent.Main == null)
                    return;

                TickPendingCarcasses(dt);

                if (Input.IsKeyPressed(InputKey.Q))
                    LootArea(Settings.Instance.AreaLootRadius);

                if (animals.Count >= _herd.GetAliveAnimalCap())
                    return;

                Vec3 position = isRandomScene
                    ? Mission.Current.GetTrueRandomPositionAroundPoint(Agent.Main.Position, 20f, 500f)
                    : GetRandomSpawnPosition(animalSpawnPositions);
                SpawnAnimalToHunt(position);
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: mission tick failed ({e.Message})", 255, 0, 0);
            }
        }

        // ------------------------------------------------------------------------------------------
        // Individual corpse looting (shared with RF custom settlements through IRFLootableMission)
        // ------------------------------------------------------------------------------------------

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            try
            {
                if (affectedAgent == null || affectedAgent.GetComponent<LootableAgentComponent>() == null)
                    return;
                // Same 2s settle delay as CustomSettlementMissionLogic, but ticked on the main thread
                // instead of Task.Delay, so no game object is ever touched off-thread.
                _pendingCarcasses.Add(new PendingCarcass(affectedAgent, RFLootableMissionHelper.CorpseSettleDelay));
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: OnAgentRemoved failed ({e.Message})", 255, 0, 0);
            }
        }

        private void TickPendingCarcasses(float dt)
        {
            for (int i = _pendingCarcasses.Count - 1; i >= 0; i--)
            {
                PendingCarcass pending = _pendingCarcasses[i];
                pending.Timer -= dt;
                if (pending.Timer > 0f)
                    continue;

                _pendingCarcasses.RemoveAt(i);
                if (pending.Agent == null || LootableAgents.ContainsKey(pending.Agent))
                    continue;
                LootableAgents.Add(pending.Agent, RFLootableMissionHelper.GetCorpsePosition(pending.Agent));
            }
        }

        /// <summary>
        /// Makes a settled carcass a valid interaction target. Vanilla's FocusTick only focuses a
        /// dead agent when some MissionBehavior claims there is an action for it, so without this the
        /// "Loot" prompt would never appear in a standalone hunting mission.
        /// </summary>
        public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
        {
            try
            {
                return otherAgent != null && !otherAgent.IsActive() && LootableAgents.ContainsKey(otherAgent);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void OnAgentLooted(Agent agent)
        {
            try
            {
                if (agent == null || !LootableAgents.ContainsKey(agent))
                    return;

                LootableAgentComponent component = agent.GetComponent<LootableAgentComponent>();
                if (component == null)
                {
                    LootableAgents.Remove(agent);
                    return;
                }

                if (Settings.Instance.CrouchNeededEnabled && Agent.Main != null && !Agent.Main.CrouchMode)
                {
                    SubModule.PrintDebugMessage("You should crouch down (default: Z) to field dress and gather loot.");
                    return;
                }

                bool playSound = component.GetItemDrops().Count != 0 || component.GoldDrop != 0;
                foreach (ItemRosterElement item in component.GetItemDrops())
                {
                    EquipmentElement element = item.EquipmentElement;
                    MobileParty.MainParty.ItemRoster.AddToCounts(element, item.Amount);
                    SubModule.PrintDebugMessage("You looted " + item.Amount + " " + element.Item.Name + "!");
                }
                if (component.GoldDrop != 0)
                {
                    Hero.MainHero.ChangeHeroGold(component.GoldDrop);
                    SubModule.PrintDebugMessage("You found " + component.GoldDrop + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                }
                component.ClearItemDrops();

                if (playSound && Mission.MainAgent != null)
                {
                    int soundId = SoundEvent.GetEventIdFromString("event:/mission/combat/pickup_arrows");
                    if (soundId >= 0)
                        Mission.MakeSoundOnlyOnRelatedPeer(soundId, agent.Position, Mission.MainAgent.Index);
                }

                LootableAgents.Remove(agent);
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: looting a carcass failed ({e.Message})", 255, 0, 0);
            }
        }

        /// <summary>Legacy convenience: skin every carcass within <paramref name="maxDistance"/> at once.</summary>
        private void LootArea(float maxDistance)
        {
            ItemRoster fullItemRoster = new ItemRoster();
            List<Agent> lootedAgents = new();
            int goldLooted = 0;

            foreach (KeyValuePair<Agent, HerdAgentComponent> pair in animals)
            {
                if (pair.Key.IsActive() || pair.Value.GetItemDrops().IsEmpty() || pair.Key.Position.Distance(Agent.Main.Position) > maxDistance)
                    continue;
                fullItemRoster.Add(pair.Value.GetItemDrops());
                goldLooted += pair.Value.GoldDrop;
                lootedAgents.Add(pair.Key);
            }

            if (lootedAgents.Count == 0)
            {
                SubModule.PrintDebugMessage("There's nothing to loot nearby...");
                return;
            }

            if (Settings.Instance.CrouchNeededEnabled && !Agent.Main.CrouchMode)
            {
                SubModule.PrintDebugMessage("You should crouch down (default: Z) to field dress and gather loot.");
                return;
            }

            InventoryScreenHelper.OpenScreenAsReceiveItems(fullItemRoster, new TextObject("Loot"), () =>
            {
                foreach (Agent looted in lootedAgents)
                {
                    looted.GetComponent<LootableAgentComponent>()?.ClearItemDrops();
                    LootableAgents.Remove(looted);
                }
            });
        }

        // ------------------------------------------------------------------------------------------
        // Spawning
        // ------------------------------------------------------------------------------------------

        private Agent SpawnPlayer()
        {
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

            if (((heroObject != null) ? heroObject.ClanBanner : null) != null)
            {
                agentBuildData2.Banner(playerCharacter.HeroObject.ClanBanner);
            }

            Agent agent = base.Mission.SpawnAgent(agentBuildData2);

            for (int i = 0; i < 3; i++)
            {
                Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), true);
            }

            return agent;
        }

        private void SpawnAnimalToHunt(Vec3 position)
        {
            MatrixFrame frame = MatrixFrame.Identity;

            ItemObject spawnObject = Game.Current.ObjectManager.GetObject<ItemObject>(_herd.SpawnId);
            if (spawnObject == null)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: there is no item with id \"{_herd.SpawnId}\" to spawn.", 255, 0, 0);
                return;
            }

            ItemRosterElement rosterElement = new ItemRosterElement(spawnObject);
            Vec2 initialDirection = frame.rotation.f.AsVec2;
            Agent agent = base.Mission.SpawnMonster(rosterElement, default(ItemRosterElement), in position, in initialDirection);

            HerdAgentComponent huntAgentComponent = _herd.IsPassive
                ? new PassiveHerdAgentComponent(agent, _herd)
                : new AggressiveHerdAgentComponent(agent, _herd);

            agent.AddComponent(huntAgentComponent);

            animals.Add(agent, huntAgentComponent);

            for (int i = 0; i < 3; i++)
            {
                agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
            }
        }

        private Vec3 GetRandomSpawnPosition(List<Vec3> spawnPositions)
        {
            if (spawnPositions.Count == 0)
            {
                SubModule.PrintDebugMessage("spawn points aren't set up properly in this scene for hunting!!!");
                Vec3 playerSpawnFallback = Mission.Current.Scene.FindEntityWithName("sp_player").GlobalPosition;
                return Mission.Current.GetTrueRandomPositionAroundPoint(playerSpawnFallback, 20, 500, false);
            }
            int randomIndex = MBRandom.RandomInt(0, spawnPositions.Count);
            return spawnPositions[randomIndex];
        }
    }
}

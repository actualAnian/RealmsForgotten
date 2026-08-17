using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds
{
    /// <summary>
    /// Shared contract for every RF mission that lets the player loot individual corpses by
    /// looking at them and pressing the interaction key.
    /// <para>
    /// It lives in HuntableHerds (and NOT in RFCustomScenes) on purpose: RFCustomScenes already
    /// references HuntableHerds, so putting the abstraction here avoids a circular reference.
    /// Both <c>CustomSettlementMissionLogic</c> (RF custom settlements) and
    /// <c>HerdMissionLogic</c> (standalone hunting missions) implement it, and the loot gates in
    /// <c>RFCustomSettlements.Helper</c> / <c>ScenePatches</c> resolve this interface instead of a
    /// concrete mission-logic type.
    /// </para>
    /// </summary>
    public interface IRFLootableMission
    {
        /// <summary>Corpses that are currently lootable, mapped to the world position used for the focus ray cast.</summary>
        Dictionary<Agent, Vec3> LootableAgents { get; }

        /// <summary>Transfers the loot of a single corpse to the main party and drops it from <see cref="LootableAgents"/>.</summary>
        void OnAgentLooted(Agent agent);
    }

    public static class RFLootableMissionHelper
    {
        /// <summary>Seconds between the death of an agent and its corpse becoming lootable (lets the ragdoll settle).</summary>
        public const float CorpseSettleDelay = 2f;

        /// <summary>Returns the first <see cref="IRFLootableMission"/> behavior of the given mission, or null.</summary>
        public static IRFLootableMission? GetLootableMission(Mission? mission = null)
        {
            try
            {
                mission ??= Mission.Current;
                if (mission == null)
                    return null;
                return mission.MissionBehaviors.FirstOrDefault(behavior => behavior is IRFLootableMission) as IRFLootableMission;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Chest position of a corpse, falling back to the agent origin when the skeleton is already gone.</summary>
        public static Vec3 GetCorpsePosition(Agent agent)
        {
            try { return agent.GetChestGlobalPosition(); }
            catch (Exception) { return agent.Position; }
        }
    }
}

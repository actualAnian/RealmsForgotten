using HarmonyLib;
using System.Collections.Generic;
using RealmsForgotten.Patches;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.RFMissionLogic
{
    internal class DeferredMissionDamageBehavior : MissionBehavior
    {
        private readonly record struct DeferredAgentBlow(Agent Victim, Blow Blow, AttackCollisionData CollisionData);

        private static readonly List<DeferredAgentBlow> PendingAgentBlows = new();
        private static readonly object Sync = new();

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            base.AfterStart();
            ClearPending();
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            ClearPending();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            IncreaseAreaOfDamagePatch.ProcessPendingAreaDamage(Mission.Current);
            ProcessPendingAgentBlows();
        }

        public static void QueueAgentBlow(Agent victim, Blow blow, AttackCollisionData collisionData)
        {
            if (victim == null)
                return;

            lock (Sync)
            {
                PendingAgentBlows.Add(new DeferredAgentBlow(victim, blow, collisionData));
            }
        }

        private static void ProcessPendingAgentBlows()
        {
            List<DeferredAgentBlow> queued;
            lock (Sync)
            {
                if (PendingAgentBlows.Count == 0)
                    return;

                queued = new List<DeferredAgentBlow>(PendingAgentBlows);
                PendingAgentBlows.Clear();
            }

            foreach (DeferredAgentBlow entry in queued)
            {
                if (entry.Victim == null || !entry.Victim.IsActive())
                    continue;

                entry.Victim.RegisterBlow(entry.Blow, entry.CollisionData);
            }
        }

        private static void ClearPending()
        {
            lock (Sync)
            {
                PendingAgentBlows.Clear();
            }

            IncreaseAreaOfDamagePatch.ClearPendingAreaDamage();
        }
    }
}

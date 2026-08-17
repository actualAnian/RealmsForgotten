using RealmsForgotten.HuntableHerds.Models;
using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.HuntableHerds.AgentComponents
{
    public class HerdAgentComponent : LootableAgentComponent
    {
        /// <summary>
        /// The build data of THIS animal, captured at spawn time. Reading
        /// <see cref="HerdBuildData.CurrentHerdBuildData"/> every tick used to mean that any scene
        /// mixing herds (or any campaign-map re-roll) retuned live animals behind the player's back.
        /// </summary>
        public HerdBuildData Data { get; }

        public HerdAgentComponent(Agent agent, HerdBuildData data) : base(agent, data.GetCopyOfItemDrops())
        {
            Data = data;
            agent.Health = data.StartingHealth;
        }

        public override void OnTick(float dt)
        {
            try
            {
                if (Agent.Main == null || Agent == null || !Agent.IsActive())
                    return;

                HuntableAITick(dt);
            }
            catch (Exception e)
            {
                // A hunt must never be able to take the game down.
                SubModule.PrintDebugMessage($"HuntableHerds: animal AI tick failed ({e.Message})", 255, 120, 0);
            }
        }

        public virtual void HuntableAITick(float dt) { }

        public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon, in Blow b, in AttackCollisionData collisionData)
        {
            try
            {
                if (affectorAgent == null || affectorAgent == Agent)
                    return;

                if (Data.FleeOnAttacked)
                {
                    GoToPositionOppositeFromOtherAgent(affectorAgent);
                    return;
                }

                // Not a fleeing animal: getting shot is a provocation, not a reason to run.
                if (Settings.Instance.AggressiveAnimalsRetaliateWhenShot)
                    OnProvoked(affectorAgent);
            }
            catch (Exception e)
            {
                SubModule.PrintDebugMessage($"HuntableHerds: animal OnHit failed ({e.Message})", 255, 120, 0);
            }
        }

        /// <summary>Hook for aggressive animals: something hurt us, go get it.</summary>
        protected virtual void OnProvoked(Agent affectorAgent) { }

        public void SetMoveToPosition(WorldPosition position, bool addHumanLikeDelay = false, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None)
        {
            Agent.SetScriptedPosition(ref position, addHumanLikeDelay, flags);
        }

        public void GoToPositionOppositeFromOtherAgent(Agent otherAgent)
        {
            Vec3 differenceWithMult = (Agent.Position - otherAgent.Position) * 2;
            Vec3 gotoPosition = Agent.Position + differenceWithMult;
            SetMoveToPosition(gotoPosition.ToWorldPosition());
        }
    }
}

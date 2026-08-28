using System;
using System.Collections.Generic;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Raiding
{
    internal enum RFRaidReaction
    {
        Fight,
        Flee,
        BegForMercy
    }

    /// <summary>
    /// A alma do saque: cada morador decide POR CONTA PROPRIA o que fazer quando a
    /// violencia chega perto dele (porte do More Raiding, 2026-08-27).
    ///
    /// Quem tem arma de mao encara; quem nao tem corre para a saida mais proxima e
    /// some da cena ao chegar; e ha quem simplesmente se ajoelhe. Nao ha formacao
    /// nem ordem: e um monte de gente reagindo, e e isso que faz o saque parecer
    /// um saque em vez de uma batalha.
    /// </summary>
    internal sealed class RFVillagerFightOrFlight
    {
        private const float ArrivalDistance = 1f;
        private const float RefreshSeconds = 3f;

        public RFRaidReaction Reaction { get; private set; }
        public Agent Owner { get; }

        private readonly Mission _mission;
        private readonly List<GameEntity> _exits;
        private WorldPosition _fleeTarget;
        private float _timer;
        private bool _finished;

        public RFVillagerFightOrFlight(Agent agent, List<GameEntity> exits)
        {
            Owner = agent;
            _mission = Mission.Current;
            _exits = exits ?? new List<GameEntity>();
            _fleeTarget = WorldPosition.Invalid;
            Decide();
        }

        private void Decide()
        {
            if (Owner.Character != null && Owner.Character.IsFemale)
            {
                Reaction = RFRaidReaction.Flee;
                _fleeTarget = FindExit();
                return;
            }

            if (Owner.Equipment != null && Owner.Equipment.ContainsMeleeWeapon())
            {
                Reaction = RFRaidReaction.Fight;
                return;
            }

            // Desarmado: alguns imploram antes de correr.
            Reaction = MBRandom.RandomFloat < 0.25f ? RFRaidReaction.BegForMercy : RFRaidReaction.Flee;
            Owner.DisableScriptedMovement();
            Owner.DisableScriptedCombatMovement();
            _fleeTarget = FindExit();
        }

        /// <summary>
        /// Saida mais proxima entre as passagens da cena; se o ponto de fuga
        /// nativo do jogo estiver mais perto, ele ganha (evita mandar o morador
        /// atravessar a aldeia inteira passando pelo saqueador).
        /// </summary>
        private WorldPosition FindExit()
        {
            WorldPosition nativeFlee = _mission.GetClosestFleePositionForAgent(Owner);
            WorldPosition best = WorldPosition.Invalid;
            float bestDistance = float.MaxValue;

            foreach (GameEntity exit in _exits)
            {
                if (exit == null)
                {
                    continue;
                }
                float distance = exit.GlobalPosition.Distance(Owner.Position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new WorldPosition(_mission.Scene, exit.GlobalPosition);
                }
            }

            if (!best.IsValid)
            {
                return nativeFlee;
            }
            if (!nativeFlee.IsValid)
            {
                return best;
            }
            float toExit = best.AsVec2.Distance(Owner.Position.AsVec2);
            float toNative = nativeFlee.AsVec2.Distance(Owner.Position.AsVec2);
            return toExit <= toNative ? best : nativeFlee;
        }

        public bool Tick(float dt)
        {
            if (_finished || Owner == null || !Owner.IsActive() || Owner.Health <= 0f)
            {
                return false;
            }

            // Chegou na saida: sai de cena (nao morre — apenas escapou).
            if (_fleeTarget.IsValid && Owner.Position.AsVec2.Distance(_fleeTarget.AsVec2) < ArrivalDistance)
            {
                Owner.FadeOut(false, true);
                _finished = true;
                return false;
            }

            _timer -= dt;
            if (_timer > 0f)
            {
                return true;
            }
            _timer = RefreshSeconds;

            try
            {
                ApplyReaction();
            }
            catch (Exception)
            {
                // Um morador com estado estranho nunca pode derrubar o saque inteiro.
                _finished = true;
            }
            return !_finished;
        }

        private void ApplyReaction()
        {
            if (Owner.Team != _mission.PlayerEnemyTeam)
            {
                Owner.SetTeam(_mission.PlayerEnemyTeam, false);
            }
            if (Owner.CurrentWatchState != Agent.WatchState.Alarmed)
            {
                Owner.SetWatchState(Agent.WatchState.Alarmed);
            }
            if (Owner.IsUsingGameObject)
            {
                Owner.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
            }

            if (Reaction == RFRaidReaction.BegForMercy)
            {
                // Ajoelha e para de lutar; se o saqueador insistir, ela vira fuga.
                Owner.SetCrouchMode(true);
                Owner.InvalidateTargetAgent();
                Owner.DisableScriptedMovement();
                Owner.ClearTargetFrame();
                Reaction = RFRaidReaction.Flee;
                return;
            }

            if (Reaction != RFRaidReaction.Flee)
            {
                Owner.DisableScriptedMovement();
                Owner.DisableScriptedCombatMovement();
                return;
            }

            if (!_fleeTarget.IsValid)
            {
                _fleeTarget = FindExit();
                if (!_fleeTarget.IsValid)
                {
                    return;
                }
            }

            if (!Owner.IsItemUseDisabled)
            {
                Owner.IsItemUseDisabled = true;
            }
            Owner.ClearTargetFrame();

            Vec2 target = _fleeTarget.AsVec2;
            Vec3 direction = new Vec3(target.x, target.y, 0f, -1f) - Owner.Position;
            Owner.SetTargetPositionAndDirection(target, direction);
            Owner.LookDirection = (Owner.Position - new Vec3(target.x, target.y, 0f, -1f)).NormalizedCopy() * -1f;

            // Sem isto o "andar pela aldeia" do dia-a-dia briga com a fuga e o
            // morador oscila entre passear e correr.
            try
            {
                CampaignAgentComponent component = Owner.GetComponent<CampaignAgentComponent>();
                component?.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>()?.RemoveBehavior<WalkingBehavior>();
            }
            catch (Exception)
            {
            }
        }
    }
}

using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RF_AIDialog
{
    internal enum BattleShoutMovementIntent
    {
        None,
        Charge,
        Advance,
        FallBack,
        Retreat,
        Stop,
        Follow
    }

    internal static class BattleShoutOrderExecutor
    {
        internal static bool TryExecuteMovement(BattleShoutMovementIntent intent, Mission mission, string? formationKey = null)
        {
            if (intent == BattleShoutMovementIntent.None)
                return false;

            if (intent == BattleShoutMovementIntent.Follow)
                return TryExecuteFollow(mission, formationKey);

            if (mission == null || (int)mission.Mode != 2)
                return false;

            switch (intent)
            {
                case BattleShoutMovementIntent.Charge:
                {
                    MovementOrder order = MovementOrder.MovementOrderCharge;
                    return ApplyOrder(mission, order.OrderType, formationKey);
                }
                case BattleShoutMovementIntent.Advance:
                {
                    MovementOrder order = MovementOrder.MovementOrderAdvance;
                    return ApplyOrder(mission, order.OrderType, formationKey);
                }
                case BattleShoutMovementIntent.FallBack:
                {
                    MovementOrder order = MovementOrder.MovementOrderFallBack;
                    return ApplyOrder(mission, order.OrderType, formationKey);
                }
                case BattleShoutMovementIntent.Retreat:
                {
                    MovementOrder order = MovementOrder.MovementOrderRetreat;
                    return ApplyOrder(mission, order.OrderType, formationKey);
                }
                case BattleShoutMovementIntent.Stop:
                {
                    MovementOrder order = MovementOrder.MovementOrderStop;
                    return ApplyOrder(mission, order.OrderType, formationKey);
                }
                default:
                    return false;
            }
        }

        internal static bool TryExecuteFollow(Mission mission, string? formationKey = null)
        {
            if (mission == null || (int)mission.Mode != 2)
                return false;

            Agent main = Agent.Main;
            if (main == null || !main.IsActive())
                return false;

            MovementOrder order = MovementOrder.MovementOrderFollow(main);
            return ApplyOrder(mission, order.OrderType, formationKey);
        }

        internal static bool TryExecuteArrangement(string? arrangementKey, Mission mission, string? formationKey = null)
        {
            if (mission == null || (int)mission.Mode != 2 || string.IsNullOrWhiteSpace(arrangementKey))
                return false;

            arrangementKey = arrangementKey.Trim().ToLowerInvariant();
            OrderType orderType;
            switch (arrangementKey)
            {
                case "line":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderLine;
                    orderType = order.OrderType;
                    break;
                }
                case "shield_wall":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderShieldWall;
                    orderType = order.OrderType;
                    break;
                }
                case "loose":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderLoose;
                    orderType = order.OrderType;
                    break;
                }
                case "column":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderColumn;
                    orderType = order.OrderType;
                    break;
                }
                case "square":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderSquare;
                    orderType = order.OrderType;
                    break;
                }
                case "circle":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderCircle;
                    orderType = order.OrderType;
                    break;
                }
                case "scatter":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderScatter;
                    orderType = order.OrderType;
                    break;
                }
                case "skein":
                {
                    ArrangementOrder order = ArrangementOrder.ArrangementOrderSkein;
                    orderType = order.OrderType;
                    break;
                }
                default:
                    return false;
            }

            return ApplyOrder(mission, orderType, formationKey);
        }

        internal static bool TryExecuteFiring(string? firingKey, Mission mission, string? formationKey = null)
        {
            if (mission == null || (int)mission.Mode != 2 || string.IsNullOrWhiteSpace(firingKey))
                return false;

            firingKey = firingKey.Trim().ToLowerInvariant();
            OrderType orderType;
            switch (firingKey)
            {
                case "fire_at_will":
                {
                    FiringOrder order = FiringOrder.FiringOrderFireAtWill;
                    orderType = order.OrderType;
                    break;
                }
                case "hold_fire":
                {
                    FiringOrder order = FiringOrder.FiringOrderHoldYourFire;
                    orderType = order.OrderType;
                    break;
                }
                default:
                    return false;
            }

            return ApplyOrder(mission, orderType, formationKey);
        }

        private static bool ApplyOrder(Mission mission, OrderType orderType, string? formationKey)
        {
            Agent main = Agent.Main;
            if (main == null || !main.IsActive() || main.Team == null)
                return false;

            OrderController? controller;
            try
            {
                controller = main.Team.GetOrderControllerOf(main);
            }
            catch
            {
                controller = null;
            }

            if (controller == null)
                return false;

            List<Formation> previousSelection = new List<Formation>();
            try
            {
                if (controller.SelectedFormations != null)
                {
                    foreach (Formation formation in (List<Formation>)(object)controller.SelectedFormations)
                    {
                        if (formation != null)
                            previousSelection.Add(formation);
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (string.IsNullOrWhiteSpace(formationKey))
                {
                    controller.SelectAllFormations(false);
                    controller.SetOrder(orderType);
                }
                else
                {
                    List<Formation> targetFormations = ResolveTargetFormations(main.Team, formationKey);
                    if (targetFormations.Count == 0)
                    {
                        controller.SelectAllFormations(false);
                        controller.SetOrder(orderType);
                    }
                    else
                    {
                        controller.ClearSelectedFormations();
                        for (int i = 0; i < targetFormations.Count; i++)
                            controller.SelectFormation(targetFormations[i]);
                        controller.SetOrder(orderType);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    controller.ClearSelectedFormations();
                    for (int i = 0; i < previousSelection.Count; i++)
                    {
                        if (previousSelection[i] != null)
                            controller.SelectFormation(previousSelection[i]);
                    }
                }
                catch
                {
                }
            }
        }

        private static List<Formation> ResolveTargetFormations(Team team, string formationKey)
        {
            List<Formation> result = new List<Formation>();
            if (team == null || string.IsNullOrWhiteSpace(formationKey))
                return result;

            formationKey = formationKey.Trim().ToLowerInvariant();
            switch (formationKey)
            {
                case "all":
                    AddIfPresent(result, team.GetFormation((FormationClass)0));
                    AddIfPresent(result, team.GetFormation((FormationClass)1));
                    AddIfPresent(result, team.GetFormation((FormationClass)2));
                    AddIfPresent(result, team.GetFormation((FormationClass)3));
                    AddIfPresent(result, team.GetFormation((FormationClass)4));
                    AddIfPresent(result, team.GetFormation((FormationClass)5));
                    AddIfPresent(result, team.GetFormation((FormationClass)6));
                    AddIfPresent(result, team.GetFormation((FormationClass)7));
                    break;
                case "infantry":
                    AddIfPresent(result, team.GetFormation((FormationClass)0));
                    break;
                case "archers":
                    AddIfPresent(result, team.GetFormation((FormationClass)1));
                    break;
                case "cavalry":
                    AddIfPresent(result, team.GetFormation((FormationClass)2));
                    break;
                case "horse_archers":
                case "horsearchers":
                    AddIfPresent(result, team.GetFormation((FormationClass)3));
                    break;
            }

            return result;
        }

        private static void AddIfPresent(List<Formation> list, Formation? formation)
        {
            if (formation == null)
                return;

            if (formation.CountOfUnits <= 0)
                return;

            if (!list.Contains(formation))
                list.Add(formation);
        }

        internal static BattleShoutMovementIntent ParseMovement(string? value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "charge" => BattleShoutMovementIntent.Charge,
                "advance" => BattleShoutMovementIntent.Advance,
                "fall_back" => BattleShoutMovementIntent.FallBack,
                "fallback" => BattleShoutMovementIntent.FallBack,
                "retreat" => BattleShoutMovementIntent.Retreat,
                "stop" => BattleShoutMovementIntent.Stop,
                "follow" => BattleShoutMovementIntent.Follow,
                _ => BattleShoutMovementIntent.None
            };
        }
    }
}

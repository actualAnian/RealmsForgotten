using System.Collections.Generic;
using RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.InciteBreak;
using RealmsForgotten.AiMade.StrategicIntrigue.SaveSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.RumorCampaigns;

public static class IntrigueOperationResolver
{
    public static List<IntrigueOperationResolution> ResolveDueOperations(
        Dictionary<Clan, ClanIntrigueState> states,
        Dictionary<Kingdom, KingdomIntrigueState> kingdomStates,
        List<SecretPact> pacts,
        List<IntrigueOperation> operations)
    {
        List<IntrigueOperationResolution> resolutions = new();
        for (int i = operations.Count - 1; i >= 0; i--)
        {
            IntrigueOperation operation = operations[i];
            if (!operation.IsDue
                || operation.TargetClan == null
                || !states.TryGetValue(operation.TargetClan, out ClanIntrigueState state))
            {
                continue;
            }

            bool wasConspirable = state.IsConspirable;
            bool wasBreakawayReady = state.IsBreakawayReady;

            switch (operation.Type)
            {
                case IntrigueOperationType.RumorCampaign:
                case IntrigueOperationType.SponsorDissidence:
                    ApplyRumorCampaignAction.Apply(state, operation.TargetRulerClan, operation.Power);
                    break;
                case IntrigueOperationType.PrepareBreakaway:
                    Kingdom originKingdom = operation.TargetClan.Kingdom;
                    IntrigueBreakOutcome breakOutcome = ApplyInciteBreakAction.Apply(states, kingdomStates, pacts, operation.TargetClan);
                    if (breakOutcome == IntrigueBreakOutcome.None)
                    {
                        operation.Status = IntrigueOperationStatus.Cancelled;
                        operations.RemoveAt(i);
                        continue;
                    }

                    resolutions.Add(new IntrigueOperationResolution(
                        operation.Type,
                        operation.InstigatorClan,
                        operation.TargetClan,
                        operation.TargetRulerClan,
                        originKingdom,
                        false,
                        false,
                        false,
                        breakOutcome));
                    operation.Status = IntrigueOperationStatus.Resolved;
                    operations.RemoveAt(i);
                    continue;
            }

            if (operation.Type == IntrigueOperationType.PrepareBreakaway)
            {
                continue;
            }

            if (IntrigueExposureService.IsExposed(state, operation))
            {
                operation.Status = IntrigueOperationStatus.Exposed;
                state.Suspicion += 15f;
                state.TrustToPlayer -= 10f;
                if (operation.TargetRulerClan?.Leader != null && operation.TargetClan.Leader != null)
                {
                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(
                        operation.TargetClan.Leader,
                        operation.TargetRulerClan.Leader,
                        -2,
                        false);
                }
            }
            else
            {
                operation.Status = IntrigueOperationStatus.Resolved;
            }

            state.ClampValues();
            resolutions.Add(new IntrigueOperationResolution(
                operation.Type,
                operation.InstigatorClan,
                operation.TargetClan,
                operation.TargetRulerClan,
                operation.TargetClan.Kingdom,
                operation.Status == IntrigueOperationStatus.Exposed,
                !wasConspirable && state.IsConspirable,
                !wasBreakawayReady && state.IsBreakawayReady,
                IntrigueBreakOutcome.None));
            operations.RemoveAt(i);
        }

        return resolutions;
    }
}

public sealed class IntrigueOperationResolution
{
    public IntrigueOperationResolution(
        IntrigueOperationType type,
        Clan instigatorClan,
        Clan targetClan,
        Clan targetRulerClan,
        Kingdom originKingdom,
        bool wasExposed,
        bool becameConspirable,
        bool becameBreakawayReady,
        IntrigueBreakOutcome breakOutcome)
    {
        Type = type;
        InstigatorClan = instigatorClan;
        TargetClan = targetClan;
        TargetRulerClan = targetRulerClan;
        OriginKingdom = originKingdom;
        WasExposed = wasExposed;
        BecameConspirable = becameConspirable;
        BecameBreakawayReady = becameBreakawayReady;
        BreakOutcome = breakOutcome;
    }

    public IntrigueOperationType Type { get; }

    public Clan InstigatorClan { get; }

    public Clan TargetClan { get; }

    public Clan TargetRulerClan { get; }

    public Kingdom OriginKingdom { get; }

    public bool WasExposed { get; }

    public bool BecameConspirable { get; }

    public bool BecameBreakawayReady { get; }

    public IntrigueBreakOutcome BreakOutcome { get; }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RF_warsystem.Diagnostics;

internal static class RFWarSystemTraceLog
{
    private const bool Enabled = false;
    internal static bool IsEnabled => Enabled;

    private static readonly string[] LogPaths =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_WarSystemTrace.log"),
        Path.Combine(BasePath.Name, "Configs", "ModLogs", "RF_WarSystemTrace.log"),
        Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "RF_WarSystemTrace.log")
    };

    internal static void Write(string message)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            foreach (string logPath in LogPaths.Distinct())
            {
                try
                {
                    string? directory = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(logPath, line);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    internal static void FocusChanged(Kingdom kingdom, Kingdom enemy, float strength, float rawScore, string source, params (string Label, float Value)[] factors)
    {
        string factorText = string.Join(" ", factors.Select(factor => $"{factor.Label}={factor.Value:0.00}"));
        Write($"focus kingdom={FormatKingdom(kingdom)} enemy={FormatKingdom(enemy)} strength={strength:0.00} raw={rawScore:0.00} source={source}{(string.IsNullOrWhiteSpace(factorText) ? string.Empty : " " + factorText)}");
    }

    internal static void FocusHold(Kingdom kingdom, Kingdom currentEnemy, Kingdom candidateEnemy, float currentScore, float candidateScore, float campaignLock, float unresolvedFront, float holdRemainingDays)
    {
        Write($"focus_hold kingdom={FormatKingdom(kingdom)} current={FormatKingdom(currentEnemy)} candidate={FormatKingdom(candidateEnemy)} currentScore={currentScore:0.00} candidateScore={candidateScore:0.00} lock={campaignLock:0.00} front={unresolvedFront:0.00} holdRemaining={holdRemainingDays:0.00}");
    }

    internal static void ObjectiveChanged(Kingdom kingdom, Kingdom enemy, Settlement? previousObjective, Settlement? nextObjective, string source)
    {
        Write($"objective kingdom={FormatKingdom(kingdom)} enemy={FormatKingdom(enemy)} previous={FormatSettlement(previousObjective)} next={FormatSettlement(nextObjective)} source={source}");
    }

    internal static void FrontChanged(Kingdom kingdom, Kingdom enemy, Settlement? previousFront, Settlement? nextFront, string source)
    {
        Write($"front kingdom={FormatKingdom(kingdom)} enemy={FormatKingdom(enemy)} previous={FormatSettlement(previousFront)} next={FormatSettlement(nextFront)} source={source}");
    }

    internal static void OperationalStateChanged(Kingdom kingdom, Kingdom enemy, string previousState, string nextState, string source, params (string Label, float Value)[] factors)
    {
        string factorText = string.Join(" ", factors.Select(factor => $"{factor.Label}={factor.Value:0.00}"));
        Write($"state kingdom={FormatKingdom(kingdom)} enemy={FormatKingdom(enemy)} previous={previousState} next={nextState} source={source}{(string.IsNullOrWhiteSpace(factorText) ? string.Empty : " " + factorText)}");
    }

    internal static void CoalitionRoleChanged(Kingdom kingdom, Kingdom enemy, string previousRole, string nextRole, string source, params (string Label, float Value)[] factors)
    {
        string factorText = string.Join(" ", factors.Select(factor => $"{factor.Label}={factor.Value:0.00}"));
        Write($"role kingdom={FormatKingdom(kingdom)} enemy={FormatKingdom(enemy)} previous={previousRole} next={nextRole} source={source}{(string.IsNullOrWhiteSpace(factorText) ? string.Empty : " " + factorText)}");
    }

    internal static void CampaignPhaseChanged(Kingdom kingdom, Kingdom enemy, string previousPhase, string nextPhase, string source, params (string Label, float Value)[] factors)
    {
        string factorText = string.Join(" ", factors.Select(factor => $"{factor.Label}={factor.Value:0.00}"));
        Write($"phase kingdom={FormatKingdom(kingdom)} enemy={FormatKingdom(enemy)} previous={previousPhase} next={nextPhase} source={source}{(string.IsNullOrWhiteSpace(factorText) ? string.Empty : " " + factorText)}");
    }

    internal static void WarProposal(Kingdom kingdom, Kingdom? target, Clan sponsor, float strategicScore, float threshold, float support, float likelihood, float bias)
    {
        Write($"proposal type=war kingdom={FormatKingdom(kingdom)} target={FormatKingdom(target)} sponsor={FormatClan(sponsor)} strategic={strategicScore:0.00} threshold={threshold:0.00} support={support:0.00} likelihood={likelihood:0.00} bias={bias:0.00}");
    }

    internal static void PeaceProposal(Kingdom kingdom, Kingdom? target, Clan sponsor, float peaceScore, float threshold, float support, float likelihood, float bias, int tributePerDay)
    {
        Write($"proposal type=peace kingdom={FormatKingdom(kingdom)} target={FormatKingdom(target)} sponsor={FormatClan(sponsor)} score={peaceScore:0.00} threshold={threshold:0.00} support={support:0.00} likelihood={likelihood:0.00} bias={bias:0.00} tribute={tributePerDay}");
    }

    internal static void ProposalReplaced(string type, Kingdom kingdom, Kingdom? previousTarget, Kingdom? nextTarget, float previousBias, float nextBias)
    {
        Write($"proposal_replace type={type} kingdom={FormatKingdom(kingdom)} previous={FormatKingdom(previousTarget)} next={FormatKingdom(nextTarget)} previousBias={previousBias:0.00} nextBias={nextBias:0.00}");
    }

    internal static void TargetWindow(MobileParty mobileParty, Army.ArmyTypes missionType, Settlement targetSettlement, float baseScore, float adjustedScore, Kingdom? focusedEnemy, float campaignLock)
    {
        float multiplier = baseScore > 0f ? adjustedScore / baseScore : 0f;
        Write($"target party={FormatParty(mobileParty)} mission={missionType} target={FormatSettlement(targetSettlement)} base={baseScore:0.00} adjusted={adjustedScore:0.00} mult={multiplier:0.00} focusEnemy={FormatKingdom(focusedEnemy)} lock={campaignLock:0.00}");
    }

    internal static string FormatKingdom(Kingdom? kingdom)
    {
        if (kingdom == null)
        {
            return "null";
        }

        return string.IsNullOrWhiteSpace(kingdom.StringId) ? kingdom.Name.ToString() : kingdom.StringId;
    }

    internal static string FormatClan(Clan? clan)
    {
        if (clan == null)
        {
            return "null";
        }

        return string.IsNullOrWhiteSpace(clan.StringId) ? clan.Name.ToString() : clan.StringId;
    }

    internal static string FormatParty(MobileParty? mobileParty)
    {
        if (mobileParty == null)
        {
            return "null";
        }

        if (!string.IsNullOrWhiteSpace(mobileParty.StringId))
        {
            return mobileParty.StringId;
        }

        return mobileParty.Name?.ToString() ?? "party";
    }

    internal static string FormatSettlement(Settlement? settlement)
    {
        if (settlement == null)
        {
            return "null";
        }

        if (!string.IsNullOrWhiteSpace(settlement.StringId))
        {
            return settlement.StringId;
        }

        return settlement.Name?.ToString() ?? "settlement";
    }
}

internal static class RFWarSystemTargetTraceCollector
{
    private const float FlushIntervalDays = 0.25f;

    private sealed class TargetWindowRecord
    {
        public float BestAdjustedScore;
        public float BestBaseScore;
        public float BestCampaignLock;
        public float LastFlushDay;
        public Settlement? BestSettlement;
        public Kingdom? FocusedEnemy;
    }

    private static readonly Dictionary<string, TargetWindowRecord> Records = new(StringComparer.Ordinal);

    internal static void Observe(MobileParty? mobileParty, Army.ArmyTypes missionType, Settlement? settlement, float baseScore, float adjustedScore)
    {
        if (!RFWarSystemTraceLog.IsEnabled)
        {
            return;
        }

        if (mobileParty == null || settlement == null || adjustedScore <= 0f)
        {
            return;
        }

        string key = $"{RFWarSystemTraceLog.FormatParty(mobileParty)}::{missionType}";
        float currentDay = (float)CampaignTime.Now.ToDays;
        Kingdom? kingdom = mobileParty.MapFaction as Kingdom;
        Kingdom? focusedEnemy = kingdom != null ? Behaviors.RFWarCampaignDirectorBehavior.GetPrimaryEnemy(kingdom) : null;
        float campaignLock = kingdom != null && focusedEnemy != null
            ? Behaviors.RFWarCampaignDirectorBehavior.GetCampaignLockFactor(kingdom, focusedEnemy)
            : 0f;

        if (!Records.TryGetValue(key, out TargetWindowRecord? record))
        {
            record = new TargetWindowRecord
            {
                BestAdjustedScore = adjustedScore,
                BestBaseScore = baseScore,
                BestCampaignLock = campaignLock,
                BestSettlement = settlement,
                FocusedEnemy = focusedEnemy,
                LastFlushDay = currentDay
            };
            Records[key] = record;
            return;
        }

        Settlement? previousBestSettlement = record.BestSettlement;
        if (adjustedScore > record.BestAdjustedScore || record.BestSettlement == null)
        {
            record.BestAdjustedScore = adjustedScore;
            record.BestBaseScore = baseScore;
            record.BestCampaignLock = campaignLock;
            record.BestSettlement = settlement;
            record.FocusedEnemy = focusedEnemy;
        }

        bool bestTargetChanged = previousBestSettlement != null && record.BestSettlement != previousBestSettlement;
        if (currentDay - record.LastFlushDay < FlushIntervalDays && !bestTargetChanged)
        {
            return;
        }

        if (record.BestSettlement != null)
        {
            RFWarSystemTraceLog.TargetWindow(mobileParty, missionType, record.BestSettlement, record.BestBaseScore, record.BestAdjustedScore, record.FocusedEnemy, record.BestCampaignLock);
        }

        record.BestAdjustedScore = adjustedScore;
        record.BestBaseScore = baseScore;
        record.BestCampaignLock = campaignLock;
        record.BestSettlement = settlement;
        record.FocusedEnemy = focusedEnemy;
        record.LastFlushDay = currentDay;
    }

}

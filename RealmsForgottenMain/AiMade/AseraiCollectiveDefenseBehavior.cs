using System;
using System.Collections.Generic;
using System.Linq;
using RF_warsystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade
{
    public class AseraiCollectiveDefenseBehavior : CampaignBehaviorBase
    {
        private readonly HashSet<string> _aseraiRealmIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "aserai",
            "aserai_a",
            "aserai_b",
            "aserai_c",
            "aserai_d",
            "aserai_e"
        };

        private Dictionary<string, CampaignTime> _lastDefenseCoalition = new();

        public override void RegisterEvents()
        {
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastDefenseCoalition", ref _lastDefenseCoalition);
        }

        private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
        {
            try
            {
                Kingdom attacker = ResolveExternalAttacker(faction1, faction2, out Kingdom attackedAserai);
                if (attacker == null || attackedAserai == null)
                    return;

                if (IsOnDefenseCooldown(attacker))
                    return;

                RallyAseraiRealmsAgainst(attacker, attackedAserai);
                _lastDefenseCoalition[attacker.StringId] = CampaignTime.Now;
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[AseraiDefense] OnWarDeclared failed: {ex}");
            }
        }

        private void OnDailyTick()
        {
            try
            {
                foreach (Kingdom attacker in Kingdom.All.Where(IsValidKingdom))
                {
                    List<Kingdom> threatenedAserai = Kingdom.All
                        .Where(IsAseraiRealm)
                        .Where(x => !x.IsEliminated && FactionManager.IsAtWarAgainstFaction(x, attacker))
                        .ToList();

                    if (threatenedAserai.Count == 0 || IsAseraiRealm(attacker))
                        continue;

                    Kingdom primaryThreatenedRealm = threatenedAserai
                        .OrderByDescending(x => x.Fiefs.Count())
                        .FirstOrDefault();

                    if (primaryThreatenedRealm != null)
                    {
                        RFWarExternalIntentApi.ReinforceCollectiveDefense(attacker, primaryThreatenedRealm, threatenedAserai);
                        RallyAseraiRealmsAgainst(attacker, primaryThreatenedRealm);
                    }
                }
            }
            catch (Exception ex)
            {
                RFLogger.Log($"[AseraiDefense] OnDailyTick failed: {ex}");
            }
        }

        private Kingdom ResolveExternalAttacker(IFaction faction1, IFaction faction2, out Kingdom attackedAserai)
        {
            attackedAserai = null;

            Kingdom kingdom1 = faction1 as Kingdom;
            Kingdom kingdom2 = faction2 as Kingdom;
            if (!IsValidKingdom(kingdom1) || !IsValidKingdom(kingdom2))
                return null;

            bool firstIsAserai = IsAseraiRealm(kingdom1);
            bool secondIsAserai = IsAseraiRealm(kingdom2);

            if (firstIsAserai == secondIsAserai)
                return null;

            attackedAserai = firstIsAserai ? kingdom1 : kingdom2;
            Kingdom attacker = firstIsAserai ? kingdom2 : kingdom1;

            return IsAseraiRealm(attacker) ? null : attacker;
        }

        private void RallyAseraiRealmsAgainst(Kingdom attacker, Kingdom attackedAserai)
        {
            List<Kingdom> defenders = Kingdom.All
                .Where(IsAseraiRealm)
                .Where(IsValidKingdom)
                .ToList();

            RFWarExternalIntentApi.ReinforceCollectiveDefense(attacker, attackedAserai, defenders);

            RFLogger.Log($"[AseraiDefense] Rally start | attacker={attacker?.StringId ?? "null"} | attacked={attackedAserai?.StringId ?? "null"} | defenders={string.Join(",", defenders.Select(x => x.StringId))}");

            for (int i = 0; i < defenders.Count; i++)
            {
                for (int j = i + 1; j < defenders.Count; j++)
                {
                    Kingdom left = defenders[i];
                    Kingdom right = defenders[j];

                    if (!FactionManager.IsAtWarAgainstFaction(left, right))
                        continue;

                    try
                    {
                        RFWarExternalIntentApi.RequestCoalitionPeace(left, right);
                    }
                    catch (Exception ex)
                    {
                        RFLogger.Log($"[AseraiDefense] Peace step failed | left={left.StringId} | right={right.StringId} | {ex.Message}");
                    }
                }
            }

            bool anyJoined = false;
            foreach (Kingdom defender in defenders)
            {
                if (defender == attacker)
                    continue;

                try
                {
                    if (!FactionManager.IsAtWarAgainstFaction(defender, attacker))
                    {
                        anyJoined = true;
                        RFLogger.Log($"[AseraiDefense] Defender joined | defender={defender.StringId} | attacker={attacker.StringId}");
                    }
                }
                catch (Exception ex)
                {
                    RFLogger.Log($"[AseraiDefense] War step failed | defender={defender.StringId} | attacker={attacker?.StringId ?? "null"} | {ex.Message}");
                }
            }

            if (anyJoined)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"{attackedAserai.Name} has called the desert realms to arms. The Aserai kingdoms unite against {attacker.Name}.",
                    Colors.Yellow));
            }
        }

        private bool IsOnDefenseCooldown(Kingdom attacker)
        {
            if (attacker == null)
                return true;

            if (!_lastDefenseCoalition.TryGetValue(attacker.StringId, out CampaignTime last))
                return false;

            return (CampaignTime.Now - last).ToDays < 3f;
        }

        private bool IsAseraiRealm(Kingdom kingdom)
        {
            if (kingdom == null)
                return false;

            return _aseraiRealmIds.Contains(kingdom.StringId)
                || string.Equals(kingdom.Culture?.StringId, "aserai", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidKingdom(Kingdom kingdom)
        {
            return kingdom != null
                && !kingdom.IsEliminated
                && kingdom.Leader != null;
        }
    }
}

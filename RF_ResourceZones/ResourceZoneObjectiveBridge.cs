using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace RF_ResourceZones
{
    /// <summary>
    /// Feeds the resource zones into the kingdoms' GRAND DESIGNS
    /// (KingdomObjectiveService in RealmsForgottenMain, author decision
    /// 2026-07-15): Nasoria's wealth supremacy counts its gold/silver mines,
    /// and the Dwarf/Urkhai mountain war counts control of the map's mines.
    /// Installed VIA REFLECTION — RealmsForgottenMain must not reference this
    /// optional add-on assembly, and a failed install degrades to score 0.
    /// </summary>
    public static class ResourceZoneObjectiveBridge
    {
        public static void Install()
        {
            try
            {
                Type? service = Type.GetType(
                    "RealmsForgotten.AiMade.StrategicIntrigue.Mechanics.KingdomObjectives.KingdomObjectiveService, RealmsForgotten");
                if (service == null)
                {
                    Debug.Print("[RF_ResourceZones] KingdomObjectiveService not found — objective bridge skipped.");
                    return;
                }

                service.GetField("ZoneWealthScoreProvider", BindingFlags.Public | BindingFlags.Static)
                    ?.SetValue(null, new Func<Kingdom, float>(GetZoneWealthScore));
                service.GetField("MineControlScoreProvider", BindingFlags.Public | BindingFlags.Static)
                    ?.SetValue(null, new Func<Kingdom, float>(GetMineControlScore));
                Debug.Print("[RF_ResourceZones] Kingdom-objective bridge installed (zone wealth + mine control).");
            }
            catch (Exception exception)
            {
                Debug.Print($"[RF_ResourceZones] Objective bridge install failed (harmless): {exception.Message}");
            }
        }

        /// <summary>0..100 — wealth the kingdom draws from its zones, gold and
        /// silver weighing the most (Nasoria's "Noble Wealth Supremacy").</summary>
        public static float GetZoneWealthScore(Kingdom kingdom)
        {
            if (kingdom == null || ResourceZonesCampaignBehavior.Instance == null)
            {
                return 0f;
            }

            float total = 0f;
            foreach ((ResourceZoneRecord record, MobileParty party) in ResourceZonesCampaignBehavior.Instance.GetLiveZones())
            {
                if (record.OwnerClan?.Kingdom != kingdom
                    || party.PartyComponent is not ResourceZonePartyComponent component)
                {
                    continue;
                }

                float typeWeight = component.ZoneType switch
                {
                    ResourceZoneType.Gold => 1.6f,
                    ResourceZoneType.Karthradium => 1.4f,
                    ResourceZoneType.Silver => 1.2f,
                    _ => 0.6f,
                };
                int richness = record.Richness > 0 ? record.Richness : 2;
                total += record.Tier * ResourceZoneRules.RichnessYieldMultiplier(richness) * typeWeight;
            }

            return Math.Min(100f, total * 12f);
        }

        /// <summary>0..100 — the kingdom's share of the map's mountain mines
        /// (iron/gold/silver zones), for the Dwarf/Urkhai hold objectives.</summary>
        public static float GetMineControlScore(Kingdom kingdom)
        {
            if (kingdom == null || ResourceZonesCampaignBehavior.Instance == null)
            {
                return 0f;
            }

            int mines = 0;
            int owned = 0;
            foreach ((ResourceZoneRecord record, MobileParty party) in ResourceZonesCampaignBehavior.Instance.GetLiveZones())
            {
                if (party.PartyComponent is not ResourceZonePartyComponent component)
                {
                    continue;
                }

                bool isMine = component.ZoneType is ResourceZoneType.Iron
                    or ResourceZoneType.Gold or ResourceZoneType.Silver
                    or ResourceZoneType.Karthradium; // THE dwarf mines above all
                if (!isMine)
                {
                    continue;
                }

                mines++;
                if (record.OwnerClan?.Kingdom == kingdom)
                {
                    owned++;
                }
            }

            return mines == 0 ? 0f : (float)owned / mines * 100f;
        }
    }
}

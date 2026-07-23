using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.TradePact
{
    public static class EconomicPactManager
    {
        private static List<EconomicPact> _activePacts = new List<EconomicPact>();

        /// <summary>Static state must not outlive a campaign: without this, a
        /// new game or a load in the same session inherited the previous
        /// campaign's pacts, holding dead IFaction references.</summary>
        public static void Reset()
        {
            _activePacts.Clear();
        }

        /// <summary>"faction1Id|faction2Id|daysRemaining|type" per pact.</summary>
        public static List<string> ExportForSave()
        {
            var result = new List<string>();
            foreach (var pact in _activePacts)
            {
                string id1 = (pact.Faction1 as MBObjectBase)?.StringId;
                string id2 = (pact.Faction2 as MBObjectBase)?.StringId;
                if (string.IsNullOrEmpty(id1) || string.IsNullOrEmpty(id2))
                {
                    continue;
                }

                result.Add($"{id1}|{id2}|{pact.DaysRemaining}|{pact.Type}");
            }

            return result;
        }

        public static void ImportFromSave(List<string> serialized)
        {
            _activePacts.Clear();
            if (serialized == null)
            {
                return;
            }

            foreach (string entry in serialized)
            {
                string[] parts = entry.Split('|');
                if (parts.Length != 4 || !int.TryParse(parts[2], out int days) || days <= 0)
                {
                    continue;
                }

                IFaction f1 = Kingdom.All.FirstOrDefault(k => k.StringId == parts[0])
                    ?? (IFaction)Clan.All.FirstOrDefault(c => c.StringId == parts[0]);
                IFaction f2 = Kingdom.All.FirstOrDefault(k => k.StringId == parts[1])
                    ?? (IFaction)Clan.All.FirstOrDefault(c => c.StringId == parts[1]);
                if (f1 == null || f2 == null || f1.IsEliminated || f2.IsEliminated)
                {
                    continue;
                }

                _activePacts.Add(new EconomicPact(f1, f2, days, parts[3]));
            }
        }

        public static void AddPact(IFaction f1, IFaction f2, int days, string type)
        {
            try
            {
                if (f1 == null || f2 == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Error: One of the factions is null."));
                    return;
                }

                if (HasActivePact(f1, f2))
                {
                    InformationManager.DisplayMessage(new InformationMessage("A trade pact already exists with this faction."));
                    return;
                }

                var pact = new EconomicPact(f1, f2, days, type);
                _activePacts.Add(pact);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"[Trade Pact Error] {ex.Message}"));
            }
        }

        public static bool HasActivePact(IFaction f1, IFaction f2)
        {
            return _activePacts.Any(p =>
                (p.Faction1 == f1 && p.Faction2 == f2) ||
                (p.Faction1 == f2 && p.Faction2 == f1));
        }

        public static bool TryGetPact(IFaction f1, IFaction f2, out EconomicPact pact)
        {
            pact = _activePacts.FirstOrDefault(p =>
                (p.Faction1 == f1 && p.Faction2 == f2) ||
                (p.Faction1 == f2 && p.Faction2 == f1));

            return pact != null;
        }

        public static void DailyTick()
        {
            // Remover expirados
            _activePacts.RemoveAll(p => p.IsExpired());

            foreach (var pact in _activePacts)
            {
                pact.Tick();

                // Aplicar benefícios à IA
                IFaction partnerFaction = pact.Faction2;

                foreach (var town in Town.AllTowns)
                {
                    if (town.Settlement.OwnerClan?.Kingdom == partnerFaction)
                    {
                        town.Prosperity += 1;
                        town.ChangeGold(300);
                    }
                }
            }
        }
    }
}
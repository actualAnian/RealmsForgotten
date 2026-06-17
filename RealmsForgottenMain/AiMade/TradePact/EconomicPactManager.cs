using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.TradePact
{
    public static class EconomicPactManager
    {
        private static List<EconomicPact> _activePacts = new List<EconomicPact>();

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
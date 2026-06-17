using RealmsForgotten.AiMade.Encounters.Interfaces;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using RealmsForgotten.AiMade.Encounters.Scenario;

namespace RealmsForgotten.AiMade.Encounters.Managers
{
    public class ScenarioDispatcher
    {
        private readonly List<IEncounterScenario> _scenarios = new()
        {
            new ProtectVillagers(),
            new TrainMilitia(),
            // new DuelSomeone()   // <- deleted
        };

        public void TryTriggerEncounter(Settlement settlement)
        {
            foreach (var scenario in _scenarios)
            {
                if (scenario.IsPossible(settlement))
                {
                    scenario.OnStartEncounter(settlement);
                    break;
                }
            }
        }
    }
}


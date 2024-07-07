using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace RealmsForgotten.AiMade.Career
{
    public static class MercenaryContractManager
    {
        public static event EventHandler<ContractCompletionEvent> ContractCompleted;

        private static MercenaryContract activeContract;

        public static void AssignContract(Hero hero, MercenaryContract contract)
        {
            activeContract = contract;
        }

        public static void CheckObjectives(Hero hero)
        {
            if (activeContract != null && !activeContract.IsCompleted)
            {
                if (activeContract.Id == "raid_villages")
                {
                    // Existing logic for raiding villages
                }
                else if (activeContract.Id == "attack_caravan")
                {
                    // New logic for attacking a merchant caravan
                    var currentMapEvent = hero.PartyBelongedTo?.MapEvent;
                    if (currentMapEvent != null && currentMapEvent.HasCaravan() && currentMapEvent.IsPlayerParticipating())
                    {
                        activeContract.Progress = 1;
                    }

                    if (activeContract.Progress >= activeContract.Goal)
                    {
                        activeContract.Progress = activeContract.Goal; // Ensure progress does not exceed the goal
                        ContractCompleted?.Invoke(null, new ContractCompletionEvent(hero));
                    }
                }
            }
        }

        public static void OnContractCompleted(object sender, ContractCompletionEvent e)
        {
            InformationManager.DisplayMessage(new InformationMessage($"Contract '{activeContract.Name}' completed by {e.Hero.Name}."));
            // Add any additional reward logic here
        }

        // Subscribe to the event somewhere in your initialization logic
        static MercenaryContractManager()
        {
            ContractCompleted += OnContractCompleted;
        }

        public static void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("activeContract", ref activeContract);
        }
    }
}

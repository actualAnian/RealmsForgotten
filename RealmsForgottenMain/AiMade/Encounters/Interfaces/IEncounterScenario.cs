using TaleWorlds.CampaignSystem.Settlements;

namespace RealmsForgotten.AiMade.Encounters.Interfaces
{
    public interface IEncounterScenario
    {
        bool IsPossible(Settlement settlement);
        void OnStartEncounter(Settlement settlement);
        string Name { get; }
    }
}
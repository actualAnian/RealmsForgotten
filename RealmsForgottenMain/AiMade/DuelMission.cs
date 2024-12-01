using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

// Defines a static class to manage duel missions.
namespace RealmsForgotten.AiMade;

public static class DuelMission
{
    // Method to open a new duel mission.
    public static void OpenDuelMission(string sceneId, CharacterObject opponent, bool onHorse, bool isFriendly, bool isInsideSettlement)
    {
        // Ensure the initializer record is setup correctly
        var initializer = new MissionInitializerRecord(sceneId) // Use the sceneId for the initializer
        {
            PlayingInCampaignMode = true // Set the playing mode if needed
        };

        // Call to open a new mission
        MissionState.OpenNew("DuelMission", initializer, (mission) => InitializeMission(mission), true, true);
    }

    // Helper method to initialize the mission with necessary behaviors.
    private static IEnumerable<MissionBehavior> InitializeMission(Mission mission)
    {
        // Create a list of mission behaviors to add to the mission.
        List<MissionBehavior> behaviors = new List<MissionBehavior>
        {
            new DuelMissionController() // Add your specific mission controller
        };

        // Return the list of mission behaviors
        return behaviors;
    }
}
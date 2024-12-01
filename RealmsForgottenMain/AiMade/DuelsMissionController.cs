using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade;

public class DuelMissionController : MissionLogic
{
    private Agent playerAgent, opponentAgent;
    private bool duelEnded = false;

    public override void AfterStart()
    {
        base.AfterStart();
        var scene = Mission.Current.Scene;

        // Define positions for player and opponent
        Vec3 playerPosition = new Vec3(0, 0, 0); // Central point of the scene for demonstration
        Vec3 opponentPosition = new Vec3(2, 0, 0); // 2 meters away on the x-axis

        // Calculate direction vectors
        Vec2 directionToOpponent = new Vec2(opponentPosition.x - playerPosition.x, opponentPosition.y - playerPosition.y).Normalized();
        Vec2 directionToPlayer = new Vec2(playerPosition.x - opponentPosition.x, playerPosition.y - opponentPosition.y).Normalized();

        // Spawn the player
        playerAgent = Mission.Current.SpawnAgent(new AgentBuildData(Hero.MainHero.CharacterObject)
            .InitialPosition(playerPosition)
            .InitialDirection(directionToOpponent)); // Facing towards the opponent

        // Spawn the opponent
        opponentAgent = Mission.Current.SpawnAgent(new AgentBuildData(Hero.OneToOneConversationHero.CharacterObject)
            .InitialPosition(opponentPosition)
            .InitialDirection(directionToPlayer)); // Facing towards the player
    }

    public override void OnMissionTick(float dt)
    {
        if (playerAgent.State == AgentState.Killed || opponentAgent.State == AgentState.Killed)
        {
            duelEnded = true;
            Mission.Current.EndMission();
        }
    }
}
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Infect
{
    public static class InfectionManager
    {
        public static void TryInfectAgent(Agent deadAgent, Agent killerAgent)
        {
            if (Mission.Current == null || !Mission.Current.IsLoadingFinished)
                return;

            if (deadAgent == null || killerAgent == null)
                return;

            if (killerAgent.Character?.StringId != "orc_base_infantry")
                return;

            if (!deadAgent.IsHuman || deadAgent.IsHero)
                return;

            if (Mission.Current.Mode != MissionMode.Battle &&
                Mission.Current.Mode != MissionMode.Stealth &&
                Mission.Current.Mode != MissionMode.Duel)
                return;

            InformationManager.DisplayMessage(new InformationMessage(
                $"☣️ Infection triggered: {deadAgent.Name} was killed by {killerAgent.Name}.", Colors.Yellow));

            CharacterObject orcInfecter = CharacterObject.All.FirstOrDefault(c => c.StringId == "orc_base_infantry");
            if (orcInfecter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Orc infecter template not found!", Colors.Red));
                return;
            }

            var spawnOrigin = new SimpleAgentOrigin(orcInfecter);

            Team enemyTeam = (deadAgent.Team == Mission.Current.AttackerTeam)
                ? Mission.Current.DefenderTeam
                : Mission.Current.AttackerTeam;

            Agent infectedAgent = Mission.Current.SpawnAgent(new AgentBuildData(spawnOrigin)
                .Team(enemyTeam)
                .InitialPosition(deadAgent.Position)
                .InitialDirection(deadAgent.LookDirection.AsVec2)
                .TroopOrigin(spawnOrigin));

            if (infectedAgent != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"🧟 Spawned infected troop at position {infectedAgent.Position}.", Colors.Green));

                  infectedAgent.AgentVisuals?.SetContourColor(Colors.Green.ToUnsignedInteger(), true);

                // Temporarily disable fading for debug
                // infectedAgent.FadeOut(false, true);
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Failed to spawn infected agent!", Colors.Red));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.AiMade.Adventurer
{
    public class ProtectVillagers : MissionLogic
    {
        private readonly Settlement _settlement;

        public ProtectVillagers(Settlement settlement)
        {
            _settlement = settlement;
        }

        public override void AfterStart()
        {
            InformationManager.DisplayMessage(
                new InformationMessage($"🏹 Defend mission started outside {_settlement.Name}!")
            );

            Mission.Current.MakeDefaultDeploymentPlans();
            // TODO: spawn player + villagers + bandits and trigger basic skirmish logic
        }

        public override void OnMissionTick(float dt)
        {
            // Optional: track when villagers die or bandits are wiped
        }
    }
}


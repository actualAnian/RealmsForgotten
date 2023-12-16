using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    internal class NecromancerStaffMissionBehavior : MissionBehavior
    {
        private bool isSpawning = false;
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {

        }
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Agent.Main != null && Agent.Main.WieldedWeapon.Item?.StringId == "necromancer_staff")
            {
                Agent main = Agent.Main;
                if (isSpawning)
                {
                    if(main.GetCurrentAction(0).Name.Contains("act_cheer") && main.GetCurrentActionProgress(0) >= 0.9)
                        main.SetActionChannel(0, ActionIndexCache.act_none, true);
                    isSpawning = false;
                }
                if (Input.IsKeyReleased(InputKey.O))
                {
                    main.SetActionChannel(0, ActionIndexCache.Create("act_cheer_1"), true);
                    
                    isSpawning = ReviveTroops(main);
                }

            }
        }

        private bool ReviveTroops(Agent main)
        {
            MapEvent playerEvent = MapEvent.PlayerMapEvent;
            if (playerEvent == null)
                return false;

            List<FlattenedTroopRosterElement> killedAllies = playerEvent.PartiesOnSide(playerEvent.PlayerSide).SelectMany(x => x.Troops.ToList())
                .Where(x=>x.IsKilled)?.ToList();

            if ((bool)killedAllies?.IsEmpty())
                return false;

            int maxTroopAmount = (int)Math.Round(main.Character.GetSkillValue(RFSkills.Arcane) * 0.1f);
            if (maxTroopAmount <= 0)
                maxTroopAmount = 1;


            killedAllies.Randomize();
            if(killedAllies.Count > maxTroopAmount)
            killedAllies.RemoveRange(0, killedAllies.Count - maxTroopAmount);

            foreach (var killed in killedAllies)
            {
                if (killed.Troop == null)
                    continue;


                Vec3 position = Mission.GetRandomPositionAroundPoint(main.Position, 1f, 6f);
                PartyBase.MainParty.AddMember(killed.Troop, 1);
                Agent agent = Mission.Current.SpawnTroop(new PartyAgentOrigin(PartyBase.MainParty, killed.Troop), true, true,
                    killed.Troop.IsMounted, false, 1, 1, true, true, false, position, position.AsVec2);

                agent.TeleportToPosition(position);
            }

            return true;
        }
        public override void OnAgentBuild(Agent agent, Banner banner)
        {

        }
    }
}

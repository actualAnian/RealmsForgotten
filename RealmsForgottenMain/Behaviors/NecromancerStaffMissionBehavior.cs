using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RealmsForgotten.Behaviors
{
    internal class NecromancerStaffMissionBehavior : MissionBehavior
    {
        private bool isSpawning = false;
        private int totalRevived = 0;
        private int maxUses;
        public SpellStatusVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private TextObject necromancyTextObject = new TextObject("{=necromancer_staff_status}Necromancy revivals: {AMOUNT}");
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        public override void AfterStart()
        {
            if (Campaign.Current != null)
                maxUses = (int)(Math.Round(Hero.MainHero.GetSkillValue(RFSkills.Arcane) / 100.0) * 100) / 100;
            else
                maxUses = 3;
        }
        
        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Agent.Main != null && Agent.Main.WieldedWeapon.Item?.StringId == "rfmisc_necromancer_staff")
            {
                Agent main = Agent.Main;
                if (isSpawning)
                {
                    if (main.GetCurrentAction(0).Name.Contains("act_cheer") && main.GetCurrentActionProgress(0) >= 0.8)
                    {
                        main.SetActionChannel(0, ActionIndexCache.act_none, true);
                        isSpawning = false;
                    }
                }
                if (Input.IsKeyReleased(InputKey.RightMouseButton) && ReviveTroops(main))
                {
                    main.SetActionChannel(0, ActionIndexCache.Create("act_cheer_1"), true);
                    isSpawning = true;
                }

            }
        }

        private bool ReviveTroops(Agent main)
        {
            MapEvent playerEvent = MapEvent.PlayerMapEvent;
            if (playerEvent == null)
                return false; 
            
            if(maxUses <= 0)
            {
                MBInformationManager.AddQuickInformation(new TextObject("You already spend the entire staff energy."));
                return false;
            }

            int killedAllies = playerEvent.PartiesOnSide(playerEvent.PlayerSide)
                .SelectMany(x=>x.Troops.ToList()).Count(x => x.IsKilled && x.Troop?.StringId != "sea_raiders_raider");
            
            killedAllies -= totalRevived;
            
            if (killedAllies <= 0)
            {
                MBInformationManager.AddQuickInformation(new TextObject("No dead allies to revive."));
                return false;
            }
            
            CharacterObject zombieTroop = CharacterObject.Find("sea_raiders_raider");
            
            int refactoredNumber = (int)((float)killedAllies * (150f / 300f) * (0.35f + MBRandom.RandomFloat));

            if(refactoredNumber > killedAllies)
                refactoredNumber = killedAllies;
            else if (refactoredNumber < 1)
                refactoredNumber = 1;
 

            List<Vec3> positions = new();
            int revivedNow = 0;
            for (int i = 0; i < refactoredNumber; i++)
            {
                Vec3 position = Mission.GetRandomPositionAroundPoint(main.Position, 1f, 6f);
                
                while (positions.Any(x => position.DistanceSquared(x) < 0.3f))
                    position = Mission.GetRandomPositionAroundPoint(main.Position, 1f, 6f);

                PartyBase.MainParty?.AddMember(zombieTroop, 1);

                IAgentOriginBase agentOriginBase = Campaign.Current != null
                    ? new PartyAgentOrigin(PartyBase.MainParty, zombieTroop)
                    : new SimpleAgentOrigin(zombieTroop);
                
                Agent agent = Mission.Current.SpawnTroop(agentOriginBase, true, true,
                    zombieTroop.IsMounted, false, 1, 1, true, true, false, position, position.AsVec2);

                agent.TeleportToPosition(position);
                agent.SetMorale(100);
                positions.Add(position);
                revivedNow++;
            }
            
            MBInformationManager.AddQuickInformation(new TextObject($"{revivedNow} soldiers revived!"));
            totalRevived += revivedNow;
            maxUses--;
            
            necromancyTextObject.SetTextVariable("AMOUNT", maxUses);
            _dataSource.SpellText = necromancyTextObject.ToString();
            
            return true;
        }
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.IsMainAgent)
            {
                MissionScreen? missionScreen = TaleWorlds.ScreenSystem.ScreenManager.TopScreen as MissionScreen;
                necromancyTextObject.SetTextVariable("AMOUNT", maxUses);
                _dataSource = new SpellStatusVM(necromancyTextObject.ToString(),agent.WieldedWeapon.Item?.StringId == "rfmisc_necromancer_staff", 20, 22);
                _gauntletLayer = new GauntletLayer(-1);
                missionScreen.AddLayer(_gauntletLayer);
                _gauntletLayer.LoadMovie("SpellStatus", _dataSource);
                
                agent.OnMainAgentWieldedItemChange += OnMainAgentWieldedItemChange;
            }
                
        }
        private void OnMainAgentWieldedItemChange()
        {
            _dataSource.Visible = Agent.Main?.WieldedWeapon.Item?.StringId == "rfmisc_necromancer_staff";
        }
    }
}

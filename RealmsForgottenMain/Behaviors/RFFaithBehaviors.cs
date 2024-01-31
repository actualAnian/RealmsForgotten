using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.CustomSkills;
using RealmsForgotten.Utility;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    internal class RFFaithMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow,
            in AttackCollisionData attackCollisionData)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
            CharacterObject characterObject = affectorAgent.Character as CharacterObject;
            if (characterObject != null && characterObject.GetPerkValue(RFPerks.Faith.DruidsWave))
            {
                affectorAgent.Health += blow.InflictedDamage * 0.3f;
                if (affectorAgent.Health > affectorAgent.HealthLimit)
                    affectorAgent.Health = affectorAgent.HealthLimit;
            }
            
        }

        private readonly SkillObject[] combatSkills = { DefaultSkills.Athletics, DefaultSkills.Bow, DefaultSkills.Crossbow, DefaultSkills.Riding, DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm, DefaultSkills.Throwing};
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            CharacterObject captain = agent.Formation?.Captain?.Character as CharacterObject;
            if (captain != null)
            {
                if(captain.GetPerkValue(RFPerks.Faith.QuatzulsPrayer))
                    RFUtility.ModifyCharacterSkillAttribute(agent.Character, DefaultSkills.Athletics, agent.Character.GetSkillValue(DefaultSkills.Athletics) * 3);

                if(captain.GetPerkValue(RFPerks.Faith.IgathurilsPrayer))
                    foreach (var skill in combatSkills)
                    {
                        RFUtility.ModifyCharacterSkillAttribute(agent.Character, skill, 90);
                    }

                float factor = captain.GetPerkValue(RFPerks.Faith.IlacsPrayer) ? captain.GetFaithPerkBonus(RFPerks.Faith.IlacsPrayer) :
                    (captain.GetPerkValue(RFPerks.Faith.ThuriksPrayer) ? captain.GetFaithPerkBonus(RFPerks.Faith.ThuriksPrayer) : 0);
                
                if (factor > 0)
                    agent.ChangeMorale(100 * factor);
            }
        }
    }
    internal class RFFaithCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
           CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnd);
           CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
           CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, MakeFaithLearnMenu);
        }

        private CampaignTime studyStartTime;

        private void MakeFaithLearnMenu(CampaignGameStarter campaignGameStarter) //Will be replaced
        {
            campaignGameStarter.AddWaitGameMenu("studying_faith", "{=faith_menu_text.1}Learning...", args => studyStartTime = CampaignTime.Now, null, null, (args, dt) =>
            {
                if (studyStartTime.ElapsedHoursUntilNow >= 1)
                {
                    Hero.MainHero.AddSkillXp(RFSkills.Faith, (Hero.MainHero.GetSkillValue(RFSkills.Faith) + 1) * 15);
                    Hero.MainHero.ChangeHeroGold(-20);
                    studyStartTime = CampaignTime.Now;
                }

            }, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.SettlementWithBoth);

            campaignGameStarter.AddGameMenuOption("studying_faith", "study_leave", "{=faith_menu_text.2}Stop studying", args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                PlayerEncounter.Current.IsPlayerWaiting = false;
                GameMenu.ExitToLast();

            }, true);

            campaignGameStarter.AddGameMenuOption("town", "learn_faith", "{=faith_menu_text.3}Study faith", args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return Settlement.CurrentSettlement.IsTown;
                },
                args =>
                {
                    GameMenu.SwitchToMenu("studying_faith");

                });

        }
        private void OnMissionStarted(IMission imission)
        {
            Mission mission = imission as Mission;
            if (mission != null)
            {
                mission.AddMissionBehavior(new RFFaithMissionBehavior());
            }
        }


        private void OnMapEventEnd(MapEvent mapEvent)
        {
            if (mapEvent == null) return;
            if (mapEvent.IsFieldBattle || mapEvent.IsHideoutBattle || mapEvent.IsSiegeAssault ||
                mapEvent.IsSiegeAmbush || mapEvent.IsRaid)
            {
                MapEventSide[] mapEventSides = { mapEvent.AttackerSide, mapEvent.DefenderSide};

                foreach (var mapEventSide in mapEventSides)
                {
                    Hero leaderHero = mapEventSide?.LeaderParty?.LeaderHero;
                    if (leaderHero != null)
                    {
                        //Heals all companions based on the faith perks
                        float leaderHealFactor = leaderHero.GetPerkValue(RFPerks.Faith.DruidsSong) ? leaderHero.CharacterObject.GetFaithPerkBonus(RFPerks.Faith.DruidsSong) :
                            (leaderHero.GetPerkValue(RFPerks.Faith.DruidsSongII) ? leaderHero.CharacterObject.GetFaithPerkBonus(RFPerks.Faith.DruidsSongII) : 0);
                        if (leaderHealFactor > 0)
                            leaderHero.Heal((int)(leaderHero.MaxHitPoints * leaderHealFactor));

                        //Heals all troops based on the last faith perk
                        float partyHealFactor = leaderHero.GetPerkValue(RFPerks.Faith.DruidsBlessing) ? leaderHero.CharacterObject.GetFaithPerkBonus(RFPerks.Faith.DruidsBlessing) : 0;

                        if (partyHealFactor > 0)
                        {
                            foreach (var companion in mapEventSide.LeaderParty.MemberRoster.GetTroopRoster().FindAll(x => x.Character?.IsHero == true))
                            {
                                if (companion.Character.IsHero)
                                    companion.Character.HeroObject?.Heal((int)(companion.Character.HeroObject?.HitPoints * partyHealFactor));
                            }

                            List<TroopRosterElement> troopRoster = mapEventSide.LeaderParty.MemberRoster
                                .GetTroopRoster().FindAll(x => x.Character?.IsHero == false);
                            float healAmount = mapEventSide.LeaderParty.NumberOfWoundedTotalMembers / 2;
                            HealRegulars(mapEventSide.LeaderParty.MobileParty, ref healAmount);
                        }



                    }
                }
            }
        }
        private static void HealRegulars(MobileParty mobileParty, ref float regularsHealingValue)
        {
            TroopRoster memberRoster = mobileParty.MemberRoster;
            if (memberRoster.TotalWoundedRegulars == 0)
            {
                regularsHealingValue = 0.0f;
            }
            else
            {
                int a = MathF.Floor(regularsHealingValue);
                regularsHealingValue -= (float)a;
                int healedTroopCount = 0;
                float num1 = 0.0f;
                int num2 = MBRandom.RandomInt(memberRoster.Count);
                for (int index1 = 0; index1 < memberRoster.Count && a > 0; ++index1)
                {
                    int index2 = (num2 + index1) % memberRoster.Count;
                    CharacterObject characterAtIndex = memberRoster.GetCharacterAtIndex(index2);
                    if (characterAtIndex.IsRegular)
                    {
                        int num3 = MathF.Min(a, memberRoster.GetElementWoundedNumber(index2));
                        if (num3 > 0)
                        {
                            memberRoster.AddToCountsAtIndex(index2, 0, -num3);
                            a -= num3;
                            healedTroopCount += num3;
                            num1 += (float)(characterAtIndex.Tier * num3);
                        }
                    }
                }
                if (healedTroopCount <= 0)
                    return;
                SkillLevelingManager.OnRegularTroopHealedWhileWaiting(mobileParty, healedTroopCount, num1 / (float)healedTroopCount);
            }
        }
        public override void SyncData(IDataStore dataStore)
        {   
            
        }
    }
}

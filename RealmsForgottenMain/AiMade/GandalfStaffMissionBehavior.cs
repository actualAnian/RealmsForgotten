using System;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RealmsForgotten.AiMade
{
    internal class GandalfStaffMissionBehavior : MissionBehavior
    {
        private bool isCasting = false;
        private int maxUses;
        public SpellStatusVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private TextObject healingTextObject = new TextObject("{=gandalf_staff_status}Healing uses left: {AMOUNT}");
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void AfterStart()
        {
            if (Campaign.Current != null)
                maxUses = (int)(Math.Round(Hero.MainHero.GetSkillValue(RFSkills.Arcane) / 100.0) * 10);
            else
                maxUses = 3;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Agent.Main != null && Agent.Main.WieldedWeapon.Item?.StringId.Contains("rfmisc_gandalf_staff_a") == true)
            {
                Agent main = Agent.Main;
                if (isCasting)
                {
                    if (main.GetCurrentAction(0).Name.Contains("act_cheer") && main.GetCurrentActionProgress(0) >= 0.8)
                    {
                        main.SetActionChannel(0, ActionIndexCache.act_none, true);
                        isCasting = false;
                    }
                }
                if (Input.IsKeyReleased(InputKey.RightMouseButton) && HealTroops(main))
                {
                    main.SetActionChannel(0, ActionIndexCache.Create("act_cheer_1"), true);
                    isCasting = true;
                }
            }
        }

        private bool HealTroops(Agent main)
        {
            if (maxUses <= 0)
            {
                MBInformationManager.AddQuickInformation(new TextObject("{=staff_energy_spent}You have used all the staff's energy."));
                return false;
            }

            int healedCount = 0;
            foreach (Agent ally in Mission.Current.Agents)
            {
                if (ally.Team == main.Team && ally.Health < ally.HealthLimit)
                {
                    ally.Health += ally.HealthLimit * 0.25f;
                    if (ally.Health > ally.HealthLimit)
                    {
                        ally.Health = ally.HealthLimit;
                    }
                    healedCount++;
                }
            }

            if (healedCount == 0)
            {
                MBInformationManager.AddQuickInformation(new TextObject("{=no_allies_to_heal}No allies need healing."));
                return false;
            }

            MBInformationManager.AddQuickInformation(new TextObject($"{healedCount} allies healed!"));
            maxUses--;

            healingTextObject.SetTextVariable("AMOUNT", maxUses);
            _dataSource.SpellText = healingTextObject.ToString();

            return true;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.IsMainAgent)
            {
                MissionScreen? missionScreen = TaleWorlds.ScreenSystem.ScreenManager.TopScreen as MissionScreen;
                healingTextObject.SetTextVariable("AMOUNT", maxUses);
                _dataSource = new SpellStatusVM(healingTextObject.ToString(), agent.WieldedWeapon.Item?.StringId.Contains("rfmisc_gandalf_staff_a") == true, 20, 22);
                _gauntletLayer = new GauntletLayer(-1);
                missionScreen.AddLayer(_gauntletLayer);
                _gauntletLayer.LoadMovie("SpellStatus", _dataSource);

                agent.OnMainAgentWieldedItemChange += OnMainAgentWieldedItemChange;
            }
        }

        private void OnMainAgentWieldedItemChange()
        {
            _dataSource.Visible = Agent.Main?.WieldedWeapon.Item?.StringId.Contains("rfmisc_gandalf_staff_a") == true;
        }
    }
}
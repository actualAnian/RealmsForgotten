using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Quest
{
    class FindRelicsHideoutMissionBehavior : MissionBehavior
    {
        private bool relicSpawned = false;
        readonly JournalLog _findMapJournalLog;

        public FindRelicsHideoutMissionBehavior(JournalLog findMapJournalLog)
        {
            _findMapJournalLog = findMapJournalLog;
            relicSpawned = false;

        }
        public override void AfterStart()
        {
            if (!relicSpawned)
            {
                ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>("relic_map_arrow");
                MissionWeapon missionWeapon = new MissionWeapon(item, new ItemModifier(), Banner.CreateOneColoredEmptyBanner(1));
                Vec3 pos = Vec3.Invalid;
                Vec3 rot = Vec3.Invalid;
                switch (Settlement.CurrentSettlement.Hideout.StringId)
                {

                    case "hideout_seaside_13":
                        if (_findMapJournalLog.CurrentProgress == 0)
                        {
                            pos = new Vec3(271.46f, 311.80f, 13.11f);
                            rot = new Vec3(20f, 15f, 0f);
                        }

                        break;
                    case "hideout_seaside_14":
                        if (_findMapJournalLog.CurrentProgress == 1)
                        {
                            pos = new Vec3(641.27f, 605.47f, 55.95f);
                            rot = new Vec3(-12f, 10f, 0f);
                        }

                        break;
                    case "hideout_seaside_11":
                        if (_findMapJournalLog.CurrentProgress == 2)
                        {
                            pos = new Vec3(221.96f, 337.70f, 53.28f);
                            rot = new Vec3(1.49f, 0.17f, 174.52f);
                        }
                        break;
                }
                if (pos != Vec3.Invalid)
                    this.Mission.SpawnWeaponWithNewEntityAux(missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, new MatrixFrame(Mat3.CreateMat3WithForward(rot),
                        pos), 0, null, false);

                this.Mission.OnItemPickUp += OnItemPickup;
                relicSpawned = true;
            }

        }

        public void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            if (item.WeaponCopy.Item.StringId == "relic_map_arrow")
            {
                TextObject textObject = GameTexts.FindText("rf_second_quest_first_part_log_info"); ;
                switch (Settlement.CurrentSettlement.Hideout.StringId)
                {
                    case "hideout_seaside_13":
                        MBInformationManager.ShowSceneNotification(new FindingRelicMapSceneNotificationItem(() =>
                        {
                            _findMapJournalLog.UpdateCurrentProgress(1);
                            textObject.SetTextVariable("CURRENT_COUNT", 1);
                            MBInformationManager.AddQuickInformation(textObject, 0, null, "");
                        }));


                        break;
                    case "hideout_seaside_14":
                        _findMapJournalLog.UpdateCurrentProgress(2);
                        textObject.SetTextVariable("CURRENT_COUNT", 2);
                        MBInformationManager.AddQuickInformation(textObject, 0, null, "");
                        break;
                    case "hideout_seaside_11":
                        _findMapJournalLog.UpdateCurrentProgress(3);
                        textObject.SetTextVariable("CURRENT_COUNT", 3);
                        MBInformationManager.AddQuickInformation(textObject, 0, null, "");
                        break;
                }

                PartyBase.MainParty.ItemRoster.AddToCounts(
                    MBObjectManager.Instance.GetObject<ItemObject>("relic_map_arrow"), 1);
            }

        }
        public override void OnMissionTick(float dt)
        {
            int i = 0;
        }
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    }
}

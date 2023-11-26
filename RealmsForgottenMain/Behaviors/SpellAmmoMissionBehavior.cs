using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.LordConversationsCampaignBehavior;

namespace RealmsForgotten.Behaviors
{
    [HarmonyPatch(typeof(Agent), "OnWeaponAmmoReload")]
    public static class OnWeaponAmmoReloadPatch
    {
        
        public static bool Prefix(EquipmentIndex slotIndex, ref EquipmentIndex ammoSlotIndex, ref short totalAmmo, Agent __instance)
        {
            if (__instance.IsMainAgent && __instance.Equipment[slotIndex].Item?.StringId.Contains("staff") == true && SpellAmmoMissionBehavior.CurrentAmmo.changedAmmo)
            {
                if (__instance.Equipment[ammoSlotIndex].Amount >= 1)
                {
                    if (ammoSlotIndex != SpellAmmoMissionBehavior.CurrentAmmo.currentAmmo)
                    {

                        __instance.SetWeaponAmountInSlot(SpellAmmoMissionBehavior.CurrentAmmo.currentAmmo, (short)(__instance.Equipment[SpellAmmoMissionBehavior.CurrentAmmo.currentAmmo].Amount - 1), true);
                        __instance.SetWeaponAmountInSlot(ammoSlotIndex, (short)(__instance.Equipment[ammoSlotIndex].Amount + 1), true);


                        totalAmmo = 0;

                        ammoSlotIndex = SpellAmmoMissionBehavior.CurrentAmmo.currentAmmo;
                    }
                }
                else
                {
                    SpellAmmoMissionBehavior.CurrentAmmo.currentAmmo = ammoSlotIndex;
                    if (__instance.Equipment[ammoSlotIndex].Amount <= 0)
                    {
                        SpellAmmoMissionBehavior.Instance?.SetUiVisible(false);
                    }
                    SpellAmmoMissionBehavior.Instance?.ChangeUiSpellName(__instance.Equipment[ammoSlotIndex].Item?.Name.ToString());
                }
                    
            }

            return true;
        }
    }
    internal class SpellAmmoMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public static (bool changedAmmo, EquipmentIndex currentAmmo) CurrentAmmo;

        public SpellStatusVM _dataSource;

        private GauntletLayer _gauntletLayer;

        private TextObject spellTextObject = new TextObject("{=spell_status}Current spell: {CURRENT_SPELL}");

        public static SpellAmmoMissionBehavior Instance;


        public SpellAmmoMissionBehavior()
        {
            Instance = this;
        }
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.IsMainAgent)
            {
                for (EquipmentIndex index = EquipmentIndex.Weapon0; index <= EquipmentIndex.Weapon3; index++)
                {
                    if (agent.Equipment[index].Item?.Type == ItemObject.ItemTypeEnum.Bullets)
                    {

                            CurrentAmmo.currentAmmo = index;

                            spellTextObject.SetTextVariable("CURRENT_SPELL", agent.Equipment[index].Item.Name);

                            MissionScreen? missionScreen = TaleWorlds.ScreenSystem.ScreenManager.TopScreen as MissionScreen;
                            _dataSource = new SpellStatusVM(spellTextObject.ToString(), agent.WieldedWeapon.Item?.StringId.Contains("staff") == true);
                            _gauntletLayer = new GauntletLayer(-1);
                            missionScreen.AddLayer(_gauntletLayer);
                            _gauntletLayer.LoadMovie("SpellStatus", _dataSource);

                            agent.OnMainAgentWieldedItemChange += OnMainAgentWieldedItemChange;
                            break;
                    }
                }
            }
        }

        private void OnMainAgentWieldedItemChange()
        {
            if (Agent.Main?.WieldedWeapon.Item?.StringId.Contains("staff") == true)
                SetUiVisible(true);
            else
                SetUiVisible(false);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Input.IsKeyReleased(InputKey.V) && Agent.Main?.WieldedWeapon.Item?.StringId.Contains("staff") == true && Agent.Main?.WieldedWeapon.Item?.Type == ItemObject.ItemTypeEnum.Musket)
            {
                SetNextAmmoSlot();
            }
        }

        public void SetUiVisible(bool visible) => _dataSource.Visible = visible;
        public void ChangeUiSpellName(string name)
        {
            spellTextObject.SetTextVariable("CURRENT_SPELL", name);
            _dataSource.SpellText = spellTextObject.ToString();
        }
        private void SetNextAmmoSlot()
        {
            Agent main = Agent.Main;;

            List<EquipmentIndex> excludedIndexes = new() { main.GetWieldedItemIndex(Agent.HandIndex.MainHand) , CurrentAmmo.currentAmmo };

            int min = 0;
            int current = (int)excludedIndexes[1];
            int max = 3;
            int index = -1;
    

            while (index != current)
            {
                if (index == -1)
                    index = current;

                if (!excludedIndexes.Contains((EquipmentIndex)index) && main.Equipment[(EquipmentIndex)index].Item?.Type == ItemObject.ItemTypeEnum.Bullets && main.Equipment[index].Item != main.Equipment[current].Item && main.Equipment[(EquipmentIndex)index].Amount >= 1)
                {
                    CurrentAmmo = (true, (EquipmentIndex)index);

                    ChangeUiSpellName(main.Equipment[index].Item.Name.ToString());

                    return;
                }

                index++;

                if (index > max)
                    index = min;
            }

            CurrentAmmo.changedAmmo = false;
        }

    }
}

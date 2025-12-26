using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RealmsForgotten.Behaviors
{
    public static class RFSpellAmmo
    {
        public static void OnWeaponAmmoReloadPatch(EquipmentIndex slotIndex, ref EquipmentIndex ammoSlotIndex, ref short totalAmmo, Agent __instance)
        {
            if (__instance.IsMainAgent && __instance.Equipment[slotIndex].Item?.Type == ItemObject.ItemTypeEnum.Musket)
            {
                SpellAmmoMissionBehavior.Instance?.SetUiVisible(true);
                if (__instance.Equipment[SpellAmmoMissionBehavior.CurrentAmmo].Amount >= 1)
                {
                    if (ammoSlotIndex != SpellAmmoMissionBehavior.CurrentAmmo)
                    {
                        __instance.SetWeaponAmountInSlot(SpellAmmoMissionBehavior.CurrentAmmo, (short)(__instance.Equipment[SpellAmmoMissionBehavior.CurrentAmmo].Amount - 1), true);
                        __instance.SetWeaponAmountInSlot(ammoSlotIndex, (short)(__instance.Equipment[ammoSlotIndex].Amount + 1), true);
                        totalAmmo = 0;
                        ammoSlotIndex = SpellAmmoMissionBehavior.CurrentAmmo;
                    }
                }
                else
                {
                    SpellAmmoMissionBehavior.CurrentAmmo = ammoSlotIndex;
                    if (__instance.Equipment[ammoSlotIndex].Amount <= 0)
                        SpellAmmoMissionBehavior.Instance?.SetUiVisible(false);
                }
                SpellAmmoMissionBehavior.Instance?.ChangeUiSpellName(__instance.Equipment[ammoSlotIndex]);
            }
        }
    }
    internal class SpellAmmoMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public static EquipmentIndex CurrentAmmo;

        public SpellStatusVM _dataSource;

        private GauntletLayer _gauntletLayer;

        private TextObject spellTextObject = new TextObject("{=spell_status}Current spell: {CURRENT_SPELL} ({AMOUNT})");

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

                            CurrentAmmo = index;

                            spellTextObject.SetTextVariable("CURRENT_SPELL", agent.Equipment[index].Item.Name);
                            spellTextObject.SetTextVariable("AMOUNT", agent.Equipment[index].Amount);
                            MissionScreen? missionScreen = TaleWorlds.ScreenSystem.ScreenManager.TopScreen as MissionScreen;
                            _dataSource = new SpellStatusVM(spellTextObject.ToString(), agent.WieldedWeapon.Item?.StringId.Contains("staff") == true, 65, 120);
                            _gauntletLayer = new GauntletLayer("SpellAmmoView", -1);
                            missionScreen.AddLayer(_gauntletLayer);
                            _gauntletLayer.LoadMovie("SpellStatus", _dataSource);

                            agent.OnMainAgentWieldedItemChange += OnMainAgentWieldedItemChange;

                            break;
                    }
                }
            }
        }

        private void MissionOnOnItemDrop(Agent agent, SpawnedItemEntity spawnedItemEntity)
        {
            
        }

        private void OnMainAgentWieldedItemChange()
        {
            if (Agent.Main?.WieldedWeapon.Item?.Type == ItemObject.ItemTypeEnum.Musket)
                SetUiVisible(true);
            else
                SetUiVisible(false);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Input.IsKeyReleased(SubModule.Instance.KeysConfig[nameof(RFSettings.Instance.ChangeSpellKey)]) && Agent.Main?.WieldedWeapon.Item?.Type == ItemObject.ItemTypeEnum.Musket)
            {
                SetNextAmmoSlot();
            }
        }

        public void SetUiVisible(bool visible) => _dataSource.Visible = visible;
        public void ChangeUiSpellName(MissionWeapon weapon)
        {
            spellTextObject.SetTextVariable("CURRENT_SPELL", weapon.Item?.Name);
            spellTextObject.SetTextVariable("AMOUNT", weapon.Amount);

            _dataSource.SpellText = spellTextObject.ToString();
        }
        private void SetNextAmmoSlot()
        {
            Agent main = Agent.Main;

            List<EquipmentIndex> excludedIndexes = new() { main.GetPrimaryWieldedItemIndex() , CurrentAmmo };

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
                    CurrentAmmo = (EquipmentIndex)index;

                    ChangeUiSpellName(main.Equipment[index]);

                    return;
                }

                index++;

                if (index > max)
                    index = min;
            }
        }

    }
}

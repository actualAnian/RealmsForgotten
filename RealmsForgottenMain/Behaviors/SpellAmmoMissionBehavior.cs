using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Behaviors
{
    internal class SpellAmmoMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Input.IsKeyReleased(InputKey.Numpad9) && Agent.Main?.WieldedWeapon.Item?.StringId.Contains("staff") == true && Agent.Main.Equipment.ContainsNonConsumableRangedWeaponWithAmmo())
            {
                ChangeToNextWeapon();
            }
        }

        private void ChangeToNextWeapon()
        {
            Agent main = Agent.Main;;
            List<EquipmentIndex> excludedIndexes = new() { main.GetWieldedItemIndex(Agent.HandIndex.MainHand) , (EquipmentIndex)main.WieldedWeapon.AmmoWeapon.CurrentUsageIndex};

            int min = 0;
            int current = main.WieldedWeapon.AmmoWeapon.CurrentUsageIndex;
            int max = 3;
            int index = -1;

            while (index != current)
            {
                if (index == -1)
                    index = current;

                if (!excludedIndexes.Contains((EquipmentIndex)index) && main.Equipment[index].Item?.StringId.Contains("spell") == true && main.Equipment[(EquipmentIndex)index].Item.Type == ItemObject.ItemTypeEnum.Bullets)
                {
                    main.WieldedWeapon.SetAmmo(main.Equipment[(EquipmentIndex)index]);
                    InformationManager.DisplayMessage(new InformationMessage($"AMMO CHANGED TO {main.Equipment[(EquipmentIndex)index].Item.Name.ToString()}"));
                    return;
                }

                index++;

                if (index >= 3)
                    index = min;
            }
        }
    }
}

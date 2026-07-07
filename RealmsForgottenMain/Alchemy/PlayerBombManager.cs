using RealmsForgotten.Alchemy.Bombs;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RealmsForgotten.Alchemy
{
    public class PlayerBombManager
    {
        private static PlayerBombManager? _instance;
        public static PlayerBombManager Instance
        {
            get
            {
                return _instance ??= new PlayerBombManager();
            }
        }
        public Dictionary<BombDefinition, int> PlayerBombs = new();
        public void AddPlayerBombsOnMissionStart(Agent playerAgent)
        {
            PlayerBombs.Clear();
            var bombSlot = -1;
            for (int i = 0; i < 4; i++)
                if (playerAgent.Equipment[i].Item?.StringId.Contains("alchemical_pouch") == true)
                {
                    bombSlot = i;
                    break;
                }
            if (bombSlot == -1) return;
            foreach (var item in Hero.MainHero.PartyBelongedTo.ItemRoster)
            {
                if (BaseBombDefinitions.BombDefinitions.ContainsKey(item.EquipmentElement.Item))
                {
                    var bombDefinition = BaseBombDefinitions.BombDefinitions[item.EquipmentElement.Item];
                    if (PlayerBombs.Count == 0)
                    {
                        MissionWeapon weapon = new(bombDefinition.Item, null, null)
                        {
                            Amount = (short)item.Amount
                        };
                        Agent.Main.EquipWeaponWithNewEntity((EquipmentIndex)bombSlot, ref weapon);
                        //playerAgent.Equipment[bombSlot] = weapon;
                    }
                    PlayerBombs.Add(bombDefinition, item.Amount);
                }
            }
        }
        public void OnBombFired(BombDefinition bomb, Agent playerAgent, EquipmentIndex bombWeaponIndex, ref bool setNewPlayerBombType)
        {
            setNewPlayerBombType = false;
            if (!PlayerBombs.ContainsKey(bomb)) return;

            PlayerBombs[bomb] -= 1;
            if (PlayerBombs[bomb] == 0)
            {
                PlayerBombs.Remove(bomb);
                setNewPlayerBombType = true;
                //if (PlayerBombs.Count != 0)
                //{
                //    var nextBomb = PlayerBombs.First();
                //    Agent.Main.SetWeaponAmountInSlot(bombWeaponIndex, 1, false);
                //    var bombMissionWeapon = new MissionWeapon(nextBomb.Key.Item, null, null)
                //    {
                //        Amount = (short)nextBomb.Value
                //    };
                //    //Agent.Main.ammo(bombWeaponIndex, 1, false);
                //    Agent.Main.EquipWeaponWithNewEntity(bombWeaponIndex, ref bombMissionWeapon);
                //}
            }
        }
        public void SetBombToNewType(BombDefinition bomb, EquipmentIndex index = EquipmentIndex.None)
        {
            if (!PlayerBombs.ContainsKey(bomb)) return;
            var aa = PlayerBombs.First(b => b.Key == bomb);
            
            if (index == EquipmentIndex.None)
                for (int i = 0; i < 4; i++)
                    if (Agent.Main.Equipment[i].Item == null)
                    {
                        index = (EquipmentIndex)i;
                        break;
                    }
            var bombMissionWeapon = new MissionWeapon(aa.Key.Item, null, null)
            {
                Amount = (short)aa.Value
            };
            Agent.Main.EquipWeaponWithNewEntity(index, ref bombMissionWeapon);
        }
        public void SetBombToNewType()
        {
            if (PlayerBombs.Count == 0) return;
            
            var nextBomb = PlayerBombs.First();
            EquipmentIndex index = EquipmentIndex.Weapon0;
            for (int i = 0; i < 4; i++)
                if (Agent.Main.Equipment[i].Item == null || BaseBombDefinitions.IsAlchemicalBomb(Agent.Main.Equipment[i].Item))
                {
                    index = (EquipmentIndex)i;
                    break;
                }
            var bombMissionWeapon = new MissionWeapon(nextBomb.Key.Item, null, null)
            {
                Amount = (short)nextBomb.Value
            };
            Agent.Main.EquipWeaponWithNewEntity(index, ref bombMissionWeapon);
        }

    }
}

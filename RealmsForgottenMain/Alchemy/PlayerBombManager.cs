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
                // the player needs to have his equipment switched to another available bomb at least on next tick to prevent an engine crash
                setNewPlayerBombType = true;
            }
        }
        public void SetBombToNewType(BombDefinition bomb, EquipmentIndex index)
        {
            if (!PlayerBombs.ContainsKey(bomb)) return;
            var bombKeyValuePair = PlayerBombs.First(b => b.Key == bomb);
            
            SetBombToNewTypeInternal(bombKeyValuePair, index);
        }
        public void SetBombToNewType()
        {
            if (PlayerBombs.Count == 0) return;
            
            var bombKey = PlayerBombs.First();
            SetBombToNewTypeInternal(bombKey);
        }
        public void SetBombToNewType(string bombStringId)
        {
            var bombKey = PlayerBombs.FirstOrDefault(bp => bp.Key.Item.StringId == bombStringId);
            if (bombKey.Equals(default(KeyValuePair<BombDefinition, int>)))
                return;
            SetBombToNewTypeInternal(bombKey);
        }
        private void SetBombToNewTypeInternal(KeyValuePair<BombDefinition, int> nextBomb, EquipmentIndex index = EquipmentIndex.None)
        {
            if (index == EquipmentIndex.None)
                for (int i = 0; i < 4; i++)
                    if (Agent.Main.Equipment[i].Item == null)
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

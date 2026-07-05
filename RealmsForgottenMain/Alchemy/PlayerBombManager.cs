using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
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
        public void AddPlayerBombsOnMissionStart()
        {
            PlayerBombs.Clear();
            foreach (var item in Hero.MainHero.PartyBelongedTo.ItemRoster)
            {
                if (BaseBombDefinitions.BombDefinitions.ContainsKey(item.EquipmentElement.Item))
                {
                    var bombDefinition = BaseBombDefinitions.BombDefinitions[item.EquipmentElement.Item];
                    PlayerBombs.Add(bombDefinition, item.Amount);
                }
            }
        }
        public bool RemovePlayerBomb(ItemObject item)
        {
            if (BaseBombDefinitions.BombDefinitions.ContainsKey(item))
            {
                var bombDefinition = BaseBombDefinitions.BombDefinitions[item];
                if (PlayerBombs.ContainsKey(bombDefinition) && PlayerBombs[bombDefinition] > 0)
                {
                    PlayerBombs[bombDefinition]--;
                    return true;
                }
                if (PlayerBombs.ContainsKey(bombDefinition) && PlayerBombs[bombDefinition] <= 0)
                    PlayerBombs.Remove(bombDefinition);
            }
            return false;
        }
    }
}

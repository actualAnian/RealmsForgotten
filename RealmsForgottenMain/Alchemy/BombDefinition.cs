using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Alchemy
{
    public record BombDefinition(ItemObject Item, string BaseDescription, string spriteStringId, Func<Hero, int> Duration);
    public static class BaseBombDefinitions
    {
        public static Dictionary<ItemObject, BombDefinition> BombDefinitions = new();
        public static void Initialize()
        {
            AddBombDefinition(Game.Current.ObjectManager.GetObject<ItemObject>("heat_bomb"), "heat_bomb_description", (hero) => 10);
            AddBombDefinition(Game.Current.ObjectManager.GetObject<ItemObject>("smoke_bomb"), "smoke_bomb_description", (hero) => 5);
            AddBombDefinition(Game.Current.ObjectManager.GetObject<ItemObject>("horse_bane"), "horse_bane_description", (hero) => 5);
            AddBombDefinition(Game.Current.ObjectManager.GetObject<ItemObject>("panic_dust"), "panic_dust_description", (hero) => 5);
        }
        private static void AddBombDefinition(ItemObject item, string baseDescription, Func<Hero, int> duration, string? spriteStringId = null)
        {
            spriteStringId ??= item.StringId;
            BombDefinitions[item] = new BombDefinition(item, baseDescription, spriteStringId, duration);
        }
    }
}

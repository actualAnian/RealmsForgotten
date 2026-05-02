using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace RealmsForgotten.Chamberlain
{
    internal class HouseTroopsConfig
    {
        public float UpgradeCostMultiplier { get; set; } = 1.5f;
    }
    internal class HouseTroopsEquipmentRecord
    {
        internal int index { get; set; }
        internal string itemId { get; set; }
        internal int equipmentSet { get; set; }
        internal bool isCivilan { get; set; }
        public HouseTroopsEquipmentRecord(int index, string itemId, int equipmentSet, bool isCivilan = false)
        {
            this.index = index;
            this.itemId = itemId;
            this.equipmentSet = equipmentSet;
            this.isCivilan = isCivilan;
        }
    }
    internal class HouseTroopsMarketData : IMarketData
    {
        public HouseTroopsMarketData() { }

        public int GetPrice(ItemObject item, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
        {
            return 0;
        }
        public int GetPrice(EquipmentElement itemRosterElement, MobileParty tradingParty, bool isSelling, PartyBase merchantParty)
        {
            return 0;
        }
    }
    internal class HouseTroopsSkillRecord
    {
        internal string skill { get; set; }
        internal int value { get; set; }
        public HouseTroopsSkillRecord(string skill, int value)
        {
            this.skill = skill;
            this.value = value;
        }
    }
    internal class HouseTroopsUtil
    {
        internal static PropertyInfo GetInstanceProperty<T>(T instance, string propertyName)
        {
            return typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        internal static FieldInfo GetInstanceField<T>(T instance, string fieldName)
        {
            return typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        private const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    }
}
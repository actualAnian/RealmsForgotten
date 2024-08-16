using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.AiMade.Career
{
    public class RaidLootBonusBehavior : CampaignBehaviorBase
    {
        private const float LootBonus = 0.20f; // 20% bonus for raiding villages

        [SaveableField(1)]
        private float _additionalVillageLootBonus;

        public float AdditionalVillageLootBonus
        {
            get => _additionalVillageLootBonus;
            internal set => _additionalVillageLootBonus = value;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        }

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (IsPlayerVictoryInRaid(mapEvent))
            {
                ApplyLootBonus(mapEvent);
            }
        }

        private bool IsPlayerVictoryInRaid(MapEvent mapEvent)
        {
            return mapEvent.MapEventSettlement != null
                && mapEvent.MapEventSettlement.IsVillage
                && IsPlayerSide(mapEvent.AttackerSide)
                && mapEvent.WinningSide == BattleSideEnum.Attacker;
        }

        private bool IsPlayerSide(MapEventSide side)
        {
            return side.Parties.Exists(party => party.Party == MobileParty.MainParty.Party);
        }

        private void ApplyLootBonus(MapEvent mapEvent)
        {
            var lootRoster = GetLootRoster(mapEvent);
            if (lootRoster != null)
            {
                for (int i = 0; i < lootRoster.Count; i++)
                {
                    var itemRosterElement = lootRoster.GetElementCopyAtIndex(i);
                    int newAmount = (int)(itemRosterElement.Amount * (1 + LootBonus + _additionalVillageLootBonus));
                    lootRoster.AddToCounts(itemRosterElement.EquipmentElement, newAmount - itemRosterElement.Amount);
                }
                InformationManager.DisplayMessage(new InformationMessage($"Loot increased by {LootBonus * 100 + _additionalVillageLootBonus * 100}% due to raid bonus."));
            }
        }

        private ItemRoster GetLootRoster(MapEvent mapEvent)
        {
            var playerParty = MobileParty.MainParty.Party;
            foreach (var party in mapEvent.AttackerSide.Parties)
            {
                if (party.Party == playerParty)
                {
                    return MobileParty.MainParty.ItemRoster;
                }
            }

            foreach (var party in mapEvent.DefenderSide.Parties)
            {
                if (party.Party == playerParty)
                {
                    return MobileParty.MainParty.ItemRoster;
                }
            }

            return null;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_additionalVillageLootBonus", ref _additionalVillageLootBonus);
        }
    }
}
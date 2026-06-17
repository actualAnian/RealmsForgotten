using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TaleWorlds.Core.ImageIdentifiers;

namespace RealmsForgotten.AiMade
{
    public class ConsulHallRecruitmentBehavior : CampaignBehaviorBase
    {
        private List<PendingRecruitment> _pending = new List<PendingRecruitment>();

        // Dictionary of settlements that have a Consul Hall
        private readonly Dictionary<string, string> _consulHallSettlements = new Dictionary<string, string>
        {
            { "town_EN1", "Consul Hall of Aispur" },
            { "town_ES1", "Consul Hall of Verbrund" },
            { "town_EW1", "Consul Hall of Vesperia" },
            { "town_B1", "Consul Hall of Myrthrail" },
            { "town_V1", "Consul Hall of Albaicin" },
            { "town_S1", "Consul Hall of Erkiduh" },
            { "town_K1", "Consul Hall of Baltakhand" },
            { "town_A1", "Consul Hall of Ityr" },
            { "town_A5", "Consul Hall of Balik" },
            { "town_dwarf_1", "Consul Hall of Khazrak Tor" },
            { "town_Urk1", "Consul Hall of Krudak's Maw" },
            { "town_G1", "Consul Hall of Uztlecot" },
            { "town_W1", "Consul Hall of Thulbrecht" },
            { "town_GW1", "Consul Hall of Wolfsreach" },
            { "town_EM1", "Consul Hall of Augurion" }
        };

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_pendingRecruitments", ref _pending);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "consul_hall",
                "Visit the Consul Hall",
                args =>
                {
                    Settlement current = Settlement.CurrentSettlement;
                    return current != null && _consulHallSettlements.ContainsKey(current.StringId);
                },
                args => ShowConsulTroopSelection(),
                false);
        }

        private void ShowConsulTroopSelection()
        {
            List<CharacterObject> availableTroops = new List<CharacterObject>();
            if (Hero.MainHero.Culture?.BasicTroop != null)
                availableTroops.Add(Hero.MainHero.Culture.BasicTroop);
            if (Hero.MainHero.Culture?.EliteBasicTroop != null)
                availableTroops.Add(Hero.MainHero.Culture.EliteBasicTroop);

            if (availableTroops.Count == 0)
            {
                InformationManager.ShowInquiry(new InquiryData("No Troops", "Your culture has no troops available here.", true, false, "OK", null, null, null), true);
                return;
            }

            string hallName = _consulHallSettlements.TryGetValue(Settlement.CurrentSettlement.StringId, out string name)
                ? name
                : "Consul Hall";

            string title = $"Recruit from the {hallName}";

            List<InquiryElement> elements = availableTroops
                .Select(troop => new InquiryElement(
                    troop,
                    troop.Name.ToString(),
                    new CharacterImageIdentifier(CharacterCode.CreateFrom(troop))
                )).ToList();

            MBInformationManager.ShowMultiSelectionInquiry(
                new MultiSelectionInquiryData(
                    title,
                    "Select the type of troop you wish to recruit.",
                    elements,
                    true, 1, 1,
                    GameTexts.FindText("str_done").ToString(),
                    GameTexts.FindText("str_cancel").ToString(),
                    list => OnTroopSelected(list.FirstOrDefault()?.Identifier as CharacterObject),
                    null
                ),
                false, false
            );
        }

        private void OnTroopSelected(CharacterObject troop)
        {
            if (troop == null) return;
            int multiplier = (troop == Hero.MainHero.Culture.EliteBasicTroop) ? 20 : 10;
            int baseCost = troop.TroopWage * multiplier;


            InformationManager.ShowTextInquiry(new TextInquiryData(
                "Select Quantity",
                $"How many {troop.Name} do you want to recruit?\nEach costs {baseCost} gold.",
                true, true,
                "Recruit", "Cancel",
                text =>
                {
                    if (int.TryParse(text, out int number) && number > 0)
                    {
                        TryRecruit(troop, number, multiplier);
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Invalid quantity."));
                    }
                },
                null
            ));
        }

        private void TryRecruit(CharacterObject troop, int number, int costMultiplier)
        {
            int cost = number * troop.TroopWage * costMultiplier;

            if (Hero.MainHero.Gold < cost)
            {
                InformationManager.DisplayMessage(new InformationMessage("You don't have enough gold."));
                return;
            }

            // Pay
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -cost);

            // Nearest settlement of same culture for delivery time
            Settlement nearest = Settlement.All
                .Where(s => s?.Culture == Hero.MainHero.Culture)
                .OrderBy(s => s.Position.Distance(MobileParty.MainParty.Position))
                .FirstOrDefault();

            if (nearest == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("No settlement of your culture was found."));
                return;
            }

            float distance = MobileParty.MainParty.Position.Distance(nearest.Position);
            float hours = distance / 4f;
            CampaignTime deliveryTime = CampaignTime.HoursFromNow(hours);

            _pending.Add(new PendingRecruitment(troop, number, deliveryTime));

            InformationManager.DisplayMessage(new InformationMessage(
                $"You paid {cost} gold. {number} {troop.Name} will arrive from {nearest.Name} in about {hours:0} hours."));
        }

        private void OnDailyTick()
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].DeliveryTime.IsPast)
                {
                    MobileParty.MainParty.AddElementToMemberRoster(_pending[i].Troop, _pending[i].Number);
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"{_pending[i].Number} {_pending[i].Troop.Name} have joined your party (delivered from the Consul Hall)."));
                    _pending.RemoveAt(i);
                }
            }
        }
    }
}

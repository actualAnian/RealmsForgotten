using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;

namespace RealmsForgotten.AiMade.MercenaryFaction
{
    public class MercenaryHireBehavior : CampaignBehaviorBase
    {
        private const int PricePerUnit = 250;
        private const float CooldownDays = 30f;
        private static readonly int[] CompanySizes = { 5, 10, 20 };

        // town ID → troop ID
        private static readonly Dictionary<string, string> TownTroopMap = new()
        {
            { "town_KTG5", "khatogai_tier_1" },
            { "town_CB7", "valthorne_knight" }
        };

        private static readonly HashSet<string> EligibleTowns = new(TownTroopMap.Keys);

        private Dictionary<string, CampaignTime> _lastHire = new();
        private string _captainHeroId;
        private MobileParty _activeMercParty;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);

            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, p =>
            {
                if (p == _activeMercParty)
                    p.Ai.SetMoveEscortParty(MobileParty.MainParty);
            });

            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastHire", ref _lastHire);
            dataStore.SyncData("_captainHeroId", ref _captainHeroId);
        }

        private void OnGameLoaded(CampaignGameStarter _)
        {
            if (!string.IsNullOrEmpty(_captainHeroId))
            {
                Hero captain = Hero.AllAliveHeroes.FirstOrDefault(h => h.StringId == _captainHeroId);
                _activeMercParty = captain?.PartyBelongedTo;
                _activeMercParty?.Ai.SetMoveEscortParty(MobileParty.MainParty);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "town", "hire_merc_root",
                "Hire Mercenary Company",
                RootCond,
                _ => GameMenu.SwitchToMenu("merc_choose_size"),
                isLeave: false);

            starter.AddGameMenu("merc_choose_size", "Choose company size:", null);

            foreach (int size in CompanySizes)
            {
                int cost = size * PricePerUnit;
                string id = $"hire_size_{size}";
                string label = $"{size} troops – {cost:N0} denars";

                starter.AddGameMenuOption(
                    "merc_choose_size", id, label,
                    args => Hero.MainHero.Gold >= cost,
                    args => Hire(size, cost),
                    isLeave: true);
            }

            starter.AddGameMenuOption(
                "merc_choose_size", "hire_cancel", "Cancel",
                args => true, args => GameMenu.ExitToLast(),
                isLeave: true);
        }

        private bool RootCond(MenuCallbackArgs args)
        {
            Settlement town = Hero.MainHero.CurrentSettlement;
            return town != null && town.IsTown && EligibleTowns.Contains(town.StringId);
        }

        private void Hire(int size, int cost)
        {
            Settlement town = Hero.MainHero.CurrentSettlement;

            if (_lastHire.TryGetValue(town.StringId, out var last))
            {
                CampaignTime next = last + CampaignTime.Days(CooldownDays);
                if (CampaignTime.Now < next)
                {
                    double daysLeft = (next - CampaignTime.Now).ToDays;
                    InformationManager.DisplayMessage(
                        new InformationMessage($"You must wait {daysLeft:0.#} more days before hiring here again."));
                    GameMenu.ExitToLast();
                    return;
                }
            }

            if (Hero.MainHero.Gold < cost)
            {
                InformationManager.DisplayMessage(new InformationMessage("You cannot afford that number of mercenaries."));
                GameMenu.ExitToLast();
                return;
            }

            Hero.MainHero.ChangeHeroGold(-cost);
            SpawnMercenaryParty(town, size);
            _lastHire[town.StringId] = CampaignTime.Now;

            InformationManager.DisplayMessage(
                new InformationMessage($"Hired {size} mercenaries for {cost:N0} denars."));
            GameMenu.ExitToLast();
        }

        private void SpawnMercenaryParty(Settlement town, int count)
        {
            string troopId = TownTroopMap.TryGetValue(town.StringId, out var tid) ? tid : "khatogai_tier_1";
            string wandererId = town.Culture.StringId switch
            {
                "empire" => "spc_wanderer_empire_0",
                "vlandia" => "spc_wanderer_vlandia_0",
                "battania" => "spc_wanderer_battania_0",
                "khuzait" => "spc_wanderer_khuzait_0",
                "aserai" => "spc_wanderer_aserai_0",
                "sturgia" => "spc_wanderer_sturgia_0",
                "katogai" => "spc_wanderer_khuzait_0",  // fallback
                "valthorne" => "spc_wanderer_vlandia_0", // add more as needed
                _ => "spc_wanderer_empire_0"
            };

            CharacterObject leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(wandererId);
            CharacterObject troopTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);

            Hero captain = HeroCreator.CreateSpecialHero(leaderTemplate, town, Clan.PlayerClan);
            captain.SetName(new TextObject("Mercenary"), new TextObject("Captain"));
            captain.SetNewOccupation(Occupation.Mercenary);

            MobileParty party = Clan.PlayerClan.CreateNewMobileParty(captain);
            party.MemberRoster.AddToCounts(troopTemplate, count);

            party.Position2D = town.GatePosition;
            party.Ai.SetMoveEscortParty(MobileParty.MainParty);

            party.SetCustomName(new TextObject("{=merc_party_name}Mercenary Company"));
            _activeMercParty = party;
            _captainHeroId = captain.StringId;
        }
    }
}
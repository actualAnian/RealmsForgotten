using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using Helpers;

namespace RealmsForgotten.AiMade.MercenaryFaction
{
    public class MercenaryHireBehavior : CampaignBehaviorBase
    {
        private const int PricePerUnit = 250;
        private const float CooldownDays = 30f;
        private static readonly int[] CompanySizes = { 5, 10, 20 };
             
        private static readonly Dictionary<string, Dictionary<int, Dictionary<string, int>>> TownMercenaryRosterMap = new()
        {
        { "town_KTG5", new Dictionary<int, Dictionary<string, int>>
            {
                // Companhia de 5: 5 Batedores
                { 5, new Dictionary<string, int> { { "khatogai_tier_1", 5 } } },
                // Companhia de 10: 7 Lanceiros e 3 Arqueiros
                { 10, new Dictionary<string, int> { { "khatogai_tier_1", 7 }, { "khatogai_tier_2", 3 } } },
                // Companhia de 20: 12 Guardas, 5 Arqueiros Montados, 3 Cavaleiros Pesados
                { 20, new Dictionary<string, int> { { "khatogai_tier_1", 12 }, { "khatogai_tier_2", 5 }, { "khatogai_tier_3", 3 } } }
            }
        },
        { "town_CB7", new Dictionary<int, Dictionary<string, int>>
            {
                
                { 5, new Dictionary<string, int> { { "valthorne_footman", 3 }, { "valthorne_levy_crossbowman", 2 } } },
               
                { 10, new Dictionary<string, int> { { "valthorne_billman", 7 }, { "valthorne_crossbowman", 3 } } },
               
                { 20, new Dictionary<string, int> { { "valthorne_knight", 15 } } }
            }
        }
    };

        private static readonly HashSet<string> EligibleTowns = new(TownMercenaryRosterMap.Keys);

        private Dictionary<string, CampaignTime> _lastHire = new();
        private string _captainHeroId;
        private MobileParty _activeMercParty;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, p =>
            {
                if (p == _activeMercParty)
                    p.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, false);
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
                _activeMercParty?.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, false);
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

        // --- MÉTODO ATUALIZADO ---
        private void SpawnMercenaryParty(Settlement town, int size)
        {
            // Define um "roster" padrão de fallback para garantir que o jogo não quebre.
            var rosterToSpawn = new Dictionary<string, int> { { "imperial_recruit", size } };

            // Tenta encontrar o roster específico para a cidade e o tamanho escolhido.
            if (TownMercenaryRosterMap.TryGetValue(town.StringId, out var sizeMap))
            {
                if (sizeMap.TryGetValue(size, out var specificRoster))
                {
                    rosterToSpawn = specificRoster;
                }
            }

            string wandererId = town.Culture.StringId switch
            {
                "empire" => "spc_wanderer_empire_0",
                "vlandia" => "spc_wanderer_vlandia_0",
                "battania" => "spc_wanderer_battania_0",
                "khuzait" => "spc_wanderer_khuzait_0",
                "aserai" => "spc_wanderer_aserai_0",
                "sturgia" => "spc_wanderer_sturgia_0",
                "katogai" => "spc_wanderer_khuzait_0",
                "valthorne" => "spc_wanderer_vlandia_0",
                _ => "spc_wanderer_empire_0"
            };

            CharacterObject leaderTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(wandererId);
            Hero captain = HeroCreator.CreateSpecialHero(leaderTemplate, town, Clan.PlayerClan);
            captain.SetName(new TextObject("Mercenary"), new TextObject("Captain"));
            captain.SetNewOccupation(Occupation.Wanderer);

            // This is what flips IsPlayerCompanion and wires up all the right state
            AddCompanionAction.Apply(Clan.PlayerClan, captain);

            MobileParty party = MobilePartyHelper.CreateNewClanMobileParty(captain, Clan.PlayerClan);
            // --- LÓGICA DE ADIÇÃO DE TROPAS ATUALIZADA ---
            // Itera sobre o roster escolhido e adiciona cada tipo de tropa com sua respectiva quantidade.
            foreach (var troopEntry in rosterToSpawn)
            {
                CharacterObject troopTemplate = MBObjectManager.Instance.GetObject<CharacterObject>(troopEntry.Key);
                if (troopTemplate != null)
                {
                    party.MemberRoster.AddToCounts(troopTemplate, troopEntry.Value);
                }
            }

            party.Position = town.GatePosition;
            party.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, false);

            party.Party.SetCustomName(new TextObject("{=merc_party_name}Mercenary Company"));
            _activeMercParty = party;
            _captainHeroId = captain.StringId;
        }
    }
}
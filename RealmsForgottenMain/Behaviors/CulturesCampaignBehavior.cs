using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.CustomSkills;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.Behaviors;

internal class MercenaryData
{
    public int Amount;
    public CampaignTime NextReset = CampaignTime.Now;


    public MercenaryData()
    {
        ResetSoldiers(MBRandom.RandomInt(5, 20));
    }
    public void ResetSoldiers(int newAmount)
    {
        Amount = newAmount;
        NextReset = CampaignTime.DaysFromNow(MBRandom.RandomInt(2, 7));
    }
}
internal class CulturesCampaignBehavior : CampaignBehaviorBase
{
    private readonly string sturgiaId = "sturgia";
    private readonly string khuzaitId = "khuzait";
    private (int currentCost, int currentAmount) currentSlaveValues;
    private (int currentCost, int currentAmount) currentMonkValues;
    private readonly int _warriorMonkCost = 2400;
    
    public static CharacterObject SlaveCharacter => CharacterObject.Find("athas_common_slave");
    public static CharacterObject WarriorMonkCharacter => CharacterObject.Find("warrior_priest");
    
    [SaveableField(1)]
    private Dictionary<Settlement, MercenaryData> townSlaveSoldiersData = new();
    [SaveableField(2)]
    private Dictionary<Settlement, MercenaryData> townMonkWarriorsData = new();
    public override void RegisterEvents()
    {
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, SturgianBonus);
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, KhuzaitBonus);
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnSettlementTick);
    }

    private void OnSettlementTick(Settlement settlement)
    {
        if (settlement?.Culture?.StringId == "aserai" && townSlaveSoldiersData?.TryGetValue(settlement, out MercenaryData data) == true)
        {
            if (data.NextReset.IsPast)
            {
                data.ResetSoldiers(MBRandom.RandomInt(1, 5));
            }
        }
        
        if (townMonkWarriorsData?.TryGetValue(settlement, out MercenaryData monksData) == true)
        {
            if (monksData.NextReset.IsPast)
            {
                monksData.ResetSoldiers((int)(Hero.MainHero.GetSkillValue(RFSkills.Faith) /
                                         RFFaithCampaignBehavior.NecessaryFaithForPriests));
            }
        }
    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
        var athasSettlements = Settlement.FindAll(settlement => settlement.Culture?.StringId == "aserai");
        foreach (var settlement in athasSettlements)
        {
            if (!townSlaveSoldiersData.ContainsKey(settlement))
                townSlaveSoldiersData.Add(settlement, new MercenaryData());
        }

        foreach (var settlement in Settlement.All)
        {
            if (!townMonkWarriorsData.ContainsKey(settlement))
                townMonkWarriorsData.Add(settlement, new MercenaryData());
        }
        
        campaignGameStarter.AddGameMenuOption("town_backstreet", "buy_slaves", "{=buy_slaves}Buy {AMOUNT} slave soldiers ({COST}{GOLD_ICON})",
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                if (townSlaveSoldiersData.TryGetValue(Settlement.CurrentSettlement, out MercenaryData data))
                {
                    int slaveCost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(SlaveCharacter, Hero.MainHero);
                    currentSlaveValues = (slaveCost * data.Amount, data.Amount);
                    GameTexts.SetVariable("AMOUNT", data.Amount);
                    GameTexts.SetVariable("COST", currentSlaveValues.currentCost);
                    if (Hero.MainHero.Gold < currentSlaveValues.currentCost)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = GameTexts.FindText("str_decision_not_enough_gold", null);
                    }
                    return data.Amount > 0 && Settlement.CurrentSettlement.Culture.StringId == "aserai";
                }
                return false;
            },
            args =>
            {
                if (townSlaveSoldiersData.TryGetValue(Settlement.CurrentSettlement, out MercenaryData data))
                {
                    MobileParty.MainParty.AddElementToMemberRoster(SlaveCharacter, currentSlaveValues.currentAmount);
                    GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, currentSlaveValues.currentCost);
                    data.Amount = 0;
                    GameMenu.SwitchToMenu("town_backstreet");
                }
            }, false, 2);

        campaignGameStarter.AddGameMenuOption("town_temple", "hire_monks", "{=hire_monks}Hire {AMOUNT} warrior priests ({COST}{GOLD_ICON})", 
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                if (townMonkWarriorsData.TryGetValue(Settlement.CurrentSettlement, out MercenaryData data))
                {
                    currentMonkValues = (_warriorMonkCost * data.Amount, data.Amount);
                    if (Hero.MainHero.Gold < currentMonkValues.currentCost)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = GameTexts.FindText("str_decision_not_enough_gold", null);
                    }
                    if (data.Amount <= 0)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=no_monks_available}No warriors available at the moment.");
                        
                        GameTexts.SetVariable("COST", 0);
                        GameTexts.SetVariable("AMOUNT", 0);
                    }
                    else
                    {
                        GameTexts.SetVariable("COST", currentMonkValues.currentCost);
                        GameTexts.SetVariable("AMOUNT", data.Amount);
                    }
                    if (Hero.MainHero.GetSkillValue(RFSkills.Faith) < RFFaithCampaignBehavior.NecessaryFaithForPriests)
                    {
                        args.IsEnabled = false;
                        TextObject textObject = new TextObject("{=low_faith_temple}You must have at least {AMOUNT} faith to recruit priests.");
                        textObject.SetTextVariable("AMOUNT", RFFaithCampaignBehavior.NecessaryFaithForPriests);
                        args.Tooltip = textObject;
                        return true;
                    }
                    return true;
                }
                return false;
            }, args =>
            {
                if (townMonkWarriorsData.TryGetValue(Settlement.CurrentSettlement, out MercenaryData data))
                {
                    MobileParty.MainParty.AddElementToMemberRoster(WarriorMonkCharacter, currentMonkValues.currentAmount);
                    GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, currentMonkValues.currentCost);
                    data.Amount = 0;
                    GameMenu.SwitchToMenu("town_temple");
                }
            });
    }

    private void KhuzaitBonus(Settlement settlement)
    {
        if ((settlement.IsTown || settlement.IsCastle) && settlement.Owner.Culture.StringId == khuzaitId && settlement.Party.PrisonRoster.TotalRegulars > 0 && settlement.MilitiaPartyComponent != null)
        {
            foreach (FlattenedTroopRosterElement troopRosterElement in settlement.Party.PrisonRoster.ToFlattenedRoster())
            {
                if (MBRandom.RandomFloat < 0.15f)
                {
                    settlement.Party.PrisonRoster.RemoveTroop(troopRosterElement.Troop);
                    settlement.MilitiaPartyComponent.Party.AddElementToMemberRoster(troopRosterElement.Troop, 1);
                }
            }
        }
    }
    private void SturgianBonus(MapEvent mapEvent)
    {

        if (mapEvent.HasWinner && mapEvent.Winner.LeaderParty.Culture.StringId == sturgiaId && SubModule.undeadRespawnConfig?.Count > 0)
        {
            List<MapEventParty> parties = mapEvent.Winner.Parties;
            foreach (MapEventParty party in parties)
            {
                //Calculation to take a number between 0.15 and 0.5 based on the level of the party owner
                float probability = 0.15f + (0.50f - 0.15f) * (party.Party.Owner.Level - 0f) / (63 - 0f);
                if (MBRandom.RandomFloat > probability)
                    continue;

                if (party.Party.Owner.Culture.StringId != sturgiaId)
                    continue;

                int wounded = party.HealthyManCountAtStart - party.Party.NumberOfHealthyMembers;

                if (wounded <= 0)
                    continue;

                int recovered = (int)(15f / 100f * wounded);

                if (recovered > 0)
                {
                    for (int i = 0; i < recovered; i++)
                    {
                        string characterId = SubModule.undeadRespawnConfig.RandomElementByWeight(x => x.Value);
                        CharacterObject characterObject = CharacterObject.Find(characterId);


                        party.Party.AddElementToMemberRoster(characterObject, 1);
                    }
                    if (party.Party == PartyBase.MainParty)
                    {
                        var textObject = new TextObject("{=undead_soldier_rejoin}{AMOUNT} undead soldiers rejoined your army");
                        textObject.SetTextVariable("AMOUNT", recovered);
                        MBInformationManager.AddQuickInformation(textObject);
                    }
                }
            }

        }
    }
    public override void SyncData(IDataStore dataStore)
    {

    }
}


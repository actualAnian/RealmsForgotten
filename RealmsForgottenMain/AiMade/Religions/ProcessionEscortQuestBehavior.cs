using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RealmsForgotten.AiMade.Religions;

public class ProcessionEscortQuestBehavior : CampaignBehaviorBase
{
    private static readonly TextObject EscortProcessionTitleText = new TextObject("Escort Religious Procession");
    private static readonly TextObject EscortProcessionText = new TextObject("A religious procession needs an escort from {START_TOWN} to {TARGET_TOWN}. Do you want to take on this task?");
    private static readonly TextObject EscortAcceptText = new TextObject("Escort Procession");
    private static readonly TextObject EscortDeclineText = new TextObject("Ignore");

    private Settlement targetSettlement;
    private bool processionQuestAccepted = false;
    private CampaignTime questDeadline;
    private MobileParty processionParty;
    private MobileParty banditParty;

    public override void RegisterEvents()
    {
        CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
        CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
        CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("ProcessionQuest_targetSettlement", ref targetSettlement);
        dataStore.SyncData("ProcessionQuest_processionQuestAccepted", ref processionQuestAccepted);
        dataStore.SyncData("ProcessionQuest_questDeadline", ref questDeadline);
        dataStore.SyncData("ProcessionQuest_processionParty", ref processionParty);
        dataStore.SyncData("ProcessionQuest_banditParty", ref banditParty);
    }

    private void OnNewGameCreated(CampaignGameStarter campaignGameStarter)
    {
        // Add any initialization logic here
    }

    private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
    {
        // Add any loading logic here
    }

    public void TriggerEscortProcessionDirectlyFromDialogue()
    {
        var playerHero = Hero.MainHero;
        var playerCulture = playerHero.Culture;

        Vec2 playerPosition = MobileParty.MainParty.Position2D;
        Settlement playerSettlement = MobileParty.MainParty.CurrentSettlement;

        var allTowns = Town.AllTowns.Where(t => t.Culture == playerCulture).ToList();

        if (playerSettlement != null)
        {
            allTowns = allTowns.Where(t => t.Settlement != playerSettlement).ToList();
        }

        var startingTown = allTowns.OrderBy(t => t.Settlement.Position2D.Distance(playerPosition)).FirstOrDefault();
        var targetTown = allTowns.Where(t => t != startingTown).OrderBy(t => t.Settlement.Position2D.Distance(startingTown.Settlement.Position2D)).FirstOrDefault();

        if (targetTown != null && startingTown != null)
        {
            StartProcessionEscort(startingTown, targetTown);
        }
    }

    private void StartProcessionEscort(Town startingTown, Town targetTown)
    {
        if (startingTown == null || targetTown == null)
        {
            InformationManager.DisplayMessage(new InformationMessage("Starting or target town is invalid.", Colors.Red));
            return;
        }

        targetSettlement = targetTown.Settlement;
        CreateProcessionParty(startingTown);
        CreateBanditParty(startingTown);

        processionQuestAccepted = true;
        questDeadline = CampaignTime.DaysFromNow(5);

        InformationManager.DisplayMessage(new InformationMessage($"Escort the procession from {startingTown.Name} to {targetTown.Name}."));
    }

    private void CreateProcessionParty(Town startingTown)
    {
        var processionPartyComponent = new ProcessionPartyComponent(startingTown.Settlement);

        processionParty = MobileParty.CreateParty("procession", processionPartyComponent, (MobileParty party) =>
        {
            Vec2 gatePosition = startingTown.Settlement.GatePosition;
            party.InitializeMobilePartyAroundPosition(new TroopRoster(party.Party), new TroopRoster(party.Party), gatePosition, 1f);
            party.SetCustomName(new TextObject("{=ReligiousProcession}Religious Procession"));
            party.Ai.SetDoNotMakeNewDecisions(true);

            ItemObject foodItem = MBObjectManager.Instance.GetObject<ItemObject>("grain");
            if (foodItem != null)
            {
                party.ItemRoster.AddToCounts(foodItem, 20);
            }

            party.MoraleExplained.Add(20, new TextObject("Initial Morale"));

            MobilePartyHelper.TryMatchPartySpeedWithItemWeight(party, MobileParty.MainParty.Speed * 2.7f);

            party.IsVisible = true;
            party.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(24));
        });

        var villagerCharacter = CharacterObject.Find("villager");

        if (villagerCharacter != null)
        {
            processionParty.MemberRoster.AddToCounts(villagerCharacter, 15); // Adding 10 villagers
        }

        var basicTroop = Hero.MainHero.Culture.BasicTroop;
        if (basicTroop != null)
        {
            processionParty.MemberRoster.AddToCounts(basicTroop, 5); // Adding 10 basic troops
        }

        if (targetSettlement != null)
        {
            processionParty.Ai.SetMoveGoToSettlement(targetSettlement);
        }
    }

    private void CreateBanditParty(Town startingTown)
    {
        var banditHideout = Hideout.All.OrderBy(h => h.Settlement.Position2D.Distance(startingTown.Settlement.Position2D)).FirstOrDefault();
        if (banditHideout != null)
        {
            Clan banditClan = Clan.BanditFactions.FirstOrDefault(clan => clan.StringId == "looters");
            if (banditClan != null)
            {
                banditParty = BanditPartyComponent.CreateBanditParty("procession_bandits", banditClan, banditHideout, false);
                banditParty.SetCustomName(new TextObject("{=ProcessionBandits}Procession Bandits"));
                banditParty.Ai.SetDoNotMakeNewDecisions(false);

                // Ensure the bandit party targets the procession party
                banditParty.Ai.SetMoveEngageParty(processionParty);

                InformationManager.DisplayMessage(new InformationMessage("Bandits have been sent to attack the procession."));
            }
        }
    }

    private void OnMapEventEnded(MapEvent mapEvent)
    {
        if (banditParty == null || processionParty == null)
        {
            return;
        }

        if (mapEvent.AttackerSide.Parties.Any(p => p.Party == banditParty.Party) && mapEvent.DefenderSide.Parties.Any(p => p.Party == processionParty.Party))
        {
            if (mapEvent.WinningSide == BattleSideEnum.Defender)
            {
                CompleteProcessionEscort();
            }
            else
            {
                FailProcessionEscort();
            }
        }
    }

    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        if (party == processionParty && settlement == targetSettlement)
        {
            CompleteProcessionEscort();
        }
    }

    private void CompleteProcessionEscort()
    {
        processionParty?.RemoveParty();
        banditParty?.RemoveParty();
        processionQuestAccepted = false;

        // Add Piety points
        PietyManager.AddPiety(Hero.MainHero, 45, true);

        // Display completion inquiry
        InformationManager.ShowInquiry(new InquiryData(
            "Procession Escort Completed",
            $"{Hero.MainHero.Name} successfully escorted the procession and gained 45 piety.",
            true,
            false,
            "OK",
            null,
            null,
            null
        ));

        InformationManager.DisplayMessage(new InformationMessage("You have successfully escorted the procession."));
    }

    private void FailProcessionEscort()
    {
        processionParty?.RemoveParty();
        banditParty?.RemoveParty();
        InformationManager.DisplayMessage(new InformationMessage("You failed to escort the procession."));
        processionQuestAccepted = false;
    }

    private void HourlyTick()
    {
        if (!processionQuestAccepted || targetSettlement == null)
            return;

        if (CampaignTime.Now > questDeadline)
        {
            processionQuestAccepted = false;
            InformationManager.DisplayMessage(new InformationMessage("You failed to escort the procession in time.", Colors.Red));
            return;
        }

        if (MobileParty.MainParty.CurrentSettlement == targetSettlement)
        {
            CompleteProcessionEscort();
        }

        if (processionParty != null && processionParty.DefaultBehavior != AiBehavior.GoToSettlement)
        {
            processionParty.Ai.SetMoveGoToSettlement(targetSettlement);
        }
    }
}

public class ProcessionPartyComponent : PartyComponent
{
    private readonly Settlement _homeSettlement;

    public ProcessionPartyComponent(Settlement homeSettlement)
    {
        _homeSettlement = homeSettlement;
    }

    public override Hero PartyOwner => Hero.MainHero;

    public override TextObject Name => new TextObject("{=ReligiousProcession}Religious Procession");

    public override Settlement HomeSettlement => _homeSettlement;
}
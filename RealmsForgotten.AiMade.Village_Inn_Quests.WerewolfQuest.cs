public void StartQuestBattle(Settlement village)
{
    if (village == null || village.Village != _targetVillage)
        return;

    var werewolfChar = CharacterObject.All.FirstOrDefault(c => c.StringId == "werewolf");
    if (werewolfChar == null)
        return;

    // Create a generic party instead of a bandit party to avoid triggering BanditSpawnCampaignBehavior
    _werewolfParty = MobileParty.CreateParty(
        "rf_werewolf_party_" + village.StringId,
        null,
        null
    );
    
    // Set up the party at the village gate
    _werewolfParty.InitializeMobilePartyAroundPosition(
        new TroopRoster(_werewolfParty.Party),
        new TroopRoster(_werewolfParty.Party),
        village.GatePosition, 
        1f
    );

    // Configure the party
    _werewolfParty.MemberRoster.Clear();
    _werewolfParty.MemberRoster.AddToCounts(werewolfChar, 5);
    _werewolfParty.Party.SetCustomName(new TextObject("Werewolf"));
    _werewolfParty.SetPartyUsedByQuest(true);
    _werewolfParty.Ai.DisableAi();
    
    // Set it as a bandit faction for behavior purposes (optional)
    _werewolfParty.ActualClan = Clan.BanditFactions.First();

    var backupRoster = MobileParty.MainParty.MemberRoster.CloneRosterData();

    try
    {
        MobileParty.MainParty.MemberRoster.Clear();
        MobileParty.MainParty.MemberRoster.AddToCounts(Hero.MainHero.CharacterObject, 1, true);

        PlayerEncounter.RestartPlayerEncounter(MobileParty.MainParty.Party, _werewolfParty.Party, false);
        PlayerEncounter.StartBattle();
        PlayerEncounter.Update();

        CampaignMission.OpenBattleMission(village.LocationComplex.GetScene("village_center", 1), false);

        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);

        AddLog(new TextObject("{=rf_werewolf_started}The duel against the werewolf has begun."));
    }
    finally
    {
        MobileParty.MainParty.MemberRoster.Clear();
        MobileParty.MainParty.MemberRoster.Add(backupRoster);
    }
}
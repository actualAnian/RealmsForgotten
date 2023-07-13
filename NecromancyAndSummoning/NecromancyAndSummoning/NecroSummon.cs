// Decompiled with JetBrains decompiler
// Type: NecromancyAndSummoning.NecroSummon
// Assembly: NecromancyAndSummoning, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: CBA83544-CC7B-4D78-8CFB-3216C3C4F741
// Assembly location: C:\Users\Pedrinho\Desktop\NecromancyAndSummoning.dll

using NecromancyAndSummoning.CustomClass;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;


#nullable enable
namespace NecromancyAndSummoning
{
    internal class NecroSummon
    {
        private static Mission previousMission;
        internal static List<BattleInfectedRecord> records;
        internal static List<CorpseLocationRecord> corpseRecords;
        private static Random random = new Random();

        public static bool Infection(Agent affectorAgent, Agent affectedAgent, string infectedTroopId)
        {
            bool flag1 = false;
            int num = NecroSummon.random.Next(1, 101);
            int probability = NecroSummon.CalculateProbability(affectorAgent, affectedAgent);
            if (num <= probability)
            {
                if (infectedTroopId != null && infectedTroopId.Length > 0)
                {
                    if (affectedAgent.State == AgentState.Unconscious && SubModule.Config.EnableNPCInfect && num <= probability - SubModule.Config.npcInfectionResistPercentage)
                        NecroSummon.NPCInfection(affectorAgent, affectedAgent);
                    flag1 = true;
                }
                else
                {
                    TextObject textObject = new TextObject("{=dead_ivalid_infect_troop}A {enemy_troop} cannot become a invalid troop", (Dictionary<string, object>)null);
                    textObject.SetTextVariable("enemy_troop", affectedAgent.Character.Name);
                    InformationManager.DisplayMessage(new InformationMessage(((object)textObject).ToString(), new Color(0.0f, 10f, 0.0f, 1f)));
                }
            }
            return flag1;
        }

        private static void AddTroopToParty(
          Agent affectorAgent,
          Agent affectedAgent,
          string infectedTroopId)
        {
            PartyBase validParty = NecroSummon.GetValidParty(affectorAgent);
            CharacterObject characterObject1 = MBObjectManager.Instance.GetObject<CharacterObject>(infectedTroopId);
            CharacterObject characterObject2 = MBObjectManager.Instance.GetObject<CharacterObject>(((MBObjectBase)affectedAgent.Character).StringId);
            if (characterObject1 != null)
            {
                validParty.AddMember(characterObject1, 1, 0);
                TextObject textObject = new TextObject("{=dead_reanimated}A fallen enemy {enemy_troop} has been reanimated to fight for {spawned_on_party}", (Dictionary<string, object>)null);
                textObject.SetTextVariable("enemy_troop", ((BasicCharacterObject)characterObject2).Name);
                textObject.SetTextVariable("spawned_on_party", validParty.Name);
                InformationManager.DisplayMessage(new InformationMessage(((object)textObject).ToString(), new Color(10f, 0.0f, 0.0f, 1f)));
            }
            else
            {
                TextObject textObject = new TextObject("{=dead_invalid_reanimated_troop}{enemy_troop} cannot reanimated as a invalid troop", (Dictionary<string, object>)null);
                textObject.SetTextVariable("enemy_troop", ((BasicCharacterObject)characterObject2).Name);
                InformationManager.DisplayMessage(new InformationMessage(((object)textObject).ToString(), new Color(0.0f, 10f, 0.0f, 1f)));
            }
        }

        private static void NPCInfection(Agent affectorAgent, Agent affectedAgent)
        {
            PartyBase validParty = NecroSummon.GetValidParty(affectorAgent);
            if (affectedAgent.Character == null || !affectedAgent.IsHero)
                return;
            CharacterObject character = (CharacterObject)affectedAgent.Character;
            if (!((BasicCharacterObject)character).IsHero)
                return;
            Hero heroObject = character.HeroObject;
            if (validParty.LeaderHero != null && validParty.LeaderHero.Clan != null && heroObject != validParty.LeaderHero.Clan.Leader)
            {
                heroObject.Clan = validParty.LeaderHero.Clan;
                heroObject.PartyBelongedTo.AddElementToMemberRoster(character.Culture.EliteBasicTroop, 1, false);
                heroObject.PartyBelongedTo.ChangePartyLeader(character.Culture.EliteBasicTroop?.HeroObject);
                heroObject.GovernorOf = (Town)null;
                heroObject.PartyBelongedTo.AddElementToMemberRoster(character, -1, false);
            }
        }

        public static void SpawnParty()
        {
            List<BattleInfectedRecord> records = NecroSummon.records;
            if (records.Count <= 0)
                return;
            foreach (KeyValuePair<Clan, List<BattleInfectedRecord>> keyValuePair in records.GroupBy<BattleInfectedRecord, Clan>((Func<BattleInfectedRecord, Clan>)(x => x.clan)).ToDictionary<IGrouping<Clan, BattleInfectedRecord>, Clan, List<BattleInfectedRecord>>((Func<IGrouping<Clan, BattleInfectedRecord>, Clan>)(x => x.Key), (Func<IGrouping<Clan, BattleInfectedRecord>, List<BattleInfectedRecord>>)(x => ((IEnumerable<BattleInfectedRecord>)x).ToList<BattleInfectedRecord>())))
            {
                TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
                TextObject textObject1 = new TextObject("{=dead_reanimated_clan_count}Clan : {clan_name}", (Dictionary<string, object>)null);
                textObject1.SetTextVariable("clan_name", ((object)keyValuePair.Key).ToString());
                InformationManager.DisplayMessage(new InformationMessage(((object)textObject1).ToString()));
                InformationManager.DisplayMessage(new InformationMessage(((object)new TextObject("{=dead_reanimated_count_title}Reanimated Troop : Reanimated Number", (Dictionary<string, object>)null)).ToString()));
                foreach (BattleInfectedRecord battleInfectedRecord in keyValuePair.Value)
                {
                    TextObject textObject2 = new TextObject("{=dead_reanimated_count}{infected_unit} : {infected_number}", (Dictionary<string, object>)null);
                    textObject2.SetTextVariable("infected_unit", battleInfectedRecord.infectedUnitId);
                    textObject2.SetTextVariable("infected_number", battleInfectedRecord.infectedUnitNumber);
                    InformationManager.DisplayMessage(new InformationMessage(((object)textObject2).ToString()));
                    CharacterObject characterObject = NecroSummon.GetCharacterObject(battleInfectedRecord.infectedUnitId);
                    if (characterObject != null)
                        dummyTroopRoster.AddToCounts(characterObject, battleInfectedRecord.infectedUnitNumber, false, 0, 0, true, -1);
                }
                if (dummyTroopRoster.TotalManCount > SubModule.Config.SpawnPartyMinUnit)
                    NecroSummon.CreateParty(dummyTroopRoster, keyValuePair.Key);
            }
        }

        public static void SpawnTroop(Agent affectorAgent, Agent affectedAgent, string infectedTroopId)
        {
            PartyBase validParty = NecroSummon.GetValidParty(affectorAgent);
            CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(infectedTroopId);
            if (NecroSummon.IsAgentOverLimit())
                return;
            Agent agent = Mission.Current.SpawnTroop((IAgentOriginBase)new PartyAgentOrigin(validParty, characterObject, -1, new UniqueTroopDescriptor(), false), NecroSummon.IsPlayerSide(validParty), true, ((BasicCharacterObject)characterObject).HasMount(), false, 1, 1, true, true, false, new Vec3?(), new Vec2?(), (string)null, (ItemObject)null, (FormationClass)10, false);
            agent.TeleportToPosition(affectedAgent.Position);
            ActionIndexCache actionIndexCache = ActionIndexCache.Create("act_stand_up_floor_1");
            agent.SetActionChannel(0, actionIndexCache, true, 0UL, 0.0f, 1f, -0.2f, 0.4f, 0.0f, false, -0.2f, 0, true);
        }

        internal static void BattleSimulationReanimation(MapEvent mapEvent)
        {
            if (!mapEvent.HasWinner)
                return;
            Random random = new Random();
            int infectionBasePercentage = SubModule.Config.InfectionBasePercentage;
            MapEventSide mapEventSide1;
            MapEventSide mapEventSide2;
            if (mapEvent.WinningSide == (BattleSideEnum)1)
            {
                mapEventSide1 = mapEvent.AttackerSide;
                mapEventSide2 = mapEvent.DefenderSide;
            }
            else
            {
                mapEventSide1 = mapEvent.DefenderSide;
                mapEventSide2 = mapEvent.AttackerSide;
            }
            List<PartyBase> parties = new List<PartyBase>();
            foreach (MapEventParty party in (List<MapEventParty>)mapEventSide1.Parties)
                parties.Add(party.Party);
            List<PartyBase> partyWithAffector = NecroSummon.GetPartyWithAffector(parties);
            if (partyWithAffector.Count <= 0)
                return;
            PartyBase strongestParty = NecroSummon.GetStrongestParty(partyWithAffector);
            List<Tuple<string, int>> tupleList = NecroSummon.CountAffectorInParty(strongestParty);
            int casualties = mapEventSide2.Casualties;
            int num = 0;
            foreach (Tuple<string, int> tuple in tupleList)
            {
                if (num > casualties)
                    break;
                for (int index = 0; index < tuple.Item2; ++index)
                {
                    if (num > casualties)
                        index = tuple.Item2;
                    if (random.Next(0, 100) <= infectionBasePercentage)
                    {
                        string unitInfectedUnitId = NecroSummon.GetUnitInfectedUnitId(tuple.Item1);
                        if (unitInfectedUnitId != null)
                        {
                            strongestParty.AddMember(NecroSummon.GetCharacterObject(unitInfectedUnitId), 1, 0);
                        }
                        else
                        {
                            string itemInfectedUnitId = NecroSummon.GetItemInfectedUnitId("");
                            if (itemInfectedUnitId != null)
                                strongestParty.AddMember(NecroSummon.GetCharacterObject(itemInfectedUnitId), 1, 0);
                        }
                        ++num;
                    }
                }
            }
        }

        internal static void RaiseCrimeRating()
        {
            Clan playerClan = Clan.PlayerClan;
            float infectedProportion = NecroSummon.GetInfectedProportion(MobileParty.MainParty.Party);
            int num = (double)infectedProportion > 80.0 ? 10 : ((double)infectedProportion > 60.0 ? 5 : ((double)infectedProportion > 40.0 ? 3 : ((double)infectedProportion > 20.0 ? 2 : ((double)infectedProportion > 0.0 ? 1 : 0))));
            playerClan.MainHeroCrimeRating += (float)num;
            TextObject textObject = new TextObject("{=raise_criminal_rating}Player Criminal Rating Raised {rating}. Current: {current_rating}", (Dictionary<string, object>)null);
            textObject.SetTextVariable("rating", num);
            textObject.SetTextVariable("current_rating", playerClan.MainHeroCrimeRating);
            InformationManager.DisplayMessage(new InformationMessage(((object)textObject).ToString(), new Color(10f, 0.0f, 0.0f, 1f)));
        }

        private static float GetInfectedProportion(PartyBase party)
        {
            int numberOfAllMembers = party.NumberOfAllMembers;
            int num = 0;
            foreach (Tuple<string, int> tuple in NecroSummon.CountAffectorInParty(party))
                num += tuple.Item2;
            return (float)(num / numberOfAllMembers * 100);
        }

        private static int CalculateProbability(Agent affectorAgent, Agent affectedAgent)
        {
            int num1 = SubModule.Config.InfectionBasePercentage;
            if (num1 > 100)
                num1 = 100;
            int num2 = 0;
            int num3 = 0;
            if (affectorAgent.Character != null && affectedAgent.Character != null)
            {
                num2 = NecroSummon.GetLevelProbabilityFactor(affectorAgent.Character.Level, affectedAgent.Character.Level);
                num3 = NecroSummon.GetTierProbabilityFactor(NecroSummon.GetUnitTier(affectorAgent), NecroSummon.GetUnitTier(affectedAgent));
            }
            int probability = num1 + num2 + num3;
            if (probability > 100)
                probability = 100;
            return probability;
        }

        private static int GetLevelProbabilityFactor(int affectorLevel, int affectedLevel)
        {
            if (affectorLevel > affectedLevel)
                return affectorLevel - affectedLevel;
            return affectorLevel == affectedLevel ? 0 : affectedLevel - affectorLevel;
        }

        private static int GetUnitTier(Agent agent)
        {
            CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(((MBObjectBase)agent.Character).StringId);
            return characterObject != null ? characterObject.Tier : 0;
        }

        private static int GetTierProbabilityFactor(int affectorLevel, int affectedLevel) => (affectorLevel > affectedLevel ? affectorLevel - affectedLevel : (affectorLevel != affectedLevel ? affectedLevel - affectorLevel : 0)) * 5;

        internal static bool IsPlayerSide(PartyBase party) => party.MapEventSide == MobileParty.MainParty.Party.MapEventSide;

        internal static CharacterObject GetCharacterObject(string unitId) => MBObjectManager.Instance.GetObject<CharacterObject>(unitId);

        private static Clan GetClan(MobileParty party) => party.ActualClan;

        private static string GetZombiePartyName(int number)
        {
            if (number > 100)
                return "{dead_party_name_large}Hordes of the Dead";
            return number > 50 ? "{dead_party_name_medium}Party of the Dead" : "{dead_party_name_small}Group of the Dead";
        }

        private static MobileParty CreateParty(TroopRoster troop, Clan clan)
        {
            TroopRoster prisonerRoster = TroopRoster.CreateDummyTroopRoster();
            TextObject partyName = new TextObject(NecroSummon.GetZombiePartyName(troop.Count), null);
            PartyComponent.OnPartyComponentCreatedDelegate delegateFunction = delegate (MobileParty _party)
            {
                _party.Party.SetCustomOwner(clan.Heroes.First<Hero>());
                _party.ChangePartyLeader(clan.Leader);
                _party.Party.SetVisualAsDirty();
                _party.SetCustomName(partyName);
                _party.ActualClan = clan;
                _party.Aggressiveness = 1f;
                ((CustomPartyComponent)_party.PartyComponent).CustomPartyBaseSpeed = 1f;
                ((CustomPartyComponent)_party.PartyComponent)._avoidHostileActions = false;
            };
            MobileParty mobileParty = MobileParty.CreateParty("dead_horde", new CustomPartyComponent(), delegateFunction);
            mobileParty.ItemRoster.AddToCounts(DefaultItems.Grain, troop.Count + 10);
            mobileParty.InitializeMobilePartyAroundPosition(troop, prisonerRoster, MobileParty.MainParty.MapEvent.Position, 0f, 0f);
            mobileParty.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
            mobileParty.SetPartyUsedByQuest(true);
            TextObject textObject = new TextObject("{=dead_party_spawned}{party_name} has been formed", null);
            textObject.SetTextVariable("party_name", partyName.ToString());
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(10f, 0f, 0f, 1f)));
            return mobileParty;
        }

        internal static void FoodSupply(MobileParty party)
        {
            if (!((MBObjectBase)party).StringId.Contains("dead_horde") || party.Party.ItemRoster.TotalFood != 0)
                return;
            party.Party.ItemRoster.AddToCounts(DefaultItems.Grain, party.Party.MemberRoster.TotalManCount);
        }

        private static List<PartyBase> GetPartyWithAffector(List<PartyBase> parties)
        {
            List<PartyBase> partyWithAffector = new List<PartyBase>();
            foreach (PartyBase party in parties)
            {
                if (NecroSummon.IsPartyWithAffector(party))
                    partyWithAffector.Add(party);
            }
            return partyWithAffector;
        }

        private static bool IsPartyWithAffector(PartyBase parties)
        {
            foreach (TroopRosterElement troopRosterElement in (List<TroopRosterElement>)parties.MemberRoster.GetTroopRoster())
            {
                if (SubModule.Config.EnableTroopInfect && NecroSummon.IsValidInfecterTroop((BasicCharacterObject)troopRosterElement.Character) || SubModule.Config.EnableItemInfect && NecroSummon.HasValidInfectItem(troopRosterElement.Character) > -1)
                    return true;
            }
            return false;
        }

        private static PartyBase GetStrongestParty(List<PartyBase> parties)
        {
            PartyBase strongestParty = parties[0];
            foreach (PartyBase party in parties)
            {
                if ((double)strongestParty.TotalStrength < (double)party.TotalStrength)
                    strongestParty = party;
            }
            return strongestParty;
        }

        private static List<Tuple<string, int>> CountAffectorInParty(PartyBase party)
        {
            List<Tuple<string, int>> list = new List<Tuple<string, int>>();
            foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
            {
                bool flag = false;
                bool enableTroopInfect = SubModule.Config.EnableTroopInfect;
                if (enableTroopInfect)
                {
                    bool flag2 = NecroSummon.IsValidInfecterTroop(troopRosterElement.Character);
                    if (flag2)
                    {
                        list.Add(new Tuple<string, int>(troopRosterElement.Character.StringId, troopRosterElement.Number));
                        flag = true;
                    }
                }
                bool flag3 = flag;
                if (!flag3)
                {
                    bool enableItemInfect = SubModule.Config.EnableItemInfect;
                    if (enableItemInfect)
                    {
                        int num = NecroSummon.HasValidInfectItem(troopRosterElement.Character);
                        bool flag4 = num > -1;
                        if (flag4)
                        {
                            list.Add(new Tuple<string, int>(troopRosterElement.Character.FirstBattleEquipment[num].Item.StringId, troopRosterElement.Number));
                        }
                    }
                }
            }
            return list;
        }

        private static bool HasRecord(string infectedTroopId)
        {
            foreach (BattleInfectedRecord record in NecroSummon.records)
            {
                if (record.infectedUnitId.Equals(infectedTroopId))
                    return true;
            }
            return false;
        }

        private static void AddRecord(Clan clan, string infectedTroopId)
        {
            BattleInfectedRecord battleInfectedRecord = new BattleInfectedRecord(clan, infectedTroopId, 1);
            NecroSummon.records.Add(battleInfectedRecord);
        }

        private static void AddRecordUnitNumber(Clan clan, string infectedTroopId)
        {
            foreach (BattleInfectedRecord record in NecroSummon.records)
            {
                if (record.clan == clan && record.infectedUnitId.Equals(infectedTroopId))
                    ++record.infectedUnitNumber;
            }
        }

        private static void RecordInfectedUnit(MobileParty affectorParty, string infectedTroopId)
        {
            Clan clan = NecroSummon.GetClan(affectorParty);
            if (NecroSummon.HasRecord(infectedTroopId))
                NecroSummon.AddRecordUnitNumber(clan, infectedTroopId);
            else
                NecroSummon.AddRecord(clan, infectedTroopId);
        }

        internal static void ResetCount()
        {
            if (NecroSummon.previousMission == null)
            {
                NecroSummon.records = new List<BattleInfectedRecord>();
                NecroSummon.corpseRecords = new List<CorpseLocationRecord>();
                NecroSummon.previousMission = Mission.Current;
            }
            else
            {
                if (NecroSummon.previousMission == Mission.Current)
                    return;
                NecroSummon.records = new List<BattleInfectedRecord>();
                NecroSummon.corpseRecords = new List<CorpseLocationRecord>();
                NecroSummon.previousMission = Mission.Current;
            }
        }


        internal static bool IsWithinSummonRange(
          float circle_x,
          float circle_y,
          float rad,
          float x,
          float y)
        {
            return ((double)x - (double)circle_x) * ((double)x - (double)circle_x) + ((double)y - (double)circle_y) * ((double)y - (double)circle_y) <= (double)rad * (double)rad;
        }

        public static void Summoning(Agent attacker, Vec3 missilePosition)
        {
            ItemObject wieldedItem = NecroSummon.GetWieldedItem(attacker);
            if (wieldedItem != null)
            {
                if (NecroSummon.IsSummonUnitItem(wieldedItem.StringId))
                {
                    int summonUnitAmount = NecroSummon.GetSummonUnitAmount(wieldedItem.StringId);
                    for (int i = 0; i < summonUnitAmount; i++)
                    {
                        if (NecroSummon.IsAgentOverLimit())
                        {
                            break;
                        }
                        Agent agent = NecroSummon.SummoningUnit(attacker, wieldedItem.StringId);
                        if (agent != null)
                        {
                            agent.TeleportToPosition(missilePosition);
                        }
                    }
                }
            }
        }

        public static Agent SummoningUnit(Agent attacker, string itemId)
        {
            string summonUnitId = NecroSummon.GetSummonUnitId(itemId);
            if (summonUnitId != null)
            {
                CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>(summonUnitId);
                if (characterObject != null)
                {
                    PartyBase validParty = NecroSummon.GetValidParty(attacker);
                    if (validParty != null)
                        return Mission.Current.SpawnTroop((IAgentOriginBase)new PartyAgentOrigin(validParty, characterObject, -1, new UniqueTroopDescriptor(), false), NecroSummon.IsPlayerSide(validParty), true, ((BasicCharacterObject)characterObject).HasMount(), false, 1, 1, true, true, false, new Vec3?(), new Vec2?(), (string)null, (ItemObject)null, (FormationClass)10, false);
                }
                else
                    InformationManager.DisplayMessage(new InformationMessage(((object)new TextObject("{=summon_invalid_unit}Unit is invalid for summon", (Dictionary<string, object>)null)).ToString()));
            }
            return (Agent)null;
        }

        internal static List<SummonKillRecord> GetTotalSummonKill()
        {
            List<SummonKillRecord> summonKillRecord = new List<SummonKillRecord>();
            foreach (Agent agent in ((IEnumerable<Agent>)Mission.Current.Agents).ToList<Agent>())
            {
                if (agent.Character != null && NecroSummon.IsSummonUnit(agent))
                {
                    if (NecroSummon.HasKillRecord(summonKillRecord, agent))
                        NecroSummon.AddCountKill(summonKillRecord, agent);
                    else
                        NecroSummon.AddRecord(summonKillRecord, agent);
                }
            }
            return summonKillRecord;
        }

        private static bool HasKillRecord(List<SummonKillRecord> summonKillRecord, Agent agent)
        {
            PartyBase party = NecroSummon.GetValidParty(agent);
            string unitId = ((MBObjectBase)agent.Character).StringId;
            return summonKillRecord.Any<SummonKillRecord>((Func<SummonKillRecord, bool>)(x => ((object)x.party).Equals((object)party) && x.unitId.Equals(unitId)));
        }

        private static List<SummonKillRecord> AddRecord(
          List<SummonKillRecord> summonKillRecord,
          Agent agent)
        {
            PartyBase validParty = NecroSummon.GetValidParty(agent);
            string stringId = ((MBObjectBase)agent.Character).StringId;
            summonKillRecord.Add(new SummonKillRecord(validParty, stringId, agent.KillCount));
            return summonKillRecord;
        }

        private static List<SummonKillRecord> AddCountKill(
          List<SummonKillRecord> summonKillRecord,
          Agent agent)
        {
            PartyBase party = NecroSummon.GetValidParty(agent);
            string unitId = ((MBObjectBase)agent.Character).StringId;
            int index = summonKillRecord.FindIndex((Predicate<SummonKillRecord>)(x => ((object)x.party).Equals((object)party) && x.unitId.Equals(unitId)));
            if (index != -1)
                ++summonKillRecord[index].killCount;
            return summonKillRecord;
        }

        internal static void DistrubuteExperience(List<SummonKillRecord> summonKillRecord)
        {
            Dictionary<PartyBase, List<SummonKillRecord>> dictionary = summonKillRecord.GroupBy<SummonKillRecord, PartyBase>((Func<SummonKillRecord, PartyBase>)(x => x.party)).ToDictionary<IGrouping<PartyBase, SummonKillRecord>, PartyBase, List<SummonKillRecord>>((Func<IGrouping<PartyBase, SummonKillRecord>, PartyBase>)(x => x.Key), (Func<IGrouping<PartyBase, SummonKillRecord>, List<SummonKillRecord>>)(x => ((IEnumerable<SummonKillRecord>)x).ToList<SummonKillRecord>()));
            int summonEachKillXp = SubModule.Config.SummonEachKillXp;
            foreach (KeyValuePair<PartyBase, List<SummonKillRecord>> keyValuePair in dictionary)
            {
                List<TroopRosterElement> summonItemInParty = NecroSummon.GetUnitWithSummonItemInParty(keyValuePair.Key);
                if (summonItemInParty.Count > 0)
                {
                    int num1 = 0;
                    foreach (SummonKillRecord summonKillRecord1 in keyValuePair.Value)
                        num1 += summonKillRecord1.killCount;
                    int num2 = num1 * summonEachKillXp / summonItemInParty.Count;
                    foreach (TroopRosterElement troopRosterElement in summonItemInParty)
                        keyValuePair.Key.MemberRoster.AddXpToTroop(num2, troopRosterElement.Character);
                    TextObject textObject = new TextObject("{=summon_kill_share_xp}Summon has share total {xp} xp among summoner in {party_name}", (Dictionary<string, object>)null);
                    textObject.SetTextVariable("xp", num1 * summonEachKillXp);
                    textObject.SetTextVariable("party_name", keyValuePair.Key.Name);
                    InformationManager.DisplayMessage(new InformationMessage(((object)textObject).ToString()));
                }
            }
        }

        private static List<TroopRosterElement> GetUnitWithSummonItemInParty(PartyBase party)
        {
            List<TroopRosterElement> list = new List<TroopRosterElement>();
            foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
            {
                for (int i = 0; i < 4; i++)
                {
                    EquipmentElement equipmentElement = troopRosterElement.Character.Equipment[i];
                    bool flag = equipmentElement.Item != null;
                    if (flag)
                    {
                        bool flag2 = NecroSummon.IsSummonUnitItem(equipmentElement.Item.StringId);
                        if (flag2)
                        {
                            list.Add(troopRosterElement);
                            i = 4;
                        }
                    }
                }
            }
            return list;
        }

        internal static bool IsValidInfecterTroop(BasicCharacterObject unit)
        {
            foreach (UnitInfectUnit unitInfectUnit in SubModule.UnitUnitConfig.UnitInfectUnit)
            {
                if (((MBObjectBase)unit).StringId.Equals(unitInfectUnit.InfectorUnitId))
                    return true;
            }
            return false;
        }

        internal static bool IsValidInfectItem(Agent affectorAgent)
        {
            List<ItemInfectUnit> itemInfectUnit1 = SubModule.ItemUnitConfig.ItemInfectUnit;
            ItemObject wieldedItem = NecroSummon.GetWieldedItem(affectorAgent);
            foreach (ItemInfectUnit itemInfectUnit2 in itemInfectUnit1)
            {
                if (wieldedItem != null && ((MBObjectBase)wieldedItem).StringId.Equals(itemInfectUnit2.ItemId))
                    return true;
            }
            return false;
        }


        internal static bool IsThrowingWeapon(ItemObject item) => item.ItemType == (ItemObject.ItemTypeEnum)10;

        internal static int HasValidInfectItem(CharacterObject unit)
        {
            List<ItemInfectUnit> itemInfectUnit = SubModule.ItemUnitConfig.ItemInfectUnit;
            Equipment equipment = unit.BattleEquipments.First<Equipment>();
            for (int i = 0; i < 12; i++)
            {
                foreach (ItemInfectUnit itemInfectUnit2 in itemInfectUnit)
                {
                    bool flag = equipment[i].Item != null;
                    if (flag)
                    {
                        bool flag2 = equipment[i].Item.StringId.Equals(itemInfectUnit2.ItemId);
                        if (flag2)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        internal static bool IsReanimatedTroop(BasicCharacterObject unit)
        {
            foreach (string str in NecroSummon.GetAllReanimatedUnit())
            {
                if (((MBObjectBase)unit).StringId.Equals(str))
                    return true;
            }
            return false;
        }

        internal static bool IsImmuneUnit(Agent affectedAgent)
        {
            foreach (string str in SubModule.Config.ImmuneInfectTroop)
            {
                if (affectedAgent.Character != null && str.Length > 0 && ((MBObjectBase)affectedAgent.Character).StringId.Contains(str))
                    return true;
            }
            foreach (string str in NecroSummon.GetAllReanimatedUnit())
            {
                if (affectedAgent.Character != null && str.Length > 0 && ((MBObjectBase)affectedAgent.Character).StringId.Equals(str))
                    return true;
            }
            return false;
        }

        private static List<string> GetAllReanimatedUnit()
        {
            List<string> allReanimatedUnit = new List<string>();
            if (SubModule.Config.EnableTroopInfect)
            {
                foreach (UnitInfectUnit unitInfectUnit in SubModule.UnitUnitConfig.UnitInfectUnit)
                {
                    foreach (string str in unitInfectUnit.UnitId)
                    {
                        if (!allReanimatedUnit.Contains(str))
                            allReanimatedUnit.Add(str);
                    }
                }
            }
            if (SubModule.Config.EnableItemInfect)
            {
                foreach (ItemInfectUnit itemInfectUnit in SubModule.ItemUnitConfig.ItemInfectUnit)
                {
                    foreach (string str in itemInfectUnit.UnitId)
                    {
                        if (!allReanimatedUnit.Contains(str))
                            allReanimatedUnit.Add(str);
                    }
                }
            }
            if (SubModule.Config.EnablePlayerSummon || SubModule.Config.EnableTroopSummon)
            {
                foreach (ItemSummonUnit itemSummonUnit in SubModule.ItemUnitConfig.ItemSummonUnit)
                {
                    foreach (string str in itemSummonUnit.UnitId)
                    {
                        if (!allReanimatedUnit.Contains(str))
                            allReanimatedUnit.Add(str);
                    }
                }
            }
            if (SubModule.Config.EnableBuildTroopFromPart)
            {
                foreach (UnitBuildFromPart unitBuildFromPart in SubModule.UnitBuildFromPartConfig.UnitBuildFromPart)
                {
                    foreach (string str in unitBuildFromPart.UnitId)
                    {
                        if (!allReanimatedUnit.Contains(str))
                            allReanimatedUnit.Add(str);
                    }
                }
            }
            return allReanimatedUnit;
        }

        internal static bool IsAgentOverLimit() => ((List<Agent>)Mission.Current.Agents).Count > 1500;

        internal static PartyBase GetValidParty(Agent affectorAgent)
        {
            PartyBase validParty = (PartyBase)null;
            try
            {
                validParty = ((PartyAgentOrigin)affectorAgent.Origin).Party;
            }
            catch
            {
            }
            try
            {
                validParty = ((PartyGroupAgentOrigin)affectorAgent.Origin).Party;
            }
            catch
            {
            }
            return validParty;
        }

        internal static string GetUnitInfectedUnitId(string unitId)
        {
            string unitInfectedUnitId = (string)null;
            bool flag = false;
            int num = 0;
            UnitInfectUnit unitInfectUnit = SubModule.UnitUnitConfig.UnitInfectUnit.FirstOrDefault<UnitInfectUnit>((Func<UnitInfectUnit, bool>)(x => x.InfectorUnitId.Equals(unitId)));
            if (unitInfectUnit != null)
            {
                while (!flag)
                {
                    int index = NecroSummon.random.Next(0, unitInfectUnit.UnitId.Length);
                    if (NecroSummon.GetCharacterObject(unitInfectUnit.UnitId[index]) != null)
                    {
                        flag = true;
                        unitInfectedUnitId = unitInfectUnit.UnitId[index];
                    }
                    ++num;
                    if (!flag && num > unitInfectUnit.UnitId.Length + 10)
                        return (string)null;
                }
            }
            return unitInfectedUnitId;
        }

        internal static string GetItemInfectedUnitId(string itemId)
        {
            string itemInfectedUnitId = (string)null;
            bool flag = false;
            int num = 0;
            ItemInfectUnit itemInfectUnit = SubModule.ItemUnitConfig.ItemInfectUnit.FirstOrDefault<ItemInfectUnit>((Func<ItemInfectUnit, bool>)(x => x.ItemId.Equals(itemId)));
            if (itemInfectUnit != null)
            {
                while (!flag)
                {
                    int index = NecroSummon.random.Next(0, itemInfectUnit.UnitId.Length);
                    if (NecroSummon.GetCharacterObject(itemInfectUnit.UnitId[index]) != null)
                    {
                        flag = true;
                        itemInfectedUnitId = itemInfectUnit.UnitId[index];
                    }
                    ++num;
                    if (!flag && num > itemInfectUnit.UnitId.Length + 10)
                        return (string)null;
                }
            }
            return itemInfectedUnitId;
        }


        internal static ItemObject GetWieldedItem(Agent attacker)
        {
            bool flag = attacker != null;
            if (flag)
            {
                ItemObject item = attacker.WieldedWeapon.Item;
                bool flag2 = item != null;
                if (flag2)
                {
                    return item;
                }
            }
            return null;
        }

        private static bool IsSummonUnit(Agent agent)
        {
            foreach (ItemSummonUnit itemSummonUnit in SubModule.ItemUnitConfig.ItemSummonUnit)
            {
                foreach (string str in itemSummonUnit.UnitId)
                {
                    if (((MBObjectBase)agent.Character).StringId.Equals(str))
                        return true;
                }
            }
            return false;
        }

        private static bool IsSummonUnitItem(string itemId)
        {
            foreach (ItemSummonUnit itemSummonUnit in SubModule.ItemUnitConfig.ItemSummonUnit)
            {
                if (itemSummonUnit.ItemId.Equals(itemId))
                {
                    ItemObject itemObject = Items.All.Where((x => x.StringId.Equals(itemId))).FirstOrDefault();
                    if (itemObject != null && NecroSummon.IsThrowingWeapon(itemObject))
                        return true;
                }
            }
            return false;
        }

        private static string GetSummonUnitId(string itemId)
        {
            ItemSummonUnit itemSummonUnit = SubModule.ItemUnitConfig.ItemSummonUnit.FirstOrDefault<ItemSummonUnit>((Func<ItemSummonUnit, bool>)(x => x.ItemId.Equals(itemId)));
            bool flag = false;
            int num = 0;
            if (itemSummonUnit != null)
            {
                while (!flag)
                {
                    int index = NecroSummon.random.Next(0, itemSummonUnit.UnitId.Length);
                    if (NecroSummon.GetCharacterObject(itemSummonUnit.UnitId[index]) != null)
                        return itemSummonUnit.UnitId[index];
                    ++num;
                    if (num > itemSummonUnit.UnitId.Length + 5)
                        return (string)null;
                }
            }
            return (string)null;
        }

        private static int GetSummonUnitAmount(string itemId)
        {
            ItemSummonUnit itemSummonUnit = SubModule.ItemUnitConfig.ItemSummonUnit.FirstOrDefault<ItemSummonUnit>((Func<ItemSummonUnit, bool>)(x => x.ItemId.Equals(itemId)));
            return itemSummonUnit != null ? itemSummonUnit.SummonAmount : 0;
        }
    }
}

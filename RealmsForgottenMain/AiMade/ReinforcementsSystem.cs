using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using SandBox;
using System.Collections;
using System.Reflection;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace RealmsForgotten.AiMade
{
    public class ADODReinforcementsSystem
    {
        public static float PowerBalanceArmy(MobileParty party, BattleSideEnum battleSide)
        {
            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
            float totalStrength = party.Army.EstimatedStrength;
            float num = MapEvent.PlayerMapEvent.StrengthOfSide[1];
            float num2 = MapEvent.PlayerMapEvent.StrengthOfSide[0];
            if (battleSide == BattleSideEnum.Attacker)
            {
                num += totalStrength;
            }
            else
            {
                num2 += totalStrength;
            }
            return num / (num + num2);
        }

        public static float PowerBalanceFirst()
        {
            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
            float num = MapEvent.PlayerMapEvent.StrengthOfSide[1];
            float num2 = MapEvent.PlayerMapEvent.StrengthOfSide[0];
            return num / (num + num2);
        }

        public static float PowerBalance(MobileParty party, BattleSideEnum battleSide)
        {
            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
            float totalStrength = party.Party.EstimatedStrength;
            float num = MapEvent.PlayerMapEvent.StrengthOfSide[1];
            float num2 = MapEvent.PlayerMapEvent.StrengthOfSide[0];
            if (battleSide == BattleSideEnum.Attacker)
            {
                num += totalStrength;
            }
            else
            {
                num2 += totalStrength;
            }
            return num / (num + num2);
        }

        public static bool FilterBandit(float balance)
        {
            int num = 10;
            if (balance > 0.55 && balance <= 0.7)
            {
                num += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -10 : 10;
            }
            else if (balance > 0.7)
            {
                num += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -20 : 20;
            }
            else if (balance < 0.45 && balance >= 0.3)
            {
                num += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 10 : -10;
            }
            else if (balance < 0.2)
            {
                num += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 20 : -20;
            }
            return num >= 0;
        }

        public static bool Filter0(MobileParty party, float balance)
        {
            return !(balance > 0.85 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker) && !(balance < 0.15 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender);
        }

        public static bool Filter1_1(MobileParty party, float firstBalance, float balance, AdodEnum adodEnum)
        {
            Random random = new Random();
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = random.Next(-5, 5);
            int relation = 0;
            int mercy = 0;
            int calculating = 0;
            int valor = 0;

            if (party.LeaderHero != null)
            {
                relation = (int)party.LeaderHero.GetRelationWithPlayer();
                mercy = party.LeaderHero.GetTraitLevel(DefaultTraits.Mercy);
                calculating = party.LeaderHero.GetTraitLevel(DefaultTraits.Calculating);
                valor = party.LeaderHero.GetTraitLevel(DefaultTraits.Valor);
            }

            if (party.Army != null)
            {
                num4 -= 5;
            }

            int troopCount = PlayerEncounter.Battle.AttackerSide.TroopCount + PlayerEncounter.Battle.DefenderSide.TroopCount;
            if (troopCount > 1500 || troopCount < 30)
            {
                num4 -= 5;
            }
            else if (troopCount > 2000 || troopCount < 10)
            {
                num4 -= 10;
            }

            if (relation >= 0)
            {
                if (relation >= 50) relation = 20;
                else if (relation >= 30) relation = 15;
                else if (relation >= 10) relation = 10;
            }
            else
            {
                if (relation <= -50) relation = -20;
                else if (relation <= -30) relation = -15;
                else if (relation <= -10) relation = -10;
            }

            if (adodEnum == AdodEnum.Father || adodEnum == AdodEnum.Mother)
            {
                num += 20;
            }
            else if (adodEnum == AdodEnum.Siblings || adodEnum == AdodEnum.SonDaughter || adodEnum == AdodEnum.HusbandWife)
            {
                num += 15;
            }
            else if (adodEnum == AdodEnum.Wanderer || adodEnum == AdodEnum.EtcClanMember || adodEnum == AdodEnum.Vassal || adodEnum == AdodEnum.King)
            {
                num += 10;
            }
            else if (adodEnum == AdodEnum.Colleague)
            {
                num += 5;
            }

            num += relation;

            if (balance > 0.55 && balance <= 0.7)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 2 : -5;
            }
            else if (balance > 0.7)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 5 : -20;
            }
            else if (balance < 0.45 && balance >= 0.3)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -5 : 2;
            }
            else if (balance < 0.3)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -20 : 5;
            }

            if (firstBalance < 0.45 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker && mercy > 0)
            {
                num3 += 15;
            }
            else if (firstBalance > 0.55 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && mercy > 0)
            {
                num3 += 15;
            }

            if (balance > 0.7 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker)
            {
                if (calculating > 0) num3 += 15;
            }
            else if (balance < 0.45 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker)
            {
                if (calculating > 0) num3 -= 15;
                if (valor > 0) num3 += 15;
                if (valor < 0) num3 -= 15;
            }
            else if (balance < 0.3 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender)
            {
                if (calculating > 0) num3 += 15;
            }
            else if (balance > 0.55 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender)
            {
                if (calculating > 0) num3 -= 15;
                if (valor > 0) num3 += 15;
                if (valor < 0) num3 -= 15;
            }

            int num10 = num + num2 + num3 + num5 + num4 + bonusPoint;
            return num10 >= 15;
        }

        public static bool Filter1_2(MobileParty party, float firstBalance, float balance, AdodEnum adodEnum)
        {
            Random random = new Random();
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = random.Next(-5, 5);
            int relation = 0;
            int mercy = 0;
            int calculating = 0;
            int valor = 0;

            if (party.LeaderHero != null)
            {
                relation = (int)party.LeaderHero.GetRelationWithPlayer();
                mercy = party.LeaderHero.GetTraitLevel(DefaultTraits.Mercy);
                calculating = party.LeaderHero.GetTraitLevel(DefaultTraits.Calculating);
                valor = party.LeaderHero.GetTraitLevel(DefaultTraits.Valor);
            }

            // Fallback for incomplete encounter context (e.g. settlement
            // party). The old code dereferenced party.LeaderHero and
            // EncounteredParty.LeaderHero inside the branch that had just
            // detected they might be null.
            if ((PlayerEncounter.EncounteredMobileParty == null || PlayerEncounter.EncounteredMobileParty.Owner == null || PlayerEncounter.EncounteredMobileParty.LeaderHero == null || party.Owner == null)
                && party.LeaderHero != null && PlayerEncounter.EncounteredParty?.LeaderHero != null)
            {
                relation = party.LeaderHero.GetBaseHeroRelation(PlayerEncounter.EncounteredParty.LeaderHero);
            }

            if (party.Army != null)
            {
                num4 -= 5;
            }

            int troopCount = PlayerEncounter.Battle.AttackerSide.TroopCount + PlayerEncounter.Battle.DefenderSide.TroopCount;
            if (troopCount > 1500 || troopCount < 30)
            {
                num4 -= 5;
            }
            else if (troopCount > 2000 || troopCount < 10)
            {
                num4 -= 10;
            }

            if (relation >= 0)
            {
                if (relation >= 50) relation = 20;
                else if (relation >= 30) relation = 15;
                else if (relation >= 10) relation = 10;
            }
            else
            {
                if (relation <= -50) relation = -20;
                else if (relation <= -30) relation = -15;
                else if (relation <= -10) relation = -10;
            }

            if (PlayerEncounter.EncounteredParty?.MobileParty != null && party.ActualClan == PlayerEncounter.EncounteredParty.MobileParty.ActualClan)
            {
                num += 15;
            }
            else if (PlayerEncounter.EncounteredParty != null && party.MapFaction == PlayerEncounter.EncounteredParty.MapFaction)
            {
                num += 10;
            }

            num += relation;

            if (balance > 0.55 && balance <= 0.7)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -5 : 10;
            }
            else if (balance > 0.7)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -20 : 20;
            }
            else if (balance < 0.45 && balance >= 0.3)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 10 : -5;
            }
            else if (balance < 0.2)
            {
                num2 += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 20 : -20;
            }

            if (firstBalance < 0.45 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && mercy > 0)
            {
                num3 += 15;
            }
            else if (firstBalance > 0.55 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker && mercy > 0)
            {
                num3 += 15;
            }

            if (balance > 0.7 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender)
            {
                if (calculating > 0) num3 += 15;
            }
            else if (balance < 0.45 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender)
            {
                if (calculating > 0) num3 -= 15;
                if (valor > 0) num3 += 15;
                if (valor < 0) num3 -= 15;
            }
            else if (balance > 0.55 && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker)
            {
                if (calculating > 0) num3 -= 15;
                if (valor > 0) num3 += 15;
                if (valor < 0) num3 -= 15;
            }

            int num10 = num + num2 + num3 + num5 + num4 + bonusPoint;
            return num10 >= 15;
        }

        public static bool Filter2_1(MobileParty party, List<MapEventParty> alreadyParties)
        {
            foreach (MapEventParty mapEventParty in alreadyParties)
            {
                if (mapEventParty.Party.Owner != null && mapEventParty.Party.LeaderHero != null && party.Owner != null && party.LeaderHero != null)
                {
                    if (mapEventParty.Party.Owner.Clan.Kingdom != null && party.Owner.Clan.Kingdom != null && mapEventParty.Party.Owner.Clan.Kingdom.IsAtWarWith(party.Owner.Clan.Kingdom) || mapEventParty.Party.Owner.Clan.MapFaction != null && party.Owner.Clan.MapFaction != null && mapEventParty.Party.Owner.Clan.MapFaction.IsAtWarWith(party.Owner.Clan.MapFaction))
                    {
                    }
                }
            }
            return true;
        }

        public static bool Filter2_2(MobileParty party, List<MapEventParty> alreadyParties)
        {
            if (PlayerEncounter.EncounteredParty?.MobileParty != null && PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
            {
                return true;
            }
            foreach (MapEventParty mapEventParty in alreadyParties)
            {
                if (mapEventParty.Party.Owner != null && mapEventParty.Party.LeaderHero != null && party.Owner != null && party.LeaderHero != null)
                {
                    if (mapEventParty.Party.Owner.Clan.Kingdom != null && party.Owner.Clan.Kingdom != null && mapEventParty.Party.Owner.Clan.Kingdom.IsAtWarWith(party.Owner.Clan.Kingdom) || mapEventParty.Party.Owner.Clan.MapFaction != null && party.Owner.Clan.MapFaction != null && mapEventParty.Party.Owner.Clan.MapFaction.IsAtWarWith(party.Owner.Clan.MapFaction))
                    {
                    }
                }
            }
            return true;
        }

        public static bool Filter3_1(MobileParty party, List<MapEventParty> alreadyParties)
        {
            if (PlayerEncounter.EncounteredParty?.MobileParty != null && PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
            {
                return true;
            }
            foreach (MapEventParty mapEventParty in alreadyParties)
            {
                if (mapEventParty.Party.Owner != null && mapEventParty.Party.LeaderHero != null && party.Owner != null && party.LeaderHero != null)
                {
                    if (mapEventParty.Party.Owner.Clan.Kingdom != null && party.Owner.Clan.Kingdom != null && !mapEventParty.Party.Owner.Clan.Kingdom.IsAtWarWith(party.Owner.Clan.Kingdom) || mapEventParty.Party.Owner.Clan.MapFaction != null && party.Owner.Clan.MapFaction != null && !mapEventParty.Party.Owner.Clan.MapFaction.IsAtWarWith(party.Owner.Clan.MapFaction))
                    {
                    }
                }
            }
            return true;
        }

        public static bool Filter3_2(MobileParty party, List<MapEventParty> alreadyParties)
        {
            if (PlayerEncounter.EncounteredParty?.MobileParty != null && PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
            {
                return true;
            }
            foreach (MapEventParty mapEventParty in alreadyParties)
            {
                if (mapEventParty.Party.Owner != null && mapEventParty.Party.LeaderHero != null && party.Owner != null && party.LeaderHero != null)
                {
                    if (mapEventParty.Party.Owner.Clan.Kingdom != null && party.Owner.Clan.Kingdom != null && !mapEventParty.Party.Owner.Clan.Kingdom.IsAtWarWith(party.Owner.Clan.Kingdom) || mapEventParty.Party.Owner.Clan.MapFaction != null && party.Owner.Clan.MapFaction != null && !mapEventParty.Party.Owner.Clan.MapFaction.IsAtWarWith(party.Owner.Clan.MapFaction))
                    {
                    }
                }
            }
            return true;
        }

        private static short bonusPoint = 0;

        public enum AdodEnum
        {
            None,
            Father,
            Mother,
            Siblings,
            SonDaughter,
            HusbandWife,
            ExHusbandWife,
            Wanderer,
            EtcClanMember,
            Friend,
            Enemy,
            King,
            Vassal,
            Colleague,
            Bandit,
            Unknown
        }

        public class ADODReinforcementsRunner : TaleWorlds.MountAndBlade.MissionLogic
        {
            public void RelationFilter(MobileParty party)
            {
                if (party.LeaderHero != null)
                {
                    if (relationPair != null && !relationPair.ContainsKey(party.LeaderHero))
                    {
                        Hero leaderHero = party.LeaderHero;
                        Hero mainHero = Hero.MainHero;
                        if (mainHero.Father != null && mainHero.Father == leaderHero)
                        {
                            relationPair.Add(leaderHero, AdodEnum.Father);
                        }
                        else if (mainHero.Mother != null && mainHero.Mother == leaderHero)
                        {
                            relationPair.Add(leaderHero, AdodEnum.Mother);
                        }
                        else if (mainHero.Siblings != null && mainHero.Siblings.Contains(leaderHero))
                        {
                            relationPair.Add(leaderHero, AdodEnum.Siblings);
                        }
                        else if (leaderHero.Father != null && leaderHero.Father == mainHero || leaderHero.Mother != null && leaderHero.Mother == mainHero)
                        {
                            relationPair.Add(leaderHero, AdodEnum.SonDaughter);
                        }
                        else if (mainHero.Spouse != null && mainHero.Spouse == leaderHero)
                        {
                            relationPair.Add(leaderHero, AdodEnum.HusbandWife);
                        }
                        else if (mainHero.ExSpouses != null && mainHero.ExSpouses.Contains(leaderHero))
                        {
                            relationPair.Add(leaderHero, AdodEnum.ExHusbandWife);
                        }
                        else if (leaderHero.Clan != null && mainHero.Clan != null && leaderHero.Clan == mainHero.Clan)
                        {
                            if (leaderHero.IsWanderer)
                            {
                                relationPair.Add(leaderHero, AdodEnum.Wanderer);
                            }
                            else
                            {
                                relationPair.Add(leaderHero, AdodEnum.EtcClanMember);
                            }
                        }
                        else if (leaderHero.MapFaction != null && mainHero.MapFaction != null && leaderHero.MapFaction == mainHero.MapFaction)
                        {
                            if (leaderHero.IsFactionLeader)
                            {
                                relationPair.Add(leaderHero, AdodEnum.King);
                            }
                            else if (mainHero.IsFactionLeader)
                            {
                                relationPair.Add(leaderHero, AdodEnum.Vassal);
                            }
                            else if (leaderHero.GetRelation(mainHero) >= 10)
                            {
                                relationPair.Add(leaderHero, AdodEnum.Friend);
                            }
                            else if (leaderHero.GetRelation(mainHero) <= -10)
                            {
                                relationPair.Add(leaderHero, AdodEnum.Enemy);
                            }
                            else
                            {
                                relationPair.Add(leaderHero, AdodEnum.Colleague);
                            }
                        }
                        else if (leaderHero.GetRelation(mainHero) >= 10)
                        {
                            relationPair.Add(leaderHero, AdodEnum.Friend);
                        }
                        else if (leaderHero.GetRelation(mainHero) <= -10)
                        {
                            relationPair.Add(leaderHero, AdodEnum.Enemy);
                        }
                        else
                        {
                            relationPair.Add(leaderHero, AdodEnum.Unknown);
                        }
                    }
                }
            }

            public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
            {
                base.OnMissionModeChange(oldMissionMode, atStart);
                if (oldMissionMode != MissionMode.Deployment)
                {
                    return;
                }

                // Nothing below is guaranteed to exist at end-of-deployment in
                // every battle shape (army events, joined encounters, quest
                // fights) — bail out instead of dereferencing.
                Mission mission = Mission.Current;
                MapEvent playerEvent = MapEvent.PlayerMapEvent;
                if (mission?.PlayerTeam == null || playerEvent == null
                    || PlayerEncounter.Current == null || Campaign.Current?.MainParty == null)
                {
                    return;
                }

                MobileParty encounteredMobile = PlayerEncounter.EncounteredParty?.MobileParty;

                // The clan whose wars decide friend-from-foe: the player's own
                // when commanding, the team leader's when serving as sergeant.
                // Team.Leader may not be spawned yet — fall back to the player.
                Clan referenceClan = mainHeros.Clan;
                if (mission.PlayerTeam.IsPlayerSergeant)
                {
                    Hero teamLeader = (mission.PlayerTeam.Leader?.Character as CharacterObject)?.HeroObject;
                    referenceClan = teamLeader?.Clan ?? referenceClan;
                }
                if (referenceClan == null)
                {
                    return;
                }

                foreach (MobileParty mobileParty in MobileParty.AllLordParties.FindAll(a => Campaign.Current.MainParty.GetPosition2D.Distance(a.GetPosition2D) <= radiousSetting))
                {
                    if (playerEvent.InvolvedParties.Contains(mobileParty.Party)
                        || mobileParty.IsMainParty
                        || mobileParty.MemberRoster.Contains(Hero.MainHero.CharacterObject)
                        || mobileParty.IsGarrison
                        || mobileParty.CurrentSettlement != null
                        || mobileParty.BesiegerCamp != null
                        || mobileParty.MapEvent != null
                        || partiesTimerDic.ContainsKey(mobileParty))
                    {
                        continue;
                    }
                    if (mobileParty.Army != null && mobileParty.Army.LeaderParty != mobileParty && mobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty))
                    {
                        continue;
                    }

                    // Leaderless clans and ownerless parties cannot be
                    // classified — skipping beats the old NRE on Owner.Clan.
                    Clan otherClan = mobileParty.Owner?.Clan ?? mobileParty.ActualClan;
                    if (otherClan == null)
                    {
                        continue;
                    }

                    float distance = Campaign.Current.MainParty.GetPosition2D.Distance(mobileParty.GetPosition2D);
                    int secondsPerUnit = timerset == 1 ? 30 : (timerset == 2 ? 16 : 23);
                    float duration = (distance - 3f) * secondsPerUnit;

                    bool atWarWithPlayerSide =
                        referenceClan.IsAtWarWith(otherClan)
                        || (referenceClan.Kingdom != null && otherClan.Kingdom != null && referenceClan.Kingdom.IsAtWarWith(otherClan.Kingdom))
                        || (referenceClan.MapFaction != null && otherClan.MapFaction != null && referenceClan.MapFaction.IsAtWarWith(otherClan.MapFaction));

                    if (atWarWithPlayerSide && encounteredMobile != null && encounteredMobile.IsBandit)
                    {
                        // Original behavior: enemy lords stay out of the
                        // player's bandit fights.
                        continue;
                    }

                    partiesTimerDic.Add(mobileParty, new MissionTimer(duration));
                    if (mission.PlayerTeam.IsPlayerGeneral)
                    {
                        RelationFilter(mobileParty);
                    }
                    else if (mobileParty.LeaderHero != null && !relationPair.ContainsKey(mobileParty.LeaderHero))
                    {
                        relationPair.Add(mobileParty.LeaderHero, AdodEnum.None);
                    }
                    (atWarWithPlayerSide ? nearPartiesEnemy : nearPartiesAlly).Add(mobileParty);
                }

                // Bandit reinforcements for bandit fights. This used to live
                // INSIDE the lord loop, so with no lord party in radius it
                // never ran at all.
                if (encounteredMobile != null && encounteredMobile.IsBandit)
                {
                    foreach (MobileParty bandit in MobileParty.AllBanditParties.FindAll(a => Campaign.Current.MainParty.GetPosition2D.Distance(a.GetPosition2D) <= radiousSetting))
                    {
                        if (playerEvent.InvolvedParties.Contains(bandit.Party)
                            || bandit.IsEngaging
                            || (bandit.CurrentSettlement != null && bandit.CurrentSettlement.IsHideout)
                            || partiesTimerDic.ContainsKey(bandit))
                        {
                            continue;
                        }
                        float distance = Campaign.Current.MainParty.GetPosition2D.Distance(bandit.GetPosition2D);
                        int secondsPerUnit = timerset == 1 ? 20 : (timerset == 2 ? 12 : 16);
                        partiesTimerDic.Add(bandit, new MissionTimer((distance - 3f) * secondsPerUnit));
                        nearPartiesEnemy.Add(bandit);
                    }
                }

                if (partiesTimerDic.Count != 0)
                {
                    DefaultBattleMissionAgentSpawnLogic spawnLogic = mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
                    if (spawnLogic != null)
                    {
                        missionSidesBoth = typeof(DefaultBattleMissionAgentSpawnLogic).GetField("_missionSides", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(spawnLogic) as IEnumerable;
                    }
                    // Without the spawn-side bookkeeping we cannot spawn
                    // safely — leave the system dormant for this mission.
                    timerStart = missionSidesBoth != null;
                }
            }

            public override void OnMissionTick(float dt)
            {
                base.OnMissionTick(dt);
                if (timerStart && !endButton)
                {
                    foreach (KeyValuePair<MobileParty, MissionTimer> keyValuePair in partiesTimerDic)
                    {
                        if (keyValuePair.Value.Check(false))
                        {
                            tempDic.Add(keyValuePair.Key);
                            RunAdod(keyValuePair.Key);
                        }
                    }
                    if (tooMany && addSpawnTimer.Check(true))
                    {
                        // Drain BOTH queues; the old TrySpawn(2) retry only
                        // ever resumed the attacker queue.
                        tooMany = false;
                        TrySpawn(0);
                        TrySpawn(1);
                    }
                    tempDic.ForEach(delegate (MobileParty party)
                    {
                        partiesTimerDic.Remove(party);
                    });
                    tempDic.Clear();
                    if (partiesTimerDic.Count <= 0 && reservedQueue0.Count == 0 && reservedQueue1.Count == 0)
                    {
                        timerStart = false;
                    }
                }
            }

            public void RunAdod(MobileParty party)
            {
                // Every filter below leans on the encounter/battle being alive.
                if (PlayerEncounter.Current == null || PlayerEncounter.Battle == null
                    || MapEvent.PlayerMapEvent == null || Mission.Current == null)
                {
                    timerStart = false;
                    return;
                }

                // Check if the player is knocked out before proceeding
                if (Hero.MainHero.HitPoints <= 0 || Mission.Current.MainAgent == null || !Mission.Current.MainAgent.IsActive())
                {
                    // Stop all reinforcement activities as the player is knocked out
                    timerStart = false;
                    return;
                }

                if (nearPartiesAlly.Contains(party))
                {
                    BattleSideEnum playerSide = PlayerEncounter.Current.PlayerSide;
                    BattleSideEnum oppositeSide = playerSide.GetOppositeSide();
                    float balance;
                    if (party.Army != null)
                    {
                        balance = PowerBalanceArmy(party, playerSide);
                    }
                    else
                    {
                        balance = PowerBalance(party, playerSide);
                    }
                    AdodEnum adodEnum2;
                    if (party.LeaderHero != null && relationPair.ContainsKey(party.LeaderHero))
                    {
                        relationPair.TryGetValue(party.LeaderHero, out adodEnum2);
                    }
                    else
                    {
                        adodEnum2 = AdodEnum.Unknown;
                    }
                    bool flag4 = Filter0(party, PowerBalanceFirst());
                    bool flag5 = Filter1_1(party, PowerBalanceFirst(), balance, adodEnum2);
                    bool flag6 = Filter2_1(party, PlayerEncounter.Battle.PartiesOnSide(playerSide));
                    bool flag7 = Filter3_1(party, PlayerEncounter.Battle.PartiesOnSide(oppositeSide));
                    if (!flag4 || !flag5 || !flag6 || !flag7)
                    {
                        return;
                    }
                }
                else if (nearPartiesEnemy.Contains(party))
                {
                    BattleSideEnum playerSide2 = PlayerEncounter.Current.PlayerSide;
                    BattleSideEnum oppositeSide2 = playerSide2.GetOppositeSide();
                    float balance2;
                    if (party.Army != null)
                    {
                        balance2 = PowerBalanceArmy(party, oppositeSide2);
                    }
                    else
                    {
                        balance2 = PowerBalance(party, oppositeSide2);
                    }
                    if (PlayerEncounter.EncounteredParty?.MobileParty != null && PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
                    {
                        if (!FilterBandit(balance2))
                        {
                            return;
                        }
                    }
                    else
                    {
                        AdodEnum adodEnum4;
                        if (party.LeaderHero != null && relationPair.ContainsKey(party.LeaderHero))
                        {
                            relationPair.TryGetValue(party.LeaderHero, out adodEnum4);
                        }
                        else
                        {
                            adodEnum4 = AdodEnum.Unknown;
                        }
                        if (!Filter1_2(party, PowerBalanceFirst(), balance2, adodEnum4) || !Filter2_2(party, PlayerEncounter.Battle.PartiesOnSide(oppositeSide2)) || !Filter3_2(party, PlayerEncounter.Battle.PartiesOnSide(playerSide2)))
                        {
                            return;
                        }
                    }
                }

                // Proceed with reinforcement logic if the player is not knocked out
                if (nearPartiesAlly.Contains(party))
                {
                    HornSystem(true);
                    BattleSideEnum playerSide3 = PlayerEncounter.Current.PlayerSide;
                    if (playerSide3 == PlayerEncounter.EncounteredBattle.AttackerSide.MissionSide)
                    {
                        if (party.Army != null && party.Army.LeaderParty == party)
                        {
                            List<MobileParty> list = new List<MobileParty>();
                            foreach (MobileParty mobileParty in party.Army.Parties)
                            {
                                if (party.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty) && !MapEvent.PlayerMapEvent.InvolvedParties.Contains(mobileParty.Party) && !mobileParty.IsMainParty && !mobileParty.MemberRoster.Contains(Hero.MainHero.CharacterObject))
                                {
                                    list.Add(mobileParty);
                                    foreach (TroopRosterElement troopRosterElement in mobileParty.MemberRoster.GetTroopRoster())
                                    {
                                        for (int i = 0; i < troopRosterElement.Number - troopRosterElement.WoundedNumber; i++)
                                        {
                                            IAgentOriginBase item = new PartyAgentOrigin(mobileParty.Party, troopRosterElement.Character, -1, default, false);
                                            reservedQueue1.Enqueue(item);
                                        }
                                    }
                                }
                            }
                            foreach (MobileParty mobileParty2 in list)
                            {
                                mobileParty2.MapEventSide = PlayerEncounter.EncounteredBattle.AttackerSide;
                                mobileParty2.Position = Campaign.Current.MainParty.Position;
                            }
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                        }
                        else
                        {
                            party.MapEventSide = PlayerEncounter.EncounteredBattle.AttackerSide;
                            party.Position = Campaign.Current.MainParty.Position;
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                            foreach (TroopRosterElement troopRosterElement2 in party.MemberRoster.GetTroopRoster())
                            {
                                for (int j = 0; j < troopRosterElement2.Number - troopRosterElement2.WoundedNumber; j++)
                                {
                                    IAgentOriginBase item2 = new PartyAgentOrigin(party.Party, troopRosterElement2.Character, -1, default, false);
                                    reservedQueue1.Enqueue(item2);
                                }
                            }
                        }
                        TrySpawn(1);
                    }
                    else if (playerSide3 == PlayerEncounter.EncounteredBattle.DefenderSide.MissionSide)
                    {
                        if (party.Army != null && party.Army.LeaderParty == party)
                        {
                            List<MobileParty> list2 = new List<MobileParty>();
                            foreach (MobileParty mobileParty3 in party.Army.Parties)
                            {
                                if (party.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty3) && !MapEvent.PlayerMapEvent.InvolvedParties.Contains(mobileParty3.Party) && !mobileParty3.IsMainParty && !mobileParty3.MemberRoster.Contains(Hero.MainHero.CharacterObject))
                                {
                                    list2.Add(mobileParty3);
                                    foreach (TroopRosterElement troopRosterElement3 in mobileParty3.MemberRoster.GetTroopRoster())
                                    {
                                        for (int k = 0; k < troopRosterElement3.Number - troopRosterElement3.WoundedNumber; k++)
                                        {
                                            IAgentOriginBase item3 = new PartyAgentOrigin(mobileParty3.Party, troopRosterElement3.Character, -1, default, false);
                                            reservedQueue0.Enqueue(item3);
                                        }
                                    }
                                }
                            }
                            foreach (MobileParty mobileParty4 in list2)
                            {
                                mobileParty4.MapEventSide = PlayerEncounter.EncounteredBattle.DefenderSide;
                                mobileParty4.Position = Campaign.Current.MainParty.Position;
                            }
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                        }
                        else
                        {
                            party.MapEventSide = PlayerEncounter.EncounteredBattle.DefenderSide;
                            party.Position = Campaign.Current.MainParty.Position;
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                            foreach (TroopRosterElement troopRosterElement4 in party.MemberRoster.GetTroopRoster())
                            {
                                for (int l = 0; l < troopRosterElement4.Number - troopRosterElement4.WoundedNumber; l++)
                                {
                                    IAgentOriginBase item4 = new PartyAgentOrigin(party.Party, troopRosterElement4.Character, -1, default, false);
                                    reservedQueue0.Enqueue(item4);
                                }
                            }
                        }
                        TrySpawn(0);
                    }
                }
                else if (nearPartiesEnemy.Contains(party))
                {
                    HornSystem(false);
                    BattleSideEnum playerSide4 = PlayerEncounter.Current.PlayerSide;
                    if (playerSide4 == PlayerEncounter.EncounteredBattle.AttackerSide.MissionSide)
                    {
                        if (party.Army != null && party.Army.LeaderParty == party)
                        {
                            List<MobileParty> list3 = new List<MobileParty>();
                            foreach (MobileParty mobileParty5 in party.Army.Parties)
                            {
                                if (party.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty5) && !MapEvent.PlayerMapEvent.InvolvedParties.Contains(mobileParty5.Party) && !mobileParty5.IsMainParty && !mobileParty5.MemberRoster.Contains(Hero.MainHero.CharacterObject))
                                {
                                    list3.Add(mobileParty5);
                                    foreach (TroopRosterElement troopRosterElement5 in mobileParty5.MemberRoster.GetTroopRoster())
                                    {
                                        for (int m = 0; m < troopRosterElement5.Number - troopRosterElement5.WoundedNumber; m++)
                                        {
                                            IAgentOriginBase item5 = new PartyAgentOrigin(mobileParty5.Party, troopRosterElement5.Character, -1, default, false);
                                            reservedQueue0.Enqueue(item5);
                                        }
                                    }
                                }
                            }
                            foreach (MobileParty mobileParty6 in list3)
                            {
                                mobileParty6.MapEventSide = PlayerEncounter.EncounteredBattle.DefenderSide;
                                mobileParty6.Position = Campaign.Current.MainParty.Position;
                            }
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                        }
                        else
                        {
                            if (MapEvent.PlayerMapEvent.InvolvedParties.Contains(party.Party))
                            {
                                return;
                            }
                            party.MapEventSide = PlayerEncounter.EncounteredBattle.DefenderSide;
                            party.Position = Campaign.Current.MainParty.Position;
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                            foreach (TroopRosterElement troopRosterElement6 in party.MemberRoster.GetTroopRoster())
                            {
                                for (int n = 0; n < troopRosterElement6.Number - troopRosterElement6.WoundedNumber; n++)
                                {
                                    IAgentOriginBase item6 = new PartyAgentOrigin(party.Party, troopRosterElement6.Character, -1, default, false);
                                    reservedQueue0.Enqueue(item6);
                                }
                            }
                        }
                        TrySpawn(0);
                    }
                    else if (playerSide4 == PlayerEncounter.EncounteredBattle.DefenderSide.MissionSide)
                    {
                        if (party.Army != null && party.Army.LeaderParty == party)
                        {
                            List<MobileParty> list4 = new List<MobileParty>();
                            foreach (MobileParty mobileParty7 in party.Army.Parties)
                            {
                                if (party.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty7) && !MapEvent.PlayerMapEvent.InvolvedParties.Contains(mobileParty7.Party) && !mobileParty7.IsMainParty && !mobileParty7.MemberRoster.Contains(Hero.MainHero.CharacterObject))
                                {
                                    list4.Add(mobileParty7);
                                    foreach (TroopRosterElement troopRosterElement7 in mobileParty7.MemberRoster.GetTroopRoster())
                                    {
                                        for (int num = 0; num < troopRosterElement7.Number - troopRosterElement7.WoundedNumber; num++)
                                        {
                                            IAgentOriginBase item7 = new PartyAgentOrigin(mobileParty7.Party, troopRosterElement7.Character, -1, default, false);
                                            reservedQueue1.Enqueue(item7);
                                        }
                                    }
                                }
                            }
                            foreach (MobileParty mobileParty8 in list4)
                            {
                                mobileParty8.MapEventSide = PlayerEncounter.EncounteredBattle.AttackerSide;
                                mobileParty8.Position = Campaign.Current.MainParty.Position;
                            }
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                        }
                        else
                        {
                            if (MapEvent.PlayerMapEvent.InvolvedParties.Contains(party.Party))
                            {
                                return;
                            }
                            party.MapEventSide = PlayerEncounter.EncounteredBattle.AttackerSide;
                            party.Position = Campaign.Current.MainParty.Position;
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                            foreach (TroopRosterElement troopRosterElement8 in party.MemberRoster.GetTroopRoster())
                            {
                                for (int num2 = 0; num2 < troopRosterElement8.Number - troopRosterElement8.WoundedNumber; num2++)
                                {
                                    IAgentOriginBase item8 = new PartyAgentOrigin(party.Party, troopRosterElement8.Character, -1, default, false);
                                    reservedQueue1.Enqueue(item8);
                                }
                            }
                        }
                        TrySpawn(1);
                    }
                }
            }


            // Synchronous on purpose. The old version was async void with
            // await Task.Delay inside the spawn loop: after the first await
            // every continuation ran on the THREAD POOL (Bannerlord has no
            // SynchronizationContext), so Mission.SpawnTroop executed off the
            // main thread — random engine corruption. Spawning now happens in
            // bounded bursts on the mission tick; the retry timer in
            // OnMissionTick paces the remainder.
            public void TrySpawn(int side)
            {
                Mission mission = Mission.Current;
                if (mission == null || missionSidesBoth == null || PlayerEncounter.Battle == null)
                {
                    return;
                }
                Queue<IAgentOriginBase> selectedQueue = side == 0 ? reservedQueue0 : reservedQueue1;
                if (selectedQueue.Count == 0)
                {
                    return;
                }
                if (mission.Agents.Count >= 1400)
                {
                    ScheduleSpawnRetry();
                    return;
                }

                bool playerTeam = PlayerEncounter.Battle.PlayerSide == (BattleSideEnum)side;
                bool hasFormation = side == 0 && mission.DefenderAllyTeam != null || side == 1 && mission.AttackerAllyTeam != null;
                // Spawn at the side's own deployment area — the old code used
                // the PLAYER team's spawn for both sides, so enemy
                // reinforcements materialized inside the player's lines.
                Team sideTeam = (side == 0 ? mission.DefenderTeam : mission.AttackerTeam) ?? mission.PlayerTeam;

                int spawnCount = 0;
                foreach (var missionSide in missionSidesBoth)
                {
                    var fieldInfo = missionSide.GetType().GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (fieldInfo != null)
                    {
                        int? spawnTroopNumber = fieldInfo.GetValue(missionSide) as int?;
                        if (spawnTroopNumber.HasValue)
                        {
                            fieldInfo.SetValue(missionSide, spawnTroopNumber.Value + 10000);
                            break;
                        }
                    }
                }
                while (spawnCount < 60 && selectedQueue.Count > 0 && mission.Agents.Count < 1600)
                {
                    spawnCount++;
                    IAgentOriginBase originBase = selectedQueue.Dequeue();
                    try
                    {
                        TaleWorlds.Library.Vec2 spawnPosition = mission.GetFormationSpawnPosition(sideTeam, originBase.Troop.DefaultFormationClass);
                        Agent nagent = mission.SpawnTroop(
                            originBase,
                            playerTeam,
                            hasFormation,
                            true, true,
                            0,
                            originBase.Troop.DefaultFormationGroup,
                            true, false,
                            spawnPosition.ToVec3(0.0f),
                            spawnPosition,
                            null,
                            null,
                            FormationClass.NumberOfAllFormations,
                            false);
                        if (side == 0)
                            newAgentsDefend.Add(nagent);
                        else
                            newAgentsAttack.Add(nagent);
                    }
                    catch
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Spawn Failed!"));
                    }
                }
                if (spawnCount > 0)
                {
                    foreach (var missionSide in missionSidesBoth)
                    {
                        var fieldInfo = missionSide.GetType().GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (fieldInfo != null)
                        {
                            int? spawnTroopNumber = fieldInfo.GetValue(missionSide) as int?;
                            if (spawnTroopNumber.HasValue)
                            {
                                fieldInfo.SetValue(missionSide, spawnTroopNumber.Value + spawnCount - 10000);
                                break;
                            }
                        }
                    }
                }
                if (selectedQueue.Count > 0)
                {
                    ScheduleSpawnRetry();
                }
            }

            private void ScheduleSpawnRetry()
            {
                if (!tooMany)
                {
                    tooMany = true;
                    addSpawnTimer = new MissionTimer(3f);
                }
            }

            public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
            {
                if (newAgentsAttack.Contains(affectedAgent) && !affectedAgent.IsRunningAway)
                {
                    short num = 1;
                    foreach (object obj in missionSidesBoth)
                    {
                        if (num != 1)
                        {
                            FieldInfo field = obj.GetType().GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                            int? num2 = (field.GetValue(obj) as int?) - 1;
                            field.SetValue(obj, num2);
                            break;
                        }
                        num -= 1;
                    }
                }
                else if (newAgentsDefend.Contains(affectedAgent) && !affectedAgent.IsRunningAway)
                {
                    short num3 = 0;
                    foreach (object obj2 in missionSidesBoth)
                    {
                        if (num3 != 1)
                        {
                            FieldInfo field2 = obj2.GetType().GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                            int? num4 = (field2.GetValue(obj2) as int?) - 1;
                            field2.SetValue(obj2, num4);
                            break;
                        }
                        num3 -= 1;
                    }
                }
            }

            public override void OnAgentPanicked(Agent affectedAgent)
            {
                if (newAgentsAttack.Contains(affectedAgent) && affectedAgent.IsHuman)
                {
                    short num = 1;
                    foreach (object obj in missionSidesBoth)
                    {
                        if (num != 1)
                        {
                            FieldInfo field = obj.GetType().GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                            int? num2 = (field.GetValue(obj) as int?) - 1;
                            field.SetValue(obj, num2);
                            break;
                        }
                        num -= 1;
                    }
                }
                else if (newAgentsDefend.Contains(affectedAgent) && affectedAgent.IsHuman)
                {
                    short num3 = 0;
                    foreach (object obj2 in missionSidesBoth)
                    {
                        if (num3 != 1)
                        {
                            FieldInfo field2 = obj2.GetType().GetField("_numSpawnedTroops", BindingFlags.Instance | BindingFlags.NonPublic);
                            int? num4 = (field2.GetValue(obj2) as int?) - 1;
                            field2.SetValue(obj2, num4);
                            break;
                        }
                        num3 -= 1;
                    }
                }
            }

            protected override void OnEndMission()
            {
                base.OnEndMission();
                if (!endButton)
                {
                    endButton = true;
                    timerStart = false;
                }
            }

            public override void OnMissionResultReady(MissionResult missionResult)
            {
                base.OnMissionResultReady(missionResult);
                if (!endButton)
                {
                    endButton = true;
                    timerStart = false;
                }
            }

            private List<MobileParty> nearPartiesAlly = new List<MobileParty>();
            private List<MobileParty> nearPartiesEnemy = new List<MobileParty>();
            private Dictionary<MobileParty, MissionTimer> partiesTimerDic = new Dictionary<MobileParty, MissionTimer>();
            private Dictionary<Hero, AdodEnum> relationPair = new Dictionary<Hero, AdodEnum>();
            private Queue<IAgentOriginBase> reservedQueue0 = new Queue<IAgentOriginBase>();
            private Queue<IAgentOriginBase> reservedQueue1 = new Queue<IAgentOriginBase>();
            private MissionTimer addSpawnTimer;
            private List<Agent> newAgentsAttack = new List<Agent>();
            private List<Agent> newAgentsDefend = new List<Agent>();
            private List<MobileParty> tempDic = new List<MobileParty>();
            private Hero mainHeros = Hero.MainHero;
            private bool timerStart = false;
            private bool tooMany = false;
            private bool endButton = false;
            private short radiousSetting = 20;
            private short timerset = 0;
            private IEnumerable missionSidesBoth;
        }

        public static void HornSystem(bool isAlly)
        {
            if (isAlly)
            {
                SoundEvent.PlaySound2D("event:/alerts/horns/reinforcements");
            }
            else
            {
                SoundEvent.PlaySound2D("event:/alerts/horns/attack");
            }
        }
    }
}

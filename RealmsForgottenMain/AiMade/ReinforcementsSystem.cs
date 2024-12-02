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

namespace RealmsForgotten.AiMade
{
    public class ADODReinforcementsSystem
    {
        public static float PowerBalanceArmy(MobileParty party, BattleSideEnum battleSide)
        {
            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
            float totalStrength = party.Army.TotalStrength;
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
            float totalStrength = party.Party.TotalStrength;
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
                if (party.LeaderHero.GetHeroTraits() != null)
                {
                    mercy = party.LeaderHero.GetHeroTraits().Mercy;
                    calculating = party.LeaderHero.GetHeroTraits().Calculating;
                    valor = party.LeaderHero.GetHeroTraits().Valor;
                }
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
                if (party.LeaderHero.GetHeroTraits() != null)
                {
                    mercy = party.LeaderHero.GetHeroTraits().Mercy;
                    calculating = party.LeaderHero.GetHeroTraits().Calculating;
                    valor = party.LeaderHero.GetHeroTraits().Valor;
                }
            }

            if (PlayerEncounter.EncounteredMobileParty == null || PlayerEncounter.EncounteredMobileParty.Owner == null || PlayerEncounter.EncounteredMobileParty.LeaderHero == null || party.Owner == null || party.LeaderHero == null)
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

            if (PlayerEncounter.EncounteredParty.MobileParty != null && party.ActualClan == PlayerEncounter.EncounteredParty.MobileParty.ActualClan)
            {
                num += 15;
            }
            else if (party.MapFaction == PlayerEncounter.EncounteredParty.MapFaction)
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
            if (PlayerEncounter.EncounteredParty.MobileParty != null && PlayerEncounter.EncounteredMobileParty.IsBandit)
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
            if (PlayerEncounter.EncounteredParty.MobileParty != null && PlayerEncounter.EncounteredMobileParty.IsBandit)
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
            if (PlayerEncounter.EncounteredParty.MobileParty != null && PlayerEncounter.EncounteredMobileParty.IsBandit)
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

        public class ADODReinforcementsRunner : MissionLogic
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
                if (oldMissionMode == MissionMode.Deployment)
                {
                    bool autoDetectRBM = false;
                    if (autoDetectRBM)
                    {
                        ModuleManager moduleManager = new ModuleManager();
                        if (moduleManager.ModuleNames.Contains("RBM"))
                        {
                            timerset = 1;
                        }
                    }
                    foreach (MobileParty mobileParty in MobileParty.AllLordParties.FindAll(a => Campaign.Current.MainParty.GetPosition2D.Distance(a.GetPosition2D) <= radiousSetting))
                    {
                        if (!MapEvent.PlayerMapEvent.InvolvedParties.Contains(mobileParty.Party))
                        {
                            if (!mobileParty.IsMainParty && !mobileParty.MemberRoster.Contains(Hero.MainHero.CharacterObject) && !mobileParty.IsGarrison && mobileParty.CurrentSettlement == null && mobileParty.BesiegerCamp == null)
                            {
                                if (mobileParty.Army != null)
                                {
                                    if (mobileParty.Army.LeaderParty != mobileParty && mobileParty.Army.DoesLeaderPartyAndAttachedPartiesContain(mobileParty))
                                    {
                                        continue;
                                    }
                                }
                                if (mobileParty.MapEvent == null)
                                {
                                    float num = Campaign.Current.MainParty.GetPosition2D.Distance(mobileParty.GetPosition2D);
                                    short num2 = timerset;
                                    short num3 = num2;
                                    int num4;
                                    if (num3 != 1)
                                    {
                                        if (num3 != 2)
                                        {
                                            num4 = 23;
                                        }
                                        else
                                        {
                                            num4 = 16;
                                        }
                                    }
                                    else
                                    {
                                        num4 = 30;
                                    }
                                    float duration = (num - 3f) * num4;
                                    if (Mission.Current.PlayerTeam.IsPlayerGeneral)
                                    {
                                        if (mainHeros.Clan != null && mobileParty.Owner.Clan != null && mainHeros.Clan.IsAtWarWith(mobileParty.Owner.Clan) || mainHeros.Clan.Kingdom != null && mobileParty.Owner.Clan.Kingdom != null && mainHeros.Clan.Kingdom.IsAtWarWith(mobileParty.Owner.Clan.Kingdom) || mainHeros.Clan.MapFaction != null && mobileParty.Owner.Clan.MapFaction != null && mainHeros.Clan.MapFaction.IsAtWarWith(mobileParty.Owner.Clan.MapFaction))
                                        {
                                            if (PlayerEncounter.EncounteredParty.MobileParty == null || !PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
                                            {
                                                if (!partiesTimerDic.ContainsKey(mobileParty))
                                                {
                                                    partiesTimerDic.Add(mobileParty, new MissionTimer(duration));
                                                    RelationFilter(mobileParty);
                                                    nearPartiesEnemy.Add(mobileParty);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!partiesTimerDic.ContainsKey(mobileParty))
                                            {
                                                partiesTimerDic.Add(mobileParty, new MissionTimer(duration));
                                                RelationFilter(mobileParty);
                                                nearPartiesAlly.Add(mobileParty);
                                            }
                                        }
                                    }
                                    else if (Mission.Current.PlayerTeam.IsPlayerSergeant)
                                    {
                                        CharacterObject characterObject = (CharacterObject)Mission.Current.PlayerTeam.Leader.Character;
                                        if (characterObject.HeroObject.Clan != null && mobileParty.Owner.Clan != null && characterObject.HeroObject.Clan.IsAtWarWith(mobileParty.Owner.Clan) || characterObject.HeroObject.Clan.Kingdom != null && mobileParty.Owner.Clan.Kingdom != null && characterObject.HeroObject.Clan.Kingdom.IsAtWarWith(mobileParty.Owner.Clan.Kingdom) || characterObject.HeroObject.Clan.MapFaction != null && mobileParty.Owner.Clan.MapFaction != null && characterObject.HeroObject.Clan.MapFaction.IsAtWarWith(mobileParty.Owner.Clan.MapFaction))
                                        {
                                            if (PlayerEncounter.EncounteredParty.MobileParty == null || !PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
                                            {
                                                if (!partiesTimerDic.ContainsKey(mobileParty))
                                                {
                                                    partiesTimerDic.Add(mobileParty, new MissionTimer(duration));
                                                    if (mobileParty.LeaderHero != null && !relationPair.ContainsKey(mobileParty.LeaderHero))
                                                    {
                                                        relationPair.Add(mobileParty.LeaderHero, AdodEnum.None);
                                                    }
                                                    nearPartiesEnemy.Add(mobileParty);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (!partiesTimerDic.ContainsKey(mobileParty))
                                            {
                                                partiesTimerDic.Add(mobileParty, new MissionTimer(duration));
                                                if (mobileParty.LeaderHero != null && !relationPair.ContainsKey(mobileParty.LeaderHero))
                                                {
                                                    relationPair.Add(mobileParty.LeaderHero, AdodEnum.None);
                                                }
                                                nearPartiesAlly.Add(mobileParty);
                                            }
                                        }
                                    }
                                    if (PlayerEncounter.EncounteredParty.MobileParty != null && PlayerEncounter.EncounteredParty.MobileParty.IsBandit)
                                    {
                                        foreach (MobileParty mobileParty2 in MobileParty.AllBanditParties.FindAll(a => Campaign.Current.MainParty.GetPosition2D.Distance(a.GetPosition2D) <= radiousSetting))
                                        {
                                            if (!MapEvent.PlayerMapEvent.InvolvedParties.Contains(mobileParty2.Party) && !mobileParty2.IsEngaging && (mobileParty2.CurrentSettlement == null || !mobileParty2.CurrentSettlement.IsHideout))
                                            {
                                                float num5 = Campaign.Current.MainParty.GetPosition2D.Distance(mobileParty2.GetPosition2D);
                                                short num6 = timerset;
                                                short num7 = num6;
                                                int num8;
                                                if (num7 != 1)
                                                {
                                                    if (num7 != 2)
                                                    {
                                                        num8 = 16;
                                                    }
                                                    else
                                                    {
                                                        num8 = 12;
                                                    }
                                                }
                                                else
                                                {
                                                    num8 = 20;
                                                }
                                                float duration2 = (num5 - 3f) * num8;
                                                if (!partiesTimerDic.ContainsKey(mobileParty2))
                                                {
                                                    partiesTimerDic.Add(mobileParty2, new MissionTimer(duration2));
                                                    nearPartiesEnemy.Add(mobileParty2);
                                                }
                                            }
                                        }
                                    }
                                    if (partiesTimerDic.Count != 0)
                                    {
                                        missionSidesBoth = typeof(MissionAgentSpawnLogic).GetField("_missionSides", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Mission.GetMissionBehavior<MissionAgentSpawnLogic>()) as IEnumerable;
                                        timerStart = true;
                                    }
                                }
                            }
                        }
                    }
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
                    if (tooMany)
                    {
                        if (addSpawnTimer.Check(true))
                        {
                            TrySpawn(2);
                        }
                    }
                    tempDic.ForEach(delegate (MobileParty party)
                    {
                        partiesTimerDic.Remove(party);
                        if (partiesTimerDic.Count <= 0 && reservedQueue0.Count == 0 && reservedQueue1.Count == 0)
                        {
                            timerStart = false;
                        }
                    });
                }
            }

            public void RunAdod(MobileParty party)
            {
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
                    if (PlayerEncounter.EncounteredParty.MobileParty != null && PlayerEncounter.EncounteredMobileParty.IsBandit)
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
                                mobileParty2.Position2D = Campaign.Current.MainParty.Position2D;
                            }
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                        }
                        else
                        {
                            party.MapEventSide = PlayerEncounter.EncounteredBattle.AttackerSide;
                            party.Position2D = Campaign.Current.MainParty.Position2D;
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
                                mobileParty4.Position2D = Campaign.Current.MainParty.Position2D;
                            }
                            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
                        }
                        else
                        {
                            party.MapEventSide = PlayerEncounter.EncounteredBattle.DefenderSide;
                            party.Position2D = Campaign.Current.MainParty.Position2D;
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
                                mobileParty6.Position2D = Campaign.Current.MainParty.Position2D;
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
                            party.Position2D = Campaign.Current.MainParty.Position2D;
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
                                mobileParty8.Position2D = Campaign.Current.MainParty.Position2D;
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
                            party.Position2D = Campaign.Current.MainParty.Position2D;
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


            public async void TrySpawn(int side)
            {
                if (Mission.Current.Agents.Count >= 1400)
                {
                    if (tooMany)
                        return;
                    tooMany = true;
                    addSpawnTimer = new MissionTimer(20f);
                }
                else
                {
                    Queue<IAgentOriginBase> selectedQueue = side == 0 ? reservedQueue0 : reservedQueue1;
                    bool playerTeam = PlayerEncounter.Battle.PlayerSide == (BattleSideEnum)side;
                    bool hasFormation = side == 0 && Mission.Current.DefenderAllyTeam != null || side == 1 && Mission.Current.AttackerAllyTeam != null;
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
                    while (spawnCount < 700 && selectedQueue.Count > 0 && Mission.Current.Agents.Count < 1600)
                    {
                        spawnCount++;
                        IAgentOriginBase originBase = selectedQueue.Dequeue();
                        try
                        {
                            Mission current = Mission.Current;
                            Agent nagent = current.SpawnTroop(
                                originBase,
                                playerTeam,
                                hasFormation,
                                true, true,
                                0,
                                originBase.Troop.DefaultFormationGroup,
                                true, false, false,
                                Mission.Current.GetFormationSpawnPosition(
                                    (BattleSideEnum)side,
                                    originBase.Troop.DefaultFormationClass,
                                    true).ToVec3(0.0f),
                                Mission.Current.GetFormationSpawnPosition(
                                    (BattleSideEnum)side,
                                    originBase.Troop.DefaultFormationClass,
                                    true),
                                null,
                                null,
                                FormationClass.NumberOfAllFormations,
                                false);
                            if (side == 0)
                                newAgentsDefend.Add(nagent);
                            else
                                newAgentsAttack.Add(nagent);
                            await Task.Delay(5);
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
                    if (selectedQueue.Count > 0 && !tooMany)
                    {
                        tooMany = true;
                        addSpawnTimer = new MissionTimer(20f);
                    }
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

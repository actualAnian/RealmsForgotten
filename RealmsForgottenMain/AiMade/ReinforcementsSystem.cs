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

namespace RealmsForgotten.AiMade
{
    public class ReinforcementsSystem
    {
        private const float BALANCE_THRESHOLD_1 = 0.55f;
        private const float BALANCE_THRESHOLD_2 = 0.7f;
        private const int MAX_TROOPS = 1500;
        private const int MIN_TROOPS = 30;
        private const int TROOP_SPAWN_LIMIT = 700;

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

        public static float PowerBalanceArmy(MobileParty party, BattleSideEnum battleSide)
        {
            return CalculatePowerBalance(party.Army.TotalStrength, battleSide);
        }

        public static float PowerBalanceFirst()
        {
            return CalculatePowerBalance(0, BattleSideEnum.None);
        }

        public static float PowerBalance(MobileParty party, BattleSideEnum battleSide)
        {
            return CalculatePowerBalance(party.Party.TotalStrength, battleSide);
        }

        private static float CalculatePowerBalance(float totalStrength, BattleSideEnum battleSide)
        {
            MapEvent.PlayerMapEvent.RecalculateStrengthOfSides();
            float attackerStrength = MapEvent.PlayerMapEvent.StrengthOfSide[1];
            float defenderStrength = MapEvent.PlayerMapEvent.StrengthOfSide[0];

            if (battleSide == BattleSideEnum.Attacker)
                attackerStrength += totalStrength;
            else if (battleSide == BattleSideEnum.Defender)
                defenderStrength += totalStrength;

            return attackerStrength / (attackerStrength + defenderStrength);
        }

        public static bool FilterBandit(float balance)
        {
            int result = 10;

            if (balance > BALANCE_THRESHOLD_1 && balance <= BALANCE_THRESHOLD_2)
                result += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -10 : 10;
            else if (balance > BALANCE_THRESHOLD_2)
                result += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -20 : 20;
            else if (balance < 0.45f && balance >= 0.3f)
                result += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 10 : -10;
            else if (balance < 0.2f)
                result += PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 20 : -20;

            return result >= 0;
        }

        public static bool Filter0(MobileParty party, float balance)
        {
            return !(balance > 0.85f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker) &&
                   !(balance < 0.15f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender);
        }

        // Extracts the filter conditions into a separate method for reuse
        private static int CalculateBalanceImpact(float balance)
        {
            if (balance > BALANCE_THRESHOLD_1 && balance <= BALANCE_THRESHOLD_2)
                return PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -5 : 10;
            else if (balance > BALANCE_THRESHOLD_2)
                return PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? -20 : 20;
            else if (balance < 0.45f && balance >= 0.3f)
                return PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 10 : -5;
            else if (balance < 0.2f)
                return PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker ? 20 : -20;
            return 0;
        }

        public static bool Filter1_1(MobileParty party, float firstBalance, float balance, AdodEnum adodEnum)
        {
            int totalScore = 0;
            int randomAdjustment = new Random().Next(-5, 5);
            int relationImpact = GetRelationImpact(party.LeaderHero);
            int balanceImpact = CalculateBalanceImpact(balance);
            int mercyImpact = CalculateMercyImpact(party, firstBalance, balance);
            int troopCountImpact = CalculateTroopCountImpact();
            int adodRelationImpact = GetAdodRelationImpact(adodEnum);

            totalScore = relationImpact + balanceImpact + mercyImpact + randomAdjustment + troopCountImpact + adodRelationImpact;

            return totalScore >= 15;
        }

        private static int GetRelationImpact(Hero leaderHero)
        {
            int relationImpact = 0;
            if (leaderHero != null)
            {
                int relation = (int)leaderHero.GetRelationWithPlayer();
                relationImpact = relation >= 50 ? 20 : relation >= 30 ? 15 : relation >= 10 ? 10 :
                                 relation <= -50 ? -20 : relation <= -30 ? -15 : relation <= -10 ? -10 : 0;
            }
            return relationImpact;
        }

        private static int GetAdodRelationImpact(AdodEnum adodEnum)
        {
            return adodEnum switch
            {
                AdodEnum.Father or AdodEnum.Mother => 20,
                AdodEnum.Siblings or AdodEnum.SonDaughter or AdodEnum.HusbandWife => 15,
                AdodEnum.Wanderer or AdodEnum.EtcClanMember or AdodEnum.Vassal or AdodEnum.King => 10,
                AdodEnum.Colleague => 5,
                _ => 0
            };
        }

        private static int CalculateTroopCountImpact()
        {
            int troopCount = PlayerEncounter.Battle.AttackerSide.TroopCount + PlayerEncounter.Battle.DefenderSide.TroopCount;
            return (troopCount > MAX_TROOPS || troopCount < MIN_TROOPS) ? -5 :
                   (troopCount > 2000 || troopCount < 10) ? -10 : 0;
        }

        private static int CalculateMercyImpact(MobileParty party, float firstBalance, float balance)
        {
            int mercyImpact = 0;
            if (party.LeaderHero?.GetHeroTraits() != null)
            {
                int mercy = party.LeaderHero.GetHeroTraits().Mercy;
                int calculating = party.LeaderHero.GetHeroTraits().Calculating;
                int valor = party.LeaderHero.GetHeroTraits().Valor;

                if (firstBalance < 0.45f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker && mercy > 0)
                    mercyImpact += 15;
                else if (firstBalance > 0.55f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender && mercy > 0)
                    mercyImpact += 15;

                if (balance > 0.7f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker)
                {
                    if (calculating > 0) mercyImpact += 15;
                }
                else if (balance < 0.45f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Attacker)
                {
                    if (calculating > 0) mercyImpact -= 15;
                    if (valor > 0) mercyImpact += 15;
                    if (valor < 0) mercyImpact -= 15;
                }
                else if (balance < 0.3f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender)
                {
                    if (calculating > 0) mercyImpact += 15;
                }
                else if (balance > 0.55f && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender)
                {
                    if (calculating > 0) mercyImpact -= 15;
                    if (valor > 0) mercyImpact += 15;
                    if (valor < 0) mercyImpact -= 15;
                }
            }
            return mercyImpact;
        }

        // Other Filter methods should follow a similar pattern for improvements
        // with refactoring, consolidating repeated logic, and improved readability.

        public static void HornSystem(bool isAlly)
        {
            SoundEvent.PlaySound2D(isAlly ? "event:/alerts/horns/reinforcements" : "event:/alerts/horns/attack");
        }
    }
}

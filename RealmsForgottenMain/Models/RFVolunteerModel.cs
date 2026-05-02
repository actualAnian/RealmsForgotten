using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RealmsForgotten.AiMade.Career;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.ObjectSystem;
namespace RealmsForgotten.Models
{
    internal class RFVolunteerModel : DefaultVolunteerModel
    {
        private VolunteerModel defaultModel;
        readonly Random random;

        public RFVolunteerModel(VolunteerModel previousModel)
        {
            defaultModel = previousModel;
            random = new Random();
        }
        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            int baseValue = defaultModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);
            if (RFSettings.Instance?.InfluenceCostForDifferentCultures == true)
            {
                IFaction buyerKingdom = buyerHero.MapFaction;
                if (buyerKingdom == null || buyerHero.Clan != null && buyerHero.Clan.IsClanTypeMercenary && buyerHero.Clan.IsMinorFaction || sellerHero.HomeSettlement.Owner == buyerHero)
                    return baseValue;
                if (buyerKingdom.IsAtWarWith(sellerHero.HomeSettlement.MapFaction) || (buyerHero.Clan?.Influence <= 0 && buyerHero?.Culture != sellerHero?.Culture))
                    return 0;
            }
            MercenaryVolunteerModel.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, ref baseValue);

            return baseValue;
        }
        public override CharacterObject GetBasicVolunteer(Hero hero)
        {
            foreach (Func<Hero, List<VolunteerChance>?> func in CallHierarchy)
            {
                List<VolunteerChance>? value = func(hero);
                if (value == null) continue;

                int total = value.Sum(x => x.Probability);
                int roll = random.Next(total);
                foreach (VolunteerChance volunteer in value)
                {
                    if (roll < volunteer.Probability)
                        return MBObjectManager.Instance.GetObject<CharacterObject>(volunteer.CharacterId);
                    roll -= volunteer.Probability;
                }
            }
            return defaultModel.GetBasicVolunteer(hero);
        }
        private static readonly List<Func<Hero, List<VolunteerChance>?>> CallHierarchy = new() { GetVolunteerFromSettlementStringId, GetVolunteerFromOwnerClanStringId, GetVolunteerFromSettlementCultureStringId };

        static List<VolunteerChance>? GetVolunteerFromSettlementStringId(Hero notable)
        {
            FromSettlementStringId.TryGetValue(notable.CurrentSettlement.StringId, out List<VolunteerChance>? value);
            return value;
        }
        static List<VolunteerChance>? GetVolunteerFromOwnerClanStringId(Hero notable)
        {
            FromSettlementOwnerClanStringId.TryGetValue(notable.CurrentSettlement.OwnerClan.StringId, out List<VolunteerChance>? value);
            return value;
        }
        static List<VolunteerChance>? GetVolunteerFromSettlementCultureStringId(Hero notable)
        {
            FromSettlementCultureStringId.TryGetValue(notable.CurrentSettlement.MapFaction.Culture.StringId, out List<VolunteerChance>? value);
            return value;
        }

        private static readonly Dictionary<string, List<VolunteerChance>> FromSettlementStringId = new()
        {
            ["town_W1"] = new List<VolunteerChance> { new("wulf_raider", 5) },
            ["town_W2"] = new List<VolunteerChance> { new("dunland_scout", 5) },
            ["town_W3"] = new List<VolunteerChance> { new("wulf_raider", 5) },
            ["town_W4"] = new List<VolunteerChance> { new("dunland_scout", 5) },
            ["town_W5"] = new List<VolunteerChance> { new("wulf_raider", 5) }
        };
        private static readonly Dictionary<string, List<VolunteerChance>> FromSettlementOwnerClanStringId = new()
        {
            ["wulf_faction_1"] = new List<VolunteerChance> { new("wulf_troop", 2) },
            ["wulf_faction_2"] = new List<VolunteerChance> { new("wulf_troop", 2) },
            ["wulf_faction_3"] = new List<VolunteerChance> { new("wulf_troop", 2) },
            ["wulf_faction_4"] = new List<VolunteerChance> { new("wulf_troop", 2) }
        };
        private static readonly Dictionary<string, List<VolunteerChance>> FromSettlementCultureStringId = new()
        {
            ["wulf"] = new List<VolunteerChance> { new("wulf_troop", 3) },
        };
        public class VolunteerChance
        {
            public string CharacterId { get; private set; }
            public int Probability { get; private set; }
            public VolunteerChance(string characterId, int probability)
            {
                CharacterId = characterId;
                Probability = probability;
            }
        }
    }
}

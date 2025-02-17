using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Career
{
    public static class PlayerCareerExtension
    {
        public static readonly int MaximumNumberOfCareerPerkPoints = 30;
        public static HeroExtendedInfo? PlayerCareerInfo { get; set; } = new();
        public static bool HasAnyCareer() => Game.Current.GameType is Campaign && GetCareer() != null;
        public static CareerObject? GetCareer()
        {
            Hero hero = Hero.MainHero;
            CareerObject? result = null;
            if (PlayerCareerInfo != null && !string.IsNullOrEmpty(PlayerCareerInfo.CareerID))
            {
                result = RFCareers.All.FirstOrDefault(x => x.StringId == PlayerCareerInfo.CareerID);
            }
            return result;
        }
        public static void RemoveAttribute(string attribute)
        {
            var info = PlayerCareerInfo;
            if (info != null && info.AllAttributes.Contains(attribute))
            {
                info.AcquiredAttributes.Remove(attribute);
            }
        }
        public static void AddAttribute(string attribute)
        {
            var info = PlayerCareerInfo;
            if (info != null && !info.AllAttributes.Contains(attribute))
            {
                info.AcquiredAttributes.Add(attribute);
            }
        }

        public static void AddCareer(CareerObject career)
        {
            Hero hero = Hero.MainHero;
            HeroExtendedInfo info = PlayerCareerInfo;
            if (info != null)
            {
                if (HasAnyCareer())
                {
                    info.CareerChoices.Clear();
                }
                info.CareerID = career.StringId;
                info.CareerChoices.Add(career.RootNode.StringId);
                var careerObj = RFCareerChoices.Instance.GetCareerChoices(GetCareer());
                RemoveAttribute( "CareerTier" + 1);
                RemoveAttribute("CareerTier" + 2);
                RemoveAttribute("CareerTier" + 3);
                careerObj.InitialCareerSetup();
            }
        }

        public static bool HasCareerChoice(string choiceID)
        {
            bool result = false;
            if (PlayerCareerInfo != null)
            {
                return PlayerCareerInfo.CareerChoices.Contains(choiceID);
            }
            return result;
        }
        public static bool HasCareerChoice(CareerChoiceObject choice)
        {
            bool result = false;
            if (PlayerCareerInfo != null)
            {
                return PlayerCareerInfo.CareerChoices.Contains(choice.StringId);
            }
            return result;
        }
        public static bool TryAddCareerChoice(CareerChoiceObject choice)
        {
            HeroExtendedInfo info = PlayerCareerInfo;
            if (info != null && !info.CareerChoices.Contains(choice.StringId))
            {
                int maxChoices = Math.Min(Hero.MainHero.Level + 1, MaximumNumberOfCareerPerkPoints + 1);
                //@TODO change this
                maxChoices = 100;
                if (info.CareerChoices.Count < maxChoices)
                {
                    info.CareerChoices.Add(choice.StringId);
                    return true;
                }
            }
            return false;
        }
        public static bool TryRemoveCareerChoice(CareerChoiceObject choice)
        {
            HeroExtendedInfo info = PlayerCareerInfo;
            if (info != null)
            {
                if (info.CareerChoices.Contains(choice.StringId))
                {
                    info.CareerChoices.Remove(choice.StringId);
                    return true;
                }
            }
            return false;
        }
        public static bool HasUnlockedCareerChoiceTier(int tier)
        {
            var tierText = "CareerTier";
            if (HasAnyCareer() && HasAttribute(tierText + tier)) return true;

            return false;
        }
        public static bool HasAttribute(string attribute)
        {
            if (PlayerCareerInfo != null)
            {
                return PlayerCareerInfo.AllAttributes.Contains(attribute);
            }
            else return false;
        }
        public static List<string> GetAllCareerChoices()
        {
            if (!HasAnyCareer())
                return new();
            return PlayerCareerInfo.CareerChoices;
        }
    }

    [Flags]
    public enum AttackTypeMask
    {
        Ranged = 1,
        Melee = 2,
        Alchemy = 3,
        Spell = 4,
        All = Ranged | Melee | Alchemy | Spell
    }

    [Serializable]
    public class DamageProportionTuple
    {
        [XmlAttribute]
        public DamageType DamageType = DamageType.Invalid;
        [XmlAttribute]
        public float Percent = 1;
        public DamageProportionTuple()
        {
        }
        public DamageProportionTuple(DamageType damageType, float percent)
        {
            DamageType = damageType;
            Percent = percent;
        }
    }
    public enum DamageType
    {
        Invalid,
        PhysicalMelee,
        PhysicalRanged,
        Magical,
        Alchemical,
        All
    }
    public enum PropertyMask : int
    {
        Attack = 0,
        Defense = 1,
        All = 2
    }
}

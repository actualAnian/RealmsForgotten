using RealmsForgotten.Career.CareerPointsSystem;
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
        public static AbstractPointsSystem PointsSystem { get { return RFCareerCampaignBehavior.PointsSystem; } }
        public static PlayerClassInfo PlayerCareerInfo { get { return RFCareerCampaignBehavior.ClassInfo; } set { RFCareerCampaignBehavior.ClassInfo = value; } }
        public static bool HasAnyCareer() => Game.Current.GameType is Campaign && GetCareer() != null;
        public static CareerObject? GetCareer()
        {
            CareerObject? result = null;
            if (PlayerCareerInfo != null && !string.IsNullOrEmpty(PlayerCareerInfo.CareerID))
            {
                result = RFCareers.All.FirstOrDefault(x => x.StringId == PlayerCareerInfo.CareerID);
            }
            return result;
        }
        public static void AddCareer(CareerObject career)
        {
            PlayerCareerInfo ??= new();
            if (HasAnyCareer())
                PlayerCareerInfo.CareerChoices.Clear();
            PlayerCareerInfo.CareerID = career.StringId;
            var careerObj = RFCareerChoices.Instance.GetCareerChoices(GetCareer());
            RFCareerCampaignBehavior.CreatePointsSystem(career.pointsSystem);
            careerObj.InitialCareerSetup();
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
            if (PlayerCareerInfo != null && !PlayerCareerInfo.CareerChoices.Contains(choice.StringId))
            {
                PlayerCareerInfo.CareerChoices.Add(choice.StringId);
                return true;
            }
            return false;
        }
        public static bool TryRemoveCareerChoice(CareerChoiceObject choice)
        {
            if (PlayerCareerInfo != null)
            {
                if (PlayerCareerInfo.CareerChoices.Contains(choice.StringId))
                {
                    PlayerCareerInfo.CareerChoices.Remove(choice.StringId);
                    return true;
                }
            }
            return false;
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

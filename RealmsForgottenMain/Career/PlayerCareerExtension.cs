using RealmsForgotten.Career.CareerPointsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace RealmsForgotten.Career
{
    public static class PlayerCareerExtension
    {
        public static AbstractPointsSystem? PointsSystem { get { return RFCareerCampaignBehavior.Instance.PointsSystem; } }
        public static PlayerClassInfo PlayerCareerInfo { get { return RFCareerCampaignBehavior.Instance.ClassInfo; } set { RFCareerCampaignBehavior.Instance.ClassInfo = value; } }
        public static bool HasAnyCareer() => Game.Current.GameType is Campaign && GetCareer() != null;
        public static CareerObject? GetCareer()
        {
            CareerObject? result = null;
            if (PlayerCareerInfo != null && !string.IsNullOrEmpty(PlayerCareerInfo.CareerID))
                result = RFCareers.All.FirstOrDefault(x => x.StringId == PlayerCareerInfo.CareerID);
            return result;
        }
        public static void AddCareer(CareerObject career)
        {
            PlayerCareerInfo ??= new();
            if (HasAnyCareer())
                PlayerCareerInfo.CareerChoices.Clear();
            PlayerCareerInfo.CareerID = career.StringId;
            CareerTypes.RFCareerChoicesBase careerObj = RFCareerChoices.Instance.GetCareerChoices(GetCareer()!);
            RFCareerCampaignBehavior.Instance.CreatePointsSystem(career.pointsSystem);
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

    [Serializable]
    public class DamageProportionTuple
    {
        public List<WeaponClass>? WeaponClasses;
        public DamageType DamageType = DamageType.Invalid;
        public float Percent = 1;
        public DamageProportionTuple(DamageType damageType, float percent)
        {
            DamageType = damageType;
            Percent = percent;
        }
        public DamageProportionTuple(List<WeaponClass> wClasses, float percent)
        {
            Percent = percent;
            WeaponClasses = wClasses;
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

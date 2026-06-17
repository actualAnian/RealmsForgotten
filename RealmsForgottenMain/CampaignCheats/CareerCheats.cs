using RealmsForgotten.Career;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace RealmsForgotten.CampaignCheats
{
    public static class CareerCheats
    {
        const string _careerPrefix = "rf.career";
        [CommandLineFunctionality.CommandLineArgumentFunction("start_career", _careerPrefix)]
        public static string StartCareerCheat(List<string> strings)
        {
            string overrideCareerText = "";
            var possibleCareers = string.Join(", ", RFCareers.All.Select(c => c.StringId));
            if (PlayerCareerExtension.GetCareer() != null)
                overrideCareerText = "overriding your existing career\n";
            if (strings.Count != 1)
                return $"Invalid number of arguments. Usage: start_career <career_id>. Possible careers: {possibleCareers}";
            var careerId = strings[0].ToLower();
            switch (careerId)
            {
                case "mercenary":
                    PlayerCareerExtension.AddCareer(RFCareers.Mercenary);
                    return overrideCareerText + "Started career: Mercenary";
                case "knight":
                    PlayerCareerExtension.AddCareer(RFCareers.Knight);
                    return overrideCareerText + "Started career: Knight";
                case "wizard":
                    PlayerCareerExtension.AddCareer(RFCareers.Wizard);
                    return overrideCareerText + "Started career: Wizard";
                default:
                    return $"Invalid career id: {careerId}. Possible careers: {possibleCareers}";
            }
        }
        [CommandLineFunctionality.CommandLineArgumentFunction("add_career_points", _careerPrefix)]
        public static string AddCareerPointsCheat(List<string> strings)
        {
            if (PlayerCareerExtension.GetCareer() == null)
                return "You do not have a career active.";
            if (strings.Count != 1 || !int.TryParse(strings[0], out int pointsToAdd))
                return "Invalid arguments. Usage: add_career_points <amount>";
            PlayerCareerExtension.PointsSystem!.AddPoints(pointsToAdd);
            return $"Added {pointsToAdd} points to your career. Available points: {PlayerCareerExtension.PointsSystem.AvailablePoints}";
        }
    }
}
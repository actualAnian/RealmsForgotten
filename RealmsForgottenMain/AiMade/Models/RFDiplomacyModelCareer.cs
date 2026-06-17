using RealmsForgotten.Career;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem;

namespace RealmsForgotten.AiMade.Models
{
    public class RFDiplomacyModel : DefaultDiplomacyModel
    {
        // bônus de chance (aditivo ou multiplicador); ajuste a gosto
        private const float JoinScoreAdd = 60f;        // soma no score para o jogador aceitar/entrar
        private const float KingdomHireMul = 1.5f;     // multiplica score do reino contratar o player
        private const float LeavePenaltyMul = 0.75f;   // opcional: reduz incentivo a te “tirar” de um reino

        private static bool IsPlayerMercenary()
        {
            return PlayerCareerExtension.HasAnyCareer() &&
                   PlayerCareerExtension.GetAllCareerChoices()
                       .Contains(RFCareers.Mercenary.StringId);
        }

        public override float GetScoreOfKingdomToHireMercenary(Kingdom kingdom, Clan mercenaryClan)
        {
            var baseScore = base.GetScoreOfKingdomToHireMercenary(kingdom, mercenaryClan);

            // Só buffa se o clã é o do jogador e ele está na carreira mercenária
            if (mercenaryClan == Clan.PlayerClan && IsPlayerMercenary())
                baseScore *= KingdomHireMul;

            return baseScore;
        }

        public override float GetScoreOfMercenaryToJoinKingdom(Clan mercenaryClan, Kingdom targetKingdom)
        {
            var baseScore = base.GetScoreOfMercenaryToJoinKingdom(mercenaryClan, targetKingdom);

            if (mercenaryClan == Clan.PlayerClan && IsPlayerMercenary())
                baseScore += JoinScoreAdd; // empurra barter para se materializar

            return baseScore;
        }

        public override float GetScoreOfMercenaryToLeaveKingdom(Clan mercenaryClan, Kingdom currentKingdom)
        {
            var baseScore = base.GetScoreOfMercenaryToLeaveKingdom(mercenaryClan, currentKingdom);

            if (mercenaryClan == Clan.PlayerClan && IsPlayerMercenary())
                baseScore *= LeavePenaltyMul; // opcional: evita ficar pingando “saia do reino”

            return baseScore;
        }
    }
}

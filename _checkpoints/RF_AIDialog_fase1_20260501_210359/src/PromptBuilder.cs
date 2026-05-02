using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace RF_AIDialog
{
    /// <summary>
    /// Constrói o system prompt enviado ao LLM com o contexto do NPC.
    /// Todo acesso a propriedades é feito com null-safety — nunca deve jogar exceção.
    /// </summary>
    public static class PromptBuilder
    {
        public static string Build(Hero npc)
        {
            try
            {
                return BuildInternal(npc);
            }
            catch (Exception ex)
            {
                // Fallback seguro — se algo falhar no contexto, ainda enviamos um prompt básico
                return $"Você é um lord medieval chamado {npc?.Name.ToString() ?? "desconhecido"} no mundo de Calradia. " +
                       $"Responda em caráter, de forma breve (2-4 frases). Erro ao carregar contexto: {ex.Message}";
            }
        }

        private static string BuildInternal(Hero npc)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Você é um personagem medieval no mundo de Calradia, num jogo chamado Mount & Blade II: Bannerlord.");
            sb.AppendLine("Nunca quebre o personagem. Nunca mencione que é uma IA.");
            sb.AppendLine();

            // ── Identidade ────────────────────────────────────────────────
            sb.AppendLine($"Seu nome é {npc.Name}.");

            if (npc.Clan != null)
                sb.AppendLine($"Você pertence ao clã {npc.Clan.Name}.");

            if (npc.MapFaction != null)
                sb.AppendLine($"Você serve ao reino de {npc.MapFaction.Name}.");

            sb.AppendLine(npc.IsFemale ? "Você é uma mulher de posição elevada." : "Você é um homem de posição elevada.");

            if (npc.Clan?.Leader != null && npc.Clan.Leader == npc)
                sb.AppendLine("Você é o líder do seu clã.");
            else if (npc.IsKingdomLeader)
                sb.AppendLine("Você é o monarca do seu reino.");

            // ── Personalidade ─────────────────────────────────────────────
            var traits = new StringBuilder();
            AppendTrait(traits, npc, DefaultTraits.Valor,      "corajoso",       "cauteloso");
            AppendTrait(traits, npc, DefaultTraits.Mercy,      "misericordioso", "impiedoso");
            AppendTrait(traits, npc, DefaultTraits.Honor,      "honrado",        "desonesto");
            AppendTrait(traits, npc, DefaultTraits.Generosity, "generoso",       "avarento");

            if (traits.Length > 0)
                sb.AppendLine($"Sua personalidade é: {traits.ToString().TrimEnd(',', ' ')}.");

            // ── Relação com o player ──────────────────────────────────────
            var player = Hero.MainHero;
            if (player != null)
            {
                int relation = (int)npc.GetRelationWithPlayer();
                string relDesc = relation > 20  ? "aliado próximo"    :
                                 relation > 0   ? "conhecido amistoso":
                                 relation == 0  ? "desconhecido"      :
                                 relation > -20 ? "desconfiado"       : "inimigo declarado";

                sb.AppendLine($"Você está conversando com {player.Name}, que você considera um {relDesc} (relação: {relation}).");

                if (player.Clan != null)
                    sb.AppendLine($"{player.Name} pertence ao clã {player.Clan.Name}.");
            }

            // ── Situação atual ────────────────────────────────────────────
            if (npc.CurrentSettlement != null)
                sb.AppendLine($"No momento você está em {npc.CurrentSettlement.Name}.");
            else if (npc.PartyBelongedTo != null)
                sb.AppendLine("No momento você está em marcha com seu exército.");

            if (npc.IsWounded)
                sb.AppendLine("Você está ferido e isso pesa em sua disposição.");

            string wealth = npc.Gold > 50000 ? "muito rico"           :
                            npc.Gold > 10000 ? "próspero"             :
                            npc.Gold > 2000  ? "com recursos modestos": "com poucos recursos";
            sb.AppendLine($"Financeiramente você está {wealth}.");

            // ── Instrução de formato JSON ─────────────────────────────────
            sb.AppendLine();
            sb.AppendLine("INSTRUÇÃO DE RESPOSTA:");
            sb.AppendLine("Responda APENAS com um objeto JSON válido, sem nenhum texto fora dele, sem blocos de markdown.");
            sb.AppendLine("Use exatamente este formato:");
            sb.AppendLine("{");
            sb.AppendLine("  \"internal_thoughts\": \"seus pensamentos internos sobre a situação, em 1-2 frases\",");
            sb.AppendLine("  \"response\": \"o que você diz em voz alta, em caráter, brevemente (2-4 frases)\",");
            sb.AppendLine("  \"tone\": \"um de: friendly, neutral, suspicious, hostile, fearful\"");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void AppendTrait(StringBuilder sb, Hero npc, TraitObject trait, string positive, string negative)
        {
            int level = npc.GetTraitLevel(trait);
            if (level > 0) sb.Append($"{positive}, ");
            else if (level < 0) sb.Append($"{negative}, ");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using RealmsForgotten.SaveShield;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace RealmsForgotten.CampaignCheats
{
    /// <summary>
    /// Comandos de console para validar em SEGUNDOS o que levaria dias de campanha
    /// (portes do estudo TAOM, 2026-08-18):
    ///
    ///   rf.debug.test_offspring [girl]  — força um nascimento inter-racial e reporta as raças
    ///   rf.debug.test_saveshield        — confere os patches do SaveShield e dispara o diálogo
    ///
    /// O pastor do RF_LivingWorld já tinha comando: "rf.living spawn herder cow" (nasce
    /// na posição do jogador).
    /// </summary>
    public static class PortTestCheats
    {
        /// <summary>
        /// Acha (ou improvisa) um casal de raças diferentes e chama o fluxo REAL de
        /// nascimento (HeroCreator.DeliverOffSpring), que passa pelo RFHeroCreationModel.
        /// Cria um herói de verdade no save — usar em save de teste.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("test_offspring", "rf.debug")]
        public static string TestOffspring(List<string> arguments)
        {
            if (Campaign.Current == null)
            {
                return "rf.debug.test_offspring: campanha não está rodando.";
            }

            bool wantGirl = arguments.Count > 0 && arguments[0].Equals("girl", StringComparison.OrdinalIgnoreCase);

            // 1º: casal casado de raças diferentes; 2º: qualquer par lorde/lady vivo de raças diferentes.
            Hero mother = null;
            Hero father = null;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero.IsFemale && hero.Spouse != null && hero.Spouse.IsAlive
                    && hero.CharacterObject != null && hero.Spouse.CharacterObject != null
                    && hero.CharacterObject.Race != hero.Spouse.CharacterObject.Race)
                {
                    mother = hero;
                    father = hero.Spouse;
                    break;
                }
            }

            if (mother == null)
            {
                List<Hero> lords = Hero.AllAliveHeroes.Where(h => h.IsLord && h.CharacterObject != null).ToList();
                Hero lady = lords.FirstOrDefault(h => h.IsFemale);
                if (lady != null)
                {
                    father = lords.FirstOrDefault(h => !h.IsFemale && h.CharacterObject.Race != lady.CharacterObject.Race);
                    mother = father != null ? lady : null;
                }
            }

            if (mother == null || father == null)
            {
                return "Nenhum par de raças diferentes encontrado entre os heróis vivos.";
            }

            string modelName = Campaign.Current.Models.HeroCreationModel?.GetType().Name ?? "(null)";

            Hero child;
            try
            {
                child = HeroCreator.DeliverOffSpring(mother, father, wantGirl);
            }
            catch (Exception e)
            {
                return $"DeliverOffSpring FALHOU ({e.GetType().Name}: {e.Message}). Model ativo: {modelName}.";
            }

            if (child?.CharacterObject == null)
            {
                return "DeliverOffSpring devolveu null.";
            }

            int expectedRace = wantGirl ? mother.CharacterObject.Race : father.CharacterObject.Race;
            bool raceOk = child.CharacterObject.Race == expectedRace;

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Teste de offspring inter-racial ===");
            report.AppendLine($"Model ativo: {modelName} (esperado: RFHeroCreationModel)");
            report.AppendLine($"Mãe: {mother.Name} (raça {mother.CharacterObject.Race}) × Pai: {father.Name} (raça {father.CharacterObject.Race})");
            report.AppendLine($"Filh{(wantGirl ? "a" : "o")}: {child.Name} — raça {child.CharacterObject.Race} " +
                              $"[{(raceOk ? "OK: raça do pai do mesmo sexo" : "ERRADO: esperava " + expectedRace)}]");
            report.AppendLine("Abra a enciclopédia do recém-nascido e confira o corpo/rosto (o bug antigo gerava no espaço facial da mãe).");
            report.AppendLine("ATENÇÃO: o herói criado é real — use um save de teste.");
            return report.ToString();
        }

        /// <summary>
        /// Confere se os 3 finalizers do SaveShield estão aplicados no SaveManager e dispara
        /// o pipeline de report com uma exceção de mentira (mostra o diálogo e escreve o log)
        /// — sem tocar em nenhum save de verdade.
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("test_saveshield", "rf.debug")]
        public static string TestSaveShield(List<string> arguments)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Teste do SaveShield ===");

            string[] targets = { "Save", "Load", "CheckSaveableTypes" };
            foreach (string methodName in targets)
            {
                var method = AccessTools.GetDeclaredMethods(typeof(SaveManager))
                    .FirstOrDefault(m => m.Name == methodName);
                var info = method != null ? Harmony.GetPatchInfo(method) : null;
                int finalizers = info?.Finalizers?.Count(f => f.PatchMethod?.DeclaringType?.Namespace == "RealmsForgotten.SaveShield") ?? 0;
                report.AppendLine($"SaveManager.{methodName}: {(finalizers == 1 ? "OK (1 finalizer nosso)" : finalizers + " finalizers nossos — ESPERAVA 1")}");
            }

            try
            {
                // throw/catch para a exceção ganhar stack de verdade — a atribuição de
                // culpado caminha a stack e deve apontar o RealmsForgotten (nós mesmos).
                throw new ArgumentException("An item with the same key has already been added (TESTE do SaveShield — ignorar)");
            }
            catch (ArgumentException fake)
            {
                SaveFailureReporter.Instance.Report(fake, "Teste do SaveShield");
            }

            report.AppendLine("Diálogo disparado: deve nomear 'RealmsForgotten' como origem e citar conflito de save ID.");
            report.AppendLine("Log em Documents\\...\\Configs\\ModLogs\\RF_SaveShield.log.");
            return report.ToString();
        }

        /// <summary>Salto de capítulo da quest principal (ferramenta de filmagem; ver
        /// RFQuestChapterJumper — mesmo gate de dev-flag). Uso: rf.quest.jump 4</summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("jump", "rf.quest")]
        public static string QuestJump(List<string> arguments)
        {
            if (arguments.Count == 0 || !int.TryParse(arguments[0], out int oneBased))
            {
                var sb = new StringBuilder("Uso: rf.quest.jump <numero>\n");
                for (int i = 0; i < Quest.RFQuestChapterJumper.ChapterNames.Length; i++)
                    sb.AppendLine($"  {i + 1} = {Quest.RFQuestChapterJumper.ChapterNames[i]}");
                return sb.ToString();
            }
            return Quest.RFQuestChapterJumper.JumpTo(oneBased - 1);
        }

        /// <summary>
        /// Raio-X dos papéis de herói de quest: para cada papel, mostra o herói original
        /// (vivo? desativado? preso? onde?) e para quem o Resolve aponta hoje. Serve para
        /// diagnosticar em segundos "por que o diálogo X não abre".
        /// </summary>
        [CommandLineFunctionality.CommandLineArgumentFunction("quest_heroes", "rf.debug")]
        public static string QuestHeroesReport(List<string> arguments)
        {
            if (Campaign.Current == null)
            {
                return "rf.debug.quest_heroes: campanha não está rodando.";
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== Papéis de herói de quest ===");

            foreach (var role in Quest.QuestHeroes.AllRoles)
            {
                Hero original = Hero.FindFirst(h => h.StringId == role.OriginalHeroId);
                Hero resolved = Quest.QuestHeroes.Resolve(role.RoleId);

                string originalState = original == null
                    ? "NÃO EXISTE no save"
                    : $"{original.Name} — {(original.IsAlive ? "vivo" : "MORTO")}" +
                      $"{(original.IsDisabled ? ", desativado" : "")}" +
                      $"{(original.IsPrisoner ? ", prisioneiro" : "")}" +
                      $"{(original.CurrentSettlement != null ? ", em " + original.CurrentSettlement.Name : "")}" +
                      $"{(original.PartyBelongedTo != null ? ", na party " + original.PartyBelongedTo.Name : "")}";

                report.AppendLine($"[{role.RoleId}] original '{role.OriginalHeroId}': {originalState}");
                report.AppendLine($"    Resolve -> {(resolved != null ? resolved.Name + " (" + resolved.StringId + ")" : "NULL — diálogos deste papel não abrem!")}");
            }

            return report.ToString();
        }
    }
}

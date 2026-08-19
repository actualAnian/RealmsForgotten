using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Library;

namespace RealmsForgotten.SaveShield
{
    /// <summary>
    /// Reporta uma falha de save/load com o provável culpado nomeado, e avisa o jogador.
    ///
    /// INVERSÃO DE SEGURANÇA DELIBERADA (herdada do SaveShield do LOTRAOM, código MIT)
    /// Engolir a exceção seria certo para uma falha de render ou de tick, e errado aqui:
    /// engolir um *save* falho faz o jogador seguir jogando acreditando que o progresso foi
    /// escrito, para descobrir horas depois, no reload, que não foi. Perda de dados
    /// silenciosa é pior que um crash visível.
    ///
    /// Então este escudo não engole nada. Ele atribui, loga, põe um diálogo na frente do
    /// jogador, e deixa a exceção seguir o caminho normal. O valor agregado é a atribuição
    /// e o aviso — não a supressão.
    /// </summary>
    public sealed class SaveFailureReporter
    {
        public static readonly SaveFailureReporter Instance = new SaveFailureReporter();

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord", "Configs", "ModLogs", "RF_SaveShield.log");

        private readonly object _gate = new object();

        /// <summary>
        /// Toda assinatura já reportada nesta sessão (não só a última): duas falhas alternadas
        /// A, B, A não podem re-reportar A — é exatamente o que o autosave em timer produz.
        /// </summary>
        private readonly HashSet<string> _reported = new HashSet<string>(StringComparer.Ordinal);

        private SaveFailureReporter()
        {
        }

        public void Report(Exception exception, string operation)
        {
            try
            {
                string culprit = ModCulpritAttributor.FindLikelyCulprit(exception);
                string signature = operation + "|" + exception.GetType().FullName + "|" + (culprit ?? "-");

                // O autosave tenta de novo em timer; sem esta guarda o mesmo diálogo subiria a
                // cada poucos minutos pelo resto da sessão.
                lock (_gate)
                {
                    if (!_reported.Add(signature))
                    {
                        WriteLog($"{operation} falhou de novo ({exception.GetType().Name}); já reportado nesta sessão.");
                        return;
                    }
                }

                WriteLog(BuildDiagnosis(operation, exception, culprit));
                WriteLog(exception.ToString());

                Notify(operation, culprit, IsDuplicateDefiner(exception));
            }
            catch (Exception reporterFailure)
            {
                // Nunca somar uma segunda falha a um save que está falhando.
                try { WriteLog("SaveFailureReporter.Report falhou: " + reporterFailure.Message); } catch { }
            }
        }

        private static string BuildDiagnosis(string operation, Exception exception, string culprit)
        {
            string diagnosis = $"{operation} FALHOU: {exception.GetType().Name}. ";

            if (IsDuplicateDefiner(exception))
            {
                diagnosis +=
                    "Definição de tipo de save duplicada: dois mods registraram o mesmo save ID, " +
                    "então o jogo não consegue montar o esquema do save. É conflito entre mods, " +
                    "não save corrompido. ";
            }

            diagnosis += culprit == null
                ? "Nenhum assembly de mod apareceu na stack — parece falha da própria engine."
                : $"O primeiro assembly de mod na stack é '{culprit}' — comece por ele.";

            return diagnosis;
        }

        /// <summary>
        /// A assinatura de dois mods reivindicando o mesmo save ID. Vale nomear explicitamente:
        /// a mensagem crua ("An item with the same key has already been added") parece bug
        /// genérico de dicionário e manda o jogador procurar no lugar errado.
        /// </summary>
        private static bool IsDuplicateDefiner(Exception exception)
        {
            try
            {
                return exception is ArgumentException
                       && exception.Message.IndexOf("same key", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private void Notify(string operation, string culprit, bool duplicateDefiner)
        {
            try
            {
                string body = duplicateDefiner
                    ? "Dois mods registraram o mesmo save ID, então o jogo não consegue salvar nem carregar.\n\n" +
                      $"O mod envolvido parece ser: {culprit ?? "não foi possível determinar"}.\n\n" +
                      "Isso é um conflito de mods, não um save danificado."
                    : $"{operation} não foi concluído.\n\n" +
                      $"Origem provável: {culprit ?? "não foi possível determinar"}.\n\n" +
                      "Não confie em nenhum save feito depois desta mensagem. Detalhes em " +
                      "Documents\\Mount and Blade II Bannerlord\\Configs\\ModLogs\\RF_SaveShield.log.";

                InformationManager.ShowInquiry(
                    new InquiryData(
                        "Realms Forgotten — problema no save",
                        body,
                        isAffirmativeOptionShown: true,
                        isNegativeOptionShown: false,
                        affirmativeText: "Entendi",
                        negativeText: string.Empty,
                        affirmativeAction: () => { },
                        negativeAction: () => { }),
                    pauseGameActiveState: false);
            }
            catch (Exception notifyFailure)
            {
                // A UI pode não existir (falha durante o load). O log já carrega tudo.
                try { WriteLog("SaveFailureReporter.Notify falhou: " + notifyFailure.Message); } catch { }
            }
        }

        private static void WriteLog(string message)
        {
            try
            {
                string directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SaveShield] {message}{Environment.NewLine}");
            }
            catch
            {
                // Sem disco, sem log — mas o diálogo ainda sobe.
            }
        }
    }
}

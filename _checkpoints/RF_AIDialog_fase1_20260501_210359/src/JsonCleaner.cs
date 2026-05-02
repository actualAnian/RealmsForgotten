using System.Text.RegularExpressions;

namespace RF_AIDialog
{
    /// <summary>
    /// Limpa a saída do LLM antes de parsear como JSON.
    /// Modelos frequentemente envolvem o JSON em blocos markdown (```json ... ```)
    /// ou adicionam texto antes/depois — isso remove essas interferências.
    /// </summary>
    public static class JsonCleaner
    {
        private static readonly Regex _codeBlock =
            new Regex(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Compiled);

        /// <summary>
        /// Extrai o JSON limpo da saída bruta do LLM.
        /// Retorna null se não conseguir encontrar um objeto JSON válido.
        /// </summary>
        public static string? ExtractJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            // 1. Tenta extrair de bloco markdown ```json ... ```
            var match = _codeBlock.Match(raw);
            if (match.Success)
                raw = match.Groups[1].Value.Trim();

            // 2. Encontra o primeiro { e o último } para isolar o objeto JSON
            int start = raw.IndexOf('{');
            int end   = raw.LastIndexOf('}');

            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);

            return null;
        }
    }
}

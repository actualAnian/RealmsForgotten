using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    // ─────────────────────────────────────────────
    //  Modelos de request / response do Ollama
    // ─────────────────────────────────────────────

    public class OllamaMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "";

        [JsonProperty("content")]
        public string Content { get; set; } = "";
    }

    public class OllamaOptions
    {
        [JsonProperty("num_predict")]
        public int? NumPredict { get; set; }

        [JsonProperty("temperature")]
        public double? Temperature { get; set; }

        [JsonProperty("num_ctx")]
        public int? NumCtx { get; set; }
    }

    public class OllamaRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        [JsonProperty("messages")]
        public OllamaMessage[] Messages { get; set; } = Array.Empty<OllamaMessage>();

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;

        [JsonProperty("options")]
        public OllamaOptions? Options { get; set; }
    }

    public class OllamaResponse
    {
        [JsonProperty("message")]
        public OllamaMessage? Message { get; set; }
    }

    // ─────────────────────────────────────────────
    //  Cliente HTTP para o Ollama
    // ─────────────────────────────────────────────

    public static class AIClient
    {
        // HttpClient DEVE ser estático — nunca instanciar um por request
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(AIConfig.TimeoutSeconds)
        };

        private const string OllamaEndpoint = AIConfig.OllamaEndpoint;

        /// <summary>
        /// Envia uma mensagem ao Ollama e retorna a resposta como string.
        /// Roda em background thread — NÃO chame InformationManager daqui.
        /// </summary>
        public static async Task<string> AskAsync(
            string model,
            string systemPrompt,
            string userMessage)
        {
            var request = new OllamaRequest
            {
                Model  = model,
                Stream = false,
                Messages = new[]
                {
                    new OllamaMessage { Role = "system", Content = systemPrompt },
                    new OllamaMessage { Role = "user",   Content = userMessage  }
                },
                Options = new OllamaOptions
                {
                    NumPredict  = AIConfig.MaxTokens,
                    Temperature = AIConfig.Temperature,
                    NumCtx      = AIConfig.ContextSize
                }
            };

            string requestJson = JsonConvert.SerializeObject(request);

            // DEBUG — grava o request JSON num arquivo para inspeção
            try
            {
                string debugPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                    "rf_ai_debug.json");
                System.IO.File.WriteAllText(debugPath, requestJson, Encoding.UTF8);
            }
            catch { /* não quebre por causa do debug */ }

            // UTF8Encoding(false) = sem BOM — Encoding.UTF8 no .NET Framework inclui BOM e quebra o Ollama
            var content = new StringContent(requestJson, new UTF8Encoding(false), "application/json");

            // CancellationToken garante timeout real independente do HttpClient
            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse = await _http.PostAsync(OllamaEndpoint, content, cts.Token).ConfigureAwait(false);

            string responseJson = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorSnippet = responseJson.Length > 300
                    ? responseJson.Substring(0, 300)
                    : responseJson;
                throw new Exception($"HTTP {(int)httpResponse.StatusCode} - {errorSnippet}");
            }
            OllamaResponse? parsed = JsonConvert.DeserializeObject<OllamaResponse>(responseJson);

            return parsed?.Message?.Content ?? "[sem resposta]";
        }
    }
}

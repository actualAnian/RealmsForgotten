using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    // ─────────────────────────────────────────────
    //  Request / response models for Ollama
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
    //  HTTP client for Ollama
    // ─────────────────────────────────────────────

    public static class AIClient
    {
        // HttpClient must be static — never instantiate per request
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(AIConfig.TimeoutSeconds)
        };

        private const string OllamaEndpoint = AIConfig.OllamaEndpoint;

        // Few-shot barter example injected as an assistant message.
        // Small models follow role examples far more reliably than written rules.
        private const string FewShotBarterUser =
            "ok deal, 3 grain for 1 wine, take it";

        private const string FewShotBarterAssistant =
            "{\"internal_thoughts\":\"Fair exchange. Three grain for one wine.\","
            + "\"response\":\"Agreed. Hand over the grain and take your wine.\","
            + "\"tone\":\"neutral\","
            + "\"actions\":[{\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":3},"
            + "{\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":1}]}";

        /// <summary>
        /// Sends a message to Ollama and returns the raw response string.
        /// Runs on a background thread — do NOT call InformationManager from here.
        /// </summary>
        public static async Task<string> AskAsync(
            string model,
            string systemPrompt,
            string userMessage,
            int maxTokens = 300)
        {
            var request = new OllamaRequest
            {
                Model  = model,
                Stream = false,
                Messages = new[]
                {
                    new OllamaMessage { Role = "system",    Content = systemPrompt },
                    // Few-shot: shows the model exactly what a confirmed barter looks like.
                    new OllamaMessage { Role = "user",      Content = FewShotBarterUser },
                    new OllamaMessage { Role = "assistant", Content = FewShotBarterAssistant },
                    new OllamaMessage { Role = "user",      Content = userMessage }
                },
                Options = new OllamaOptions
                {
                    NumPredict  = maxTokens,
                    Temperature = AIConfig.Temperature,
                    NumCtx      = AIConfig.ContextSize
                }
            };

            string requestJson = JsonConvert.SerializeObject(request);

            // DEBUG — write request JSON to desktop for inspection
            try
            {
                string debugPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                    "rf_ai_debug.json");
                System.IO.File.WriteAllText(debugPath, requestJson, Encoding.UTF8);
            }
            catch { /* do not crash on debug write failure */ }

            // UTF8Encoding(false) = no BOM — Encoding.UTF8 in .NET Framework includes BOM which breaks Ollama
            var content = new StringContent(requestJson, new UTF8Encoding(false), "application/json");

            // CancellationToken ensures real timeout independent of HttpClient
            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse = await _http.PostAsync(OllamaEndpoint, content, cts.Token)
                .ConfigureAwait(false);

            string responseJson = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorSnippet = responseJson.Length > 300
                    ? responseJson.Substring(0, 300)
                    : responseJson;
                throw new Exception($"HTTP {(int)httpResponse.StatusCode} - {errorSnippet}");
            }

            OllamaResponse? parsed = JsonConvert.DeserializeObject<OllamaResponse>(responseJson);
            string result = parsed?.Message?.Content ?? "(no response)";

            // DEBUG — write raw LLM response to desktop for inspection
            try
            {
                string debugPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
                    "rf_ai_response.txt");
                System.IO.File.WriteAllText(debugPath, result, Encoding.UTF8);
            }
            catch { }

            return result;
        }
    }
}

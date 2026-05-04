using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    // ─────────────────────────────────────────────
    //  Shared message model (role + content)
    // ─────────────────────────────────────────────

    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "";

        [JsonProperty("content")]
        public string Content { get; set; } = "";
    }

    // ─────────────────────────────────────────────
    //  Ollama-specific models
    // ─────────────────────────────────────────────

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
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;

        [JsonProperty("options")]
        public OllamaOptions? Options { get; set; }
    }

    public class OllamaResponse
    {
        [JsonProperty("message")]
        public ChatMessage? Message { get; set; }
    }

    // ─────────────────────────────────────────────
    //  OpenAI-compatible models (DeepSeek, OpenAI)
    // ─────────────────────────────────────────────

    public class OpenAIRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        [JsonProperty("messages")]
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("stream")]
        public bool Stream { get; set; } = false;
    }

    public class OpenAIChoice
    {
        [JsonProperty("message")]
        public ChatMessage? Message { get; set; }
    }

    public class OpenAIResponse
    {
        [JsonProperty("choices")]
        public OpenAIChoice[]? Choices { get; set; }
    }

    // ─────────────────────────────────────────────
    //  Unified HTTP client
    // ─────────────────────────────────────────────

    public static class AIClient
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(AIConfig.TimeoutSeconds)
        };

        // Few-shot barter example — small models follow role examples far more
        // reliably than written rules. Kept even for remote APIs (no cost impact).
        private const string FewShotBarterUser =
            "ok deal, 3 grain for 1 wine, take it";

        private const string FewShotBarterAssistant =
            "{\"internal_thoughts\":\"Fair exchange. Three grain for one wine.\","
            + "\"response\":\"Agreed. Hand over the grain and take your wine.\","
            + "\"tone\":\"neutral\","
            + "\"actions\":[{\"type\":\"take_item\",\"item_id\":\"grain\",\"value\":3},"
            + "{\"type\":\"give_item\",\"item_id\":\"wine\",\"value\":1}]}";

        /// <summary>
        /// Sends a message to the configured backend and returns the raw response string.
        /// Runs on a background thread — do NOT call InformationManager from here.
        /// </summary>
        public static async Task<string> AskAsync(
            string model,
            string systemPrompt,
            string userMessage,
            int maxTokens = 300)
        {
            var messages = new[]
            {
                new ChatMessage { Role = "system",    Content = systemPrompt },
                new ChatMessage { Role = "user",      Content = FewShotBarterUser },
                new ChatMessage { Role = "assistant", Content = FewShotBarterAssistant },
                new ChatMessage { Role = "user",      Content = userMessage }
            };

            return AIConfig.UseRemoteAPI
                ? await AskRemoteAsync(messages, maxTokens).ConfigureAwait(false)
                : await AskOllamaAsync(model, messages, maxTokens).ConfigureAwait(false);
        }

        // ── Remote OpenAI-compatible (DeepSeek, OpenAI, etc.) ─────────────

        private static async Task<string> AskRemoteAsync(ChatMessage[] messages, int maxTokens)
        {
            var request = new OpenAIRequest
            {
                Model       = AIConfig.APIModelName,
                Messages    = messages,
                MaxTokens   = maxTokens,
                Temperature = AIConfig.Temperature,
                Stream      = false
            };

            string requestJson = JsonConvert.SerializeObject(request);
            var content = new StringContent(requestJson, new UTF8Encoding(false), "application/json");

            // Clone the default headers per-request to attach the auth token
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, AIConfig.APIEndpoint)
            {
                Content = content
            };
            reqMsg.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", AIConfig.APIKey);

            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse =
                await _http.SendAsync(reqMsg, cts.Token).ConfigureAwait(false);

            string responseJson = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string snippet = responseJson.Length > 300
                    ? responseJson.Substring(0, 300) : responseJson;
                throw new Exception($"HTTP {(int)httpResponse.StatusCode} - {snippet}");
            }

            OpenAIResponse? parsed = JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
            return parsed?.Choices?[0]?.Message?.Content ?? "(no response)";
        }

        // ── Local Ollama ──────────────────────────────────────────────────

        private static async Task<string> AskOllamaAsync(
            string model, ChatMessage[] messages, int maxTokens)
        {
            var request = new OllamaRequest
            {
                Model    = model,
                Stream   = false,
                Messages = messages,
                Options  = new OllamaOptions
                {
                    NumPredict  = maxTokens,
                    Temperature = AIConfig.Temperature,
                    NumCtx      = AIConfig.ContextSize
                }
            };

            string requestJson = JsonConvert.SerializeObject(request);
            var content = new StringContent(requestJson, new UTF8Encoding(false), "application/json");

            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse =
                await _http.PostAsync(AIConfig.OllamaEndpoint, content, cts.Token)
                    .ConfigureAwait(false);

            string responseJson = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string snippet = responseJson.Length > 300
                    ? responseJson.Substring(0, 300) : responseJson;
                throw new Exception($"HTTP {(int)httpResponse.StatusCode} - {snippet}");
            }

            OllamaResponse? parsed = JsonConvert.DeserializeObject<OllamaResponse>(responseJson);
            return parsed?.Message?.Content ?? "(no response)";
        }
    }
}

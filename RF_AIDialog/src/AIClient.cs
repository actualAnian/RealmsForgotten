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

    public class SpeechRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = "";

        [JsonProperty("input")]
        public string Input { get; set; } = "";

        [JsonProperty("voice")]
        public string Voice { get; set; } = "";

        [JsonProperty("response_format")]
        public string ResponseFormat { get; set; } = "wav";

        [JsonProperty("instructions")]
        public string? Instructions { get; set; }
    }

    public class FishSpeechRequest
    {
        [JsonProperty("text")]
        public string Text { get; set; } = "";

        [JsonProperty("format")]
        public string Format { get; set; } = "mp3";

        [JsonProperty("reference_id")]
        public string? ReferenceId { get; set; }
    }

    public class TranscriptionResponse
    {
        [JsonProperty("text")]
        public string? Text { get; set; }
    }

    public sealed class SpeechSynthesisResult
    {
        public byte[] AudioBytes { get; set; } = Array.Empty<byte>();
        public string RequestedFormat { get; set; } = "wav";
        public string DetectedFormat { get; set; } = "unknown";
        public string ContentType { get; set; } = "";
        public string HeaderPreview { get; set; } = "";
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

        private static readonly string[] SupportedSpeechFormats = { "wav", "mp3", "flac", "opus", "pcm" };

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

        public static async Task<SpeechSynthesisResult> SynthesizeSpeechAsync(string text, string? instructions = null)
        {
            if (!AIConfig.TTSEnabled)
                throw new Exception("TTS is disabled in AI config.");

            string provider = AIConfig.TTSProvider;
            if (provider == "fish")
                return await SynthesizeFishSpeechAsync(text).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(AIConfig.TTSApiKey))
                throw new Exception("TTS API key not configured.");

            string responseFormat = NormalizeSpeechFormat(AIConfig.TTSResponseFormat);
            string mergedInstructions = string.IsNullOrWhiteSpace(instructions)
                ? AIConfig.TTSInstructions
                : (string.IsNullOrWhiteSpace(AIConfig.TTSInstructions)
                    ? instructions
                    : $"{AIConfig.TTSInstructions} {instructions}".Trim());

            var request = new SpeechRequest
            {
                Model = AIConfig.TTSModelName,
                Input = text,
                Voice = AIConfig.TTSVoice,
                ResponseFormat = responseFormat,
                Instructions = string.IsNullOrWhiteSpace(mergedInstructions) ? null : mergedInstructions
            };

            string requestJson = JsonConvert.SerializeObject(request);
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, AIConfig.TTSEndpoint)
            {
                Content = new StringContent(requestJson, new UTF8Encoding(false), "application/json")
            };
            reqMsg.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", AIConfig.TTSApiKey);

            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse =
                await _http.SendAsync(reqMsg, cts.Token).ConfigureAwait(false);

            byte[] audioBytes = await httpResponse.Content.ReadAsByteArrayAsync()
                .ConfigureAwait(false);

            string contentType = httpResponse.Content.Headers.ContentType?.MediaType ?? "";
            string detectedFormat = GuessAudioFormat(audioBytes, contentType, responseFormat);
            string headerPreview = BuildHeaderPreview(audioBytes);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorText = TryDecodeUtf8(audioBytes);
                string snippet = errorText.Length > 300 ? errorText.Substring(0, 300) : errorText;
                throw new Exception($"TTS HTTP {(int)httpResponse.StatusCode} - {snippet}");
            }

            RFAIDebug.Log(
                $"TTS synth ok | status={(int)httpResponse.StatusCode} | requested={responseFormat} | detected={detectedFormat} | contentType={contentType} | bytes={audioBytes.Length} | header={headerPreview}");

            return new SpeechSynthesisResult
            {
                AudioBytes = audioBytes,
                RequestedFormat = responseFormat,
                DetectedFormat = detectedFormat,
                ContentType = contentType,
                HeaderPreview = headerPreview
            };
        }

        private static async Task<SpeechSynthesisResult> SynthesizeFishSpeechAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(AIConfig.FishTTSApiKey))
                throw new Exception("Fish TTS API key not configured.");

            string responseFormat = NormalizeFishSpeechFormat(AIConfig.FishTTSResponseFormat);
            var request = new FishSpeechRequest
            {
                Text = text,
                Format = responseFormat,
                ReferenceId = string.IsNullOrWhiteSpace(AIConfig.FishTTSReferenceId)
                    ? null
                    : AIConfig.FishTTSReferenceId
            };

            string requestJson = JsonConvert.SerializeObject(request);
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, AIConfig.FishTTSEndpoint)
            {
                Content = new StringContent(requestJson, new UTF8Encoding(false), "application/json")
            };
            reqMsg.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", AIConfig.FishTTSApiKey);
            reqMsg.Headers.Add("model", AIConfig.FishTTSModelName);

            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse =
                await _http.SendAsync(reqMsg, cts.Token).ConfigureAwait(false);

            byte[] audioBytes = await httpResponse.Content.ReadAsByteArrayAsync()
                .ConfigureAwait(false);

            string contentType = httpResponse.Content.Headers.ContentType?.MediaType ?? "";
            string detectedFormat = GuessAudioFormat(audioBytes, contentType, responseFormat);
            string headerPreview = BuildHeaderPreview(audioBytes);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorText = TryDecodeUtf8(audioBytes);
                string snippet = errorText.Length > 300 ? errorText.Substring(0, 300) : errorText;
                throw new Exception($"Fish TTS HTTP {(int)httpResponse.StatusCode} - {snippet}");
            }

            RFAIDebug.Log(
                $"Fish TTS synth ok | status={(int)httpResponse.StatusCode} | requested={responseFormat} | detected={detectedFormat} | contentType={contentType} | bytes={audioBytes.Length} | header={headerPreview}");

            return new SpeechSynthesisResult
            {
                AudioBytes = audioBytes,
                RequestedFormat = responseFormat,
                DetectedFormat = detectedFormat,
                ContentType = contentType,
                HeaderPreview = headerPreview
            };
        }

        public static async Task<string> TranscribeSpeechAsync(byte[] wavBytes, string? prompt = null)
        {
            if (!AIConfig.STTEnabled)
                throw new Exception("STT is disabled in AI config.");

            if (string.IsNullOrWhiteSpace(AIConfig.STTApiKey))
                throw new Exception("STT API key not configured.");

            using var form = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(wavBytes ?? Array.Empty<byte>());
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", "rf_voice_input.wav");
            form.Add(new StringContent(AIConfig.STTModelName), "model");
            form.Add(new StringContent("json"), "response_format");

            string mergedPrompt = string.IsNullOrWhiteSpace(prompt)
                ? AIConfig.STTPrompt
                : (string.IsNullOrWhiteSpace(AIConfig.STTPrompt)
                    ? prompt
                    : $"{AIConfig.STTPrompt} {prompt}".Trim());

            if (!string.IsNullOrWhiteSpace(mergedPrompt))
                form.Add(new StringContent(mergedPrompt), "prompt");

            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, AIConfig.STTEndpoint)
            {
                Content = form
            };
            reqMsg.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", AIConfig.STTApiKey);

            using var cts = new System.Threading.CancellationTokenSource(
                TimeSpan.FromSeconds(AIConfig.TimeoutSeconds));

            HttpResponseMessage httpResponse =
                await _http.SendAsync(reqMsg, cts.Token).ConfigureAwait(false);

            string responseText = await httpResponse.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                string snippet = responseText.Length > 300 ? responseText.Substring(0, 300) : responseText;
                throw new Exception($"STT HTTP {(int)httpResponse.StatusCode} - {snippet}");
            }

            var parsed = JsonConvert.DeserializeObject<TranscriptionResponse>(responseText);
            return parsed?.Text?.Trim() ?? "";
        }

        // ── Remote OpenAI-compatible (DeepSeek, OpenAI, etc.) ─────────────

        private static async Task<string> AskRemoteAsync(ChatMessage[] messages, int maxTokens)
        {
            if (string.IsNullOrWhiteSpace(AIConfig.APIKey))
                throw new Exception(
                    "Remote API key not configured. Use RF_AIDIALOG_API_KEY, RF_AIDIALOG_API_KEY_FILE, or ai_config.local.json.");

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

        private static string NormalizeSpeechFormat(string? responseFormat)
        {
            if (string.IsNullOrWhiteSpace(responseFormat))
                return "wav";

            string trimmed = responseFormat.Trim().ToLowerInvariant();
            foreach (string supported in SupportedSpeechFormats)
            {
                if (trimmed == supported)
                    return trimmed;
            }

            return "wav";
        }

        private static string NormalizeFishSpeechFormat(string? responseFormat)
        {
            if (string.IsNullOrWhiteSpace(responseFormat))
                return "mp3";

            string trimmed = responseFormat.Trim().ToLowerInvariant();
            return trimmed == "mp3" ? "mp3" : "mp3";
        }

        private static string TryDecodeUtf8(byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes ?? Array.Empty<byte>());
            }
            catch
            {
                return "(unreadable error body)";
            }
        }

        private static string GuessAudioFormat(byte[] bytes, string? contentType, string requestedFormat)
        {
            if (bytes == null || bytes.Length < 4)
                return requestedFormat;

            if (bytes.Length >= 12
                && bytes[0] == (byte)'R'
                && bytes[1] == (byte)'I'
                && bytes[2] == (byte)'F'
                && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W'
                && bytes[9] == (byte)'A'
                && bytes[10] == (byte)'V'
                && bytes[11] == (byte)'E')
            {
                return "wav";
            }

            if (bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3')
                return "mp3";

            if (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0)
                return "mp3";

            if (bytes[0] == (byte)'f' && bytes[1] == (byte)'L' && bytes[2] == (byte)'a' && bytes[3] == (byte)'C')
                return "flac";

            if (bytes[0] == (byte)'O' && bytes[1] == (byte)'g' && bytes[2] == (byte)'g' && bytes[3] == (byte)'S')
                return "opus";

            string lowerContentType = (contentType ?? string.Empty).ToLowerInvariant();
            if (lowerContentType.Contains("mpeg") || lowerContentType.Contains("mp3"))
                return "mp3";
            if (lowerContentType.Contains("wav") || lowerContentType.Contains("wave"))
                return "wav";
            if (lowerContentType.Contains("flac"))
                return "flac";
            if (lowerContentType.Contains("ogg") || lowerContentType.Contains("opus"))
                return "opus";

            return requestedFormat;
        }

        private static string BuildHeaderPreview(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return "empty";

            int count = Math.Min(bytes.Length, 16);
            var hex = new StringBuilder(count * 3);
            var ascii = new StringBuilder(count);

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    hex.Append(' ');

                byte value = bytes[i];
                hex.Append(value.ToString("X2"));
                ascii.Append(value >= 32 && value <= 126 ? (char)value : '.');
            }

            return $"{hex} | {ascii}";
        }
    }
}

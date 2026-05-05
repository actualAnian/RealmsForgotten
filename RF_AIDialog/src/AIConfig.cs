using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using TaleWorlds.Library;

namespace RF_AIDialog
{
    /// <summary>
    /// Central configuration for RF_AIDialog.
    ///
    /// Loading order:
    /// 1. Modules/RealmsForgotten/ai_config.local.json
    /// 2. Environment variables
    /// 3. Safe defaults in code
    /// </summary>
    public static class AIConfig
    {
        private const string DefaultApiEndpoint = "https://api.deepseek.com/chat/completions";
        private const string DefaultApiModelName = "deepseek-chat";
        private const string DefaultOllamaModelName = "gemma3:4b";
        private const string DefaultOllamaEndpoint = "http://localhost:11434/api/chat";

        private static readonly Lazy<LocalAIConfig> _localConfig =
            new Lazy<LocalAIConfig>(LoadLocalConfig);

        public static bool UseRemoteAPI => GetBool(
            _localConfig.Value.UseRemoteAPI,
            "RF_AIDIALOG_USE_REMOTE_API",
            true);

        public static string APIEndpoint => GetString(
            _localConfig.Value.APIEndpoint,
            "RF_AIDIALOG_API_ENDPOINT",
            DefaultApiEndpoint);

        public static string APIKey => GetString(
            ResolveApiKey(),
            "RF_AIDIALOG_API_KEY",
            "");

        public static string APIModelName => GetString(
            _localConfig.Value.APIModelName,
            "RF_AIDIALOG_API_MODEL",
            DefaultApiModelName);

        public static string ModelName => GetString(
            _localConfig.Value.ModelName,
            "RF_AIDIALOG_OLLAMA_MODEL",
            DefaultOllamaModelName);

        public static string OllamaEndpoint => GetString(
            _localConfig.Value.OllamaEndpoint,
            "RF_AIDIALOG_OLLAMA_ENDPOINT",
            DefaultOllamaEndpoint);

        public const int ContextSize = 2048;
        public const int TimeoutSeconds = 30;
        public const int MaxTokensFirstConversation = 650;
        public const int MaxTokensSubsequent = 450;
        public const int MaxGoldTransfer = 10000;
        public const int MaxRelationDelta = 5;
        public const double Temperature = 0.7;

        private static LocalAIConfig LoadLocalConfig()
        {
            try
            {
                foreach (string path in GetCandidatePaths())
                {
                    if (!File.Exists(path))
                        continue;

                    string json = File.ReadAllText(path);
                    var config = JsonConvert.DeserializeObject<LocalAIConfig>(json);
                    if (config != null)
                    {
                        RFAIDebug.Log($"AIConfig: loaded local config from {path}");
                        return config;
                    }
                }

                return new LocalAIConfig();
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIConfig: failed to load local config - {ex.Message}");
                return new LocalAIConfig();
            }
        }

        private static string[] GetCandidatePaths()
        {
            string? explicitPath = Environment.GetEnvironmentVariable("RF_AIDIALOG_CONFIG_PATH");
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            string moduleRootFromAssembly = "";
            try
            {
                moduleRootFromAssembly = Directory.GetParent(assemblyDir)?.Parent?.Parent?.FullName ?? "";
            }
            catch { }

            return new[]
            {
                explicitPath ?? "",
                Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", "ai_config.local.json"),
                Path.Combine(BasePath.Name, "RF_AIDialog", "ai_config.local.json"),
                string.IsNullOrWhiteSpace(moduleRootFromAssembly) ? "" : Path.Combine(moduleRootFromAssembly, "ai_config.local.json")
            };
        }

        private static string? ResolveApiKey()
        {
            if (!string.IsNullOrWhiteSpace(_localConfig.Value.APIKeyFile))
            {
                string? fromFile = TryReadSecretFile(_localConfig.Value.APIKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            string? envKeyFile = Environment.GetEnvironmentVariable("RF_AIDIALOG_API_KEY_FILE");
            if (!string.IsNullOrWhiteSpace(envKeyFile))
            {
                string? fromFile = TryReadSecretFile(envKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            return _localConfig.Value.APIKey;
        }

        private static string? TryReadSecretFile(string path)
        {
            try
            {
                string expanded = Environment.ExpandEnvironmentVariables(path).Trim();
                if (string.IsNullOrWhiteSpace(expanded) || !File.Exists(expanded))
                    return null;

                string content = File.ReadAllText(expanded).Trim();
                return string.IsNullOrWhiteSpace(content) ? null : content;
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIConfig: failed to read secret file - {ex.Message}");
                return null;
            }
        }

        private static string GetString(string? localValue, string envName, string defaultValue)
        {
            if (!string.IsNullOrWhiteSpace(localValue))
                return localValue.Trim();

            string? envValue = Environment.GetEnvironmentVariable(envName);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue.Trim();

            return defaultValue;
        }

        private static bool GetBool(bool? localValue, string envName, bool defaultValue)
        {
            if (localValue.HasValue)
                return localValue.Value;

            string? envValue = Environment.GetEnvironmentVariable(envName);
            if (bool.TryParse(envValue, out bool parsed))
                return parsed;

            return defaultValue;
        }

        private sealed class LocalAIConfig
        {
            [JsonProperty("use_remote_api")]
            public bool? UseRemoteAPI { get; set; }

            [JsonProperty("api_endpoint")]
            public string? APIEndpoint { get; set; }

            [JsonProperty("api_key")]
            public string? APIKey { get; set; }

            [JsonProperty("api_key_file")]
            public string? APIKeyFile { get; set; }

            [JsonProperty("api_model_name")]
            public string? APIModelName { get; set; }

            [JsonProperty("ollama_model_name")]
            public string? ModelName { get; set; }

            [JsonProperty("ollama_endpoint")]
            public string? OllamaEndpoint { get; set; }
        }
    }
}

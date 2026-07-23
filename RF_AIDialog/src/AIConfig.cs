using System;
using System.Collections.Generic;
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
        private const string DefaultTtsEndpoint = "https://api.openai.com/v1/audio/speech";
        private const string DefaultTtsModelName = "gpt-4o-mini-tts";
        private const string DefaultTtsVoice = "cedar";
        private const string DefaultTtsFormat = "wav";
        private const string DefaultTtsProvider = "openai";
        private const string DefaultFishTtsEndpoint = "https://api.fish.audio/v1/tts";
        private const string DefaultFishTtsModelName = "s2-pro";
        private const string DefaultFishTtsFormat = "mp3";
        private const string DefaultSttEndpoint = "https://api.openai.com/v1/audio/transcriptions";
        private const string DefaultSttModelName = "gpt-4o-mini-transcribe";
        private const string DefaultSttHotkey = "M";
        private const string DefaultBattleShoutOpenHotkey = "J";
        private const string DefaultBattleShoutVoiceHotkey = "M";
        private const bool DefaultLettersEnabled = true;

        private static readonly Lazy<LocalAIConfig> _localConfig =
            new Lazy<LocalAIConfig>(LoadLocalConfig);

        private static readonly Lazy<AIProfileConfig> _activeProfile =
            new Lazy<AIProfileConfig>(ResolveActiveProfile);

        public static bool UseRemoteAPI => GetBool(
            _activeProfile.Value.UseRemoteAPI,
            "RF_AIDIALOG_USE_REMOTE_API",
            true);

        public static string APIEndpoint => GetString(
            _activeProfile.Value.APIEndpoint,
            "RF_AIDIALOG_API_ENDPOINT",
            DefaultApiEndpoint);

        public static string APIKey => GetString(
            ResolveApiKey(),
            "RF_AIDIALOG_API_KEY",
            "");

        public static string APIModelName => GetString(
            _activeProfile.Value.APIModelName,
            "RF_AIDIALOG_API_MODEL",
            DefaultApiModelName);

        public static string ModelName => GetString(
            _activeProfile.Value.ModelName,
            "RF_AIDIALOG_OLLAMA_MODEL",
            DefaultOllamaModelName);

        public static string OllamaEndpoint => GetString(
            _activeProfile.Value.OllamaEndpoint,
            "RF_AIDIALOG_OLLAMA_ENDPOINT",
            DefaultOllamaEndpoint);

        public static bool DebugMenuEnabled => GetBool(
            _activeProfile.Value.DebugMenuEnabled,
            "RF_AIDIALOG_DEBUG_MENU_ENABLED",
            false);

        public static bool TTSEnabled => GetBool(
            _activeProfile.Value.TTSEnabled,
            "RF_AIDIALOG_TTS_ENABLED",
            false);

        public static bool TTSAutoPlay => GetBool(
            _activeProfile.Value.TTSAutoPlay,
            "RF_AIDIALOG_TTS_AUTOPLAY",
            true);

        public static string TTSEndpoint => GetString(
            _activeProfile.Value.TTSEndpoint,
            "RF_AIDIALOG_TTS_ENDPOINT",
            DefaultTtsEndpoint);

        public static string TTSProvider => GetString(
            _activeProfile.Value.TTSProvider,
            "RF_AIDIALOG_TTS_PROVIDER",
            DefaultTtsProvider).ToLowerInvariant();

        public static string TTSModelName => GetString(
            _activeProfile.Value.TTSModelName,
            "RF_AIDIALOG_TTS_MODEL",
            DefaultTtsModelName);

        public static string TTSVoice => GetString(
            _activeProfile.Value.TTSVoice,
            "RF_AIDIALOG_TTS_VOICE",
            DefaultTtsVoice);

        public static string TTSResponseFormat => GetString(
            _activeProfile.Value.TTSResponseFormat,
            "RF_AIDIALOG_TTS_FORMAT",
            DefaultTtsFormat);

        public static string TTSInstructions => GetString(
            _activeProfile.Value.TTSInstructions,
            "RF_AIDIALOG_TTS_INSTRUCTIONS",
            "");

        public static bool STTEnabled => GetBool(
            _activeProfile.Value.STTEnabled,
            "RF_AIDIALOG_STT_ENABLED",
            false);

        public static string STTEndpoint => GetString(
            _activeProfile.Value.STTEndpoint,
            "RF_AIDIALOG_STT_ENDPOINT",
            DefaultSttEndpoint);

        public static string STTModelName => GetString(
            _activeProfile.Value.STTModelName,
            "RF_AIDIALOG_STT_MODEL",
            DefaultSttModelName);

        public static string STTPrompt => GetString(
            _activeProfile.Value.STTPrompt,
            "RF_AIDIALOG_STT_PROMPT",
            "");

        public static string STTHotkey => GetString(
            _activeProfile.Value.STTHotkey,
            "RF_AIDIALOG_STT_HOTKEY",
            DefaultSttHotkey);

        public static bool BattleShoutsEnabled => GetBool(
            _activeProfile.Value.BattleShoutsEnabled,
            "RF_AIDIALOG_BATTLE_SHOUTS_ENABLED",
            true);

        public static string BattleShoutOpenHotkey => GetString(
            _activeProfile.Value.BattleShoutOpenHotkey,
            "RF_AIDIALOG_BATTLE_SHOUT_OPEN_HOTKEY",
            DefaultBattleShoutOpenHotkey);

        public static string BattleShoutVoiceHotkey => GetString(
            _activeProfile.Value.BattleShoutVoiceHotkey,
            "RF_AIDIALOG_BATTLE_SHOUT_VOICE_HOTKEY",
            DefaultBattleShoutVoiceHotkey);

        public static int BattleShoutMaxChars => GetInt(
            _activeProfile.Value.BattleShoutMaxChars,
            "RF_AIDIALOG_BATTLE_SHOUT_MAX_CHARS",
            180,
            32,
            600);

        public static int BattleShoutAllyReplies => GetInt(
            _activeProfile.Value.BattleShoutAllyReplies,
            "RF_AIDIALOG_BATTLE_SHOUT_ALLY_REPLIES",
            2,
            0,
            6);

        public static int BattleShoutEnemyReplies => GetInt(
            _activeProfile.Value.BattleShoutEnemyReplies,
            "RF_AIDIALOG_BATTLE_SHOUT_ENEMY_REPLIES",
            1,
            0,
            6);

        public static float BattleShoutReactionRadiusMeters => GetFloat(
            _activeProfile.Value.BattleShoutReactionRadiusMeters,
            "RF_AIDIALOG_BATTLE_SHOUT_RADIUS_METERS",
            25f,
            5f,
            60f);

        public static bool LettersEnabled => GetBool(
            _activeProfile.Value.LettersEnabled,
            "RF_AIDIALOG_LETTERS_ENABLED",
            DefaultLettersEnabled);

        public static int LettersMinDelayDays => GetInt(
            _activeProfile.Value.LettersMinDelayDays,
            "RF_AIDIALOG_LETTERS_MIN_DELAY_DAYS",
            1,
            1,
            7);

        public static int LettersMaxDelayDays => GetInt(
            _activeProfile.Value.LettersMaxDelayDays,
            "RF_AIDIALOG_LETTERS_MAX_DELAY_DAYS",
            6,
            1,
            20);

        public static int LettersReplyReminderDays => GetInt(
            _activeProfile.Value.LettersReplyReminderDays,
            "RF_AIDIALOG_LETTERS_REPLY_REMINDER_DAYS",
            3,
            1,
            14);

        public static string STTApiKey => GetString(
            ResolveSttApiKey(),
            "RF_AIDIALOG_STT_API_KEY",
            "");

        public static string TTSApiKey => GetString(
            ResolveTtsApiKey(),
            "RF_AIDIALOG_TTS_API_KEY",
            "");

        public static string FishTTSEndpoint => GetString(
            _activeProfile.Value.FishTTSEndpoint,
            "RF_AIDIALOG_FISH_TTS_ENDPOINT",
            DefaultFishTtsEndpoint);

        public static string FishTTSModelName => GetString(
            _activeProfile.Value.FishTTSModelName,
            "RF_AIDIALOG_FISH_TTS_MODEL",
            DefaultFishTtsModelName);

        public static string FishTTSResponseFormat => GetString(
            _activeProfile.Value.FishTTSResponseFormat,
            "RF_AIDIALOG_FISH_TTS_FORMAT",
            DefaultFishTtsFormat);

        public static string FishTTSReferenceId => GetString(
            _activeProfile.Value.FishTTSReferenceId,
            "RF_AIDIALOG_FISH_TTS_REFERENCE_ID",
            "");

        public static string FishTTSApiKey => GetString(
            ResolveFishTtsApiKey(),
            "RF_AIDIALOG_FISH_TTS_API_KEY",
            "");

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

        private static AIProfileConfig ResolveActiveProfile()
        {
            LocalAIConfig config = _localConfig.Value;
            if (config.Profiles == null || config.Profiles.Count == 0)
                return config;

            string profileName = GetString(
                config.ActiveProfile,
                "RF_AIDIALOG_ACTIVE_PROFILE",
                "");

            if (!string.IsNullOrWhiteSpace(profileName)
                && config.Profiles.TryGetValue(profileName, out AIProfileConfig profile)
                && profile != null)
            {
                RFAIDebug.Log($"AIConfig: using AI profile '{profileName}'");
                return profile.WithFallbacks(config);
            }

            foreach (KeyValuePair<string, AIProfileConfig> entry in config.Profiles)
            {
                if (entry.Value == null)
                    continue;

                RFAIDebug.Log($"AIConfig: active profile not found; using first AI profile '{entry.Key}'");
                return entry.Value.WithFallbacks(config);
            }

            return config;
        }

        private static string[] GetCandidatePaths()
        {
            string? explicitPath = Environment.GetEnvironmentVariable("RF_AIDIALOG_CONFIG_PATH");
            string moduleRootFromAssembly = GetModuleRootFromAssembly();

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
            AIProfileConfig profile = _activeProfile.Value;
            if (!string.IsNullOrWhiteSpace(profile.APIKeyFile))
            {
                string? fromFile = TryReadSecretFile(profile.APIKeyFile);
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

            return profile.APIKey;
        }

        private static string? ResolveTtsApiKey()
        {
            AIProfileConfig profile = _activeProfile.Value;
            if (!string.IsNullOrWhiteSpace(profile.TTSApiKeyFile))
            {
                string? fromFile = TryReadSecretFile(profile.TTSApiKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            string? envKeyFile = Environment.GetEnvironmentVariable("RF_AIDIALOG_TTS_API_KEY_FILE");
            if (!string.IsNullOrWhiteSpace(envKeyFile))
            {
                string? fromFile = TryReadSecretFile(envKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            if (!string.IsNullOrWhiteSpace(profile.TTSApiKey))
                return profile.TTSApiKey;

            // NO fallback to the main LLM key: TTS talks to a different
            // provider (OpenAI) than the main endpoint (DeepSeek by default),
            // and falling back used to transmit the DeepSeek secret to OpenAI
            // as a Bearer token. Unconfigured means unconfigured.
            return null;
        }

        private static string? ResolveSttApiKey()
        {
            AIProfileConfig profile = _activeProfile.Value;
            if (!string.IsNullOrWhiteSpace(profile.STTApiKeyFile))
            {
                string? fromFile = TryReadSecretFile(profile.STTApiKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            string? envKeyFile = Environment.GetEnvironmentVariable("RF_AIDIALOG_STT_API_KEY_FILE");
            if (!string.IsNullOrWhiteSpace(envKeyFile))
            {
                string? fromFile = TryReadSecretFile(envKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            if (!string.IsNullOrWhiteSpace(profile.STTApiKey))
                return profile.STTApiKey;

            // STT and TTS both default to api.openai.com, so sharing the
            // dedicated TTS key is a same-provider fallback. The main LLM key
            // is a different provider — never send it here (secret leak).
            string? ttsKey = ResolveTtsApiKey();
            if (!string.IsNullOrWhiteSpace(ttsKey))
                return ttsKey;

            return null;
        }

        private static string? ResolveFishTtsApiKey()
        {
            AIProfileConfig profile = _activeProfile.Value;
            if (!string.IsNullOrWhiteSpace(profile.FishTTSApiKeyFile))
            {
                string? fromFile = TryReadSecretFile(profile.FishTTSApiKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            string? envKeyFile = Environment.GetEnvironmentVariable("RF_AIDIALOG_FISH_TTS_API_KEY_FILE");
            if (!string.IsNullOrWhiteSpace(envKeyFile))
            {
                string? fromFile = TryReadSecretFile(envKeyFile);
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile;
            }

            if (!string.IsNullOrWhiteSpace(profile.FishTTSApiKey))
                return profile.FishTTSApiKey;

            // Fish Audio is its own provider: neither the OpenAI TTS key nor
            // the main LLM key belongs there. No cross-provider fallback —
            // an unconfigured Fish key means Fish TTS is off, not "send some
            // other service's secret and see".
            return null;
        }

        private static string? TryReadSecretFile(string path)
        {
            try
            {
                string expanded = Environment.ExpandEnvironmentVariables(path).Trim();
                if (string.IsNullOrWhiteSpace(expanded))
                    return null;

                foreach (string candidate in GetSecretFileCandidates(expanded))
                {
                    if (!File.Exists(candidate))
                        continue;

                    string content = File.ReadAllText(candidate).Trim();
                    if (!string.IsNullOrWhiteSpace(content))
                        return content;
                }

                return null;
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"AIConfig: failed to read secret file - {ex.Message}");
                return null;
            }
        }

        private static IEnumerable<string> GetSecretFileCandidates(string path)
        {
            yield return path;

            if (Path.IsPathRooted(path))
                yield break;

            string moduleRoot = GetModuleRootFromAssembly();
            if (!string.IsNullOrWhiteSpace(moduleRoot))
                yield return Path.Combine(moduleRoot, path);

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            if (!string.IsNullOrWhiteSpace(assemblyDir))
                yield return Path.Combine(assemblyDir, path);

            yield return Path.Combine(BasePath.Name, "Modules", "RealmsForgotten", path);
        }

        private static string GetModuleRootFromAssembly()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            try
            {
                return Directory.GetParent(assemblyDir)?.Parent?.Parent?.FullName ?? "";
            }
            catch
            {
                return "";
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

        private static int GetInt(int? localValue, string envName, int defaultValue, int minValue, int maxValue)
        {
            int value = defaultValue;

            if (localValue.HasValue)
                value = localValue.Value;
            else
            {
                string? envValue = Environment.GetEnvironmentVariable(envName);
                if (int.TryParse(envValue, out int parsed))
                    value = parsed;
            }

            if (value < minValue) value = minValue;
            if (value > maxValue) value = maxValue;
            return value;
        }

        private static float GetFloat(float? localValue, string envName, float defaultValue, float minValue, float maxValue)
        {
            float value = defaultValue;

            if (localValue.HasValue)
                value = localValue.Value;
            else
            {
                string? envValue = Environment.GetEnvironmentVariable(envName);
                if (float.TryParse(envValue, out float parsed))
                    value = parsed;
            }

            if (value < minValue) value = minValue;
            if (value > maxValue) value = maxValue;
            return value;
        }

        private class AIProfileConfig
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

            [JsonProperty("debug_menu_enabled")]
            public bool? DebugMenuEnabled { get; set; }

            [JsonProperty("tts_enabled")]
            public bool? TTSEnabled { get; set; }

            [JsonProperty("tts_autoplay")]
            public bool? TTSAutoPlay { get; set; }

            [JsonProperty("tts_endpoint")]
            public string? TTSEndpoint { get; set; }

            [JsonProperty("tts_provider")]
            public string? TTSProvider { get; set; }

            [JsonProperty("tts_api_key")]
            public string? TTSApiKey { get; set; }

            [JsonProperty("tts_api_key_file")]
            public string? TTSApiKeyFile { get; set; }

            [JsonProperty("tts_model_name")]
            public string? TTSModelName { get; set; }

            [JsonProperty("tts_voice")]
            public string? TTSVoice { get; set; }

            [JsonProperty("tts_response_format")]
            public string? TTSResponseFormat { get; set; }

            [JsonProperty("tts_instructions")]
            public string? TTSInstructions { get; set; }

            [JsonProperty("fish_tts_endpoint")]
            public string? FishTTSEndpoint { get; set; }

            [JsonProperty("fish_tts_api_key")]
            public string? FishTTSApiKey { get; set; }

            [JsonProperty("fish_tts_api_key_file")]
            public string? FishTTSApiKeyFile { get; set; }

            [JsonProperty("fish_tts_model")]
            public string? FishTTSModelName { get; set; }

            [JsonProperty("fish_tts_reference_id")]
            public string? FishTTSReferenceId { get; set; }

            [JsonProperty("fish_tts_format")]
            public string? FishTTSResponseFormat { get; set; }

            [JsonProperty("stt_enabled")]
            public bool? STTEnabled { get; set; }

            [JsonProperty("stt_endpoint")]
            public string? STTEndpoint { get; set; }

            [JsonProperty("stt_api_key")]
            public string? STTApiKey { get; set; }

            [JsonProperty("stt_api_key_file")]
            public string? STTApiKeyFile { get; set; }

            [JsonProperty("stt_model_name")]
            public string? STTModelName { get; set; }

            [JsonProperty("stt_prompt")]
            public string? STTPrompt { get; set; }

            [JsonProperty("stt_hotkey")]
            public string? STTHotkey { get; set; }

            [JsonProperty("battle_shouts_enabled")]
            public bool? BattleShoutsEnabled { get; set; }

            [JsonProperty("battle_shout_open_hotkey")]
            public string? BattleShoutOpenHotkey { get; set; }

            [JsonProperty("battle_shout_voice_hotkey")]
            public string? BattleShoutVoiceHotkey { get; set; }

            [JsonProperty("battle_shout_max_chars")]
            public int? BattleShoutMaxChars { get; set; }

            [JsonProperty("battle_shout_ally_replies")]
            public int? BattleShoutAllyReplies { get; set; }

            [JsonProperty("battle_shout_enemy_replies")]
            public int? BattleShoutEnemyReplies { get; set; }

            [JsonProperty("battle_shout_radius_meters")]
            public float? BattleShoutReactionRadiusMeters { get; set; }

            [JsonProperty("letters_enabled")]
            public bool? LettersEnabled { get; set; }

            [JsonProperty("letters_min_delay_days")]
            public int? LettersMinDelayDays { get; set; }

            [JsonProperty("letters_max_delay_days")]
            public int? LettersMaxDelayDays { get; set; }

            [JsonProperty("letters_reply_reminder_days")]
            public int? LettersReplyReminderDays { get; set; }

            public AIProfileConfig WithFallbacks(AIProfileConfig fallback)
            {
                return new AIProfileConfig
                {
                    UseRemoteAPI = UseRemoteAPI ?? fallback.UseRemoteAPI,
                    APIEndpoint = string.IsNullOrWhiteSpace(APIEndpoint) ? fallback.APIEndpoint : APIEndpoint,
                    APIKey = string.IsNullOrWhiteSpace(APIKey) ? fallback.APIKey : APIKey,
                    APIKeyFile = string.IsNullOrWhiteSpace(APIKeyFile) ? fallback.APIKeyFile : APIKeyFile,
                    APIModelName = string.IsNullOrWhiteSpace(APIModelName) ? fallback.APIModelName : APIModelName,
                    ModelName = string.IsNullOrWhiteSpace(ModelName) ? fallback.ModelName : ModelName,
                    OllamaEndpoint = string.IsNullOrWhiteSpace(OllamaEndpoint) ? fallback.OllamaEndpoint : OllamaEndpoint,
                    DebugMenuEnabled = DebugMenuEnabled ?? fallback.DebugMenuEnabled,
                    TTSEnabled = TTSEnabled ?? fallback.TTSEnabled,
                    TTSAutoPlay = TTSAutoPlay ?? fallback.TTSAutoPlay,
                    TTSEndpoint = string.IsNullOrWhiteSpace(TTSEndpoint) ? fallback.TTSEndpoint : TTSEndpoint,
                    TTSProvider = string.IsNullOrWhiteSpace(TTSProvider) ? fallback.TTSProvider : TTSProvider,
                    TTSApiKey = string.IsNullOrWhiteSpace(TTSApiKey) ? fallback.TTSApiKey : TTSApiKey,
                    TTSApiKeyFile = string.IsNullOrWhiteSpace(TTSApiKeyFile) ? fallback.TTSApiKeyFile : TTSApiKeyFile,
                    TTSModelName = string.IsNullOrWhiteSpace(TTSModelName) ? fallback.TTSModelName : TTSModelName,
                    TTSVoice = string.IsNullOrWhiteSpace(TTSVoice) ? fallback.TTSVoice : TTSVoice,
                    TTSResponseFormat = string.IsNullOrWhiteSpace(TTSResponseFormat) ? fallback.TTSResponseFormat : TTSResponseFormat,
                    TTSInstructions = string.IsNullOrWhiteSpace(TTSInstructions) ? fallback.TTSInstructions : TTSInstructions,
                    FishTTSEndpoint = string.IsNullOrWhiteSpace(FishTTSEndpoint) ? fallback.FishTTSEndpoint : FishTTSEndpoint,
                    FishTTSApiKey = string.IsNullOrWhiteSpace(FishTTSApiKey) ? fallback.FishTTSApiKey : FishTTSApiKey,
                    FishTTSApiKeyFile = string.IsNullOrWhiteSpace(FishTTSApiKeyFile) ? fallback.FishTTSApiKeyFile : FishTTSApiKeyFile,
                    FishTTSModelName = string.IsNullOrWhiteSpace(FishTTSModelName) ? fallback.FishTTSModelName : FishTTSModelName,
                    FishTTSReferenceId = string.IsNullOrWhiteSpace(FishTTSReferenceId) ? fallback.FishTTSReferenceId : FishTTSReferenceId,
                    FishTTSResponseFormat = string.IsNullOrWhiteSpace(FishTTSResponseFormat) ? fallback.FishTTSResponseFormat : FishTTSResponseFormat,
                    STTEnabled = STTEnabled ?? fallback.STTEnabled,
                    STTEndpoint = string.IsNullOrWhiteSpace(STTEndpoint) ? fallback.STTEndpoint : STTEndpoint,
                    STTApiKey = string.IsNullOrWhiteSpace(STTApiKey) ? fallback.STTApiKey : STTApiKey,
                    STTApiKeyFile = string.IsNullOrWhiteSpace(STTApiKeyFile) ? fallback.STTApiKeyFile : STTApiKeyFile,
                    STTModelName = string.IsNullOrWhiteSpace(STTModelName) ? fallback.STTModelName : STTModelName,
                    STTPrompt = string.IsNullOrWhiteSpace(STTPrompt) ? fallback.STTPrompt : STTPrompt,
                    STTHotkey = string.IsNullOrWhiteSpace(STTHotkey) ? fallback.STTHotkey : STTHotkey,
                    BattleShoutsEnabled = BattleShoutsEnabled ?? fallback.BattleShoutsEnabled,
                    BattleShoutOpenHotkey = string.IsNullOrWhiteSpace(BattleShoutOpenHotkey) ? fallback.BattleShoutOpenHotkey : BattleShoutOpenHotkey,
                    BattleShoutVoiceHotkey = string.IsNullOrWhiteSpace(BattleShoutVoiceHotkey) ? fallback.BattleShoutVoiceHotkey : BattleShoutVoiceHotkey,
                    BattleShoutMaxChars = BattleShoutMaxChars ?? fallback.BattleShoutMaxChars,
                    BattleShoutAllyReplies = BattleShoutAllyReplies ?? fallback.BattleShoutAllyReplies,
                    BattleShoutEnemyReplies = BattleShoutEnemyReplies ?? fallback.BattleShoutEnemyReplies,
                    BattleShoutReactionRadiusMeters = BattleShoutReactionRadiusMeters ?? fallback.BattleShoutReactionRadiusMeters,
                    LettersEnabled = LettersEnabled ?? fallback.LettersEnabled,
                    LettersMinDelayDays = LettersMinDelayDays ?? fallback.LettersMinDelayDays,
                    LettersMaxDelayDays = LettersMaxDelayDays ?? fallback.LettersMaxDelayDays,
                    LettersReplyReminderDays = LettersReplyReminderDays ?? fallback.LettersReplyReminderDays
                };
            }
        }

        private sealed class LocalAIConfig : AIProfileConfig
        {
            [JsonProperty("active_profile")]
            public string? ActiveProfile { get; set; }

            [JsonProperty("profiles")]
            public Dictionary<string, AIProfileConfig>? Profiles { get; set; }
        }
    }
}

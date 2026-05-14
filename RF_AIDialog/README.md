# RF_AIDialog Configuration

`RF_AIDialog` no longer stores API secrets in code.

Configuration priority:
1. `RF_AIDIALOG_CONFIG_PATH`
2. `Modules/RealmsForgotten/ai_config.local.json`
3. `RF_AIDialog/ai_config.local.json`
4. Environment variables
5. Safe defaults in code

The old single-provider format is still supported. To configure multiple providers, set
`active_profile` and add entries under `profiles`:

```json
{
  "use_remote_api": true,
  "active_profile": "deepseek",
  "profiles": {
    "deepseek": {
      "api_endpoint": "https://api.deepseek.com/chat/completions",
      "api_model_name": "deepseek-chat",
      "api_key_file": "C:\\secure\\deepseek.key"
    },
    "openai": {
      "api_endpoint": "https://api.openai.com/v1/chat/completions",
      "api_model_name": "gpt-4o-mini",
      "api_key_file": "C:\\secure\\openai.key"
    },
    "local_ollama": {
      "use_remote_api": false,
      "ollama_model_name": "gemma3:4b",
      "ollama_endpoint": "http://localhost:11434/api/chat"
    }
  }
}
```

Profile fields fall back to the root config when omitted. `RF_AIDIALOG_ACTIVE_PROFILE`
can override `active_profile`.

Recommended secret options:

1. Environment variable only
```powershell
$env:RF_AIDIALOG_API_KEY="your_key_here"
```

2. Local JSON file ignored by git
Create `ai_config.local.json` based on `ai_config.example.json`.

3. Secret file path
Put only the key text in a file outside the repo and set either:
```powershell
$env:RF_AIDIALOG_API_KEY_FILE="C:\secure\deepseek.key"
```
or in `ai_config.local.json`:
```json
{
  "use_remote_api": true,
  "api_key_file": "C:\\secure\\deepseek.key"
}
```

Supported environment variables:
- `RF_AIDIALOG_USE_REMOTE_API`
- `RF_AIDIALOG_API_ENDPOINT`
- `RF_AIDIALOG_API_MODEL`
- `RF_AIDIALOG_API_KEY`
- `RF_AIDIALOG_API_KEY_FILE`
- `RF_AIDIALOG_OLLAMA_MODEL`
- `RF_AIDIALOG_OLLAMA_ENDPOINT`
- `RF_AIDIALOG_CONFIG_PATH`
- `RF_AIDIALOG_ACTIVE_PROFILE`

Notes:
- Keep `ai_config.local.json` out of git.
- Prefer `api_key_file` or env vars over inline `api_key`.
- If this repo ever contained a real key, rotate it in the provider dashboard.

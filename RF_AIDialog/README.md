# RF_AIDialog Configuration

`RF_AIDialog` no longer stores API secrets in code.

Configuration priority:
1. `RF_AIDIALOG_CONFIG_PATH`
2. `Modules/RealmsForgotten/ai_config.local.json`
3. `RF_AIDialog/ai_config.local.json`
4. Environment variables
5. Safe defaults in code

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

Notes:
- Keep `ai_config.local.json` out of git.
- Prefer `api_key_file` or env vars over inline `api_key`.
- If this repo ever contained a real key, rotate it in the provider dashboard.

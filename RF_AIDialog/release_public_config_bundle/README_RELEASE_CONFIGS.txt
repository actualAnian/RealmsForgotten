RF_AIDialog Public Release Config Bundle

This folder is safe to package with the public release.
It contains:

- ai_config.local.json
- ai_config.default.json
- ai_config.example.json
- deepseek.key
- openai.key
- openai_tts.key
- fish_tts.key
- gemini.key

Important:
- All API key files are blank on purpose.
- No private keys are included.
- Users should place these files in their RealmsForgotten module folder.

Recommended user setup:
1. Copy ai_config.local.json into the module folder.
2. Put their own provider key into the matching .key file.
3. Edit ai_config.local.json only if they want a different provider/profile.

Current default profile:
- active_profile = deepseek

Supported providers in this template:
- DeepSeek for chat
- OpenAI for chat / STT / TTS
- Fish Audio for NPC TTS
- Gemini Flash for chat
- Ollama for local chat


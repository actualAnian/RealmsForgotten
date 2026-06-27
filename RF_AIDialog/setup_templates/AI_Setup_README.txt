REALMS FORGOTTEN AI SETUP

Files created by build in the mod folder:
- ai_config.default.json
- ai_config.example.json
- ai_config.local.json (only if missing)
- deepseek.key
- openai.key
- openai_tts.key
- fish_tts.key
- gemini.key

How to use:
1. Open ai_config.local.json
2. Choose active_profile
3. Paste your provider keys into the matching .key files
4. Keep the .key files in the same RealmsForgotten module folder

Typical setup:
- deepseek.key for text chat with DeepSeek
- fish_tts.key for Fish Audio NPC voice
- openai_tts.key for OpenAI speech-to-text and/or OpenAI TTS
- openai.key for OpenAI chat profile
- gemini.key for Gemini chat profile

Notes:
- The AI memory files are created automatically at runtime.
- Empty .key files are safe and will simply be ignored until filled.

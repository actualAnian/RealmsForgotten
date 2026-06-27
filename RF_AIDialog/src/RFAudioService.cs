using System;
using System.Threading.Tasks;

namespace RF_AIDialog
{
    internal static class RFAudioService
    {
        public static void TrySpeakNpcReply(string text, string? voiceInstructions = null)
        {
            if (!AIConfig.TTSEnabled || !AIConfig.TTSAutoPlay)
                return;

            if (string.IsNullOrWhiteSpace(text))
                return;

            string trimmed = text.Trim();
            if (trimmed.StartsWith("(") && trimmed.Contains("AI Error"))
                return;

            int requestGeneration = RFAudioPlaybackManager.Instance.NextGeneration();

            _ = Task.Run(async () =>
            {
                try
                {
                    SpeechSynthesisResult result = await AIClient.SynthesizeSpeechAsync(trimmed, voiceInstructions)
                        .ConfigureAwait(false);

                    if (result.AudioBytes == null || result.AudioBytes.Length == 0)
                        return;

                    if (requestGeneration != RFAudioPlaybackManager.Instance.CurrentGeneration)
                        return;

                    RFAIDebug.Log(
                        $"RFAudioService.TrySpeakNpcReply: playing detected={result.DetectedFormat} requested={result.RequestedFormat} bytes={result.AudioBytes.Length}");

                    RFAudioPlaybackManager.Instance.PlayAudio(
                        result.AudioBytes,
                        result.DetectedFormat,
                        requestGeneration,
                        "npc reply");
                }
                catch (Exception ex)
                {
                    RFAIDebug.Log($"RFAudioService.TrySpeakNpcReply failed: {ex.GetType().Name}: {ex.Message}");
                }
            });
        }

        public static void StopAll(string reason)
        {
            RFAudioPlaybackManager.Instance.StopAll(reason);
        }
    }
}

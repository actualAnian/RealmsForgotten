using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RF_AIDialog
{
    internal sealed class BattleShoutMissionBehavior : MissionBehavior
    {
        private readonly RFVoiceCaptureService _voiceCaptureService = new RFVoiceCaptureService();
        private bool _isPromptOpen;
        private bool _isRecording;
        private bool _transcriptionInFlight;
        private bool _voiceHotkeyWasDown;
        private string? _pendingAutoSendShout;
        private string? _pendingInfoMessage;
        private string? _pendingErrorMessage;
        // Set from the async continuation (thread pool); consumed on the MAIN
        // thread in FlushPendingResults. Applying battle orders (SetOrder) and
        // reply messages directly in the continuation mutated mission
        // formations off-thread, racing with the mission tick.
        private PendingBattleResult? _pendingBattleResult;

        private sealed class PendingBattleResult
        {
            public readonly Mission Mission;
            public readonly BattleShoutResponse Parsed;
            public readonly string Shout;
            public readonly Agent[] Allies;
            public readonly Agent[] Enemies;

            public PendingBattleResult(Mission mission, BattleShoutResponse parsed, string shout, Agent[] allies, Agent[] enemies)
            {
                Mission = mission;
                Parsed = parsed;
                Shout = shout;
                Allies = allies;
                Enemies = enemies;
            }
        }

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            if (!IsBattleMission(Mission))
                return;

            FlushPendingResults();

            if (_isPromptOpen)
                return;

            InputKey openKey = ResolveBattleShoutOpenHotkey();
            if (Input.IsKeyReleased(openKey))
            {
                OpenShoutPrompt(string.Empty);
                return;
            }

            if (!AIConfig.STTEnabled)
                return;

            InputKey? voiceKey = ResolveHotkey(AIConfig.BattleShoutVoiceHotkey);
            if (!voiceKey.HasValue)
                return;

            bool keyDown = Input.IsKeyDown(voiceKey.Value);
            if (keyDown && !_voiceHotkeyWasDown && !_transcriptionInFlight && !_isRecording)
            {
                StartLiveVoiceRecording();
            }
            else if (!keyDown && _voiceHotkeyWasDown && _isRecording && !_transcriptionInFlight)
            {
                StopLiveVoiceRecordingAndSend();
            }

            _voiceHotkeyWasDown = keyDown;
        }

        public override void OnRemoveBehavior()
        {
            RF_AIDialogSubModule.Instance?.CloseVoiceInputPopup();
            try
            {
                if (_voiceCaptureService.IsRecording)
                    _voiceCaptureService.StopRecording();
            }
            catch
            {
            }
            _voiceCaptureService.Dispose();
            base.OnRemoveBehavior();
        }

        private void FlushPendingResults()
        {
            if (!string.IsNullOrWhiteSpace(_pendingErrorMessage))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    _pendingErrorMessage,
                    Color.FromUint(0xFF_FF_80_80u)));
                _pendingErrorMessage = null;
            }

            if (!string.IsNullOrWhiteSpace(_pendingInfoMessage))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    _pendingInfoMessage,
                    Color.FromUint(0xFF_A0_D0_FFu)));
                _pendingInfoMessage = null;
            }

            if (!string.IsNullOrWhiteSpace(_pendingAutoSendShout))
            {
                string cleaned = _pendingAutoSendShout!;
                _pendingAutoSendShout = null;
                ShowShoutMessage("[SHOUT]", "Player", cleaned, Color.FromUint(0xFF_66_AA_FFu));
                _ = HandleShoutAsync(cleaned);
            }

            // Battle orders + replies applied here on the MAIN thread.
            PendingBattleResult? result = _pendingBattleResult;
            if (result != null)
            {
                _pendingBattleResult = null;
                if (result.Mission == Mission && Mission != null)
                {
                    ApplyBattleOrders(result.Mission, result.Parsed, result.Shout);
                    ScheduleReplies(result.Allies, result.Parsed.AllyReplies, true);
                    ScheduleReplies(result.Enemies, result.Parsed.EnemyReplies, false);
                }
            }
        }

        private void OpenShoutPrompt(string prefilledText)
        {
            _isPromptOpen = true;
            RF_AIDialogSubModule.Instance?.ShowVoiceInputPopup(
                "Battle",
                "Battle Shout",
                "Use the microphone button to dictate your battle shout, or type directly into the field below.",
                OnConfirm,
                OnCancel,
                allowOutsideConversation: true,
                hotkeyOverride: AIConfig.BattleShoutVoiceHotkey);
        }

        private void OnConfirm(string input)
        {
            _isPromptOpen = false;

            string shout = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(shout))
                return;

            string cleaned = shout.Length > AIConfig.BattleShoutMaxChars
                ? shout.Substring(0, AIConfig.BattleShoutMaxChars)
                : shout;

            ShowShoutMessage("[SHOUT]", "Player", cleaned, Color.FromUint(0xFF_66_AA_FFu));
            _ = HandleShoutAsync(cleaned);
        }

        private void OnCancel()
        {
            _isPromptOpen = false;
        }

        private void StartLiveVoiceRecording()
        {
            try
            {
                _voiceCaptureService.StartRecording();
                _isRecording = true;
                InformationManager.DisplayMessage(new InformationMessage(
                    "[Battle Voice] Listening...",
                    Color.FromUint(0xFF_A0_D0_FFu)));
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"Battle shout StartLiveVoiceRecording failed: {ex.GetType().Name}: {ex.Message}");
                _pendingErrorMessage = "[Battle Voice] Could not start microphone.";
            }
        }

        private void StopLiveVoiceRecordingAndSend()
        {
            _isRecording = false;
            _transcriptionInFlight = true;

            byte[] wavBytes;
            try
            {
                wavBytes = _voiceCaptureService.StopRecording();
            }
            catch (Exception ex)
            {
                _transcriptionInFlight = false;
                RFAIDebug.Log($"Battle shout StopLiveVoiceRecording failed: {ex.GetType().Name}: {ex.Message}");
                _pendingErrorMessage = "[Battle Voice] Could not stop recording.";
                return;
            }

            _pendingInfoMessage = "[Battle Voice] Sending order...";

            Task.Run(async () =>
            {
                try
                {
                    string transcript = await AIClient.TranscribeSpeechAsync(
                        wavBytes,
                        "Battlefield voice commands. Short military orders. Medieval fantasy setting.")
                        .ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(transcript))
                    {
                        _pendingErrorMessage = "[Battle Voice] I did not catch that.";
                        return;
                    }

                    string cleaned = transcript.Trim();
                    if (cleaned.Length > AIConfig.BattleShoutMaxChars)
                        cleaned = cleaned.Substring(0, AIConfig.BattleShoutMaxChars);

                    _pendingInfoMessage = $"[Battle Voice] Heard: {cleaned}";
                    _pendingAutoSendShout = cleaned;
                }
                catch (Exception ex)
                {
                    RFAIDebug.Log($"Battle shout live transcription failed: {ex.GetType().Name}: {ex.Message}");
                    _pendingErrorMessage = "[Battle Voice] Transcription failed.";
                }
                finally
                {
                    _transcriptionInFlight = false;
                }
            });
        }

        private async Task HandleShoutAsync(string shout)
        {
            try
            {
                Mission? mission = Mission;
                Agent? main = Agent.Main;
                if (mission == null || main == null || !main.IsActive())
                    return;

                Agent[] nearbyAllies = PickNearby(main, AIConfig.BattleShoutReactionRadiusMeters, AIConfig.BattleShoutAllyReplies, true);
                Agent[] nearbyEnemies = PickNearby(main, AIConfig.BattleShoutReactionRadiusMeters, AIConfig.BattleShoutEnemyReplies, false);

                string prompt = BuildBattleShoutPrompt(main, shout, nearbyAllies, nearbyEnemies);
                string raw = await AIClient.AskAsync(
                    AIConfig.ModelName,
                    "You are a battlefield intent interpreter and reaction generator. Output JSON only.",
                    prompt,
                    280).ConfigureAwait(false);

                string? json = JsonCleaner.ExtractJson(raw);
                if (string.IsNullOrWhiteSpace(json))
                {
                    RFAIDebug.Log($"Battle shout parse failed: no JSON | raw={raw}");
                    return;
                }

                BattleShoutResponse? parsed = null;
                try
                {
                    parsed = JsonConvert.DeserializeObject<BattleShoutResponse>(json);
                }
                catch (Exception ex)
                {
                    RFAIDebug.Log($"Battle shout deserialize failed: {ex.GetType().Name}: {ex.Message} | json={json}");
                    return;
                }

                if (parsed == null)
                    return;

                // Hand off to the main thread — do NOT touch mission state here
                // (this continuation runs on the thread pool after ConfigureAwait).
                _pendingBattleResult = new PendingBattleResult(mission, parsed, shout, nearbyAllies, nearbyEnemies);
            }
            catch (Exception ex)
            {
                RFAIDebug.Log($"HandleShoutAsync failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void ApplyBattleOrders(Mission mission, BattleShoutResponse parsed, string originalShout)
        {
            bool executed = false;
            string? formation = NormalizeFormation(parsed.Formation);
            string? arrangement = NormalizeArrangement(parsed.Arrangement);
            string? firing = NormalizeFiring(parsed.Firing);
            BattleShoutMovementIntent movement = BattleShoutOrderExecutor.ParseMovement(parsed.Movement);

            string? inferredFormation = InferFormationFromShout(originalShout);
            if ((string.IsNullOrWhiteSpace(formation) || formation == "all") && !string.IsNullOrWhiteSpace(inferredFormation))
                formation = inferredFormation;

            BattleShoutMovementIntent inferredMovement = InferMovementFromShout(originalShout);
            if (movement == BattleShoutMovementIntent.None && inferredMovement != BattleShoutMovementIntent.None)
                movement = inferredMovement;

            if (firing != null)
                executed |= BattleShoutOrderExecutor.TryExecuteFiring(firing, mission, formation);

            if (arrangement != null)
                executed |= BattleShoutOrderExecutor.TryExecuteArrangement(arrangement, mission, formation);

            if (movement != BattleShoutMovementIntent.None)
                executed |= BattleShoutOrderExecutor.TryExecuteMovement(movement, mission, formation);

            if (!executed)
                return;

            string ack = string.IsNullOrWhiteSpace(parsed.Response)
                ? $"Order understood for {(formation ?? "all formations")}."
                : parsed.Response!.Trim();

            ShowShoutMessage("[ORDER]", "Command", ack, Color.FromUint(0xFF_80_FF_80u));
        }

        private void ScheduleReplies(Agent[] agents, string[]? replies, bool allies)
        {
            if (agents == null || replies == null || agents.Length == 0 || replies.Length == 0)
                return;

            int count = Math.Min(agents.Length, replies.Length);
            for (int i = 0; i < count; i++)
            {
                Agent agent = agents[i];
                string line = (replies[i] ?? string.Empty).Trim();
                if (agent == null || !agent.IsActive() || string.IsNullOrWhiteSpace(line))
                    continue;

                string speaker = agent.Name?.ToString() ?? (allies ? "Ally" : "Enemy");
                Color color = allies
                    ? Color.FromUint(0xFF_80_FF_80u)
                    : Color.FromUint(0xFF_FF_80_80u);
                ShowShoutMessage(allies ? "[ALLY]" : "[ENEMY]", speaker, line, color);
            }
        }

        private static void ShowShoutMessage(string prefix, string speaker, string line, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"{prefix} ({speaker}) : {line}",
                color));
        }

        private static string BuildBattleShoutPrompt(Agent main, string shout, Agent[] nearbyAllies, Agent[] nearbyEnemies)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Interpret this battlefield shout and answer with JSON only.");
            sb.AppendLine("You may convert the shout into battle orders if the wording clearly implies one.");
            sb.AppendLine("Allowed movement: none, charge, advance, fall_back, retreat, stop, follow");
            sb.AppendLine("Allowed formation: all, infantry, archers, cavalry, horse_archers");
            sb.AppendLine("Allowed arrangement: none, line, shield_wall, loose, column, square, circle, scatter, skein");
            sb.AppendLine("Allowed firing: none, fire_at_will, hold_fire");
            sb.AppendLine("Return compact JSON:");
            sb.AppendLine("{\"response\":\"...\",\"movement\":\"none\",\"formation\":\"all\",\"arrangement\":\"none\",\"firing\":\"none\",\"confidence\":0.0,\"ally_replies\":[\"...\"],\"enemy_replies\":[\"...\"]}");
            sb.AppendLine("Keep response and replies short. Max 8 words per ally/enemy reply. Medieval battlefield tone.");
            sb.AppendLine("If the shout is not clearly an order, keep movement/arrangement/firing as none.");
            sb.AppendLine("Speech aliases: 'arches' means 'archers'. 'horse archers' means 'horse_archers'.");
            sb.AppendLine("If the shout clearly names a formation, do not default to 'all'.");
            sb.AppendLine("Examples:");
            sb.AppendLine("Input: 'archers retreat' => formation='archers', movement='retreat'");
            sb.AppendLine("Input: 'infantry shield wall and advance' => formation='infantry', arrangement='shield_wall', movement='advance'");
            sb.AppendLine("Input: 'cavalry follow me' => formation='cavalry', movement='follow'");
            sb.AppendLine();
            sb.AppendLine($"Player shout: {shout}");
            sb.AppendLine($"Nearby ally listeners requested: {nearbyAllies.Length}");
            sb.AppendLine($"Nearby enemy listeners requested: {nearbyEnemies.Length}");
            sb.AppendLine($"Player culture: {GetCultureId(main)}");
            sb.AppendLine();
            sb.AppendLine("Do not include markdown or explanations outside the JSON.");
            return sb.ToString();
        }

        private static string GetCultureId(Agent agent)
        {
            try
            {
                return agent?.Character?.Culture?.StringId ?? "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private static Agent[] PickNearby(Agent player, float radius, int max, bool allies)
        {
            Mission mission = player.Mission;
            if (mission == null || max <= 0 || player.Team == null)
                return Array.Empty<Agent>();

            float maxDistanceSquared = radius * radius;
            List<Agent> candidates = new List<Agent>();
            foreach (Agent agent in mission.Agents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman || agent == player || agent.Team == null)
                    continue;

                bool isFriend = agent.Team.IsFriendOf(player.Team);
                if (allies != isFriend)
                    continue;

                Vec3 position = agent.Position;
                if (position.DistanceSquared(player.Position) > maxDistanceSquared)
                    continue;

                candidates.Add(agent);
            }

            return candidates
                .OrderBy(a => a.Position.DistanceSquared(player.Position))
                .Take(max)
                .ToArray();
        }

        private static bool IsBattleMission(Mission mission)
        {
            if (mission == null)
                return false;

            try
            {
                return (int)mission.Mode == 2;
            }
            catch
            {
                return false;
            }
        }

        private static string? NormalizeFormation(string? formation)
        {
            string value = (formation ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "" => null,
                "none" => null,
                "all" => "all",
                "infantry" => "infantry",
                "archers" => "archers",
                "cavalry" => "cavalry",
                "horse_archers" => "horse_archers",
                "horsearchers" => "horse_archers",
                _ => null
            };
        }

        private static string? InferFormationFromShout(string? shout)
        {
            string value = CanonicalizeBattleShout(shout);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value.Contains("horse archers") || value.Contains("horse_archers"))
                return "horse_archers";
            if (value.Contains("archers") || value.Contains("arches"))
                return "archers";
            if (value.Contains("infantry"))
                return "infantry";
            if (value.Contains("cavalry"))
                return "cavalry";

            return null;
        }

        private static BattleShoutMovementIntent InferMovementFromShout(string? shout)
        {
            string value = CanonicalizeBattleShout(shout);
            if (string.IsNullOrWhiteSpace(value))
                return BattleShoutMovementIntent.None;

            if (value.Contains("fall back") || value.Contains("fallback"))
                return BattleShoutMovementIntent.FallBack;
            if (value.Contains("retreat"))
                return BattleShoutMovementIntent.Retreat;
            if (value.Contains("advance"))
                return BattleShoutMovementIntent.Advance;
            if (value.Contains("charge"))
                return BattleShoutMovementIntent.Charge;
            if (value.Contains("follow"))
                return BattleShoutMovementIntent.Follow;
            if (value.Contains("stop") || value.Contains("hold position") || value.Contains("hold"))
                return BattleShoutMovementIntent.Stop;

            return BattleShoutMovementIntent.None;
        }

        private static string CanonicalizeBattleShout(string? shout)
        {
            string value = (shout ?? string.Empty).Trim().ToLowerInvariant();
            if (value.Length == 0)
                return string.Empty;

            value = value.Replace("horsearchers", "horse archers");
            value = value.Replace("horse-archers", "horse archers");
            value = value.Replace("archer s", "archers");
            value = value.Replace("arches", "archers");
            value = value.Replace("cavalries", "cavalry");
            return value;
        }

        private static string? NormalizeArrangement(string? arrangement)
        {
            string value = (arrangement ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "" => null,
                "none" => null,
                "line" => "line",
                "shield_wall" => "shield_wall",
                "shieldwall" => "shield_wall",
                "loose" => "loose",
                "column" => "column",
                "square" => "square",
                "circle" => "circle",
                "scatter" => "scatter",
                "skein" => "skein",
                _ => null
            };
        }

        private static string? NormalizeFiring(string? firing)
        {
            string value = (firing ?? string.Empty).Trim().ToLowerInvariant();
            return value switch
            {
                "" => null,
                "none" => null,
                "fire_at_will" => "fire_at_will",
                "hold_fire" => "hold_fire",
                _ => null
            };
        }

        private static InputKey ResolveBattleShoutOpenHotkey()
        {
            return ResolveHotkey(AIConfig.BattleShoutOpenHotkey) ?? InputKey.J;
        }

        private static InputKey? ResolveHotkey(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (Enum.TryParse(raw, true, out InputKey key))
                return key;

            return null;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RF_AIDialog
{
    /// <summary>
    /// One atomic objective within a QuestMechanic.
    /// AtomType is the string key (e.g. "VISIT_SETTLEMENT").
    /// Params holds named parameters specific to each type.
    /// Label is shown in the quest log.
    /// </summary>
    public class QuestAtom
    {
        [JsonProperty("atom")]
        public string AtomType { get; set; } = "";

        [JsonProperty("params")]
        public Dictionary<string, string> Params { get; set; } = new Dictionary<string, string>();

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        // ── param helpers ──────────────────────────────────────────────────

        public string GetParam(string key, string fallback = "")
            => Params.TryGetValue(key, out var v) ? v : fallback;

        public int GetParamInt(string key, int fallback = 0)
            => int.TryParse(GetParam(key), out var n) ? n : fallback;
    }

    /// <summary>
    /// Structured quest mechanic attached to a PendingRequest.
    /// Holds the list of objectives (atoms), per-atom completion state,
    /// and the reward that fires when all objectives are satisfied.
    ///
    /// Persisted via JSON inside NPCContext.PendingRequest — no extra
    /// SaveableTypeDefiner needed.
    /// </summary>
    public class QuestMechanic
    {
        [JsonProperty("quest_kind")]
        public string QuestKind { get; set; } = "";

        [JsonProperty("objectives")]
        public List<QuestAtom> Objectives { get; set; } = new List<QuestAtom>();

        /// <summary>
        /// Parallel bool array tracking which objectives are complete.
        /// Kept in sync with Objectives by QuestAtomEngine.
        /// Persisted so progress survives saves.
        /// </summary>
        [JsonProperty("completed")]
        public List<bool> Completed { get; set; } = new List<bool>();

        /// <summary>Per-atom integer progress (e.g. defeat count).</summary>
        [JsonProperty("progress")]
        public Dictionary<string, int> Progress { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Per-objective list of unique targets already counted for progress-style atoms
        /// such as TALK_TO_PARTY, so the same patrol cannot be counted twice.
        /// </summary>
        [JsonProperty("seen_targets")]
        public Dictionary<string, List<string>> SeenTargets { get; set; } =
            new Dictionary<string, List<string>>();

        [JsonProperty("reward_gold")]
        public int RewardGold { get; set; } = 0;

        [JsonProperty("days")]
        public int DurationDays { get; set; } = 30;

        // ── helpers ────────────────────────────────────────────────────────

        /// <summary>Ensures Completed list matches Objectives length.</summary>
        public void Normalize()
        {
            Objectives ??= new List<QuestAtom>();
            Completed ??= new List<bool>();
            Progress ??= new Dictionary<string, int>();
            SeenTargets ??= new Dictionary<string, List<string>>();

            while (Completed.Count < Objectives.Count)
                Completed.Add(false);
            while (Completed.Count > Objectives.Count)
                Completed.RemoveAt(Completed.Count - 1);
        }

        [JsonIgnore]
        public bool AllCompleted
        {
            get
            {
                Normalize();
                return Objectives.Count > 0 && Completed.All(c => c);
            }
        }

        public bool IsCompleted(int idx)
        {
            Normalize();
            return idx >= 0 && idx < Completed.Count && Completed[idx];
        }

        public void MarkCompleted(int idx)
        {
            Normalize();
            if (idx >= 0 && idx < Completed.Count)
                Completed[idx] = true;
        }

        public int GetProgress(string key) =>
            Progress.TryGetValue(key, out var v) ? v : 0;

        public void IncrementProgress(string key, int amount = 1) =>
            Progress[key] = GetProgress(key) + amount;

        public bool HasSeenTarget(string key, string targetId)
        {
            Normalize();
            return SeenTargets.TryGetValue(key, out var seen) && seen.Contains(targetId);
        }

        public void MarkTargetSeen(string key, string targetId)
        {
            Normalize();
            if (!SeenTargets.TryGetValue(key, out var seen))
            {
                seen = new List<string>();
                SeenTargets[key] = seen;
            }

            if (!seen.Contains(targetId))
                seen.Add(targetId);
        }

        /// <summary>
        /// Index of the first incomplete atom of the given type, or -1.
        /// </summary>
        public int IndexOfFirstIncomplete(string atomType)
        {
            Normalize();
            for (int i = 0; i < Objectives.Count; i++)
                if (!Completed[i] && Objectives[i].AtomType == atomType)
                    return i;
            return -1;
        }

        /// <summary>True if at least one atom of the given type exists and is not complete.</summary>
        public bool HasPendingAtom(string atomType) => IndexOfFirstIncomplete(atomType) >= 0;

        /// <summary>
        /// True when every atom EXCEPT RETURN_TO_NPC is complete.
        /// Used to notify the player to return to the NPC before the final conversation.
        /// </summary>
        [JsonIgnore]
        public bool AllExceptReturnCompleted
        {
            get
            {
                Normalize();
                if (Objectives.Count == 0) return false;
                for (int i = 0; i < Objectives.Count; i++)
                    if (Objectives[i].AtomType != "RETURN_TO_NPC" && !Completed[i])
                        return false;
                return true;
            }
        }

        /// <summary>True if any objective uses RETURN_TO_NPC (quest closes via conversation).</summary>
        [JsonIgnore]
        public bool HasReturnStep =>
            Objectives.Any(a => a.AtomType == "RETURN_TO_NPC");
    }
}

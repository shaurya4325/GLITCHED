using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Networking;

namespace Glitched
{
    /// <summary>
    /// Loads every StreamingAssets/Questions/*.json file at start (so teachers can add questions without opening
    /// Unity) and validates them, logging problems to the Console.
    /// </summary>
    public class QuestionBank : MonoBehaviour
    {
        public static readonly string[] Files = { "CodeCity", "PhysicsValley", "ChemicalChaos", "BioZone", "TheCore", "FinalExam", "Custom",
                                                  "Class11_CS", "Class11_Physics", "Class11_Chemistry", "Class11_Biology" };
        readonly Dictionary<string, QuestionData> byId = new Dictionary<string, QuestionData>();
        readonly List<QuestionData> all = new List<QuestionData>();
        readonly Dictionary<string, LessonData> lessons = new Dictionary<string, LessonData>();
        readonly Dictionary<string, List<QuestionData>> byTopic = new Dictionary<string, List<QuestionData>>();
        public IReadOnlyList<QuestionData> All => all;
        public IEnumerable<LessonData> Lessons => lessons.Values;
        public bool Loaded { get; private set; }
        public int ErrorCount { get; private set; }

        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };

        [Serializable]
        class FileDoc { public string arc; public int version; public List<QuestionData> questions; }

        [Serializable]
        class LessonDoc { public int version; public List<LessonData> topics; }

        const string LessonFile = "Class11";

        void ParseLessons(string json)
        {
            try
            {
                var doc = JsonConvert.DeserializeObject<LessonDoc>(json, Settings);
                if (doc?.topics == null) return;
                foreach (var l in doc.topics) if (l != null && !string.IsNullOrEmpty(l.id)) lessons[l.id] = l;
            }
            catch (Exception e) { ErrorCount++; Debug.LogError($"[Questions] Could not read Lessons/{LessonFile}.json: {e.Message}"); }
        }

        public void LoadAll()
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer)
                StartCoroutine(LoadRemote());
            else
            {
                foreach (var f in Files)
                {
                    string path = Path.Combine(Application.streamingAssetsPath, "Questions", f + ".json");
                    if (File.Exists(path)) Parse(f, File.ReadAllText(path));
                }
                string lp = Path.Combine(Application.streamingAssetsPath, "Lessons", LessonFile + ".json");
                if (File.Exists(lp)) ParseLessons(File.ReadAllText(lp));
                Finish();
            }
        }

        IEnumerator LoadRemote()
        {
            foreach (var f in Files)
            {
                string url = Path.Combine(Application.streamingAssetsPath, "Questions", f + ".json");
                using (var req = UnityWebRequest.Get(url))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) Parse(f, req.downloadHandler.text);
                }
            }
            using (var req = UnityWebRequest.Get(Path.Combine(Application.streamingAssetsPath, "Lessons", LessonFile + ".json")))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) ParseLessons(req.downloadHandler.text);
            }
            Finish();
        }

        void Parse(string file, string json)
        {
            try
            {
                var doc = JsonConvert.DeserializeObject<FileDoc>(json, Settings);
                if (doc?.questions == null) { Debug.LogError($"[Questions] {file}.json has no 'questions' array."); ErrorCount++; return; }
                foreach (var q in doc.questions)
                {
                    if (q == null) continue;
                    if (string.IsNullOrEmpty(q.arc)) q.arc = doc.arc;
                    var errors = Validate(q);
                    if (errors.Count > 0)
                    {
                        ErrorCount += errors.Count;
                        Debug.LogError($"[Questions] {file}.json / {q.id}: " + string.Join("; ", errors));
                        continue;
                    }
                    if (byId.ContainsKey(q.id)) { Debug.LogError($"[Questions] Duplicate id {q.id} in {file}.json"); ErrorCount++; continue; }
                    byId[q.id] = q;
                    all.Add(q);
                    if (!string.IsNullOrEmpty(q.topic))
                    {
                        if (!byTopic.TryGetValue(q.topic, out var tl)) byTopic[q.topic] = tl = new List<QuestionData>();
                        tl.Add(q);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorCount++;
                Debug.LogError($"[Questions] Could not read {file}.json: {e.Message}");
            }
        }

        void Finish()
        {
            Loaded = true;
            Debug.Log($"[Questions] Loaded {all.Count} questions, {lessons.Count} lessons ({ErrorCount} errors).");
        }

        public QuestionData Get(string id) => id != null && byId.TryGetValue(id, out var q) ? q : null;
        public LessonData Lesson(string id) => id != null && lessons.TryGetValue(id, out var l) ? l : null;

        /// <summary>The MCQs of one Class XI topic (empty if none).</summary>
        public IReadOnlyList<QuestionData> ForTopic(string topic) => topic != null && byTopic.TryGetValue(topic, out var l) ? l : (IReadOnlyList<QuestionData>)System.Array.Empty<QuestionData>();

        /// <summary>Checks a question has everything its challenge type needs.</summary>
        public static List<string> Validate(QuestionData q)
        {
            var e = new List<string>();
            if (string.IsNullOrEmpty(q.id)) e.Add("missing id");
            if (string.IsNullOrEmpty(q.concept)) e.Add("missing concept");
            if (string.IsNullOrEmpty(q.prompt)) e.Add("missing prompt");
            if (q.difficulty < 1 || q.difficulty > 5) e.Add("difficulty must be 1-5");
            var d = q.data;
            if (d == null) { e.Add("missing data"); return e; }
            bool Opts() => d.options != null && d.options.Count >= 2 && d.answerIndex >= 0 && d.answerIndex < d.options.Count;
            switch (q.type)
            {
                case ChallengeType.DragDrop:
                    if (string.IsNullOrEmpty(d.template)) e.Add("DragDrop needs 'template' with {0} slots");
                    if (d.items == null || d.items.Count == 0) e.Add("DragDrop needs 'items'");
                    if ((d.answer == null || d.answer.Count == 0) && (d.answerSets == null || d.answerSets.Count == 0)) e.Add("DragDrop needs 'answer' or 'answerSets'");
                    break;
                case ChallengeType.Logic:
                    if (d.mode == "order") { if (d.items == null || d.items.Count < 2) e.Add("Logic/order needs 'items' (correct order)"); }
                    else if (d.mode == "truth") { if (d.rows == null || d.answers == null) e.Add("Logic/truth needs 'rows' and 'answers'"); }
                    else if (!Opts()) e.Add("Logic/choice needs 'options' and a valid 'answerIndex'");
                    break;
                case ChallengeType.Numerical:
                    if (d.tolerance < 0) e.Add("tolerance must be >= 0");
                    break;
                case ChallengeType.Debugging:
                    if (d.lines == null || d.answerIndex < 0 || d.answerIndex >= d.lines.Count) e.Add("Debugging needs 'lines' and a valid 'answerIndex'");
                    break;
                case ChallengeType.Coding:
                    if (d.lines == null || d.lines.Count == 0) e.Add("Coding needs 'lines'");
                    if (d.mode == "blank" && !Opts()) e.Add("Coding/blank needs 'options' and 'answerIndex'");
                    break;
                case ChallengeType.Simulation:
                    if (string.IsNullOrEmpty(d.sim)) e.Add("Simulation needs 'sim'");
                    if (d.mode == "goal" && d.goal == null) e.Add("Simulation/goal needs 'goal'");
                    if (d.mode == "read" && !Opts()) e.Add("Simulation/read needs 'options' and 'answerIndex'");
                    break;
                case ChallengeType.Matching:
                    if (d.left == null || d.right == null || d.left.Count != d.right.Count || d.left.Count < 2) e.Add("Matching needs 'left' and 'right' of equal length");
                    break;
                case ChallengeType.Prediction:
                    if (!Opts()) e.Add("Prediction needs 'options' and 'answerIndex'");
                    break;
                case ChallengeType.ShortAnswer:
                    if ((d.accepted == null || d.accepted.Count == 0) && (d.keywords == null || d.keywords.Count == 0)) e.Add("ShortAnswer needs 'accepted' or 'keywords'");
                    break;
                case ChallengeType.Construction:
                    if (d.parts == null || d.parts.Count == 0) e.Add("Construction needs 'parts'");
                    if (d.mode == "sequence" && (d.sequence == null || d.sequence.Count == 0)) e.Add("Construction/sequence needs 'sequence'");
                    if (d.mode != "sequence" && (d.required == null || d.required.Count == 0)) e.Add("Construction/count needs 'required'");
                    break;
            }
            return e;
        }
    }
}

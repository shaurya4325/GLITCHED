using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A Nexus citizen. While corrupted it is frozen mid-sentence and loops one glitched line forever.
    /// After restoration it moves, talks normally and gives clues.
    /// </summary>
    public class NPC : Interactable
    {
        public string displayName = "Citizen";
        public List<string> corruptedLines = new List<string> { "WELC0ME T0 C0DE C1TY! WELC0ME T0 C0DE C1TY!" };
        public List<string> restoredLines = new List<string> { "Thank you! I can finish my sentences again!" };
        [Tooltip("A clue about the region's concept, said after the first line.")]
        public string clue;
        public Color shirt = new Color(0.5f, 0.6f, 0.9f);
        public bool walkIntoWall;       // the classic glitched-robot gag
        CharacterVisual visual;
        RectTransform loopBubble;
        TextMeshProUGUI loopText;
        float loopTimer;
        int lineIndex;
        Vector3 home;
        bool talking;

        void Start()
        {
            prompt = "Talk";
            home = transform.position;
            var vGo = new GameObject("Visual");
            vGo.transform.SetParent(transform, false);
            visual = vGo.AddComponent<CharacterVisual>();
            var prof = ScriptableObject.CreateInstance<CharacterProfileSO>();
            var baseProf = Services.Db.Character("npc");
            prof.id = "npc"; prof.displayName = displayName; prof.height = Random.Range(1.4f, 1.7f);
            prof.primaryColor = shirt; prof.skinColor = Color.HSVToRGB(Random.Range(0.04f, 0.09f), Random.Range(0.25f, 0.55f), Random.Range(0.5f, 0.95f));
            prof.modelPrefab = baseProf != null ? baseProf.modelPrefab : null;
            var db = Services.Db;
            if (db.citizenModels != null && db.citizenModels.Count > 0)
            {
                // a generated citizen, always the same one for the same NPC
                int k = (int)((uint)(displayName ?? name).GetHashCode() % (uint)db.citizenModels.Count);
                prof.modelPrefab = db.citizenModels[k];
                if (k < db.citizenHeights.Count) prof.height = db.citizenHeights[k];
            }
            prof.animator = baseProf != null ? baseProf.animator : null;
            prof.idleStyle = "calm";
            visual.Build(prof, false);
            visual.Face(Random.value > 0.5f ? 1 : -1);
            GameEvents.CorruptionChanged += OnCorruption;
            OnCorruption(Services.Corruption.Value);
        }

        void OnDestroy() { GameEvents.CorruptionChanged -= OnCorruption; if (loopBubble) Destroy(loopBubble.gameObject); }

        bool Corrupted => Services.Corruption.Value > 0.5f;

        void OnCorruption(float v)
        {
            if (visual == null) return;
            visual.SetFrozen(v > 0.5f && !walkIntoWall);
            visual.SetGlitch(v > 0.5f ? 0.35f : 0f);
        }

        public override void Interact(PlayerController p)
        {
            if (talking) return;
            StartCoroutine(Talk(p));
        }

        IEnumerator Talk(PlayerController p)
        {
            talking = true;
            p.Lock(this);
            visual.SetTalking(true);
            var box = Services.UI.Dialogue;
            if (Corrupted)
            {
                string line = corruptedLines[lineIndex % corruptedLines.Count];
                yield return box.Say(displayName, line);
                yield return box.Say(displayName, line);
                yield return box.Say("aarav", "...Are you okay?", "flustered");
                yield return box.Say(displayName, line);
                if (!string.IsNullOrEmpty(clue)) yield return box.Say("pixel", "He's stuck. But listen between the glitches: " + clue, "normal");
            }
            else
            {
                string line = restoredLines[lineIndex % restoredLines.Count];
                yield return box.Say(displayName, line);
                if (!string.IsNullOrEmpty(clue) && lineIndex == 0) yield return box.Say(displayName, "Oh, and a tip: " + clue);
            }
            lineIndex++;
            box.Show(false);
            visual.SetTalking(false);
            p.Unlock(this);
            talking = false;
        }

        void Update()
        {
            if (visual == null) return;
            if (walkIntoWall && Corrupted)
            {
                // walks into an invisible wall, bonks, turns around, repeats. Forever.
                float t = Mathf.Repeat(Time.time * 0.6f + home.x, 2f);
                float x = t < 1f ? t : 2f - t;
                transform.position = home + Vector3.right * x * 1.2f;
                visual.SetLocomotionVelocity(0.6f * 1.2f, true, 0);
                visual.Face(t < 1f ? 1 : -1);
                if (Mathf.Abs(t - 1f) < 0.02f) { visual.Trigger("Hit"); }
            }
            else if (!Corrupted)
            {
                // a stroll: walk to one end, pause, walk back, at a real walking speed so the feet stay planted
                float t = Mathf.Repeat(Time.time * 0.12f + home.x * 0.37f, 1f);
                float phase = t < 0.4f ? t / 0.4f : t < 0.5f ? 1f : t < 0.9f ? 1f - (t - 0.5f) / 0.4f : 0f;
                float x = Mathf.SmoothStep(-1.8f, 1.8f, phase);
                float v = (x - (transform.position.x - home.x)) / Mathf.Max(Time.deltaTime, 1e-4f);
                transform.position = home + Vector3.right * x;
                visual.SetLocomotionVelocity(v, true, 0);
                if (Mathf.Abs(v) > 0.05f) visual.Face(v);
            }

            // frozen mid-sentence: a looping speech fragment above the head
            if (Corrupted && PlayerController.Instance != null && Vector2.Distance(PlayerController.Instance.transform.position, transform.position) < 9f)
            {
                if (loopBubble == null) BuildLoopBubble();
                loopBubble.gameObject.SetActive(true);
                loopTimer -= Time.deltaTime;
                if (loopTimer <= 0)
                {
                    loopTimer = 1.6f;
                    string l = corruptedLines[0];
                    int cut = Mathf.Clamp(l.Length / 2 + Random.Range(-4, 4), 4, l.Length);
                    loopText.text = GlitchText.Corrupt(l.Substring(0, cut), 0.25f) + "—";
                }
            }
            else if (loopBubble != null) loopBubble.gameObject.SetActive(false);
        }

        void BuildLoopBubble()
        {
            loopBubble = UIFactory.Rect("NPCLoop", Services.UI.WorldLayer);
            loopBubble.sizeDelta = new Vector2(460, 64);
            var bg = UIFactory.Panel(loopBubble, new Color(0.08f, 0.02f, 0.08f, 0.85f), 14, "Bg");
            UIFactory.Stretch(bg.rectTransform);
            loopText = UIFactory.Text(bg.transform, "", 22, UIFactory.Theme.magenta, TextAlignmentOptions.Center);
            UIFactory.Stretch(loopText.rectTransform, 10, 4, 10, 4);
            loopBubble.gameObject.AddComponent<WorldFollower>().Init(transform, new Vector3(0, 2.3f, 0));
        }
    }
}

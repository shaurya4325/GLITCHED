using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Pixel, the floating companion: follows the player with a spring-damped offset and bobs, shows moods on its
    /// screen-eye, and delivers the tutorial and most of the comedy in speech bubbles. Never fully explains anything.
    /// </summary>
    public class Companion : MonoBehaviour
    {
        public static Companion Instance { get; private set; }
        public Vector3 offset = new Vector3(-1.1f, 2.1f, -0.6f);
        public float stiffness = 9f, damping = 0.7f;

        Transform target;
        Vector3 vel;
        Renderer screen;
        Transform body, model;
        MaterialPropertyBlock mpb;
        RectTransform bubble;
        TextMeshProUGUI bubbleText;
        GlitchText bubbleGlitch;
        Coroutine sayRoutine;
        string mood = "normal";
        float moodUntil;
        float idleTimer = 25f;
        public Vector3? holdPosition;

        static readonly string[] IdleLines =
        {
            "Did you know the Nexus has 4 regions? I know. I'm very knowledgeable.",
            "If you see a robot walking into a wall, don't help it. It's doing its best.",
            "The world is ending. Anyway, how's your day?",
            "I'm not floating. I'm just very good at standing on air.",
            "Ask me anything. I won't answer, but you can ask.",
            "Sometimes I count in binary to relax. 1, 10, 11, 100...",
        };

        public static Companion Spawn(Transform player)
        {
            if (Instance != null) { Instance.target = player; Instance.Warp(); return Instance; }
            var go = new GameObject("Pixel");
            var c = go.AddComponent<Companion>();
            c.target = player;
            c.Build();
            c.Warp();
            return c;
        }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; if (bubble) Destroy(bubble.gameObject); }

        void Build()
        {
            var prof = Services.Db.Character("pixel");
            if (prof != null && prof.modelPrefab != null)
            {
                model = Instantiate(prof.modelPrefab, transform).transform;
                CharacterVisual.Normalize(model, prof.height);
                screen = model.GetComponentInChildren<Renderer>();
                body = model;
            }
            else model = PlaceholderFactory.BuildCompanion(transform, out screen, out body);
            mpb = new MaterialPropertyBlock();
            SetMood("normal");

            bubble = UIFactory.Rect("PixelBubble", Services.UI.WorldLayer);
            bubble.sizeDelta = new Vector2(620, 120);
            var p = UIFactory.Panel(bubble, new Color(0.03f, 0.05f, 0.09f, 0.95f), 18, "Bubble");
            UIFactory.Stretch(p.rectTransform);
            var tag = UIFactory.Text(p.transform, "PIXEL", 18, UIFactory.Theme.cyan, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(tag.rectTransform, new Vector2(0, 1), new Vector2(18, -8), new Vector2(200, 24), new Vector2(0, 1));
            bubbleText = UIFactory.Text(p.transform, "", 26, null, TextAlignmentOptions.TopLeft);
            UIFactory.Stretch(bubbleText.rectTransform, 18, 34, 18, 10);
            bubbleGlitch = bubbleText.gameObject.AddComponent<GlitchText>();
            bubbleGlitch.intensityOverride = 0.15f;
            bubbleGlitch.onCharacter = () => Services.Audio.PlayBlip("blip_pixel", 1.8f);
            var f = bubble.gameObject.AddComponent<WorldFollower>();
            f.Init(transform, new Vector3(0, 0.9f, 0));
            f.clampToScreen = true;
            bubble.gameObject.SetActive(false);
        }

        public void Warp()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            vel = Vector3.zero;
        }

        void Update()
        {
            if (target == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var pc = target.GetComponent<PlayerController>();
            float facing = pc != null ? pc.Facing : 1f;
            Vector3 desired = holdPosition ?? (target.position + new Vector3(offset.x * facing, offset.y * (pc != null && pc.IsShrunk ? 0.6f : 1f), offset.z));
            // spring-damper follow
            Vector3 diff = desired - transform.position;
            vel += diff * stiffness * dt;
            vel *= Mathf.Pow(1f - damping, dt * 10f);
            transform.position += vel * dt;
            if (model != null)
            {
                float bob = Mathf.Sin(Time.time * 2.4f) * 0.12f;
                model.localPosition = new Vector3(0, bob, 0);
                float tilt = Mathf.Clamp(-vel.x * 4f, -25f, 25f);
                float yaw = facing > 0 ? 160f : 200f;
                model.localRotation = Quaternion.Slerp(model.localRotation, Quaternion.Euler(0, yaw, tilt), dt * 6f);
            }
            if (mood != "normal" && Time.unscaledTime > moodUntil) SetMood("normal");
            if (Random.value < dt * 0.15f && mood == "normal") Blink();

            idleTimer -= dt;
            if (idleTimer <= 0)
            {
                idleTimer = Random.Range(35f, 60f);
                if (pc != null && !pc.Locked && !Services.UI.Dialogue.Open) Say(IdleLines[Random.Range(0, IdleLines.Length)], 4f);
            }
        }

        void Blink()
        {
            SetMood("blink", 0.12f);
        }

        public void SetMood(string m, float seconds = 0f)
        {
            mood = m;
            moodUntil = seconds > 0 ? Time.unscaledTime + seconds : float.MaxValue;
            if (m == "normal") moodUntil = 0;
            if (screen == null) return;
            var tex = Resources.Load<Texture2D>("Faces/Pixel/eye_" + m) ?? Resources.Load<Texture2D>("Faces/Pixel/eye_normal");
            screen.GetPropertyBlock(mpb);
            if (tex != null) mpb.SetTexture("_BaseMap", tex);
            mpb.SetColor("_BaseColor", m == "alarm" || m == "error" ? new Color(2f, 0.6f, 1.6f) : new Color(1.6f, 1.8f, 2f));
            screen.SetPropertyBlock(mpb);
        }

        /// <summary>Pixel says something in a speech bubble (does not block gameplay).</summary>
        /// <param name="voiceText">The line to look up a recording for, when the shown text has runtime extras appended.</param>
        public static void Say(string text, float seconds = 3.5f, string mood = "happy", string voiceText = null)
        {
            if (Instance == null || string.IsNullOrEmpty(text)) return;
            Instance.SayInternal(text, seconds, mood, voiceText ?? text);
        }

        void SayInternal(string text, float seconds, string m, string voiceText)
        {
            if (sayRoutine != null) StopCoroutine(sayRoutine);
            sayRoutine = StartCoroutine(SayRoutine(text, seconds, m, voiceText));
        }

        IEnumerator SayRoutine(string text, float seconds, string m, string voiceText)
        {
            bubble.gameObject.SetActive(true);
            bubble.localScale = new Vector3(0.6f, 0.6f, 1);
            Tween.Scale(bubble, Vector3.one, 0.25f);
            bubbleGlitch.Show(text);
            SetMood(m, seconds);
            float total = seconds + text.Length / 42f;
            float voiceLength = 0f;
            Services.Audio.SpeakLine("pixel", voiceText, clip => voiceLength = clip.length);
            for (float t = 0; t < total || t < voiceLength + 0.6f; t += Time.unscaledDeltaTime) yield return null;   // stay up while the line plays
            Tween.Scale(bubble, new Vector3(0.6f, 0.6f, 1), 0.15f, Ease.InQuad).OnComplete(() => { if (bubble) bubble.gameObject.SetActive(false); });
            sayRoutine = null;
        }

        public static void Hide() { if (Instance != null && Instance.bubble != null) Instance.bubble.gameObject.SetActive(false); }
    }
}

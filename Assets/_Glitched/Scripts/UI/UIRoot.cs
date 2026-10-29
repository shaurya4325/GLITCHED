using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The persistent UI canvas (1920x1080 reference, match 0.5) with layers for HUD, world UI, challenge panels,
    /// dialogue, menus, popups, the glitch overlay, fades and the debug overlay.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public RectTransform Safe { get; private set; }
        public RectTransform HudLayer { get; private set; }
        /// <summary>UI owned by the current scene (title menu, world map...). Cleared on every scene load.</summary>
        public RectTransform SceneLayer { get; private set; }
        public RectTransform WorldLayer { get; private set; }
        public RectTransform PanelLayer { get; private set; }
        public RectTransform DialogueLayer { get; private set; }
        public RectTransform MenuLayer { get; private set; }
        public RectTransform PopupLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }
        public FadeController Fade { get; private set; }
        public HUD Hud { get; private set; }
        public TouchControls Touch { get; private set; }
        public DebugOverlay Debug { get; private set; }
        public PauseMenu Pause { get; private set; }
        public DialogueBox Dialogue { get; private set; }
        public BattleLog Log { get; private set; }

        RawImage glitchOverlay;
        Material glitchMat;
        readonly Queue<string> toasts = new Queue<string>();
        bool toastBusy;

        public void Init()
        {
            gameObject.layer = 5;
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(transform, false);
            esGo.AddComponent<EventSystem>();
            var module = esGo.AddComponent<InputSystemUIInputModule>();
            module.deselectOnBackgroundClick = false;

            Safe = UIFactory.Stretch(UIFactory.Rect("SafeArea", transform));
            Safe.gameObject.AddComponent<SafeArea>();
            SceneLayer = Layer("Scene");
            HudLayer = Layer("HUD");
            WorldLayer = Layer("WorldUI");
            PanelLayer = Layer("Panels");
            DialogueLayer = Layer("Dialogue");
            MenuLayer = Layer("Menus");
            PopupLayer = Layer("Popups");

            OverlayLayer = UIFactory.Stretch(UIFactory.Rect("Overlay", transform));
            glitchOverlay = UIFactory.Rect("GlitchOverlay", OverlayLayer).gameObject.AddComponent<RawImage>();
            UIFactory.Stretch(glitchOverlay.rectTransform);
            glitchOverlay.raycastTarget = false;
            var lib = Services.Db.materials;
            if (lib != null && lib.uiGlitch != null) glitchMat = new Material(lib.uiGlitch);
            else { var sh = Shader.Find("Glitched/UIGlitch"); if (sh != null) glitchMat = new Material(sh); }
            if (glitchMat != null) glitchOverlay.material = glitchMat;
            else glitchOverlay.enabled = false;

            Fade = UIFactory.Stretch(UIFactory.Rect("Fade", transform)).gameObject.AddComponent<FadeController>();
            Fade.Build();

            Log = UIFactory.Rect("BattleLog", HudLayer).gameObject.AddComponent<BattleLog>();
            Log.Build();
            Hud = UIFactory.Stretch(UIFactory.Rect("HUD", HudLayer)).gameObject.AddComponent<HUD>();
            Hud.Build();
            Hud.Show(false);
            Dialogue = UIFactory.Stretch(UIFactory.Rect("DialogueBox", DialogueLayer)).gameObject.AddComponent<DialogueBox>();
            Dialogue.Build();
            Touch = UIFactory.Stretch(UIFactory.Rect("TouchControls", HudLayer)).gameObject.AddComponent<TouchControls>();
            Touch.Build();
            Pause = UIFactory.Stretch(UIFactory.Rect("Pause", MenuLayer)).gameObject.AddComponent<PauseMenu>();
            Pause.Build();
            var dbgRt = UIFactory.Rect("Debug", transform);
            Debug = dbgRt.gameObject.AddComponent<DebugOverlay>();
            Debug.Build();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, mode) => { if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single) OnSceneChanged(); };
        }

        void OnSceneChanged()
        {
            SubScreen.CloseAll();
            UIFactory.Clear(SceneLayer);
            UIFactory.Clear(PanelLayer);
            for (int i = WorldLayer.childCount - 1; i >= 0; i--)
            {
                var c = WorldLayer.GetChild(i);
                if (c.GetComponent<PooledTag>() == null) Destroy(c.gameObject); else Pool.Release(c.gameObject);
            }
            Dialogue.Show(false);
            Dialogue.ClearSkip();
            Hud.Show(false);
            Hud.HideBoss();
            Touch.SetVisible(false);
            Log.Clear();
            if (Pause.IsOpen) Pause.Close(true);
        }

        RectTransform Layer(string name) => UIFactory.Stretch(UIFactory.Rect(name, Safe));

        void Update()
        {
            // F8 anywhere: Voice Studio (record your own dialogue)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame && !CaptureRunner.Active) VoiceStudio.Open();
            TimeController.Tick();
            if (glitchMat != null)
            {
                bool heavy = Services.Graphics.UIOverlayGlitch;
                float c = Services.Corruption != null ? Services.Corruption.Value : 0f;
                float scan = Services.Settings.Data.reduceFlashing ? 0.02f : 0.05f + c * 0.04f;
                glitchMat.SetFloat("_Scanlines", scan);
                glitchMat.SetFloat("_Intensity", heavy ? GlitchFX.Level : GlitchFX.Level * 0.35f);
                var t = UIFactory.Theme;
                glitchMat.SetColor("_Accent", t.magenta);
                glitchMat.SetColor("_Accent2", t.cyan);
            }
            if (Services.Input.DebugPressed) Debug.Toggle();
        }

        // ------------------------------------------------------------------ toasts
        public void Toast(string message)
        {
            toasts.Enqueue(message);
            if (!toastBusy) StartCoroutine(ToastRoutine());
        }

        IEnumerator ToastRoutine()
        {
            toastBusy = true;
            while (toasts.Count > 0)
            {
                string msg = toasts.Dequeue();
                var p = UIFactory.Panel(PopupLayer, new Color(0.03f, 0.04f, 0.08f, 0.95f), 16, "Toast");
                var rt = UIFactory.Place(p.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 80), new Vector2(900, 84), new Vector2(0.5f, 1f));
                var t = UIFactory.Text(p.transform, msg, 28, null, TextAlignmentOptions.Center);
                UIFactory.Stretch(t.rectTransform, 20, 8, 20, 8);
                var cg = p.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0;
                Tween.Fade(cg, 1, 0.2f);
                Tween.Anchored(rt, new Vector2(0, -30), 0.35f, Ease.OutBack);
                yield return new WaitForSecondsRealtime(2.4f);
                Tween.Fade(cg, 0, 0.25f);
                yield return new WaitForSecondsRealtime(0.3f);
                Destroy(p.gameObject);
            }
            toastBusy = false;
        }

        // ------------------------------------------------------------------ confirm dialog
        public void Confirm(string title, string body, Action yes, Action no = null, string yesLabel = "YES", string noLabel = "NO")
        {
            var root = UIFactory.Stretch(UIFactory.Rect("Confirm", PopupLayer));
            UIFactory.Blocker(root, 0.6f);
            var panel = UIFactory.Panel(root, null, 22, "Panel");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 380));
            var v = UIFactory.VLayout(panel, 18, 36);
            var tt = UIFactory.Text(panel.transform, title, 40, UIFactory.Theme.cyan, TextAlignmentOptions.Center, true);
            UIFactory.Size(tt, -1, 60);
            var bt = UIFactory.Text(panel.transform, body, 28, UIFactory.Theme.textDim, TextAlignmentOptions.Center);
            UIFactory.Size(bt, -1, 90);
            var row = UIFactory.Rect("Buttons", panel.transform);
            UIFactory.Size(row, -1, 90);
            UIFactory.HLayout(row, 24);
            var y = UIFactory.Button(row, yesLabel, () => { Destroy(root.gameObject); yes?.Invoke(); });
            UIFactory.Button(row, noLabel, () => { Destroy(root.gameObject); no?.Invoke(); }, ButtonStyle.Secondary);
            UIFactory.Select(y);
            panel.transform.localScale = Vector3.one * 0.85f;
            Tween.Scale(panel.transform, Vector3.one, 0.3f);
        }

        public void AchievementPopup(AchievementSO a)
        {
            var p = UIFactory.Panel(PopupLayer, new Color(0.08f, 0.05f, 0.12f, 0.97f), 18, "Achievement");
            var rt = UIFactory.Place(p.rectTransform, new Vector2(1f, 1f), new Vector2(600, -150), new Vector2(560, 130), new Vector2(1, 1));
            var icon = UIFactory.Image(p.transform, UIFactory.Theme.yellow, UISprites.Circle(64), "Icon");
            UIFactory.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(72, 72), new Vector2(0.5f, 0.5f));
            var star = UIFactory.Text(icon.transform, "★", 44, new Color(0.2f, 0.1f, 0f), TextAlignmentOptions.Center, true);
            UIFactory.Stretch(star.rectTransform);
            var t1 = UIFactory.Text(p.transform, "ACHIEVEMENT UNLOCKED", 18, UIFactory.Theme.yellow, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(t1.rectTransform, new Vector2(0, 1), new Vector2(116, -16), new Vector2(430, 30), new Vector2(0, 1));
            var t2 = UIFactory.Text(p.transform, $"<b>{a.title}</b>\n<size=70%>{a.description}</size>", 26, null, TextAlignmentOptions.TopLeft);
            UIFactory.Place(t2.rectTransform, new Vector2(0, 1), new Vector2(116, -44), new Vector2(430, 80), new Vector2(0, 1));
            Tween.Anchored(rt, new Vector2(-24, -150), 0.45f, Ease.OutBack);
            Tween.After(4f, () => { if (rt) Tween.Anchored(rt, new Vector2(600, -150), 0.4f, Ease.InCubic).OnComplete(() => { if (p) Destroy(p.gameObject); }); });
        }

        /// <summary>A big centred banner (ATTACK!, CRITICAL!, LEVEL 3, AREA NAMES...).</summary>
        public void Banner(string text, Color color, float size = 110, float hold = 0.7f, string sub = null)
        {
            var rt = UIFactory.Rect("Banner", PopupLayer);
            UIFactory.Place(rt, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1600, 260));
            var t = UIFactory.Text(rt, text, size, color, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold | FontStyles.Italic;
            t.outlineWidth = 0.18f;
            t.outlineColor = new Color32(10, 5, 20, 255);
            if (!string.IsNullOrEmpty(sub))
            {
                var s = UIFactory.Text(rt, sub, 34, UIFactory.Theme.text, TextAlignmentOptions.Center);
                UIFactory.Place(s.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -10), new Vector2(1500, 60), new Vector2(0.5f, 1f));
            }
            var g = t.gameObject.AddComponent<GlitchText>();
            g.typewriter = false; g.intensityOverride = 0.5f;
            g.Show(text, true);
            rt.localScale = Vector3.one * 1.6f;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            Tween.Scale(rt, Vector3.one, 0.25f, Ease.OutBack);
            Tween.After(hold, () => { if (cg) Tween.Fade(cg, 0, 0.3f).OnComplete(() => { if (rt) Destroy(rt.gameObject); }); });
        }

        /// <summary>Floating damage number at a world position.</summary>
        public void DamageNumber(Vector3 world, string text, Color color, float size = 64)
        {
            var cam = CameraRig.Instance != null ? CameraRig.Instance.Cam : Camera.main;
            if (cam == null) return;
            var go = Pool.Get("dmgnum", () =>
            {
                var r = UIFactory.Rect("DamageNumber", WorldLayer);
                var tt = UIFactory.Text(r, "", 64, Color.white, TextAlignmentOptions.Center, true);
                UIFactory.Stretch(tt.rectTransform);
                tt.outlineWidth = 0.22f; tt.outlineColor = new Color32(10, 5, 20, 255);
                tt.fontStyle = FontStyles.Bold;
                r.sizeDelta = new Vector2(500, 120);
                return r.gameObject;
            }, Vector3.zero, Quaternion.identity, WorldLayer);
            var rt = (RectTransform)go.transform;
            var t = go.GetComponentInChildren<TextMeshProUGUI>();
            t.text = text; t.color = color; t.fontSize = size;
            var follow = go.GetComponent<WorldFollower>() ?? go.AddComponent<WorldFollower>();
            follow.Init(world, new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), 0, 0));
            rt.localScale = Vector3.one * 1.5f;
            Tween.Scale(rt, Vector3.one, 0.25f, Ease.OutBack);
            float start = Time.unscaledTime;
            Tween.Raw(1.1f, k => { if (follow) follow.rise = k * 1.6f; if (t) t.alpha = k < 0.7f ? 1 : 1 - (k - 0.7f) / 0.3f; }).OnComplete(() => Pool.Release(go));
        }
    }
}

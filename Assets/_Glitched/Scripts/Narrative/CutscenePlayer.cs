using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Helpers for code-driven cutscenes (30-90 s, at least one joke, skippable): letterbox bars, camera shots on the
    /// rig's cutscene camera, dialogue, and hold-Esc / hold-tap to skip.
    /// </summary>
    public static class CutscenePlayer
    {
        public static bool Active { get; private set; }
        static RectTransform top, bottom, skipHint;
        static float skipHold;

        public static bool Skipping
        {
            get
            {
                if (!Active) return false;
                if (Services.Input.PauseHeld || (Services.Input.AdvanceHeld && Services.Input.UsingTouch)) skipHold += Time.unscaledDeltaTime; else skipHold = 0;
                return skipHold > 0.8f || Services.UI.Dialogue.SkipRequested;
            }
        }

        /// <summary>
        /// When above 0, dialogue lines move on by themselves after a reading pause (scaled by this): the cutscene
        /// plays like a film, no clicks or keys needed. Cleared by End().
        /// </summary>
        public static float AutoAdvance;

        public static void Begin()
        {
            Active = true;
            skipHold = 0;
            PauseMenu.Allowed = false;
            Services.UI.Hud.Show(false);
            Services.UI.Touch.SetVisible(false);
            Services.UI.Dialogue.ClearSkip();
            if (PlayerController.Instance != null) PlayerController.Instance.Lock("cutscene");
            var cam = CameraRig.Instance;
            if (cam != null)
            {
                cam.CutsceneCam.transform.SetPositionAndRotation(cam.Cam.transform.position, cam.Cam.transform.rotation);
                var lens = cam.CutsceneCam.Lens; lens.FieldOfView = cam.Cam.fieldOfView; cam.CutsceneCam.Lens = lens;
                cam.SetCutscene(true);
            }
            if (top == null)
            {
                top = Bar("Top", new Vector2(0.5f, 1));
                bottom = Bar("Bottom", new Vector2(0.5f, 0));
                skipHint = UIFactory.Rect("SkipHint", Services.UI.OverlayLayer);
                UIFactory.Place(skipHint, new Vector2(1, 0), new Vector2(-30, 24), new Vector2(520, 50), new Vector2(1, 0));
                var t = UIFactory.Text(skipHint, InputHub.IsMobile ? "hold the screen to skip" : "hold ESC to skip", 22, new Color(1, 1, 1, 0.5f), TextAlignmentOptions.BottomRight, true);
                UIFactory.Stretch(t.rectTransform);
            }
            top.gameObject.SetActive(true); bottom.gameObject.SetActive(true); skipHint.gameObject.SetActive(true);
            Tween.Anchored(top, Vector2.zero, 0.5f);
            Tween.Anchored(bottom, Vector2.zero, 0.5f);
        }

        static RectTransform Bar(string name, Vector2 anchor)
        {
            var img = UIFactory.Image(Services.UI.OverlayLayer, Color.black, null, "Letterbox" + name);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0, anchor.y); rt.anchorMax = new Vector2(1, anchor.y); rt.pivot = new Vector2(0.5f, anchor.y);
            rt.sizeDelta = new Vector2(0, 110);
            rt.anchoredPosition = new Vector2(0, anchor.y > 0.5f ? 120 : -120);
            return rt;
        }

        public static void End()
        {
            Active = false;
            AutoAdvance = 0f;
            PostFXController.CutsceneFocus = -1f;
            PauseMenu.Allowed = true;
            Services.UI.Dialogue.Show(false);
            Services.UI.Dialogue.ClearSkip();
            if (top != null)
            {
                Tween.Anchored(top, new Vector2(0, 120), 0.4f);
                Tween.Anchored(bottom, new Vector2(0, -120), 0.4f);
                skipHint.gameObject.SetActive(false);
            }
            CameraRig.Instance?.SetCutscene(false);
            if (PlayerController.Instance != null) PlayerController.Instance.Unlock("cutscene");
            Services.UI.Hud.Show(PlayerController.Instance != null);
            Services.UI.Touch.SetVisible(PlayerController.Instance != null);
        }

        /// <summary>Moves the cutscene camera to a shot over 'seconds'.</summary>
        public static IEnumerator Shot(Vector3 position, Vector3 lookAt, float fov, float seconds, Ease ease = Ease.InOutSine)
        {
            var cam = CameraRig.Instance;
            if (cam == null) yield break;
            var t = cam.CutsceneCam.transform;
            PostFXController.CutsceneFocus = Vector3.Distance(position, lookAt);   // the lens focuses on what the shot looks at
            Vector3 p0 = t.position; Quaternion r0 = t.rotation;
            float f0 = cam.CutsceneCam.Lens.FieldOfView;
            Quaternion r1 = Quaternion.LookRotation(lookAt - position);
            if (seconds <= 0) { t.SetPositionAndRotation(position, r1); SetFov(fov); yield break; }
            for (float x = 0; x < seconds; x += Time.deltaTime)
            {
                if (Skipping) break;
                float k = Easing.Evaluate(ease, x / seconds);
                t.SetPositionAndRotation(Vector3.LerpUnclamped(p0, position, k), Quaternion.SlerpUnclamped(r0, r1, k));
                SetFov(Mathf.Lerp(f0, fov, k));
                yield return null;
            }
            t.SetPositionAndRotation(position, r1);
            SetFov(fov);
        }

        static void SetFov(float f)
        {
            var cam = CameraRig.Instance;
            var lens = cam.CutsceneCam.Lens; lens.FieldOfView = f; cam.CutsceneCam.Lens = lens;
        }

        /// <summary>
        /// Character introduction (snap zoom + freeze frame + name card), like the intro shots in Borderlands or
        /// Persona: the camera snaps onto the character, time freezes in a punchy desaturated frame, the name slams in.
        /// </summary>
        public static IEnumerator IntroCard(Transform subject, Vector3 camOffset, Vector3 lookOffset, string name, string subtitle, Color accent, float fov = 26f)
        {
            if (Skipping || subject == null) yield break;
            Services.Audio.Play("whoosh");
            var target = subject.position + lookOffset;
            yield return Shot(target + camOffset, target, fov, 0.16f, Ease.OutCubic);
            // freeze
            float prevScale = Time.timeScale;
            Time.timeScale = 0f;
            PostFXController.Freeze = 1f;
            GlitchFX.Pulse(0.22f, 0.12f);
            Services.Audio.Play("crit");
            Services.UI.Dialogue.Show(false);   // the card owns the screen
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("IntroCard", Services.UI.PopupLayer));
            var slash = UIFactory.Image(root, accent, null, "Slash");
            UIFactory.Place(slash.rectTransform, new Vector2(0f, 0.42f), new Vector2(-2400, 0), new Vector2(2400, 150), new Vector2(0f, 0.5f));
            slash.rectTransform.localRotation = Quaternion.Euler(0, 0, 6f);
            var nameT = UIFactory.Text(root, name.ToUpperInvariant(), 150, Color.white, TMPro.TextAlignmentOptions.Left, true);
            UIFactory.Place(nameT.rectTransform, new Vector2(0.06f, 0.44f), new Vector2(-900, 0), new Vector2(1500, 180), new Vector2(0f, 0.5f));
            nameT.rectTransform.localRotation = Quaternion.Euler(0, 0, 6f);
            var subT = UIFactory.Text(root, subtitle, 34, th.text, TMPro.TextAlignmentOptions.Left);
            UIFactory.Place(subT.rectTransform, new Vector2(0.065f, 0.34f), new Vector2(-900, 0), new Vector2(1500, 60), new Vector2(0f, 0.5f));
            subT.rectTransform.localRotation = Quaternion.Euler(0, 0, 6f);
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            // slam in
            for (float x = 0; x < 0.22f; x += Time.unscaledDeltaTime)
            {
                float k = Easing.Evaluate(Ease.OutBack, x / 0.22f);
                slash.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-2400, -300, k), 0);
                nameT.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-900, 0, k), 0);
                subT.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-900, 0, Mathf.Clamp01(k * 0.9f)), 0);
                yield return null;
            }
            for (float x = 0; x < 2.1f; x += Time.unscaledDeltaTime) { if (Skipping) break; yield return null; }
            // out
            for (float x = 0; x < 0.2f; x += Time.unscaledDeltaTime) { cg.alpha = 1f - x / 0.2f; PostFXController.Freeze = 1f - x / 0.2f; yield return null; }
            Object.Destroy(root.gameObject);
            PostFXController.Freeze = 0f;
            Time.timeScale = prevScale <= 0f ? 1f : prevScale;
        }

        public static IEnumerator Wait(float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime) { if (Skipping) yield break; yield return null; }
        }

        public static IEnumerator Say(string speaker, string line, string expression = null)
        {
            if (Skipping) yield break;
            var pc = PlayerController.Instance;
            if (speaker == "aarav" && pc != null && pc.Visual.Expression != null && !string.IsNullOrEmpty(expression)) pc.Visual.Expression.SetExpression(expression);
            if (speaker == "aarav" && pc != null) pc.Visual.SetTalking(true);
            // auto-play: a reading pause after the typewriter finishes, longer for longer lines
            float auto = AutoAdvance > 0f ? Mathf.Clamp(0.9f + line.Length * 0.024f, 1.4f, 3.4f) * AutoAdvance : 0f;
            yield return Services.UI.Dialogue.Say(speaker, line, expression, auto);
            if (pc != null) pc.Visual.SetTalking(false);
        }
    }
}

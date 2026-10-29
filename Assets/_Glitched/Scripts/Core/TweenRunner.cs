using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, OutCubic, InCubic, OutBack, OutElastic, InOutSine, OutBounce }

    /// <summary>A tiny tweening engine (no plugin needed). Use the static Tween class to start tweens.</summary>
    public class TweenRunner : MonoBehaviour
    {
        internal static TweenRunner Instance;
        readonly List<TweenHandle> active = new List<TweenHandle>(64);
        readonly List<TweenHandle> adding = new List<TweenHandle>(16);

        void Awake() => Instance = this;

        internal void Add(TweenHandle h) => adding.Add(h);

        void Update()
        {
            if (adding.Count > 0) { active.AddRange(adding); adding.Clear(); }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var t = active[i];
                if (t.killed || (t.owner == null && t.hadOwner)) { active.RemoveAt(i); continue; }
                float dt = t.unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                if (t.delay > 0) { t.delay -= dt; continue; }
                t.time += dt;
                float k = t.duration <= 0 ? 1 : Mathf.Clamp01(t.time / t.duration);
                try { t.onUpdate?.Invoke(Easing.Evaluate(t.ease, k)); }
                catch (Exception e) { Debug.LogException(e); t.killed = true; }
                if (k >= 1f)
                {
                    active.RemoveAt(i);
                    if (!t.killed) { t.killed = true; t.onComplete?.Invoke(); }
                }
            }
        }
    }

    /// <summary>A running tween. "yield return handle;" in a coroutine waits until it finishes.</summary>
    public class TweenHandle : CustomYieldInstruction
    {
        public override bool keepWaiting => !killed;
        internal float duration, time, delay;
        internal Ease ease;
        internal bool unscaled, killed, hadOwner;
        internal UnityEngine.Object owner;
        internal Action<float> onUpdate;
        internal Action onComplete;
        public bool IsDone => killed;
        public void Kill() => killed = true;
        public TweenHandle OnComplete(Action a) { onComplete += a; return this; }
        public TweenHandle Delay(float d) { delay = d; return this; }
    }

    public static class Easing
    {
        public static float Evaluate(Ease e, float t)
        {
            switch (e)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1 - (1 - t) * (1 - t);
                case Ease.InOutQuad: return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
                case Ease.OutCubic: return 1 - Mathf.Pow(1 - t, 3);
                case Ease.InCubic: return t * t * t;
                case Ease.OutBack: { const float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2); }
                case Ease.OutElastic:
                    if (t <= 0 || t >= 1) return t;
                    return Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
                case Ease.OutBounce:
                    {
                        const float n1 = 7.5625f, d1 = 2.75f;
                        if (t < 1 / d1) return n1 * t * t;
                        if (t < 2 / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                        if (t < 2.5 / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                        t -= 2.625f / d1; return n1 * t * t + 0.984375f;
                    }
                default: return t;
            }
        }
    }

    /// <summary>Start tweens: Tween.Value(0, 1, 0.5f, v => ..., Ease.OutQuad).</summary>
    public static class Tween
    {
        public static TweenHandle Value(float from, float to, float duration, Action<float> onUpdate, Ease ease = Ease.OutQuad, bool unscaled = true, UnityEngine.Object owner = null)
        {
            var h = new TweenHandle
            {
                duration = duration, ease = ease, unscaled = unscaled, owner = owner, hadOwner = owner != null,
                onUpdate = k => onUpdate(Mathf.LerpUnclamped(from, to, k))
            };
            if (TweenRunner.Instance == null) { onUpdate(to); h.killed = true; return h; }
            TweenRunner.Instance.Add(h);
            return h;
        }

        public static TweenHandle Raw(float duration, Action<float> onUpdate01, Ease ease = Ease.Linear, bool unscaled = true, UnityEngine.Object owner = null)
            => Value(0, 1, duration, onUpdate01, ease, unscaled, owner);

        public static TweenHandle Move(Transform t, Vector3 to, float d, Ease e = Ease.OutQuad, bool unscaled = true)
        {
            Vector3 from = t.position;
            return Raw(d, k => { if (t) t.position = Vector3.LerpUnclamped(from, to, k); }, e, unscaled, t);
        }

        public static TweenHandle MoveLocal(Transform t, Vector3 to, float d, Ease e = Ease.OutQuad, bool unscaled = true)
        {
            Vector3 from = t.localPosition;
            return Raw(d, k => { if (t) t.localPosition = Vector3.LerpUnclamped(from, to, k); }, e, unscaled, t);
        }

        public static TweenHandle Scale(Transform t, Vector3 to, float d, Ease e = Ease.OutBack, bool unscaled = true)
        {
            Vector3 from = t.localScale;
            return Raw(d, k => { if (t) t.localScale = Vector3.LerpUnclamped(from, to, k); }, e, unscaled, t);
        }

        public static TweenHandle Anchored(RectTransform t, Vector2 to, float d, Ease e = Ease.OutCubic, bool unscaled = true)
        {
            Vector2 from = t.anchoredPosition;
            return Raw(d, k => { if (t) t.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, e, unscaled, t);
        }

        public static TweenHandle Fade(CanvasGroup g, float to, float d, Ease e = Ease.OutQuad, bool unscaled = true)
        {
            float from = g.alpha;
            return Raw(d, k => { if (g) g.alpha = Mathf.LerpUnclamped(from, to, k); }, e, unscaled, g);
        }

        public static TweenHandle Color(Graphic g, Color to, float d, Ease e = Ease.OutQuad, bool unscaled = true)
        {
            Color from = g.color;
            return Raw(d, k => { if (g) g.color = UnityEngine.Color.LerpUnclamped(from, to, k); }, e, unscaled, g);
        }

        /// <summary>Calls an action after a delay (unscaled by default).</summary>
        public static TweenHandle After(float seconds, Action a, bool unscaled = true)
            => Raw(seconds, _ => { }, Ease.Linear, unscaled).OnComplete(a);

        public static TweenHandle Punch(Transform t, float amount, float d)
        {
            Vector3 baseScale = t.localScale;
            return Raw(d, k => { if (t) t.localScale = baseScale * (1 + amount * Mathf.Sin(k * Mathf.PI) * (1 - k)); }, Ease.Linear, true, t)
                .OnComplete(() => { if (t) t.localScale = baseScale; });
        }
    }
}

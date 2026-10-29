using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Base for full-screen modal screens (Settings, Mastery, Achievements, Shop, Credits, Save slots).
    /// They stack; Esc / Back closes the top one.
    /// </summary>
    public abstract class SubScreen : MonoBehaviour
    {
        static readonly List<SubScreen> stack = new List<SubScreen>();
        public static bool AnyOpen => stack.Count > 0;
        protected RectTransform Body { get; private set; }
        protected RectTransform Panel { get; private set; }
        System.Action onClosed;

        public static T Create<T>(string title, Vector2 size, System.Action onClosed = null) where T : SubScreen
        {
            var root = UIFactory.Stretch(UIFactory.Rect(typeof(T).Name, Services.UI.MenuLayer));
            root.SetAsLastSibling();
            var s = root.gameObject.AddComponent<T>();
            s.onClosed = onClosed;
            UIFactory.Blocker(root, 0.82f);
            var panel = UIFactory.Panel(root, null, 26, "Panel");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            s.Panel = panel.rectTransform;
            var t = UIFactory.Text(panel.transform, title, 54, UIFactory.Theme.cyan, TextAlignmentOptions.Left, true);
            UIFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(48, -26), new Vector2(size.x - 300, 80), new Vector2(0, 1));
            t.gameObject.AddComponent<GlitchText>().intensityOverride = 0.25f;
            var back = UIFactory.Button(panel.transform, "BACK", () => s.Close(), ButtonStyle.Secondary, 26);
            UIFactory.Place(back.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-36, -30), new Vector2(180, 70), new Vector2(1, 1));
            s.Body = UIFactory.Rect("Body", panel.transform);
            UIFactory.Stretch(s.Body, 48, 120, 48, 40);
            stack.Add(s);
            panel.transform.localScale = Vector3.one * 0.94f;
            Tween.Scale(panel.transform, Vector3.one, 0.22f);
            s.Build();
            return s;
        }

        protected abstract void Build();

        public virtual void Close()
        {
            stack.Remove(this);
            onClosed?.Invoke();
            Destroy(gameObject);
        }

        public static void CloseTop() { if (stack.Count > 0) stack[stack.Count - 1].Close(); }
        public static void CloseAll() { for (int i = stack.Count - 1; i >= 0; i--) if (stack[i] != null) stack[i].Close(); stack.Clear(); }
    }
}

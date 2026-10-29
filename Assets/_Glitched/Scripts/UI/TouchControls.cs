using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Android/touch controls: a virtual joystick (left) and Jump / Interact / Dash / Shrink buttons (right).
    /// They drive the Gamepad bindings of PlayerControls. All buttons are at least 48 dp, and can be
    /// moved and resized in Settings -> Controls -> Edit Touch Layout.
    /// </summary>
    public class TouchControls : MonoBehaviour
    {
        readonly Dictionary<string, RectTransform> controls = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Vector2> defaults = new Dictionary<string, Vector2>();
        CanvasGroup group;
        public bool EditMode { get; private set; }
        bool wantVisible;

        public void Build()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            Stick("stick", new Vector2(250, 250));
            Btn("jump", "JUMP", "<Gamepad>/buttonSouth", new Vector2(-190, 200), 190, UIFactory.Theme.cyan);
            Btn("interact", "E", "<Gamepad>/buttonWest", new Vector2(-400, 150), 140, UIFactory.Theme.yellow);
            Btn("dash", "DASH", "<Gamepad>/buttonEast", new Vector2(-190, 420), 140, UIFactory.Theme.magenta);
            Btn("shrink", "SIZE", "<Gamepad>/leftShoulder", new Vector2(-400, 360), 120, UIFactory.Theme.green);
            ApplyLayout();
            SetVisible(false);
        }

        static bool TouchAvailable => Application.isMobilePlatform || Touchscreen.current != null;

        public void SetVisible(bool on)
        {
            wantVisible = on;
            bool show = on && TouchAvailable && Services.Settings.Data.showTouchControls || EditMode;
            group.alpha = show ? 1 : 0;
            group.blocksRaycasts = show;
            group.interactable = show;
            if (controls.TryGetValue("dash", out var d)) d.gameObject.SetActive(EditMode || Services.Game.HasAbility("dash"));
            if (controls.TryGetValue("shrink", out var s)) s.gameObject.SetActive(EditMode || Services.Game.HasAbility("shrink"));
        }

        void Stick(string id, Vector2 pos)
        {
            var area = UIFactory.Image(transform, new Color(1, 1, 1, 0.06f), UISprites.Circle(256), "Stick");
            var rt = area.rectTransform;
            UIFactory.Place(rt, Vector2.zero, pos, new Vector2(300, 300), new Vector2(0.5f, 0.5f));
            var ring = UIFactory.Image(rt, new Color(UIFactory.Theme.cyan.r, UIFactory.Theme.cyan.g, UIFactory.Theme.cyan.b, 0.5f), UISprites.Circle(256, true, 8), "Ring");
            UIFactory.Stretch(ring.rectTransform);
            var knob = UIFactory.Image(rt, new Color(1, 1, 1, 0.55f), UISprites.Circle(128), "Knob");
            UIFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
            knob.raycastTarget = true;
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 90;
            controls[id] = rt; defaults[id] = pos;
            rt.gameObject.AddComponent<TouchDraggable>().Init(this, id);
        }

        void Btn(string id, string label, string path, Vector2 pos, float size, Color color)
        {
            var img = UIFactory.Image(transform, new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.3f, 0.55f), UISprites.Circle(256), "Btn_" + id);
            img.raycastTarget = true;
            var rt = img.rectTransform;
            UIFactory.Place(rt, new Vector2(1, 0), pos, new Vector2(size, size), new Vector2(0.5f, 0.5f));
            var ring = UIFactory.Image(rt, new Color(color.r, color.g, color.b, 0.85f), UISprites.Circle(256, true, 10), "Ring");
            UIFactory.Stretch(ring.rectTransform);
            var t = UIFactory.Text(rt, label, size * 0.2f, Color.white, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(t.rectTransform);
            var b = img.gameObject.AddComponent<OnScreenButton>();
            b.controlPath = path;
            controls[id] = rt; defaults[id] = pos;
            rt.gameObject.AddComponent<TouchDraggable>().Init(this, id);
        }

        public void ApplyLayout()
        {
            var s = Services.Settings.Data;
            foreach (var kv in controls)
            {
                var e = s.touchLayout.Find(x => x.id == kv.Key);
                kv.Value.anchoredPosition = e != null ? new Vector2(e.x, e.y) : defaults[kv.Key];
                float sc = (e != null ? e.scale : 1f) * s.touchButtonScale;
                kv.Value.localScale = Vector3.one * Mathf.Clamp(sc, 0.6f, 1.8f);
            }
        }

        public void SetEditMode(bool on)
        {
            EditMode = on;
            foreach (var kv in controls)
            {
                foreach (var c in kv.Value.GetComponentsInChildren<OnScreenControl>(true)) c.enabled = !on;
                var img = kv.Value.GetComponent<Image>();
                if (img != null) img.color = on ? new Color(1, 0.3f, 0.8f, 0.4f) : new Color(img.color.r, img.color.g, img.color.b, 0.55f);
            }
            SetVisible(wantVisible);
            if (!on) Services.Settings.Apply();
        }

        public void ResetLayout()
        {
            Services.Settings.Data.touchLayout.Clear();
            Services.Settings.Data.touchButtonScale = 1f;
            ApplyLayout();
            Services.Settings.Apply();
        }

        internal void Store(string id, Vector2 pos, float scale)
        {
            var list = Services.Settings.Data.touchLayout;
            var e = list.Find(x => x.id == id);
            if (e == null) { e = new TouchLayoutEntry { id = id }; list.Add(e); }
            e.x = pos.x; e.y = pos.y; e.scale = scale;
        }
    }
}

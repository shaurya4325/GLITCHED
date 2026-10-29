using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    public enum ButtonStyle { Primary, Secondary, Danger, Ghost, Answer }

    /// <summary>Builds uGUI + TextMeshPro elements in the GLITCHED style (dark panels, cyan/magenta accents).</summary>
    public static class UIFactory
    {
        public static UIThemeSO Theme
        {
            get
            {
                var t = Services.Db != null ? Services.Db.uiTheme : null;
                if (t == null) t = fallbackTheme ?? (fallbackTheme = ScriptableObject.CreateInstance<UIThemeSO>());
                return t;
            }
        }
        static UIThemeSO fallbackTheme;

        public static TMP_FontAsset DisplayFont => Theme.displayFont != null ? Theme.displayFont : TMP_Settings.defaultFontAsset;
        public static TMP_FontAsset BodyFont
        {
            get
            {
                if (Services.Settings != null && Services.Settings.Data.dyslexiaFont && Theme.dyslexicFont != null) return Theme.dyslexicFont;
                return Theme.bodyFont != null ? Theme.bodyFont : TMP_Settings.defaultFontAsset;
            }
        }
        public static TMP_FontAsset MonoFont => Theme.monoFont != null ? Theme.monoFont : BodyFont;

        // ------------------------------------------------------------------ rect helpers
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        // ------------------------------------------------------------------ images and panels
        public static Image Image(Transform parent, Color color, Sprite sprite = null, string name = "Image")
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(Transform parent, Color? color = null, int radius = 18, string name = "Panel", bool outline = true)
        {
            var img = Image(parent, color ?? Theme.panel, UISprites.Rounded(radius), name);
            img.raycastTarget = true;
            if (outline)
            {
                var o = Image(img.transform, new Color(Theme.cyan.r, Theme.cyan.g, Theme.cyan.b, 0.35f), UISprites.Rounded(radius, 2), "Outline");
                Stretch(o.rectTransform);
            }
            return img;
        }

        // ------------------------------------------------------------------ text
        public static TextMeshProUGUI Text(Transform parent, string text, float size = 28, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool display = false, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = display ? DisplayFont : BodyFont;
            float scale = Services.Settings != null ? Services.Settings.TextScale : 1f;
            t.fontSize = size * (display ? 1f : scale);
            t.color = color ?? Theme.text;
            t.alignment = align;
            t.text = text;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static TextMeshProUGUI Mono(Transform parent, string text, float size = 26, Color? color = null)
        {
            var t = Text(parent, text, size, color ?? new Color(0.8f, 0.95f, 1f), TextAlignmentOptions.TopLeft);
            t.font = MonoFont;
            return t;
        }

        // ------------------------------------------------------------------ buttons
        public static Button Button(Transform parent, string label, Action onClick, ButtonStyle style = ButtonStyle.Primary, float fontSize = 30, string name = null)
        {
            Color bg, border, text;
            switch (style)
            {
                case ButtonStyle.Danger: bg = new Color(0.25f, 0.05f, 0.1f, 0.95f); border = Theme.red; text = Theme.text; break;
                case ButtonStyle.Secondary: bg = new Color(0.08f, 0.1f, 0.16f, 0.95f); border = new Color(1, 1, 1, 0.25f); text = Theme.text; break;
                case ButtonStyle.Ghost: bg = new Color(1, 1, 1, 0.04f); border = new Color(1, 1, 1, 0.15f); text = Theme.textDim; break;
                case ButtonStyle.Answer: bg = new Color(0.07f, 0.09f, 0.15f, 0.97f); border = new Color(Theme.cyan.r, Theme.cyan.g, Theme.cyan.b, 0.55f); text = Theme.text; break;
                default: bg = new Color(0.05f, 0.2f, 0.28f, 0.95f); border = Theme.cyan; text = Color.white; break;
            }
            var img = Image(parent, bg, UISprites.Rounded(16), name ?? ("Btn_" + label));
            img.raycastTarget = true;
            var outline = Image(img.transform, border, UISprites.Rounded(16, 2), "Border");
            Stretch(outline.rectTransform);
            var txt = Text(img.transform, label, fontSize, text, TextAlignmentOptions.Center, style == ButtonStyle.Primary, "Label");
            Stretch(txt.rectTransform, 14, 6, 14, 6);
            txt.textWrappingMode = TextWrappingModes.Normal;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.25f, 1.25f, 1.35f); colors.pressedColor = new Color(0.8f, 0.8f, 0.9f);
            colors.selectedColor = new Color(1.2f, 1.2f, 1.3f); colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); colors.colorMultiplier = 1.4f; colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() =>
            {
                Services.Audio.Play("click");
                onClick?.Invoke();
            });
            var fx = img.gameObject.AddComponent<UIButtonFX>();
            fx.border = outline;
            fx.label = txt;
            fx.baseBorder = border;
            return btn;
        }

        public static TextMeshProUGUI Label(Button b) => b.transform.Find("Label").GetComponent<TextMeshProUGUI>();

        // ------------------------------------------------------------------ layouts
        static T Reuse<T>(Component c) where T : Component
        {
            var t = c.GetComponent<T>();
            return t != null ? t : c.gameObject.AddComponent<T>();
        }

        public static VerticalLayoutGroup VLayout(Component c, float spacing = 12, int padding = 0, TextAnchor align = TextAnchor.UpperCenter, bool expandWidth = true)
        {
            // a rebuilt screen (Refresh) lays out the same object again: reuse its group (a second AddComponent returns null)
            var v = Reuse<VerticalLayoutGroup>(c);
            v.spacing = spacing; v.padding = new RectOffset(padding, padding, padding, padding); v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = expandWidth; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(Component c, float spacing = 12, int padding = 0, TextAnchor align = TextAnchor.MiddleCenter, bool expandWidth = true)
        {
            var h = Reuse<HorizontalLayoutGroup>(c);
            h.spacing = spacing; h.padding = new RectOffset(padding, padding, padding, padding); h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = expandWidth; h.childForceExpandHeight = true;
            return h;
        }

        public static ContentSizeFitter Fit(Component c, bool vertical = true, bool horizontal = false)
        {
            var f = c.gameObject.AddComponent<ContentSizeFitter>();
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        /// <summary>A vertical scroll view; returns the content RectTransform (with a vertical layout).</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll, float spacing = 10)
        {
            var view = Image(parent, new Color(0, 0, 0, 0.01f), null, "Scroll");
            view.raycastTarget = true;
            scroll = view.gameObject.AddComponent<ScrollRect>();
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", view.transform);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(0, 0); content.offsetMax = new Vector2(0, 0);
            var vl = VLayout(content, spacing, 4);
            Fit(content);
            scroll.content = content;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }

        // ------------------------------------------------------------------ bars
        public static UIBar Bar(Transform parent, Color fill, Color? back = null, string name = "Bar", int radius = 10)
        {
            var bg = Image(parent, back ?? new Color(1, 1, 1, 0.08f), UISprites.Rounded(radius), name);
            var mask = bg.gameObject.AddComponent<RectMask2D>();
            var f = Image(bg.transform, fill, UISprites.Rounded(radius), "Fill");
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = Vector2.one;
            f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
            var ghost = Image(bg.transform, new Color(1, 1, 1, 0.5f), UISprites.Rounded(radius), "Ghost");
            ghost.rectTransform.SetSiblingIndex(0);
            ghost.rectTransform.anchorMin = Vector2.zero; ghost.rectTransform.anchorMax = Vector2.one;
            ghost.rectTransform.offsetMin = ghost.rectTransform.offsetMax = Vector2.zero;
            var bar = bg.gameObject.AddComponent<UIBar>();
            bar.fill = f; bar.ghost = ghost;
            return bar;
        }

        // ------------------------------------------------------------------ settings widgets
        public static Slider Slider(Transform parent, string label, float min, float max, float value, Action<float> onChange, Func<float, string> format = null, bool whole = false, float labelWidth = 360, float valueWidth = 120)
        {
            var row = Rect("Slider_" + label, parent);
            Size(row, -1, 64);
            var h = HLayout(row, 16, 0, TextAnchor.MiddleLeft, false);
            h.childControlWidth = true;
            var lbl = Text(row, label, 26);
            Size(lbl, labelWidth, 60);
            var sRt = Rect("Slider", row);
            Size(sRt, -1, 40, 1);
            var bg = Image(sRt, new Color(1, 1, 1, 0.1f), UISprites.Rounded(8), "Track");
            bg.rectTransform.anchorMin = new Vector2(0, 0.35f); bg.rectTransform.anchorMax = new Vector2(1, 0.65f);
            bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
            var fillArea = Rect("Fill Area", sRt);
            fillArea.anchorMin = new Vector2(0, 0.35f); fillArea.anchorMax = new Vector2(1, 0.65f);
            fillArea.offsetMin = new Vector2(6, 0); fillArea.offsetMax = new Vector2(-6, 0);
            var fill = Image(fillArea, Theme.cyan, UISprites.Rounded(8), "Fill");
            fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect("Handle Area", sRt);
            Stretch(handleArea, 12, 0, 12, 0);
            var handle = Image(handleArea, Color.white, UISprites.Circle(64), "Handle");
            handle.rectTransform.sizeDelta = new Vector2(34, 0);
            handle.raycastTarget = true;
            var s = sRt.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.wholeNumbers = whole;
            s.value = value;
            var val = Text(row, "", 24, Theme.cyan, TextAlignmentOptions.MidlineRight);
            Size(val, valueWidth, 60);
            Func<float, string> fmt = format ?? (v => whole ? v.ToString("0") : v.ToString("0.00"));
            val.text = fmt(value);
            s.onValueChanged.AddListener(v => { val.text = fmt(v); onChange?.Invoke(v); });
            return s;
        }

        public static Toggle Toggle(Transform parent, string label, bool value, Action<bool> onChange)
        {
            var row = Rect("Toggle_" + label, parent);
            Size(row, -1, 60);
            var bgImg = row.gameObject.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.001f);
            var h = HLayout(row, 16, 0, TextAnchor.MiddleLeft, false);
            var lbl = Text(row, label, 26);
            Size(lbl, -1, 56, 1);
            var box = Image(row, new Color(1, 1, 1, 0.1f), UISprites.Rounded(14), "Box");
            Size(box, 88, 44);
            var knob = Image(box.transform, Color.white, UISprites.Circle(64), "Knob");
            Place(knob.rectTransform, new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(34, 34), new Vector2(0.5f, 0.5f));
            var t = row.gameObject.AddComponent<Toggle>();
            t.targetGraphic = bgImg;
            t.isOn = value;
            void Visual(bool on)
            {
                box.color = on ? new Color(Theme.cyan.r, Theme.cyan.g, Theme.cyan.b, 0.8f) : new Color(1, 1, 1, 0.12f);
                knob.rectTransform.anchoredPosition = new Vector2(on ? 64 : 24, 0);
            }
            Visual(value);
            t.onValueChanged.AddListener(on => { Visual(on); Services.Audio.Play("click"); onChange?.Invoke(on); });
            return t;
        }

        /// <summary>A left/right option picker (works with mouse, touch, keyboard and gamepad).</summary>
        public static UISelector Selector(Transform parent, string label, IList<string> options, int index, Action<int> onChange)
        {
            var row = Rect("Selector_" + label, parent);
            Size(row, -1, 64);
            var h = HLayout(row, 14, 0, TextAnchor.MiddleLeft, false);
            var lbl = Text(row, label, 26);
            Size(lbl, 360, 60);
            var sel = row.gameObject.AddComponent<UISelector>();
            var left = Button(row, "<", () => sel.Step(-1), ButtonStyle.Secondary, 30);
            Size(left, 64, 56);
            var val = Text(row, "", 26, Theme.cyan, TextAlignmentOptions.Center);
            Size(val, -1, 56, 1);
            var right = Button(row, ">", () => sel.Step(1), ButtonStyle.Secondary, 30);
            Size(right, 64, 56);
            sel.Init(options, index, val, onChange);
            return sel;
        }

        public static TMP_InputField Input(Transform parent, string placeholder, float fontSize = 30)
        {
            var bg = Panel(parent, new Color(0.02f, 0.03f, 0.06f, 1f), 12, "Input");
            var area = Rect("Text Area", bg.transform);
            Stretch(area, 18, 8, 18, 8);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Text(area, placeholder, fontSize, new Color(1, 1, 1, 0.3f));
            Stretch(ph.rectTransform);
            ph.fontStyle = FontStyles.Italic;
            var txt = Text(area, "", fontSize, Theme.text);
            Stretch(txt.rectTransform);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.textViewport = area;
            input.textComponent = txt;
            input.placeholder = ph;
            input.fontAsset = txt.font;
            input.pointSize = txt.fontSize;
            input.caretColor = Theme.cyan;
            input.customCaretColor = true;
            input.caretWidth = 3;
            input.selectionColor = new Color(Theme.cyan.r, Theme.cyan.g, Theme.cyan.b, 0.35f);
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        public static Image Chip(Transform parent, string text, Color color, float fontSize = 20)
        {
            var img = Image(parent, new Color(color.r, color.g, color.b, 0.18f), UISprites.Rounded(12), "Chip");
            var o = Image(img.transform, new Color(color.r, color.g, color.b, 0.8f), UISprites.Rounded(12, 2), "Border");
            Stretch(o.rectTransform);
            var t = Text(img.transform, text, fontSize, color, TextAlignmentOptions.Center, true);
            Stretch(t.rectTransform, 12, 2, 12, 2);
            var fit = img.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var hl = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(14, 14, 4, 4);
            hl.childControlWidth = true; hl.childControlHeight = true;
            Size(img, -1, fontSize + 16);
            return img;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>Makes a dark full-screen blocker behind modal panels.</summary>
        public static Image Blocker(Transform parent, float alpha = 0.7f)
        {
            var b = Image(parent, new Color(0.01f, 0.01f, 0.03f, alpha), null, "Blocker");
            b.raycastTarget = true;
            Stretch(b.rectTransform);
            return b;
        }

        public static void Select(Selectable s)
        {
            if (s == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(s.gameObject);
        }
    }
}

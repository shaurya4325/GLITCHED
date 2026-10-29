using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Rough paper and a scientific calculator on every question screen (QuickQuiz, ChallengeEngine), like the
    /// rough-work column of a real exam sheet. Two buttons (or R / C) open them beside the question; the attack
    /// timer waits while either is open, and the calculator takes the number keys while it is open.
    /// </summary>
    public static class ScratchTools
    {
        static bool paperOpen, calcOpen;
        /// <summary>Capture runs only (-captureTools 1): open both tools with some working on them.</summary>
        public static bool Demo;
        /// <summary>Either tool is open: question timers hold.</summary>
        public static bool Open => paperOpen || calcOpen;
        /// <summary>The calculator has the keyboard: hosts ignore answer / submit keys.</summary>
        public static bool Capturing => calcOpen;

        static RectTransform paper, calc;
        static ScratchPadCanvas sheet;
        static TextMeshProUGUI exprT, resultT;
        static Button penB, eraserB;
        static string expr = "";
        static double ans;
        static bool justEvaluated;

        /// <summary>
        /// Adds the two tool buttons to <paramref name="buttonRow"/> and the tool panels to <paramref name="host"/>
        /// (above it when the question sits at the bottom of the screen, below it when the question sits at the top).
        /// </summary>
        public static void Attach(RectTransform host, RectTransform buttonRow, bool above, float room)
        {
            paperOpen = calcOpen = false;
            expr = ""; justEvaluated = false;
            var th = UIFactory.Theme;
            float h = Mathf.Clamp(room, 330f, 470f);

            var tools = UIFactory.Rect("ScratchTools", host);
            UIFactory.Place(tools, new Vector2(0.5f, above ? 1f : 0f), new Vector2(0, above ? 14f : -14f), new Vector2(1480, h), new Vector2(0.5f, above ? 0f : 1f));
            tools.gameObject.AddComponent<ScratchToolsHost>();

            // ---------------------------------------------------------------- rough paper
            paper = UIFactory.Panel(tools, new Color(0.03f, 0.04f, 0.07f, 0.97f), 18, "RoughPaper").rectTransform;
            UIFactory.Place(paper, new Vector2(0, 0.5f), Vector2.zero, new Vector2(1010, h), new Vector2(0, 0.5f));
            var pt = UIFactory.Text(paper, "<b>ROUGH WORK</b>  <color=#9fb0cc>write your working here · it is wiped after the question</color>", 19, th.text, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(pt.rectTransform, new Vector2(0, 1), new Vector2(20, -8), new Vector2(620, 40), new Vector2(0, 1));
            var tb = UIFactory.Rect("Tools", paper);
            UIFactory.Place(tb, new Vector2(1, 1), new Vector2(-12, -8), new Vector2(380, 40), new Vector2(1, 1));
            UIFactory.HLayout(tb, 8, 0, TextAnchor.MiddleRight, false);
            penB = SmallButton(tb, "✎ PEN", () => SetEraser(false), 88);
            eraserB = SmallButton(tb, "ERASER", () => SetEraser(true), 96);
            SmallButton(tb, "CLEAR", () => sheet.Clear(), 88);
            SmallButton(tb, "✕", () => Toggle(true), 44);
            var sheetImg = new GameObject("Sheet", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            sheetImg.transform.SetParent(paper, false);
            UIFactory.Stretch(sheetImg.rectTransform, 12, 54, 12, 12);
            sheet = sheetImg.gameObject.AddComponent<ScratchPadCanvas>();
            paper.gameObject.SetActive(false);

            // ---------------------------------------------------------------- calculator
            calc = UIFactory.Panel(tools, new Color(0.03f, 0.04f, 0.07f, 0.97f), 18, "Calculator").rectTransform;
            UIFactory.Place(calc, new Vector2(1, 0.5f), Vector2.zero, new Vector2(456, h), new Vector2(1, 0.5f));
            var ct = UIFactory.Text(calc, "<b>CALCULATOR</b>  <color=#9fb0cc>DEG</color>", 19, th.text, TextAlignmentOptions.MidlineLeft);
            UIFactory.Place(ct.rectTransform, new Vector2(0, 1), new Vector2(18, -8), new Vector2(300, 40), new Vector2(0, 1));
            var cx = SmallButton(calc, "✕", () => Toggle(false), 44);
            UIFactory.Place(cx.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(-12, -8), new Vector2(44, 38), new Vector2(1, 1));
            var disp = UIFactory.Panel(calc, new Color(0.11f, 0.16f, 0.13f, 1f), 10, "Display", false);
            UIFactory.Place(disp.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(428, 74), new Vector2(0.5f, 1));
            exprT = UIFactory.Mono(disp.transform, "", 20, new Color(0.7f, 0.85f, 0.75f));
            UIFactory.Place(exprT.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(408, 28), new Vector2(0.5f, 1));
            exprT.alignment = TextAlignmentOptions.MidlineRight; exprT.textWrappingMode = TextWrappingModes.NoWrap; exprT.overflowMode = TextOverflowModes.Ellipsis;
            resultT = UIFactory.Mono(disp.transform, "0", 34, new Color(0.75f, 1f, 0.82f));
            UIFactory.Place(resultT.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(408, 42), new Vector2(0.5f, 0));
            resultT.alignment = TextAlignmentOptions.MidlineRight; resultT.textWrappingMode = TextWrappingModes.NoWrap;
            resultT.enableAutoSizing = true; resultT.fontSizeMin = 18; resultT.fontSizeMax = 34;

            string[] keys =
            {
                "sin", "cos", "tan", "log", "ln",
                "C", "⌫", "(", ")", "÷",
                "7", "8", "9", "×", "√",
                "4", "5", "6", "−", "x²",
                "1", "2", "3", "+", "^",
                "0", ".", "E", "π", "=",
            };
            var grid = UIFactory.Rect("Keys", calc);
            UIFactory.Stretch(grid, 14, 134, 14, 12);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            float cell = Mathf.Floor((h - 134 - 12 - 5 * 6) / 6f);
            gl.cellSize = new Vector2(80, cell); gl.spacing = new Vector2(6, 6);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 5;
            foreach (var k in keys)
            {
                string key = k;
                bool op = "÷×−+^=".Contains(key) && key.Length == 1;
                var b = UIFactory.Button(grid, key, () => Press(key), key == "=" ? ButtonStyle.Primary : op || key == "C" || key == "⌫" ? ButtonStyle.Secondary : ButtonStyle.Ghost, key.Length > 2 ? 20 : 26);
                if (key == "C") UIFactory.Label(b).color = th.red;
                else if (char.IsDigit(key[0]) || key == ".") UIFactory.Label(b).color = Color.white;
            }
            calc.gameObject.SetActive(false);

            // ---------------------------------------------------------------- buttons on the question card
            UIFactory.HLayout(buttonRow, 10, 0, TextAnchor.MiddleRight, false);
            SmallButton(buttonRow, InputHub.IsMobile ? "✎ ROUGH PAPER" : "✎ ROUGH PAPER  <color=#9fb0cc>R</color>", () => Toggle(true), 210);
            SmallButton(buttonRow, InputHub.IsMobile ? "▦ CALCULATOR" : "▦ CALCULATOR  <color=#9fb0cc>C</color>", () => Toggle(false), 200);
            SetEraser(false);
            Refresh();
            if (Demo)
            {
                Toggle(true); Toggle(false);
                foreach (var k in new[] { "√", "2", "×", "9", ".", "8", "×", "1", "0", ")", "=", "×", "3" }) Press(k);
                sheet.DemoScribble();
            }
        }

        static Button SmallButton(Transform parent, string label, Action click, float width)
        {
            var b = UIFactory.Button(parent, label, click, ButtonStyle.Secondary, 17);
            UIFactory.Size(b, width, 38);
            b.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 38);
            return b;
        }

        static void SetEraser(bool on)
        {
            if (sheet != null) sheet.eraser = on;
            var th = UIFactory.Theme;
            if (penB != null) UIFactory.Label(penB).color = on ? th.textDim : th.cyan;
            if (eraserB != null) UIFactory.Label(eraserB).color = on ? th.cyan : th.textDim;
        }

        /// <summary>Opens or closes the rough paper (true) or the calculator (false).</summary>
        public static void Toggle(bool roughPaper)
        {
            if (paper == null || calc == null) return;
            if (roughPaper) { paperOpen = !paperOpen; paper.gameObject.SetActive(paperOpen); Pop(paper, paperOpen); }
            else { calcOpen = !calcOpen; calc.gameObject.SetActive(calcOpen); Pop(calc, calcOpen); }
            Services.Audio.Play(Open ? "panel_in" : "click", 0.6f);
        }

        static void Pop(RectTransform rt, bool open)
        {
            if (!open) return;
            rt.localScale = Vector3.one * 0.85f;
            Tween.Scale(rt, Vector3.one, 0.16f, Ease.OutBack);
        }

        /// <summary>The host went away (question answered or scene changed).</summary>
        internal static void Detach()
        {
            paperOpen = calcOpen = false;
            paper = calc = null; sheet = null; exprT = resultT = null;
        }

        // ------------------------------------------------------------------ calculator
        static readonly string[] Funcs = { "sin(", "cos(", "tan(", "log(", "ln(", "√(" };

        public static void Press(string key)
        {
            if (exprT == null) return;
            Services.Audio.Play("click", 0.35f);
            switch (key)
            {
                case "C": expr = ""; justEvaluated = false; Refresh(); return;
                case "⌫":
                    foreach (var f in Funcs) if (expr.EndsWith(f)) { expr = expr.Substring(0, expr.Length - f.Length); Refresh(); return; }
                    if (expr.EndsWith("ans")) { expr = expr.Substring(0, expr.Length - 3); Refresh(); return; }
                    if (expr.Length > 0) expr = expr.Substring(0, expr.Length - 1);
                    Refresh(); return;
                case "=":
                    if (string.IsNullOrEmpty(expr)) return;
                    if (Eval(expr, out double v, out string err))
                    {
                        ans = v;
                        exprT.text = Pretty(expr) + " =";
                        resultT.text = Format(v);
                        resultT.color = new Color(0.75f, 1f, 0.82f);
                        expr = "ans";
                        justEvaluated = true;
                    }
                    else { resultT.text = err; resultT.color = UIFactory.Theme.red; }
                    return;
            }
            string add = key == "x²" ? "^2" : key == "sin" || key == "cos" || key == "tan" || key == "log" || key == "ln" ? key + "(" : key == "√" ? "√(" : key;
            // a fresh number after "=" starts a new sum; an operator carries on from the answer
            if (justEvaluated && expr == "ans" && (char.IsDigit(add[0]) || add == "." || add == "π" || add == "(" || add.EndsWith("("))) expr = "";
            justEvaluated = false;
            if (expr.Length < 64) expr += add;
            Refresh();
        }

        static void Refresh()
        {
            if (exprT == null) return;
            exprT.text = "";
            resultT.color = new Color(0.75f, 1f, 0.82f);
            resultT.text = string.IsNullOrEmpty(expr) ? "0" : Pretty(expr);
            // a live preview of the value while you type
            if (!string.IsNullOrEmpty(expr) && expr != "ans" && Eval(expr, out double v, out _)) exprT.text = "= " + Format(v);
        }

        static string Pretty(string e) => e.Replace("ans", "Ans").Replace("^2", "²");

        static string Format(double v)
        {
            if (double.IsNaN(v)) return "Math error";
            if (double.IsInfinity(v)) return v > 0 ? "∞" : "−∞";
            double a = Math.Abs(v);
            if (a != 0 && (a >= 1e10 || a < 1e-5))
            {
                int exp = (int)Math.Floor(Math.Log10(a));
                double m = v / Math.Pow(10, exp);
                return m.ToString("0.######", CultureInfo.InvariantCulture) + "×10^" + exp;
            }
            return Math.Round(v, 10).ToString("0.##########", CultureInfo.InvariantCulture).Replace("-", "−");
        }

        // recursive descent: sum = term (± term)*; term = power ((×|÷|implicit) power)*; power = unary (^ power)?
        static string src; static int pos;

        public static bool Eval(string e, out double value, out string error)
        {
            src = e; pos = 0; error = null; value = 0;
            try
            {
                value = Sum();
                if (pos < src.Length) throw new FormatException();
                if (double.IsNaN(value)) { error = "Math error"; return false; }
                return true;
            }
            catch (DivideByZeroException) { error = "Can't ÷ by 0"; return false; }
            catch (Exception) { error = "Syntax error"; return false; }
        }

        static bool Peek(string s) => string.CompareOrdinal(src, pos, s, 0, s.Length) == 0;
        static bool Eat(string s) { if (!Peek(s)) return false; pos += s.Length; return true; }

        static double Sum()
        {
            double v = Term();
            while (pos < src.Length)
            {
                if (Eat("+")) v += Term();
                else if (Eat("−") || Eat("-")) v -= Term();
                else break;
            }
            return v;
        }

        static double Term()
        {
            double v = Power();
            while (pos < src.Length)
            {
                if (Eat("×") || Eat("*")) v *= Power();
                else if (Eat("÷") || Eat("/")) { double d = Power(); if (d == 0) throw new DivideByZeroException(); v /= d; }
                else if (StartsFactor()) v *= Power();   // 2π, 3(4+1), 2√(9)
                else break;
            }
            return v;
        }

        static bool StartsFactor()
        {
            if (pos >= src.Length) return false;
            char c = src[pos];
            return char.IsDigit(c) || c == '.' || c == '(' || c == 'π' || c == '√' || Peek("sin") || Peek("cos") || Peek("tan") || Peek("log") || Peek("ln") || Peek("ans");
        }

        static double Power()
        {
            double b = Unary();
            if (Eat("^")) return Math.Pow(b, Unary2());
            return b;
        }

        static double Unary2() { double v = Unary(); if (Eat("^")) return Math.Pow(v, Unary2()); return v; }

        static double Unary()
        {
            if (Eat("−") || Eat("-")) return -Unary();
            if (Eat("+")) return Unary();
            return Primary();
        }

        static double Primary()
        {
            if (Eat("(")) { double v = Sum(); Eat(")"); return v; }   // a missing ")" at the end is forgiven
            if (Eat("π")) return Math.PI;
            if (Eat("ans")) return ans;
            if (Eat("√(")) { double v = Sum(); Eat(")"); return v < 0 ? double.NaN : Math.Sqrt(v); }
            if (Eat("sin(")) return Math.Round(Math.Sin(Arg() * Math.PI / 180.0), 12);
            if (Eat("cos(")) return Math.Round(Math.Cos(Arg() * Math.PI / 180.0), 12);
            if (Eat("tan(")) { double a = Arg(); if (Math.Abs(Math.IEEERemainder(a - 90, 180)) < 1e-9) return double.NaN; return Math.Round(Math.Tan(a * Math.PI / 180.0), 12); }
            if (Eat("log(")) { double v = Arg(); return v <= 0 ? double.NaN : Math.Log10(v); }
            if (Eat("ln(")) { double v = Arg(); return v <= 0 ? double.NaN : Math.Log(v); }
            int start = pos;
            while (pos < src.Length && (char.IsDigit(src[pos]) || src[pos] == '.')) pos++;
            if (pos == start) throw new FormatException();
            double n = double.Parse(src.Substring(start, pos - start), CultureInfo.InvariantCulture);
            if (Eat("E"))
            {
                // scientific notation: 6.67E−11
                int sign = 1;
                if (Eat("−") || Eat("-")) sign = -1; else Eat("+");
                int es = pos;
                while (pos < src.Length && char.IsDigit(src[pos])) pos++;
                if (pos == es) throw new FormatException();
                n *= Math.Pow(10, sign * int.Parse(src.Substring(es, pos - es), CultureInfo.InvariantCulture));
            }
            return n;
        }

        static double Arg() { double v = Sum(); Eat(")"); return v; }
    }
}

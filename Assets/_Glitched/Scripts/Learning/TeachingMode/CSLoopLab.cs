using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Computer Science lab: live code that can be changed and RUN, with the output beside it, so a loop can be
    /// broken and fixed on purpose. Binary questions get a bit-toggle lab; logic questions get a gate lab.
    /// </summary>
    public class CSLoopLab : TeachingLab
    {
        enum Kind { Loop, Binary, Gates }
        Kind kind;

        // loop state
        readonly int[] starts = { 0, 1, 2 };
        readonly string[] ops = { "<=", "<", ">", "!=" };
        readonly int[] ends = { 4, 5, 6, 10 };
        readonly string[] steps = { "- 1", "+ 1", "+ 2", "+ 0" };
        int si = 1, oi = 0, ei = 1, sti = 0;
        bool incInside = true;
        TextMeshProUGUI output;
        readonly List<Button> tokenButtons = new List<Button>();

        // binary state
        readonly bool[] bits = new bool[8];
        int binaryTarget = 13;
        TextMeshProUGUI binaryReadout;
        readonly List<Button> bitButtons = new List<Button>();

        // gate state
        bool ga, gb;
        int gate;
        readonly string[] gates = { "AND", "OR", "XOR", "NOT A" };
        readonly HashSet<string> tried = new HashSet<string>();
        TextMeshProUGUI gateReadout;
        UIGraph gateGraph;

        protected override string Title => kind == Kind.Binary ? "THE BIT LAB" : kind == Kind.Gates ? "THE LOGIC GATE LAB" : "THE LOOP SANDBOX";

        protected override string GoalDescription => kind == Kind.Binary ? $"Make the register show <b>{binaryTarget}</b> by toggling bits."
            : kind == Kind.Gates ? $"Try every input combination for a gate ({tried.Count}/4), and watch the output lamp."
            : "Edit the code so it prints <b>1 2 3 4 5</b>, then press RUN.";

        protected override string Opening()
        {
            if (Boss != null && Boss.teachingLines.Count > 0) return Boss.teachingLines[0];
            return kind == Kind.Binary ? "Each bit is worth double the one to its right: 1, 2, 4, 8... Tap them and watch the number."
                : kind == Kind.Gates ? "A gate is just a rule. Flip A and B and watch the lamp."
                : "This code is broken on purpose. Tap the pink bits to change them, then RUN it and see what happens.";
        }

        protected override void BuildLab(RectTransform area)
        {
            string c = Q != null ? Q.concept : "Loops";
            kind = c == "Binary" ? Kind.Binary : (c == "Boolean Logic" || c == "Conditions") ? Kind.Gates : Kind.Loop;
            if (kind == Kind.Binary) BuildBinary(area);
            else if (kind == Kind.Gates) BuildGates(area);
            else BuildLoop(area);
        }

        // ------------------------------------------------------------------ loop sandbox
        void BuildLoop(RectTransform area)
        {
            UIFactory.HLayout(area, 24, 0, TextAnchor.UpperLeft, false);
            var codeP = UIFactory.Panel(area, UIFactory.Theme.code, 16, "Code", false);
            UIFactory.Size(codeP, 1000, -1, 1, 1);
            UIFactory.VLayout(codeP, 10, 24, TextAnchor.UpperLeft, false);
            Row(codeP.transform, new[] { "i = ", "#start" });
            Row(codeP.transform, new[] { "while i ", "#op", " ", "#end", ":" });
            Row(codeP.transform, new[] { "    print(i)" });
            Row(codeP.transform, new[] { "#indent", "i = i ", "#step" });
            var run = UIFactory.Button(codeP.transform, "RUN  ▶", Run, ButtonStyle.Primary, 30);
            UIFactory.Size(run, 260, 76);

            var term = UIFactory.Panel(area, new Color(0.01f, 0.03f, 0.02f, 1f), 16, "Output", false);
            UIFactory.Size(term, 560, -1, 0, 1);
            output = UIFactory.Mono(term.transform, "<color=#3f6b4a>> press RUN</color>", 28, new Color(0.45f, 1f, 0.6f));
            UIFactory.Stretch(output.rectTransform, 20, 20, 20, 20);
            RefreshTokens();
        }

        void Row(Transform parent, string[] parts)
        {
            var row = UIFactory.Rect("Row", parent);
            UIFactory.Size(row, -1, 70);
            var h = UIFactory.HLayout(row, 4, 0, TextAnchor.MiddleLeft, false);
            h.childForceExpandHeight = false;
            foreach (var p in parts)
            {
                if (p.StartsWith("#"))
                {
                    string key = p.Substring(1);
                    var b = UIFactory.Button(row, key, () => Cycle(key), ButtonStyle.Answer, 30);
                    UIFactory.Label(b).font = UIFactory.MonoFont;
                    UIFactory.Size(b, key == "indent" ? 150 : 120, 62);
                    b.name = "Token_" + key;
                    tokenButtons.Add(b);
                }
                else
                {
                    var t = UIFactory.Mono(row, ChallengeBase.Syntax(p), 32);
                    t.alignment = TextAlignmentOptions.MidlineLeft;
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    var fit = t.gameObject.AddComponent<ContentSizeFitter>();
                    fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    UIFactory.Size(t, -1, 62);
                }
            }
        }

        void Cycle(string key)
        {
            switch (key)
            {
                case "start": si = (si + 1) % starts.Length; break;
                case "op": oi = (oi + 1) % ops.Length; break;
                case "end": ei = (ei + 1) % ends.Length; break;
                case "step": sti = (sti + 1) % steps.Length; break;
                case "indent": incInside = !incInside; break;
            }
            Services.Audio.Play("select");
            RefreshTokens();
            Interacted();
        }

        void RefreshTokens()
        {
            foreach (var b in tokenButtons)
            {
                string key = b.name.Substring(6);
                string text = key == "start" ? starts[si].ToString() : key == "op" ? ops[oi] : key == "end" ? ends[ei].ToString() : key == "step" ? steps[sti] : (incInside ? "····" : "|");
                UIFactory.Label(b).text = $"<color=#ff5fd2>{text}</color>";
            }
        }

        void Run()
        {
            Interacted();
            int i = starts[si];
            var sb = new StringBuilder("> run loop.py\n");
            var printed = new List<int>();
            int guard = 0;
            bool infinite = false;
            int step = sti == 0 ? -1 : sti == 1 ? 1 : sti == 2 ? 2 : 0;
            while (Cond(i))
            {
                printed.Add(i);
                if (incInside) i += step;
                if (++guard > 24) { infinite = true; break; }
            }
            if (!incInside && !infinite) i += step;
            for (int k = 0; k < Mathf.Min(printed.Count, 14); k++) sb.Append(printed[k]).Append(k % 7 == 6 ? "\n" : "  ");
            if (infinite) sb.Append("\n<color=#ff5a6a>...\nINFINITE LOOP! Stopped after 25 steps.</color>");
            output.text = sb.ToString();
            Services.Audio.Play(infinite ? "error" : "type");
            if (infinite) { GlitchFX.Pulse(0.4f, 0.4f); Say(Boss != null ? "SEE? It never stops. NEVER. That's ME. That's my whole tragic life. What changes each time?" : "Infinite loop! The condition never becomes false, because i never moves toward the end."); return; }
            bool correct = printed.Count == 5;
            for (int k = 0; k < printed.Count && correct; k++) if (printed[k] != k + 1) correct = false;
            if (correct) MarkSolved(Boss != null ? "...Correct. I hate this. A START, a CONDITION, a CHANGE. That's all a loop is." : "That's a healthy loop: it starts, checks, changes, and stops.");
            else if (printed.Count == 4 || printed.Count == 6) Say("Close! Off by one. That's the most famous bug in history. Check where it starts and where it stops.");
            else if (printed.Count == 0) Say("Nothing printed: the condition was false from the very start.");
            else Say("It stopped, but not at 1 2 3 4 5. Try changing one thing at a time.");
        }

        bool Cond(int i)
        {
            int e = ends[ei];
            switch (ops[oi])
            {
                case "<=": return i <= e;
                case "<": return i < e;
                case ">": return i > e;
                default: return i != e;
            }
        }

        // ------------------------------------------------------------------ binary lab
        void BuildBinary(RectTransform area)
        {
            if (Q != null && Q.type == ChallengeType.Numerical && Q.data.value > 0 && Q.data.value < 256) binaryTarget = (int)Q.data.value;
            UIFactory.VLayout(area, 24, 0, TextAnchor.MiddleCenter, false);
            var row = UIFactory.Rect("Bits", area);
            UIFactory.Size(row, 1500, 200);
            UIFactory.HLayout(row, 14);
            for (int b = 7; b >= 0; b--)
            {
                int bit = b;
                var col = UIFactory.Rect("Bit" + b, row);
                UIFactory.VLayout(col, 6);
                var btn = UIFactory.Button(col, "0", () => ToggleBit(bit), ButtonStyle.Answer, 54);
                UIFactory.Size(btn, -1, 130);
                bitButtons.Insert(0, btn);
                var lbl = UIFactory.Text(col, (1 << b).ToString(), 26, UIFactory.Theme.textDim, TextAlignmentOptions.Center, true);
                UIFactory.Size(lbl, -1, 40);
            }
            binaryReadout = UIFactory.Text(area, "", 60, UIFactory.Theme.cyan, TextAlignmentOptions.Center, true);
            UIFactory.Size(binaryReadout, 1500, 120);
            RefreshBinary();
        }

        void ToggleBit(int b)
        {
            bits[b] = !bits[b];
            Services.Audio.Play("select");
            Interacted();
            RefreshBinary();
        }

        void RefreshBinary()
        {
            int value = 0;
            var sum = new List<string>();
            for (int b = 7; b >= 0; b--) if (bits[b]) { value += 1 << b; sum.Add((1 << b).ToString()); }
            for (int b = 0; b < 8; b++) UIFactory.Label(bitButtons[b]).text = bits[b] ? "<color=#7cffa0>1</color>" : "0";
            binaryReadout.text = $"{(sum.Count > 0 ? string.Join(" + ", sum) : "0")} = <b>{value}</b>";
            if (value == binaryTarget) MarkSolved($"{binaryTarget} in binary is {System.Convert.ToString(binaryTarget, 2)}. Each 1 switches on its place value.");
        }

        // ------------------------------------------------------------------ gate lab
        void BuildGates(RectTransform area)
        {
            UIFactory.HLayout(area, 30, 0, TextAnchor.MiddleCenter, false);
            var left = UIFactory.Rect("Inputs", area);
            UIFactory.Size(left, 420, -1);
            UIFactory.VLayout(left, 18, 20, TextAnchor.MiddleCenter);
            UIFactory.Size(UIFactory.Button(left, "A = 0", null, ButtonStyle.Answer, 40), -1, 110).name = "A";
            UIFactory.Size(UIFactory.Button(left, "B = 0", null, ButtonStyle.Answer, 40), -1, 110).name = "B";
            UIFactory.Size(UIFactory.Button(left, "GATE: AND", null, ButtonStyle.Primary, 34), -1, 100).name = "G";
            var btns = left.GetComponentsInChildren<Button>();
            btns[0].onClick.AddListener(() => { ga = !ga; GateChanged(); });
            btns[1].onClick.AddListener(() => { gb = !gb; GateChanged(); });
            btns[2].onClick.AddListener(() => { gate = (gate + 1) % gates.Length; tried.Clear(); GateChanged(); });
            var stage = UIFactory.Panel(area, new Color(0.02f, 0.03f, 0.06f, 1f), 16, "Stage", false);
            UIFactory.Size(stage, 1000, 460);
            gateGraph = UIGraph.Create(stage.transform);
            UIFactory.Stretch(gateGraph.rectTransform, 10, 10, 10, 70);
            gateReadout = UIFactory.Text(stage.transform, "", 34, UIFactory.Theme.text, TextAlignmentOptions.Center, true);
            UIFactory.Place(gateReadout.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(960, 56), new Vector2(0.5f, 0));
            GateChanged(false);
        }

        void GateChanged(bool interacted = true)
        {
            var btns = Content.GetComponentsInChildren<Button>();
            UIFactory.Label(btns[0]).text = $"A = {(ga ? 1 : 0)}";
            UIFactory.Label(btns[1]).text = $"B = {(gb ? 1 : 0)}";
            UIFactory.Label(btns[2]).text = $"GATE: {gates[gate]}";
            bool o = gate == 0 ? ga && gb : gate == 1 ? ga || gb : gate == 2 ? ga ^ gb : !ga;
            gateReadout.text = $"{gates[gate]}({(ga ? 1 : 0)}, {(gb ? 1 : 0)}) = <color={(o ? "#7cffa0" : "#ff5a6a")}>{(o ? 1 : 0)}</color>";
            var g = gateGraph; g.Clear();
            var s = new Vector2(980, 380);
            Color on = UIFactory.Theme.green, off = new Color(1, 1, 1, 0.2f);
            g.Line(new Vector2(60, 280), new Vector2(380, 280), ga ? on : off, 8);
            g.Line(new Vector2(60, 120), new Vector2(380, 120), gb ? on : off, 8);
            g.Box(new Vector2(380, 80), new Vector2(600, 320), UIFactory.Theme.cyan, false, 6);
            g.Line(new Vector2(600, 200), new Vector2(800, 200), o ? on : off, 8);
            g.Circle(new Vector2(860, 200), 50, o ? new Color(1f, 0.9f, 0.3f) : new Color(0.2f, 0.2f, 0.25f));
            if (interacted)
            {
                Services.Audio.Play("select");
                Interacted();
                tried.Add($"{ga}{gb}");
                RefreshGoal();
                if (tried.Count >= 4) MarkSolved($"That's the whole truth table for {gates[gate]}. A gate is just a rule you can test.");
            }
        }
    }
}

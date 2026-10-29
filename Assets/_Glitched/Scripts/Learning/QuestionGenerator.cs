using System;
using System.Collections.Generic;
using System.Linq;

namespace Glitched
{
    /// <summary>
    /// Makes fresh Class XI MCQs on the fly, so no two saves (or enemies) get the same questions: numbers are random
    /// (binary conversions, F = ma, moles, pendulum periods...) and fact questions draw from NCERT-based fact tables
    /// (organelles, brain parts, oxidation numbers, molecular shapes...) with random wrong options. Every topic in
    /// Lessons/Class11.json has generators here.
    /// </summary>
    public static class QuestionGenerator
    {
        static Random rng = new Random(Environment.TickCount);
        static int counter;

        /// <summary>Reseeds (a new save starts a new sequence).</summary>
        public static void Reseed(int seed) => rng = new Random(seed);

        public static bool Has(string topic) => topic != null && Makers.ContainsKey(topic);

        public static QuestionData Make(string topic)
        {
            if (!Makers.TryGetValue(topic, out var list)) return null;
            for (int tries = 0; tries < 6; tries++)
            {
                var q = list[rng.Next(list.Count)]();
                if (q != null && q.data.options.Distinct().Count() == q.data.options.Count) { Finish(q, topic); return q; }
            }
            return null;
        }

        // ------------------------------------------------------------------ helpers
        static int R(int lo, int hiIncl) => rng.Next(lo, hiIncl + 1);
        static T Pick<T>(IList<T> l) => l[rng.Next(l.Count)];
        static string N(double v) => QuestionData.FormatNumber(Math.Round(v, 3));

        /// <summary>An MCQ: the right answer and wrong ones (deduplicated, topped up from 'spare').</summary>
        static QuestionData Q(string prompt, string right, IEnumerable<string> wrong, string why, int diff = 1, string code = null, IEnumerable<string> spare = null)
        {
            var opts = new List<string> { right };
            foreach (var w in wrong.Concat(spare ?? Enumerable.Empty<string>()))
                if (opts.Count < 4 && !string.IsNullOrEmpty(w) && !opts.Contains(w)) opts.Add(w);
            if (opts.Count < 3) return null;
            // shuffle (the quiz card shuffles again, but the answer index must be valid)
            for (int i = opts.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (opts[i], opts[j]) = (opts[j], opts[i]); }
            return new QuestionData
            {
                type = ChallengeType.Logic, prompt = prompt, difficulty = diff, explanation = why,
                data = new QuestionPayload { mode = "choice", options = opts, answerIndex = opts.IndexOf(right), code = code },
                fastSeconds = diff == 1 ? 7 : diff == 2 ? 9 : 12,
            };
        }

        /// <summary>Numeric answer with plausible slips (off by a factor, sign, a step).</summary>
        static QuestionData Num(string prompt, double right, string unit, string why, int diff, params double[] slips)
        {
            string U(double v) => N(v) + (string.IsNullOrEmpty(unit) ? "" : " " + unit);
            var wrong = slips.Where(s => Math.Abs(s - right) > 1e-9).Select(U);
            var spare = new[] { right * 2, right / 2, right + 1, right * 10, right - 1 }.Where(s => Math.Abs(s - right) > 1e-9).Select(U);
            return Q(prompt, U(right), wrong, why, diff, null, spare);
        }

        /// <summary>A fact question: pick a row, ask for its answer, the wrong options are other rows' answers.</summary>
        static QuestionData Fact(string[,] table, string template, string why = "{0}: {1}.", int diff = 1, bool reverse = false)
        {
            int n = table.GetLength(0), i = rng.Next(n);
            string a = table[i, reverse ? 1 : 0], b = table[i, reverse ? 0 : 1];
            var others = Enumerable.Range(0, n).Where(k => k != i).Select(k => table[k, reverse ? 0 : 1]).Where(o => o != b).OrderBy(_ => rng.Next()).ToList();
            return Q(string.Format(template, a), b, others, string.Format(why, table[i, 0], table[i, 1]), diff);
        }

        static QuestionData Code(QuestionData q, string code) { if (q != null) q.data.code = code; return q; }

        static void Finish(QuestionData q, string topic)
        {
            var l = Services.Questions != null ? Services.Questions.Lesson(topic) : null;
            q.id = $"GEN_{topic}_{++counter}_{rng.Next(100000)}";
            q.topic = topic;
            q.arc = l != null ? l.arc : "";
            q.subject = l != null ? l.subject.Replace(" ", "") : "";
            q.concept = l != null ? l.concept : topic;
            q.hint = l != null ? l.keyIdea : "";
            q.prompt = Rich(q.prompt); q.explanation = Rich(q.explanation);
            for (int i = 0; i < q.data.options.Count; i++) q.data.options[i] = Rich(q.data.options[i]);
        }

        /// <summary>Digits after '_' become subscripts (H_2O), after '^' superscripts (10^23).</summary>
        static string Rich(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"_(\d+)", "<sub>$1</sub>");
            return System.Text.RegularExpressions.Regex.Replace(s, @"\^([\-+]?\d+)", "<sup>$1</sup>");
        }

        static readonly Dictionary<string, List<Func<QuestionData>>> Makers = new Dictionary<string, List<Func<QuestionData>>>
        {
            // ================================================================ COMPUTER SCIENCE
            ["cs_bool"] = new List<Func<QuestionData>>
            {
                () =>
                {
                    string g = Pick(new[] { "AND", "OR", "NAND", "NOR", "XOR" }); int a = R(0, 1), b = R(0, 1);
                    int o = Gate(g, a, b);
                    return Q($"A = {a}, B = {b}. What is A {g} B?", o.ToString(), new[] { (1 - o).ToString(), "10", "Undefined" }, $"{g} of {a} and {b} gives {o}.");
                },
                () =>
                {
                    string g = Pick(new[] { "AND", "OR", "NAND", "NOR", "XOR" });
                    string rows = string.Join(",  ", new[] { (0, 0), (0, 1), (1, 0), (1, 1) }.Select(t => $"{t.Item1}{t.Item2}→{Gate(g, t.Item1, t.Item2)}"));
                    return Q($"Which gate has this truth table?  (AB → output)  {rows}", g, new[] { "AND", "OR", "NAND", "NOR", "XOR" }.Where(x => x != g).OrderBy(_ => rng.Next()), $"That pattern is {g}.", 2);
                },
                () => { int n = R(2, 5); int r = 1 << n; return Num($"A truth table with {n} inputs has how many rows?", r, "", $"2^{n} = {r}.", 1, n * 2, n * n, r - 1); },
                () =>
                {
                    int a = R(0, 1), b = R(0, 1); int o = 1 - (a & b);
                    return Q($"A = {a}, B = {b}. What is NOT(A AND B)?", o.ToString(), new[] { (1 - o).ToString(), "2", "Undefined" }, $"A AND B = {a & b}, so NOT gives {o}. Same as (NOT A) OR (NOT B).", 2);
                },
            },
            ["cs_python"] = new List<Func<QuestionData>>
            {
                () => { int a = R(7, 40), b = R(2, 9); return Num($"What does print({a} // {b}) display?", a / b, "", $"// floors: {a} / {b} = {N(a / (double)b)} → {a / b}.", 1, a % b, Math.Round(a / (double)b, 2), a / b + 1); },
                () => { int a = R(7, 50), b = R(2, 9); return Num($"What is the value of {a} % {b}?", a % b, "", $"% is the remainder: {a} = {a / b} × {b} + {a % b}.", 1, a / b, b - a % b, (a % b) + 1); },
                () => { int a = R(2, 5), b = R(2, 4); return Num($"What does print({a} ** {b}) display?", Math.Pow(a, b), "", $"** is power: {a}^{b}.", 1, a * b, Math.Pow(b, a), a + b); },
                () => { int a = R(1, 9), b = R(2, 5), c = R(2, 3); double r = a + b * Math.Pow(c, 2); return Num($"Evaluate: {a} + {b} * {c} ** 2", r, "", $"** first ({c}² = {c * c}), then ×, then +.", 2, Math.Pow(a + b * c, 2), (a + b) * c * c, a + Math.Pow(b * c, 2)); },
                () =>
                {
                    var t = Pick(new[] { ("7 / 2", "float"), ("7 // 2", "int"), ("'7' + '2'", "str"), ("7 > 2", "bool"), ("[7, 2]", "list"), ("(7, 2)", "tuple"), ("{'a': 7}", "dict"), ("3 + 2j", "complex") });
                    return Q($"What is type({t.Item1})?", t.Item2, new[] { "int", "float", "str", "bool", "list", "tuple", "dict" }.Where(x => x != t.Item2).OrderBy(_ => rng.Next()), $"{t.Item1} is a {t.Item2}.", 2);
                },
                () => { string w = Pick(new[] { "ab", "py", "ok", "hi" }); int k = R(2, 4); string r = string.Concat(Enumerable.Repeat(w, k)); return Q($"What is '{w}' * {k}?", $"'{r}'", new[] { $"'{w}{k}'", "Error", $"'{w}'" }, "* repeats a string.", 1); },
                () => { int a = R(2, 9), b = R(2, 9); return Q($"x = '{a}' + '{b}'. What is x?", $"'{a}{b}'", new[] { $"{a + b}", $"'{a + b}'", "Error" }, "+ on two strings joins them.", 2); },
            },
            ["cs_flow"] = new List<Func<QuestionData>>
            {
                () => { int a = R(0, 5), b = a + R(4, 12), s = R(1, 3); int c = (b - a + s - 1) / s; return Num($"How many numbers does list(range({a}, {b}, {s})) contain?", c, "", $"Start {a}, step {s}, stop before {b}.", 2, c + 1, (b - a), c - 1); },
                () => { int a = R(1, 4), s = R(2, 4), n = R(3, 4); int b = a + s * n + R(0, s - 1); var l = Enumerable.Range(0, 99).Select(i => a + i * s).TakeWhile(v => v < b).ToList(); string L(IEnumerable<int> x) => "[" + string.Join(", ", x) + "]"; return Q($"What does list(range({a}, {b}, {s})) give?", L(l), new[] { L(l.Concat(new[] { l.Last() + s })), L(l.Skip(1)), L(Enumerable.Range(a, n)) }, $"Start at {a}, add {s}, stop before {b}.", 2); },
                () => { int n = R(3, 9); int r = n * (n + 1) / 2; return Code(Num("What prints?", r, "", $"1 + 2 + … + {n} = {r}.", 2, r - n, r + n + 1, n), $"total = 0\nfor i in range(1, {n + 1}):\n    total += i\nprint(total)"); },
                () => { int k = R(2, 3), lim = R(10, 60); int v = 1; while (v < lim) v *= k; return Code(Num("What prints?", v, "", $"n keeps multiplying by {k} until it is no longer below {lim}.", 3, v / k, lim, v * k), $"n = 1\nwhile n < {lim}:\n    n = n * {k}\nprint(n)"); },
                () => { int a = R(2, 5), b = R(2, 5); return Code(Num("What is printed?", a * b, "", $"The inner loop runs {b} times for each of {a} outer rounds.", 2, a + b, a * b + 1, Math.Max(a, b)), $"count = 0\nfor i in range({a}):\n    for j in range({b}):\n        count += 1\nprint(count)"); },
                () => { int n = R(6, 9), k = R(3, n - 1); string r = string.Join(" ", Enumerable.Range(1, k - 1)); return Q("What prints?", r, new[] { string.Join(" ", Enumerable.Range(1, k)), string.Join(" ", Enumerable.Range(1, n - 1).Where(i => i != k)), k.ToString() }, $"break leaves the loop as soon as i == {k}.", 2, $"for i in range(1, {n}):\n    if i == {k}:\n        break\n    print(i, end=' ')"); },
                () => { int x = R(1, 30); string r = x > 20 ? "A" : x > 10 ? "B" : "C"; return Q("What prints?", r, new[] { "A", "B", "C", "A then B" }.Where(o => o != r), "Only the first true branch runs.", 1, $"x = {x}\nif x > 20:\n    print('A')\nelif x > 10:\n    print('B')\nelse:\n    print('C')"); },
            },
            ["cs_numsys"] = new List<Func<QuestionData>>
            {
                () => { int n = R(5, 63); string b = Convert.ToString(n, 2); return Q($"{n} in binary is:", b, new[] { Convert.ToString(n + 1, 2), Convert.ToString(n - 1, 2), new string(b.Reverse().ToArray()) }, $"Keep dividing by 2: {n} = {b}_2.", 1); },
                () => { int n = R(5, 63); string b = Convert.ToString(n, 2); return Num($"{b}_2 in decimal is:", n, "", $"Add the place values of the 1s: {n}.", 1, n + 1, n - 1, n * 2); },
                () => { int n = R(16, 255); string h = n.ToString("X"); return Q($"{n} in hexadecimal is:", h, new[] { (n + 1).ToString("X"), (n + 16).ToString("X"), new string(h.Reverse().ToArray()) }, $"{n} = {n / 16} × 16 + {n % 16} → {h}.", 2); },
                () => { int n = R(16, 255); string h = n.ToString("X"); return Num($"{h}_16 in decimal is:", n, "", $"{h} = {n / 16} × 16 + {n % 16} = {n}.", 2, n + 16, n - 1, n / 16 * 10 + n % 16); },
                () => { int n = R(8, 63); string o = Convert.ToString(n, 8); return Num($"{o}_8 in decimal is:", n, "", $"{o[0]} × 8 + {o.Substring(1)} = {n}.", 2, n + 8, int.Parse(o), n - 1); },
                () => { char c = (char)R('B', 'Y'); return Num($"ASCII 'A' is 65. What is '{c}'?", c, "", "Capital letters are consecutive from 65.", 1, c + 1, c - 1, c + 32); },
                () => { int b = R(3, 8); int m = (1 << b) - 1; return Num($"Largest number you can store in {b} bits?", m, "", $"2^{b} − 1 = {m}.", 1, m + 1, b * 2, m / 2); },
                () => { int n = R(5, 15); string b = Convert.ToString(n, 2).PadLeft(4, '0'); string h = n.ToString("X"); return Q($"{b}_2 as one hexadecimal digit:", h, new[] { (n + 1).ToString("X"), n.ToString(), ((n + 5) % 16).ToString("X") }, "Four bits make one hex digit.", 2); },
            },
            ["cs_lists"] = new List<Func<QuestionData>>
            {
                () => { string w = Pick(Words); int i = R(0, w.Length - 1); return Q($"s = '{w}'. What is s[{i}]?", $"'{w[i]}'", new[] { $"'{w[(i + 1) % w.Length]}'", $"'{w[(i + w.Length - 1) % w.Length]}'", "Error" }, "Indexes start at 0.", 1); },
                () => { string w = Pick(Words); int k = R(1, 3); return Q($"s = '{w}'. What is s[-{k}]?", $"'{w[w.Length - k]}'", new[] { $"'{w[k - 1]}'", $"'{w[w.Length - k - 1]}'", "Error" }, $"-{k} counts {k} from the end.", 1); },
                () => { string w = Pick(Words); int a = R(0, 2), b = a + R(2, 3); return Q($"s = '{w}'. What is s[{a}:{b}]?", $"'{w.Substring(a, b - a)}'", new[] { $"'{w.Substring(a, Math.Min(b - a + 1, w.Length - a))}'", $"'{w.Substring(a + 1, Math.Min(b - a, w.Length - a - 1))}'", $"'{w.Substring(0, Math.Min(b, w.Length))}'" }, $"From index {a} up to (not including) {b}.", 2); },
                () => { string w = Pick(Words); return Num($"len('{w}') returns:", w.Length, "", "len counts the characters.", 1, w.Length - 1, w.Length + 1); },
                () => { string w = Pick(Words); char c = w[R(0, w.Length - 1)]; int n = w.Count(x => x == c); return Num($"'{w}'.count('{c}') returns:", n, "", $"'{c}' appears {n} time(s).", 1, n + 1, w.IndexOf(c), 0); },
                () => { string w = Pick(Words); char c = Pick("ZQXJ".ToCharArray()); return Q($"'{w}'.find('{c}') returns:", "-1", new[] { "0", "None", "Error" }, "find() gives -1 when the text is missing.", 2); },
                () => { var l = Enumerable.Range(0, R(3, 5)).Select(_ => R(1, 9)).ToList(); int x = R(1, 9); return Num($"L = [{string.Join(", ", l)}]; L.append({x}). What is len(L)?", l.Count + 1, "", "append adds one item.", 1, l.Count, l.Count + 2); },
                () => { var l = Enumerable.Range(0, R(3, 5)).Select(_ => R(1, 20)).ToList(); return Num($"L = [{string.Join(", ", l)}]. What does L.pop() return?", l.Last(), "", "pop() removes and returns the last item.", 2, l.First(), l.Max(), l.Min()); },
                () => { var l = Enumerable.Range(0, 4).Select(_ => R(1, 20)).Distinct().ToList(); if (l.Count < 3) return null; return Num($"max([{string.Join(", ", l)}]) + min([{string.Join(", ", l)}]) = ?", l.Max() + l.Min(), "", "Largest plus smallest.", 2, l.Sum(), l.Max(), l.Max() - l.Min()); },
            },

            // ================================================================ PHYSICS
            ["ph_laws"] = new List<Func<QuestionData>>
            {
                () => { int m = R(2, 20), a = R(1, 6); return Num($"A net force acts on a {m} kg body giving it an acceleration of {a} m/s². The force is:", m * a, "N", $"F = ma = {m} × {a}.", 1, m + a, m / (double)a, a / (double)m); },
                () => { int m = R(2, 10), a = R(1, 6); return Num($"A net force of {m * a} N acts on a {m} kg body. Its acceleration:", a, "m/s²", $"a = F / m = {m * a} / {m}.", 1, m * a * m, m, a + 1); },
                () => { int m = R(2, 12), v = R(2, 15); return Num($"Momentum of a {m} kg body moving at {v} m/s:", m * v, "kg m/s", $"p = mv = {m} × {v}.", 1, m + v, 0.5 * m * v * v, m * v * 2); },
                () => { int gm = R(2, 10) * 10, M = R(2, 8), v = R(2, 5) * 50; double r = gm / 1000.0 * v / M; return Num($"A {gm} g bullet leaves a {M} kg gun at {v} m/s. The gun's recoil speed:", r, "m/s", $"Momentum is conserved: {gm / 1000.0} × {v} = {M} × v.", 3, r * 10, gm * v / (double)M, r / 10); },
                () => { double m = R(1, 5) / 10.0; int v = R(5, 20); return Num($"A {N(m)} kg ball hits a wall at {v} m/s and rebounds at {v} m/s. Size of its change in momentum:", 2 * m * v, "kg m/s", "Δp = m(v − u) and the velocity reverses.", 3, m * v, 0, m * v / 2); },
                () => { double mu = R(1, 6) / 10.0; int m = R(2, 10); return Num($"A {m} kg block slides on a floor with μₖ = {N(mu)} (g = 10 m/s²). Kinetic friction:", mu * m * 10, "N", "f = μₖ N = μₖ mg.", 2, mu * m, m * 10, mu * 10); },
                () => Fact(new[,] { { "SI unit of force", "newton (kg m/s²)" }, { "SI unit of momentum", "kg m/s" }, { "SI unit of impulse", "N s" }, { "force = rate of change of", "momentum" }, { "impulse equals change in", "momentum" }, { "a moving body keeps moving unless acted on (law)", "Newton's first law" }, { "equal and opposite forces on different bodies (law)", "Newton's third law" }, { "force needed for circular motion", "centripetal force mv²/r" }, { "static friction compared with kinetic friction", "greater" }, { "friction that resists rolling", "rolling friction (the smallest)" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "Inertia of motion", "passengers lurch forward when a bus brakes" }, { "Third law", "a gun recoils when fired" }, { "Second law", "a heavier cart needs more force for the same acceleration" }, { "Inertia of rest", "a coin drops into the glass when the card is flicked" }, { "Conservation of momentum", "a rocket moves forward as gases rush backward" } }, "Which example shows: {0}?", "{0} → {1}.", 2),
            },
            ["ph_kinematics"] = new List<Func<QuestionData>>
            {
                () => { int u = R(0, 10), a = R(1, 5), t = R(2, 8); return Num($"u = {u} m/s, a = {a} m/s², t = {t} s. Final velocity v:", u + a * t, "m/s", "v = u + at.", 1, u * a * t, a * t, u + a + t); },
                () => { int a = R(1, 6) * 2, t = R(2, 6); return Num($"A car starts from rest with a = {a} m/s². Distance in {t} s:", 0.5 * a * t * t, "m", "s = ut + ½at² with u = 0.", 2, a * t * t, a * t, 0.5 * a * t); },
                () => { int t = R(1, 5); return Num($"A stone is dropped from rest (g = 10 m/s²). Distance fallen in {t} s:", 5 * t * t, "m", "s = ½gt².", 2, 10 * t * t, 10 * t, 5 * t); },
                () => { int u = R(1, 5) * 10; return Num($"A ball is thrown at {u} m/s at 45° (g = 10 m/s²). Its range:", u * u / 10.0, "m", "R = u² sin 2θ / g, and sin 90° = 1.", 3, u * u / 20.0, u, u * u / 5.0); },
                () => { int u = R(1, 4) * 10; return Num($"A ball is thrown straight up at {u} m/s (g = 10 m/s²). Maximum height:", u * u / 20.0, "m", "v² = u² − 2gh, so h = u² / 2g.", 2, u * u / 10.0, u / 10.0, u * 2); },
                () => { int u = R(1, 5) * 10; return Num($"A ball is thrown straight up at {u} m/s (g = 10 m/s²). Time to come back down:", 2 * u / 10.0, "s", "Up time u/g, and the same back down.", 2, u / 10.0, u, u / 5.0 * 2); },
                () => Fact(new[,] { { "slope of a velocity-time graph", "acceleration" }, { "area under a velocity-time graph", "displacement" }, { "slope of a position-time graph", "velocity" }, { "angle for maximum range", "45°" }, { "vertical velocity at the top of a projectile's path", "zero" } }, "In kinematics, the {0} gives:", "{0} → {1}.", 2),
            },
            ["ph_work"] = new List<Func<QuestionData>>
            {
                () => { int f = R(2, 20), d = R(2, 10); return Num($"Work done when a {f} N force moves a box {d} m along the force:", f * d, "J", "W = F × d.", 1, f + d, f / (double)d, 0); },
                () => { int m = R(1, 6) * 2, v = R(2, 8); return Num($"Kinetic energy of a {m} kg body at {v} m/s:", 0.5 * m * v * v, "J", "KE = ½mv².", 1, m * v * v, m * v, 0.5 * m * v); },
                () => { int m = R(2, 10), h = R(2, 10); return Num($"Potential energy gained by lifting {m} kg through {h} m (g = 10 m/s²):", m * 10 * h, "J", "PE = mgh.", 1, m * h, m * 10 + h, m * 10 * h / 2.0); },
                () => { int m = R(4, 8) * 10, h = R(2, 6), t = R(2, 5); return Num($"A {m} kg student climbs {h} m in {t} s (g = 10 m/s²). Power:", m * 10.0 * h / t, "W", "P = mgh / t.", 2, m * h / (double)t, m * 10 * h * t, m * 10.0 * h); },
                () => { int k = R(2, 4); return Q($"A body's speed is made {k} times. Its kinetic energy becomes:", $"{k * k}×", new[] { $"{k}×", $"{2 * k}×", "unchanged" }, "KE ∝ v².", 2); },
                () => { int v = Pick(new[] { 10, 20 }); int h = v * v / 20; return Num($"A body falls freely from {h} m (g = 10 m/s²). Speed just before landing:", v, "m/s", "v = √(2gh).", 3, v * 2, h, v / 2.0); },
                () => Fact(new[,] { { "SI unit of work", "joule" }, { "SI unit of power", "watt" }, { "1 horsepower", "746 W" }, { "net work done on a body equals", "its change in kinetic energy" }, { "a collision where KE is conserved", "elastic" }, { "a collision where bodies stick together", "perfectly inelastic" }, { "a force whose work doesn't depend on path", "conservative" }, { "power also equals", "force × velocity" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "friction on a sliding box", "negative" }, { "gravity on a satellite in a circular orbit", "zero" }, { "your push on a box moving the way you push", "positive" }, { "gravity on a falling ball", "positive" } }, "The work done by {0} is:", "{0}: {1}.", 2),
            },
            ["ph_osc"] = new List<Func<QuestionData>>
            {
                () => { double f = Pick(new[] { 2.0, 4.0, 5.0, 10.0, 20.0, 0.5, 0.25 }); return Num($"A body vibrates at {N(f)} Hz. Its period:", 1 / f, "s", "T = 1 / ν.", 1, f, 2 / f, 1 / (2 * f)); },
                () => { double T = Pick(new[] { 0.1, 0.2, 0.25, 0.5, 2.0, 4.0 }); return Num($"An oscillation has a period of {N(T)} s. Its frequency:", 1 / T, "Hz", "ν = 1 / T.", 1, T, 2 / T, 1 / (2 * T)); },
                () => { int k = Pick(new[] { 4, 9, 16, 25 }); int r = (int)Math.Sqrt(k); return Q($"A pendulum is made {k} times longer. Its period becomes:", $"{r}×", new[] { $"{k}×", $"{k * k}×", $"{N(1.0 / r)}×" }, "T = 2π√(L/g) ∝ √L.", 2); },
                () => { int k = Pick(new[] { 4, 9, 16 }); int r = (int)Math.Sqrt(k); return Q($"The mass on a spring is made {k} times heavier. The period becomes:", $"{r}×", new[] { $"{k}×", $"{N(1.0 / r)}×", "unchanged" }, "T = 2π√(m/k) ∝ √m.", 2); },
                () => { int L = Pick(new[] { 1, 4, 9, 16 }); return Num($"Taking g = π² m/s², the period of a {L} m simple pendulum is:", 2 * Math.Sqrt(L), "s", "T = 2π√(L/g) = 2√L when g = π².", 3, Math.Sqrt(L), 2 * L, 4 * Math.Sqrt(L)); },
                () => Fact(new[,] { { "restoring force in SHM", "F = −kx" }, { "period of a spring-mass system", "2π√(m/k)" }, { "period of a simple pendulum", "2π√(L/g)" }, { "period of a seconds pendulum", "2 s" }, { "total energy in SHM", "½kA² (constant)" }, { "unit of frequency", "hertz (s^-1)" }, { "angular frequency ω", "2π/T" } }, "The {0} is:", "{0}: {1}.", 2),
                () => { int k = Pick(new[] { 4, 9 }); int r = (int)Math.Sqrt(k); return Q($"A pendulum's bob is made {k} times heavier (same length). The period:", "stays the same", new[] { $"becomes {r}×", $"becomes {k}×", $"becomes 1/{r}" }, "The bob's mass doesn't appear in T = 2π√(L/g).", 2); },
                () => Fact(new[,] { { "mean (centre) position", "maximum speed" }, { "extreme position", "maximum acceleration" }, { "mean position", "all kinetic energy" }, { "extreme position", "zero speed" } }, "In SHM, at the {0} the body has:", "At the {0}: {1}.", 2),
            },
            ["ph_grav"] = new List<Func<QuestionData>>
            {
                () => { int k = R(2, 5); return Q($"The distance between two masses is made {k} times. The gravitational force becomes:", $"1/{k * k}", new[] { $"1/{k}", $"{k}×", $"{k * k}×" }, "F ∝ 1/r².", 1); },
                () => { int a = R(2, 4), b = R(2, 4); return Q($"One mass is made {a}× and the other {b}× (same distance). The force becomes:", $"{a * b}×", new[] { $"{a + b}×", $"1/{a * b}", $"{a * a * b * b}×" }, "F ∝ m₁m₂.", 2); },
                () => { int w = R(2, 10) * 60; return Num($"A body weighs {w} N on Earth. On the Moon (g/6) it weighs:", w / 6.0, "N", "Weight = mg; the Moon's g is about one sixth.", 2, w, w * 6, w / 2.0); },
                () => Fact(new[,] { { "escape velocity from Earth", "about 11.2 km/s" }, { "orbital speed near Earth's surface", "about 7.9 km/s" }, { "period of a geostationary satellite", "24 hours" }, { "value of g at Earth's centre", "zero" }, { "value of G", "6.67 × 10^-11 N m^2 kg^-2" } }, "The {0} is:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "Kepler's first law", "planets move in ellipses with the Sun at a focus" }, { "Kepler's second law", "equal areas are swept in equal times" }, { "Kepler's third law", "T² ∝ r³" }, { "g at a height equal to Earth's radius", "g/4" }, { "g at Earth's centre", "zero" }, { "a geostationary orbit's height", "about 36 000 km" }, { "weight of a body in free fall (orbit)", "zero (weightless)" }, { "gravitational force between two 1 kg masses 1 m apart", "6.67 × 10^-11 N" } }, "{0}:", "{0} → {1}.", 2),
                () => Q("As you go higher above Earth's surface, g:", "decreases", new[] { "increases", "stays the same", "becomes negative" }, "You're farther from Earth's centre.", 1),
                () => Q("Near Earth, escape speed vₑ and orbital speed v₀ are related by:", "vₑ = √2 · v₀", new[] { "vₑ = 2v₀", "vₑ = v₀/√2", "vₑ = v₀" }, "√(2gR) = √2 × √(gR).", 2),
            },

            // ================================================================ CHEMISTRY
            ["ch_basic"] = new List<Func<QuestionData>>
            {
                () => { var c = Pick(Compounds); double k = Pick(new[] { 0.5, 1, 2, 3, 4, 5 }); double m = c.M * k; return Num($"How many moles are in {N(m)} g of {c.F} (M = {N(c.M)} g/mol)?", k, "mol", "moles = mass ÷ molar mass.", 1, m * c.M, k * 2, 1 / k); },
                () => { var c = Pick(Compounds); int n = R(2, 5); return Num($"Mass of {n} mol of {c.F} (M = {N(c.M)} g/mol):", n * c.M, "g", "mass = moles × molar mass.", 1, c.M / n, c.M + n, n * c.M / 2); },
                () => { int n = R(2, 6); return Num($"Volume of {n} mol of an ideal gas at STP (taking 22.4 L/mol):", n * 22.4, "L", "V = n × 22.4 L.", 2, 22.4 / n, n * 11.2, 22.4); },
                () => { var c = Pick(Compounds.Where(x => x.M <= 60).ToArray()); double v = Pick(new[] { 0.25, 0.5, 1, 2 }); double n = Pick(new[] { 0.1, 0.2, 0.5, 1.0 }); return Num($"{N(n * c.M)} g of {c.F} (M = {N(c.M)}) dissolved to make {N(v * 1000)} mL of solution. Molarity:", n / v, "M", "M = moles ÷ litres.", 2, n, n * v, n / v / 1000); },
                () => { var c = Pick(Compounds); return Q($"The molar mass of {c.F} is:", N(c.M) + " g/mol", new[] { N(c.M + 2) + " g/mol", N(c.M - 1) + " g/mol", N(c.M * 2) + " g/mol" }, "Add the atomic masses (H 1, C 12, N 14, O 16, Na 23, S 32, Cl 35.5, Ca 40).", 2); },
                () => { int n = R(2, 9); return Q($"How many molecules are in {n} mol of a substance?", $"{n} × 6.022 × 10^23", new[] { $"{n} × 6.022 × 10^22", "6.022 × 10^23", $"{n} × 22.4" }, "N = n × Nₐ.", 2); },
                () => Fact(new[,] { { "SI unit of amount of substance", "mole" }, { "unit of molarity", "mol/L" }, { "molality is moles of solute per", "kg of solvent" }, { "STP (old) is", "273 K and 1 atm" }, { "Avogadro's number", "6.022 × 10^23" }, { "1 amu (u) is", "1/12 the mass of a C-12 atom" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "Water always has H and O in the mass ratio 1 : 8", "law of definite proportions" }, { "Mass is neither created nor destroyed in a reaction", "law of conservation of mass" }, { "The reactant that runs out first", "limiting reagent" }, { "The simplest whole-number ratio of atoms", "empirical formula" }, { "1 mole of particles", "6.022 × 10^23" } }, "{0}: this is the", "{0} → {1}.", 2),
            },
            ["ch_atom"] = new List<Func<QuestionData>>
            {
                () => { var e = Pick(Elements); return Num($"How many neutrons are in {e.S} (Z = {e.Z}, A = {e.A})?", e.A - e.Z, "", "neutrons = A − Z.", 1, e.Z, e.A, e.A + e.Z); },
                () => { var e = Pick(Elements); return Num($"How many electrons does a neutral {e.S} atom (Z = {e.Z}) have?", e.Z, "", "Neutral atom: electrons = protons = Z.", 1, e.A - e.Z, e.A, e.Z + 1); },
                () => { int n = R(1, 4); return Num($"Maximum number of electrons in shell n = {n}:", 2 * n * n, "", "2n².", 1, n * n, 2 * n, 8); },
                () => { var s = Pick(new[] { ("s", 1), ("p", 3), ("d", 5), ("f", 7) }); return Num($"How many orbitals are in a {s.Item1} subshell?", s.Item2, "", "s 1, p 3, d 5, f 7.", 1, s.Item2 * 2, s.Item2 + 2, 2); },
                () => { var i = Pick(new[] { ("Na⁺", 10, 11), ("Cl⁻", 18, 17), ("Mg²⁺", 10, 12), ("O²⁻", 10, 8), ("Al³⁺", 10, 13), ("S²⁻", 18, 16), ("K⁺", 18, 19), ("F⁻", 10, 9) }); return Num($"How many electrons does {i.Item1} have? (Z = {i.Item3})", i.Item2, "", "Positive ions lose electrons, negative ions gain them.", 2, i.Item3, i.Item2 + 2, i.Item2 - 1); },
                () => Fact(new[,] { { "electron", "J. J. Thomson" }, { "proton (canal rays)", "E. Goldstein" }, { "neutron", "James Chadwick" }, { "the nucleus (gold foil)", "Ernest Rutherford" }, { "quantised orbits of hydrogen", "Niels Bohr" }, { "matter waves λ = h/mv", "de Broglie" } }, "Who discovered / proposed: {0}?", "{0}: {1}.", 2),
                () => Fact(new[,] { { "Isotopes", "same atomic number, different mass number" }, { "Isobars", "same mass number, different atomic number" }, { "Atomic number", "number of protons" }, { "Mass number", "protons + neutrons" }, { "Principal quantum number n", "the shell (energy level)" }, { "Spin quantum number", "+½ or −½" } }, "{0} means:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "l = 0", "s" }, { "l = 1", "p" }, { "l = 2", "d" }, { "l = 3", "f" } }, "The subshell with {0} is:", "{0} → {1}.", 1),
                () => Fact(new[,] { { "No two electrons have the same four quantum numbers", "Pauli exclusion principle" }, { "Fill equal-energy orbitals singly first", "Hund's rule" }, { "Fill the lowest-energy orbitals first", "Aufbau principle" }, { "Position and momentum can't both be known exactly", "Heisenberg's uncertainty principle" } }, "\"{0}\" is:", "{1}.", 2),
            },
            ["ch_bond"] = new List<Func<QuestionData>>
            {
                () => Fact(Molecules(1), "The shape of {0} is:", "{0}: {1}.", 2),
                () => Fact(Molecules(2), "The bond angle in {0} is about:", "{0}: {1}.", 2),
                () => Fact(Molecules(3), "The hybridisation of the central atom in {0} is:", "{0}: {1}.", 2),
                () => Fact(new[,] { { "NaCl", "ionic" }, { "MgO", "ionic" }, { "KBr", "ionic" }, { "CaF_2", "ionic" }, { "HCl", "covalent" }, { "CH_4", "covalent" }, { "H_2O", "covalent" }, { "NH_3", "covalent" } }, "The bonding in {0} is mainly:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "N_2 (N≡N)", "1 σ + 2 π" }, { "O_2 (O=O)", "1 σ + 1 π" }, { "H_2 (H–H)", "1 σ" }, { "C_2H_2", "3 σ + 2 π" }, { "C_2H_4", "5 σ + 1 π" } }, "The bonds in {0}:", "{0}: {1}.", 3),
            },
            ["ch_periodic"] = new List<Func<QuestionData>>
            {
                () => { var p = Pick(new[] { Period2, Period3 }); var c = Three(p.Length); bool big = rng.Next(2) == 0; int ans = big ? c.Min() : c.Max(); return Q($"Which atom is {(big ? "LARGER" : "SMALLER")}?", p[ans], c.Where(i => i != ans).Select(i => p[i]), "Across a period atoms get smaller.", 1); },
                () => { var p = Pick(new[] { Period2, Period3 }); var c = Three(p.Length); int ans = c.Max(); return Q("Which is the MOST electronegative?", p[ans], c.Where(i => i != ans).Select(i => p[i]), "Electronegativity rises across a period.", 2); },
                () => { var g = Pick(new[] { Group1, Group17 }); var c = Three(g.Length); bool big = rng.Next(2) == 0; int ans = big ? c.Max() : c.Min(); return Q($"Which atom is {(big ? "LARGER" : "SMALLER")}?", g[ans], c.Where(i => i != ans).Select(i => g[i]), "Down a group atoms get bigger.", 1); },
                () => { var g = Pick(new[] { Group1, Group17 }); var c = Three(g.Length); int ans = c.Max(); return Q("Which has the LOWEST first ionisation enthalpy?", g[ans], c.Where(i => i != ans).Select(i => g[i]), "Down a group the outer electron is easier to remove.", 2); },
                () => Fact(new[,] { { "Li", "Group 1" }, { "Na", "Group 1" }, { "Mg", "Group 2" }, { "Cl", "Group 17" }, { "F", "Group 17" }, { "Ne", "Group 18" }, { "Ar", "Group 18" }, { "O", "Group 16" }, { "N", "Group 15" } }, "{0} is in:", "{0}: {1}.", 1),
                () => { var g = Group2; var c = Three(g.Length); bool big = rng.Next(2) == 0; int ans = big ? c.Max() : c.Min(); return Q($"Which atom is {(big ? "LARGER" : "SMALLER")}?", g[ans], c.Where(i => i != ans).Select(i => g[i]), "Down a group atoms get bigger.", 1); },
                () => Fact(new[,] { { "the modern periodic law is based on", "atomic number" }, { "elements in the same group have the same", "number of valence electrons" }, { "the most electronegative element", "fluorine" }, { "the d-block elements are called", "transition metals" }, { "the f-block elements are called", "inner transition metals" }, { "the number of periods", "7" }, { "the number of groups", "18" }, { "Mendeleev arranged elements by", "atomic mass" } }, "In the periodic table, {0}:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "alkali metals", "Group 1" }, { "halogens", "Group 17" }, { "noble gases", "Group 18" }, { "alkaline earth metals", "Group 2" }, { "chalcogens", "Group 16" } }, "The {0} are in:", "{0}: {1}.", 1),
            },
            ["ch_redox"] = new List<Func<QuestionData>>
            {
                () =>
                {
                    var c = Pick(OxNumbers);
                    string S(int v) => v > 0 ? "+" + v : v.ToString();
                    return Q($"The oxidation number of {c.Item2} in {c.Item1} is:", S(c.Item3), new[] { S(-c.Item3), S(c.Item3 - 2), S(c.Item3 + 1), S(c.Item3 - 1) }, $"Use O = −2, H = +1, alkali metal = +1, and make the total equal the charge: {c.Item2} = {S(c.Item3)}.", 2);
                },
                () => Fact(new[,] { { "loss of electrons", "oxidation" }, { "gain of electrons", "reduction" }, { "a species that takes electrons", "oxidising agent" }, { "a species that gives electrons", "reducing agent" }, { "one element both oxidised and reduced", "disproportionation" } }, "{0} is called:", "{0} → {1}.", 1),
                () => Fact(new[,] { { "the oxidation number of a free element", "0" }, { "the usual oxidation number of oxygen", "−2" }, { "oxygen's oxidation number in peroxides", "−1" }, { "hydrogen's oxidation number in metal hydrides", "−1" }, { "the sum of oxidation numbers in a neutral compound", "0" }, { "fluorine's oxidation number in compounds", "always −1" } }, "{0} is:", "{0}: {1}.", 2),
                () => { var r = Pick(new[] { ("Zn + Cu²⁺ → Zn²⁺ + Cu", "Zn", "Cu²⁺"), ("Mg + 2H⁺ → Mg²⁺ + H_2", "Mg", "H⁺"), ("Fe + Cu²⁺ → Fe²⁺ + Cu", "Fe", "Cu²⁺"), ("2Na + Cl_2 → 2NaCl", "Na", "Cl_2") }); return Q($"In {r.Item1}, the species OXIDISED is:", r.Item2, new[] { r.Item3, "neither", "both" }, $"{r.Item2} loses electrons (its oxidation number goes up).", 2); },
            },

            // ================================================================ BIOLOGY
            ["bio_cell"] = new List<Func<QuestionData>>
            {
                () => Fact(Organelles, "Which organelle does this: {0}?", "{0}: {1}.", 1, true),
                () => Fact(Organelles, "The job of the {0} is:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "prokaryotic ribosomes", "70S" }, { "eukaryotic (cytoplasm) ribosomes", "80S" }, { "the plant cell wall", "cellulose" }, { "the fungal cell wall", "chitin" }, { "the cell membrane model", "fluid mosaic (Singer and Nicolson)" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "the smallest cells", "mycoplasma (PPLO)" }, { "the largest single cell", "an ostrich egg" }, { "bacterial DNA region", "nucleoid" }, { "small extra circular DNA in bacteria", "plasmid" }, { "subunits of an 80S ribosome", "60S + 40S" }, { "subunits of a 70S ribosome", "50S + 30S" }, { "cell theory was given by", "Schleiden and Schwann" }, { "the 'powerhouse of the cell'", "mitochondrion" }, { "the 'suicide bags'", "lysosomes" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "Omnis cellula-e cellula", "Rudolf Virchow" }, { "First saw and named cells (cork)", "Robert Hooke" }, { "Fluid mosaic model", "Singer and Nicolson" }, { "Discovered the nucleus", "Robert Brown" } }, "\"{0}\" is credited to:", "{0}: {1}.", 2),
            },
            ["bio_breath"] = new List<Func<QuestionData>>
            {
                () => { var path = new[] { "nostrils", "pharynx", "larynx", "trachea", "bronchi", "bronchioles", "alveoli" }; int i = R(0, path.Length - 2); return Q($"In the airway, right after the {path[i]} comes the:", path[i + 1], path.Where((p, k) => k != i + 1 && k != i).OrderBy(_ => rng.Next()), "Nostrils → pharynx → larynx → trachea → bronchi → bronchioles → alveoli.", 2); },
                () => Fact(new[,] { { "tidal volume", "about 500 mL" }, { "share of O_2 carried by haemoglobin", "about 97%" }, { "share of CO_2 carried as bicarbonate", "about 70%" }, { "number of alveoli in the lungs", "about 300 million" } }, "The {0} is:", "{0}: {1}.", 2),
                () => Fact(new[,] { { "normal breathing rate", "12–16 breaths per minute" }, { "residual volume", "air left after a forced breath out" }, { "muscles that lift the ribs when you breathe in", "external intercostals" }, { "enzyme that turns CO_2 into bicarbonate", "carbonic anhydrase" }, { "respiratory rhythm centre", "medulla" }, { "pneumotaxic centre", "pons" }, { "damaged alveolar walls (smoking)", "emphysema" }, { "inflamed bronchi and bronchioles", "asthma" }, { "breathing in is also called", "inspiration" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "voice box", "larynx" }, { "windpipe", "trachea" }, { "flap over the windpipe", "epiglottis" }, { "site of gas exchange", "alveoli" }, { "muscle that flattens when you breathe in", "diaphragm" } }, "The {0} is the:", "{0}: {1}.", 1),
            },
            ["bio_circ"] = new List<Func<QuestionData>>
            {
                () => { var path = new[] { "right atrium", "right ventricle", "lungs", "left atrium", "left ventricle", "body" }; int i = R(0, path.Length - 2); return Q($"Blood goes from the {path[i]} to the:", path[i + 1], path.Where((p, k) => k != i + 1 && k != i).OrderBy(_ => rng.Next()), "Body → RA → RV → lungs → LA → LV → body.", 2); },
                () => Fact(new[,] { { "pacemaker of the heart", "SA node" }, { "universal donor blood group", "O" }, { "universal recipient blood group", "AB" }, { "valve between left atrium and ventricle", "bicuspid (mitral)" }, { "valve between right atrium and ventricle", "tricuspid" }, { "cells that help in clotting", "platelets" } }, "The {0}:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "share of blood that is plasma", "about 55%" }, { "lifespan of an RBC", "about 120 days" }, { "the 'graveyard' of RBCs", "spleen" }, { "most abundant white blood cells", "neutrophils" }, { "white cells for immune responses", "lymphocytes" }, { "normal blood pressure", "120/80 mm Hg" }, { "stroke volume", "about 70 mL" }, { "the 'dub' heart sound", "semilunar valves closing" }, { "the 'lub' heart sound", "AV valves closing" }, { "Rh problem in a newborn", "erythroblastosis foetalis" }, { "vessels that carry blood away from the heart", "arteries" }, { "the only artery with deoxygenated blood", "pulmonary artery" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "P wave", "atria depolarising" }, { "QRS complex", "ventricles depolarising" }, { "T wave", "ventricles recovering" } }, "In an ECG, the {0} shows:", "{0}: {1}.", 3),
                () => { int bpm = Pick(new[] { 60, 72, 75, 80, 100, 120 }); return Num($"At {bpm} beats per minute, one cardiac cycle lasts:", 60.0 / bpm, "s", "60 ÷ beats per minute.", 2, bpm / 60.0, 60.0 / bpm * 2, 1); },
            },
            ["bio_neural"] = new List<Func<QuestionData>>
            {
                () => Fact(new[,] { { "cerebellum", "balance and coordination" }, { "medulla", "breathing and heartbeat" }, { "hypothalamus", "temperature and hunger" }, { "cerebrum", "thinking and memory" } }, "The {0} controls:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "cerebellum", "balance and coordination" }, { "medulla", "breathing and heartbeat" }, { "hypothalamus", "temperature and hunger" }, { "cerebrum", "thinking and memory" } }, "Which part of the brain controls {0}?", "{0}: {1}.", 1, true),
                () => Fact(new[,] { { "dendrite", "receives signals" }, { "axon", "carries the impulse away from the cell body" }, { "myelin sheath", "speeds up conduction" }, { "synaptic cleft", "the gap between two neurons" } }, "The {0}:", "{0}: {1}.", 1),
                () => Fact(new[,] { { "pons", "relaying signals and helping regulate breathing" }, { "thalamus", "relaying sensory signals" }, { "corpus callosum", "joining the two cerebral hemispheres" }, { "spinal cord", "reflex actions" }, { "cerebrospinal fluid", "cushioning the brain" }, { "meninges", "three protective layers around the brain" } }, "The {0} is for:", "{0}: {1}.", 2),
                () => Fact(new[,] { { "gaps in the myelin sheath", "nodes of Ranvier" }, { "cells that make myelin outside the brain", "Schwann cells" }, { "a common neurotransmitter", "acetylcholine" }, { "resting membrane potential", "about −70 mV" }, { "what the Na⁺/K⁺ pump moves", "3 Na⁺ out, 2 K⁺ in" }, { "impulse jumping node to node", "saltatory conduction" } }, "{0}:", "{0} → {1}.", 2),
                () => Fact(new[,] { { "rush in during depolarisation", "Na⁺" }, { "flow out during repolarisation", "K⁺" } }, "Which ions {0}?", "{1} ions {0}.", 2),
            },
            ["bio_division"] = new List<Func<QuestionData>>
            {
                () => Fact(new[,] { { "DNA replication", "S phase" }, { "chromosomes line up at the equator", "metaphase" }, { "sister chromatids separate", "anaphase" }, { "chromosomes condense", "prophase" }, { "nuclear envelope re-forms", "telophase" }, { "crossing over", "pachytene of prophase I" } }, "When does this happen: {0}?", "{0}: {1}.", 2),
                () => { var o = Pick(new[] { ("human", 46), ("onion", 16), ("dog", 78), ("fruit fly", 8), ("pea", 14), ("potato", 48) }); bool mei = rng.Next(2) == 0; int r = mei ? o.Item2 / 2 : o.Item2; return Num($"A {o.Item1} body cell has {o.Item2} chromosomes. After {(mei ? "meiosis" : "mitosis")}, each daughter cell has:", r, "", mei ? "Meiosis halves the number." : "Mitosis keeps the number.", 2, mei ? o.Item2 : o.Item2 / 2, o.Item2 * 2, r + 1); },
                () => Fact(new[,] { { "G₁ phase", "the cell grows" }, { "G₂ phase", "it prepares for mitosis" }, { "G₀ phase", "a resting, non-dividing state" }, { "leptotene", "chromosomes become visible" }, { "zygotene", "homologous chromosomes pair (synapsis)" }, { "diplotene", "chiasmata appear" }, { "diakinesis", "chiasmata terminalise" }, { "cytokinesis in plant cells", "a cell plate forms" } }, "During {0}:", "{0}: {1}.", 2),
                () => { bool mei = rng.Next(2) == 0; return Q($"{(mei ? "Meiosis" : "Mitosis")} produces:", mei ? "4 cells (n)" : "2 identical cells (2n)", new[] { mei ? "2 identical cells (2n)" : "4 cells (n)", "2 cells (n)", "4 identical cells (2n)" }, mei ? "Two divisions, chromosome number halved." : "One division, same number.", 1); },
            },
        };

        static int Gate(string g, int a, int b)
        {
            switch (g) { case "AND": return a & b; case "OR": return a | b; case "NAND": return 1 - (a & b); case "NOR": return 1 - (a | b); default: return a ^ b; }
        }

        static int[] Three(int n) { var l = Enumerable.Range(0, n).OrderBy(_ => rng.Next()).Take(3).ToArray(); return l; }

        static readonly string[] Words = { "PYTHON", "GLITCH", "NEXUS", "SCHOOL", "BINARY", "KANHA", "PIXEL", "BANANA", "MATRIX", "DEHRADUN", "SHAURYA" };
        static readonly string[] Period2 = { "Li", "Be", "B", "C", "N", "O", "F" };
        static readonly string[] Period3 = { "Na", "Mg", "Al", "Si", "P", "S", "Cl" };
        static readonly string[] Group1 = { "Li", "Na", "K", "Rb", "Cs" };
        static readonly string[] Group17 = { "F", "Cl", "Br", "I" };
        static readonly string[] Group2 = { "Be", "Mg", "Ca", "Sr", "Ba" };

        static readonly (string F, double M)[] Compounds =
        {
            ("H_2O", 18), ("CO_2", 44), ("NH_3", 17), ("CH_4", 16), ("NaCl", 58.5), ("O_2", 32), ("N_2", 28), ("H_2SO_4", 98),
            ("NaOH", 40), ("CaCO_3", 100), ("HCl", 36.5), ("C_6H_12O_6", 180), ("H_2", 2), ("KOH", 56),
        };

        static readonly (string S, int Z, int A)[] Elements =
        {
            ("carbon-12", 6, 12), ("carbon-14", 6, 14), ("nitrogen-14", 7, 14), ("oxygen-16", 8, 16), ("sodium-23", 11, 23), ("magnesium-24", 12, 24),
            ("aluminium-27", 13, 27), ("chlorine-35", 17, 35), ("chlorine-37", 17, 37), ("potassium-39", 19, 39), ("calcium-40", 20, 40), ("iron-56", 26, 56),
        };

        static readonly (string, string, int)[] OxNumbers =
        {
            ("KMnO_4", "Mn", 7), ("K_2Cr_2O_7", "Cr", 6), ("H_2SO_4", "S", 6), ("SO_2", "S", 4), ("HNO_3", "N", 5), ("NH_3", "N", -3),
            ("H_2O_2", "O", -1), ("CO_2", "C", 4), ("CH_4", "C", -4), ("Fe_2O_3", "Fe", 3), ("KClO_3", "Cl", 5), ("MnO_2", "Mn", 4),
            ("NO_2", "N", 4), ("H_2S", "S", -2), ("P_2O_5", "P", 5), ("NaH", "H", -1), ("HClO_4", "Cl", 7), ("SO_3", "S", 6),
            ("CO", "C", 2), ("N_2O", "N", 1), ("HNO_2", "N", 3), ("Cl_2O_7", "Cl", 7), ("CaH_2", "H", -1), ("OF_2", "O", 2), ("Na_2SO_3", "S", 4),
            ("K_2MnO_4", "Mn", 6), ("CrO_3", "Cr", 6), ("FeO", "Fe", 2), ("Cu_2O", "Cu", 1), ("H_3PO_4", "P", 5), ("NaClO", "Cl", 1), ("BaO_2", "O", -1),
        };

        static string[,] Molecules(int col)
        {
            var m = new[,]
            {
                { "BeCl_2", "linear", "180°", "sp" }, { "BF_3", "trigonal planar", "120°", "sp^2" }, { "CH_4", "tetrahedral", "109.5°", "sp^3" },
                { "NH_3", "pyramidal", "107°", "sp^3" }, { "H_2O", "bent", "104.5°", "sp^3" }, { "CO_2", "linear", "180°", "sp" },
                { "SF_6", "octahedral", "90°", "sp^3d^2" }, { "PCl_5", "trigonal bipyramidal", "120° and 90°", "sp^3d" }, { "C_2H_2", "linear", "180°", "sp" },
                { "NH_4^+", "tetrahedral", "109.5°", "sp^3" }, { "C_2H_4", "planar (each C trigonal)", "120°", "sp^2" },
            };
            // a two-column table: formula -> the asked property (duplicates are fine, Fact skips them as distractors)
            int n = m.GetLength(0);
            var t = new string[n, 2];
            for (int i = 0; i < n; i++) { t[i, 0] = m[i, 0]; t[i, 1] = m[i, col]; }
            return t;
        }

        static readonly string[,] Organelles =
        {
            { "mitochondrion", "aerobic respiration (makes ATP)" }, { "ribosome", "protein synthesis" }, { "chloroplast", "photosynthesis" },
            { "Golgi body", "packaging and secretion" }, { "lysosome", "digestion with its enzymes" }, { "rough ER", "making proteins for export" },
            { "smooth ER", "making lipids" }, { "nucleolus", "making ribosomal RNA" }, { "centriole", "forming the spindle in animal cells" },
            { "vacuole", "storage and keeping the cell firm" }, { "nucleus", "holding DNA and controlling the cell" }, { "plasma membrane", "controlling what enters and leaves" },
            { "cell wall", "support and protection in plants" }, { "cytoskeleton", "shape, support and movement" }, { "chromoplast", "giving colour to petals and fruits" },
            { "leucoplast", "storing starch, oils or proteins" }, { "cilia", "moving the cell or fluid past it" },
        };
    }
}

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// Chemistry lab: drop atoms into the reactor. Each shows its valence electrons; when the atoms can fill their
    /// outer shells, they bond and the reaction plays out as animation (not as an equation on a page).
    /// </summary>
    public class ChemBondLab : TeachingLab
    {
        static readonly Dictionary<string, int> Valence = new Dictionary<string, int> { { "H", 1 }, { "O", 6 }, { "C", 4 }, { "N", 5 }, { "Na", 1 }, { "Cl", 7 } };
        static readonly Dictionary<string, string> Known = new Dictionary<string, string>
        {
            { "H2O", "WATER: oxygen shares one pair with each hydrogen (polar covalent)." },
            { "CO2", "CARBON DIOXIDE: carbon makes a double bond with each oxygen." },
            { "CH4", "METHANE: carbon's 4 valence electrons pair with 4 hydrogens (tetrahedral)." },
            { "H3N", "AMMONIA: nitrogen shares 3 pairs and keeps 1 lone pair." },
            { "ClNa", "SALT: sodium GIVES its 1 electron to chlorine. Opposite ions attract: an IONIC bond." },
            { "O2", "OXYGEN GAS: two oxygens share 2 pairs, a double bond." },
            { "N2", "NITROGEN GAS: a triple bond, one of the strongest there is." },
            { "H2", "HYDROGEN GAS: the simplest covalent bond, one shared pair." },
            { "HCl", "HYDROGEN CHLORIDE: one shared pair; in water it becomes hydrochloric acid." },
            { "C2H6O", "ETHANOL: two carbons, an -OH group, and six hydrogens." },
        };

        readonly List<string> atoms = new List<string>();
        RectTransform reactor;
        UIGraph bonds;
        TextMeshProUGUI status;
        string targetKey = "H2O";
        string targetName = "H₂O";
        float bondAnim = -1;

        protected override string Title => "THE BOND LAB";
        protected override string GoalDescription => $"Build <b>{targetName}</b> in the reactor.";

        protected override string Opening()
        {
            if (Boss != null && Boss.teachingLines.Count > 0) return Boss.teachingLines[0];
            return "Atoms bond to fill their outer shell. The dots show the outer electrons. Drop some in and watch.";
        }

        protected override void BuildLab(RectTransform area)
        {
            if (Q != null && Q.type == ChallengeType.Construction && Q.data.required != null && Q.data.mode != "sequence")
            {
                targetKey = Key(Q.data.required);
                targetName = string.IsNullOrEmpty(Q.data.target) ? targetKey : Q.data.target;
            }
            UIFactory.VLayout(area, 16, 0, TextAnchor.UpperCenter);
            var stage = UIFactory.Panel(area, new Color(0.02f, 0.05f, 0.05f, 1f), 18, "Reactor", false);
            UIFactory.Size(stage, -1, 400);
            bonds = UIGraph.Create(stage.transform);
            UIFactory.Stretch(bonds.rectTransform);
            reactor = UIFactory.Rect("Atoms", stage.transform);
            UIFactory.Stretch(reactor);
            status = UIFactory.Text(stage.transform, "", 26, UIFactory.Theme.textDim, TextAlignmentOptions.Bottom);
            UIFactory.Stretch(status.rectTransform, 20, 10, 20, 12);
            var palette = UIFactory.Rect("Palette", area);
            UIFactory.Size(palette, -1, 100);
            UIFactory.HLayout(palette, 16, 0, TextAnchor.MiddleCenter, false);
            foreach (var el in new[] { "H", "O", "C", "N", "Na", "Cl" })
            {
                string e = el;
                var b = UIFactory.Button(palette, $"<b>{el}</b>\n<size=50%>{Valence[el]} outer e⁻</size>", () => Add(e), ButtonStyle.Primary, 34);
                UIFactory.Size(b, 150, 96);
                var c = ConstructionChallenge.AtomColor(el);
                b.GetComponent<Image>().color = new Color(c.r * 0.45f, c.g * 0.45f, c.b * 0.45f, 1f);
            }
            UIFactory.Size(UIFactory.Button(palette, "EMPTY", () => { atoms.Clear(); bondAnim = -1; Redraw(); }, ButtonStyle.Ghost, 24), 160, 96);
            Redraw();
        }

        static string Key(Dictionary<string, int> counts)
        {
            var keys = new List<string>(counts.Keys);
            keys.Sort((a, b) => a == "C" ? -1 : b == "C" ? 1 : string.CompareOrdinal(a, b));
            // Hill order: C first, then H, then alphabetical
            keys.Sort((a, b) =>
            {
                int Rank(string s) => s == "C" ? 0 : s == "H" ? 1 : 2;
                int r = Rank(a).CompareTo(Rank(b));
                return r != 0 ? r : string.CompareOrdinal(a, b);
            });
            var sb = new StringBuilder();
            foreach (var k in keys) sb.Append(k).Append(counts[k] > 1 ? counts[k].ToString() : "");
            return sb.ToString();
        }

        void Add(string el)
        {
            if (atoms.Count >= 10) return;
            atoms.Add(el);
            Services.Audio.Play("drop");
            Interacted();
            bondAnim = -1;
            Redraw();
            var counts = new Dictionary<string, int>();
            foreach (var a in atoms) counts[a] = counts.TryGetValue(a, out var n) ? n + 1 : 1;
            string key = Key(counts);
            if (Known.TryGetValue(key, out var info))
            {
                bondAnim = Time.unscaledTime;
                Services.Audio.Play("bond");
                Say(info);
                Services.Achievements.Add("molecules_built");
                if (key == targetKey) MarkSolved(Boss != null ? "Ugh. They BONDED. Disgusting teamwork. " + info : info);
            }
        }

        void Redraw()
        {
            UIFactory.Clear(reactor);
            bonds.Clear();
            var size = ((RectTransform)reactor).rect.size;
            if (size.x < 10) size = new Vector2(1600, 400);
            if (atoms.Count == 0) { status.text = "Reactor empty. Tap atoms below."; return; }
            int centre = 0; int best = -1;
            for (int i = 0; i < atoms.Count; i++) { int bonding = Mathf.Min(Valence[atoms[i]], 8 - Valence[atoms[i]]); if (bonding > best) { best = bonding; centre = i; } }
            Vector2 c = size / 2;
            bool bonded = bondAnim > 0;
            float spread = bonded ? 150 : 230;
            DrawAtom(c, atoms[centre], 60);
            int k = 0, others = atoms.Count - 1;
            for (int i = 0; i < atoms.Count; i++)
            {
                if (i == centre) continue;
                float a = (k / Mathf.Max(1f, others)) * Mathf.PI * 2 + 0.4f;
                Vector2 p = c + new Vector2(Mathf.Cos(a) * spread * 1.5f, Mathf.Sin(a) * spread * 0.6f);
                if (bonded) bonds.Line(c, p, new Color(UIFactory.Theme.green.r, UIFactory.Theme.green.g, UIFactory.Theme.green.b, 0.8f), 10);
                DrawAtom(p, atoms[i], atoms[i] == "H" ? 38 : 50);
                k++;
            }
            var counts = new Dictionary<string, int>();
            foreach (var at in atoms) counts[at] = counts.TryGetValue(at, out var n) ? n + 1 : 1;
            string key = Key(counts);
            status.text = bonded ? $"<color=#7cffa0>BONDED: {key}</color>" : $"{key}  <color=#ff9a3d>(unstable: shells not full yet)</color>";
        }

        void DrawAtom(Vector2 p, string el, float r)
        {
            var img = UIFactory.Image(reactor, ConstructionChallenge.AtomColor(el), UISprites.Circle(128), "Atom");
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = p; rt.sizeDelta = Vector2.one * r * 2;
            var t = UIFactory.Text(img.transform, el, r * 0.75f, el == "C" ? Color.white : new Color(0.05f, 0.05f, 0.1f), TextAlignmentOptions.Center, true);
            UIFactory.Stretch(t.rectTransform);
            int ve = Valence[el];
            for (int i = 0; i < ve; i++)
            {
                float a = i / (float)ve * Mathf.PI * 2 + Time.unscaledTime;
                bonds.Circle(p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (r + 14), 6, UIFactory.Theme.cyan);
            }
        }

        protected override void Update()
        {
            base.Update();
            if (atoms.Count > 0 && Time.frameCount % 3 == 0) Redraw();
        }
    }
}

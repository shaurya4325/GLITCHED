using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A quiz pit: a Class XI MCQ on a holo-board, three answer pillars standing in the pit and a firewall behind
    /// them. Jump onto the pillar with the right answer: it springs you over as the firewall dissolves. A wrong
    /// pillar sinks (jump off quick!) and Pixel explains. The right pillar never sinks, so nobody gets stuck.
    /// </summary>
    public class AnswerGate : MonoBehaviour
    {
        public string topic;
        public List<Transform> pillars = new List<Transform>();
        public List<TextMeshPro> labels = new List<TextMeshPro>();
        public TextMeshPro question, header;
        public Collider firewall;
        public Transform firewallVisual;
        public Vector2 landing;   // where the launch from the right pillar comes down
        QuestionData q;
        readonly List<int> map = new List<int>();   // pillar -> option index
        readonly HashSet<int> sunk = new HashSet<int>();
        bool solved, missed;
        float cooldown;

        void Start()
        {
            q = Services.Selector.PickTopic(topic, null, true) ?? Services.Selector.PickTopic(topic);
            if (q == null) { Solve(false); return; }
            // the right answer + two wrong ones, shuffled over the pillars
            var opts = new List<int> { q.data.answerIndex };
            var wrong = new List<int>();
            for (int i = 0; i < q.data.options.Count; i++) if (i != q.data.answerIndex) wrong.Add(i);
            while (opts.Count < pillars.Count && wrong.Count > 0) { int k = Random.Range(0, wrong.Count); opts.Add(wrong[k]); wrong.RemoveAt(k); }
            for (int i = opts.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (opts[i], opts[j]) = (opts[j], opts[i]); }
            map.AddRange(opts);
            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < labels.Count; i++)
            {
                bool used = i < map.Count;
                labels[i].text = used ? $"<size=60%><color=#ffd640>{letters[i]}</color></size>\n{q.data.options[map[i]]}" : "";
                if (!used && i < pillars.Count) pillars[i].gameObject.SetActive(false);
                if (used && DevMode.On && map[i] == q.data.answerIndex) labels[i].text = "<size=60%><color=#ffd640>★ DEV</color></size>\n" + q.data.options[map[i]];
            }
            var lesson = Services.Questions.Lesson(topic);
            if (header != null) header.text = lesson != null ? $"QUIZ JUMP  ·  CLASS XI {lesson.subject.ToUpperInvariant()}  ·  {lesson.title.ToUpperInvariant()}" : "QUIZ JUMP";
            if (question != null) question.text = q.prompt + "\n<size=55%><color=#9fb0cc>Jump onto the pillar with the right answer</color></size>";
        }

        void Update() => cooldown -= Time.deltaTime;

        /// <summary>The player stands on this pillar.</summary>
        public void Land(PlayerController p, Transform pillar)
        {
            int i = pillars.IndexOf(pillar);
            if (q == null || i < 0 || i >= map.Count || cooldown > 0f || sunk.Contains(i)) return;
            if (map[i] == q.data.answerIndex)
            {
                cooldown = 0.6f;
                if (!solved) Solve(!missed);
                Launch(p);
                return;
            }
            // wrong: the pillar sinks under you
            cooldown = 0.3f;
            sunk.Add(i);
            missed = true;
            Services.Mastery.Record(q, false, false);
            ParkourRun.Current?.QuizAnswered(false);
            Services.Audio.Play("wrong");
            GlitchFX.Pulse(0.35f, 0.3f);
            labels[i].color = new Color(1f, 0.35f, 0.35f);
            Companion.Say("Nope! " + q.explanation, 4.5f);
            StartCoroutine(Sink(pillars[i]));
        }

        void Solve(bool firstTry)
        {
            solved = true;
            if (q != null)
            {
                Services.Mastery.Record(q, true, firstTry);
                ParkourRun.Current?.QuizAnswered(firstTry);
                Services.Game.AddXP(firstTry ? 30 : 15);
                Services.Audio.Play("correct");
                Services.UI.Banner(firstTry ? "CORRECT!" : "GOT IT!", UIFactory.Theme.green, 90, 0.6f, q.data.options[q.data.answerIndex]);
                for (int i = 0; i < map.Count && i < labels.Count; i++) if (map[i] == q.data.answerIndex) labels[i].color = new Color(0.4f, 1f, 0.55f);
            }
            if (firewall != null) firewall.enabled = false;
            if (firewallVisual != null) StartCoroutine(Dissolve(firewallVisual));
        }

        void Launch(PlayerController p)
        {
            // a spring jump that comes down beyond the firewall
            var pos = p.transform.position;
            float g = p.gravity, h = Mathf.Max(3.8f, landing.y - pos.y + 2.8f), vx = 0f;
            for (int i = 0; i < 12; i++)
            {
                // flight time up to the top and back down to the landing height; go higher until the speed is sane
                float tUp = Mathf.Sqrt(2f * h / g);
                float tDown = Mathf.Sqrt(2f * Mathf.Max(0.1f, pos.y + h - landing.y) / (g * p.fallMultiplier));
                vx = (landing.x - pos.x) / (tUp + tDown);
                if (Mathf.Abs(vx) <= 12f || h >= 9f) break;
                h += 0.5f;
            }
            p.Launch(h, vx, true);
            Services.Audio.Play("double_jump");
            VFX.Burst(pos + Vector3.up * 0.3f, UIFactory.Theme.green, 24, "spark", 6f);
        }

        /// <summary>A wrong pillar drops into the pit, then rises back as a plain stepping stone (marked ✖), so the
        /// other pillars stay reachable.</summary>
        IEnumerator Sink(Transform pillar)
        {
            Vector3 home = pillar.localPosition;
            for (float t = 0f; t < 0.35f; t += Time.deltaTime) { pillar.localPosition = home + new Vector3(Mathf.Sin(t * 80f) * 0.06f, 0f, 0f); yield return null; }
            var cols = pillar.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;
            Services.Audio.Play("fall", 0.5f);
            for (float t = 0f; t < 1.2f; t += Time.deltaTime) { pillar.localPosition = home + Vector3.down * Mathf.Min(9f, 6f * t * t); yield return null; }
            yield return new WaitForSeconds(1.2f);
            int i = pillars.IndexOf(pillar);
            if (i >= 0 && i < labels.Count && i < map.Count) labels[i].text = "<size=60%><color=#ff5a6a>✖</color></size>\n" + q.data.options[map[i]];
            for (float t = 0f; t < 1f; t += Time.deltaTime) { pillar.localPosition = Vector3.Lerp(home + Vector3.down * 9f, home, Mathf.SmoothStep(0f, 1f, t)); yield return null; }
            pillar.localPosition = home;
            foreach (var c in cols) c.enabled = true;
        }

        IEnumerator Dissolve(Transform wall)
        {
            Services.Audio.Play("door_open");
            Vector3 s = wall.localScale;
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float k = 1f - t / 0.6f;
                wall.localScale = new Vector3(s.x * (1f + Mathf.Sin(t * 60f) * 0.2f * (1 - k)), s.y * k, s.z);
                yield return null;
            }
            wall.gameObject.SetActive(false);
        }
    }
}

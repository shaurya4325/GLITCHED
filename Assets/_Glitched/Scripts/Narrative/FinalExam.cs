using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// THE FINAL EXAM: Kanha the Dard, the one behind the Glitch, in his Examination Hall (scene Kanha_Final, after
    /// The Source). The fight is a test paper: Section A Physics, B Chemistry, C Biology, four hard questions each
    /// (StreamingAssets/Questions/FinalExam.json). Kanha throws every question at you as a flying paper, from his
    /// hand, the windows, the ceiling: anywhere. A right answer flies back and cracks his composure; a wrong one is
    /// the "dard" (pain): it hurts. 8 / 12 passes. Fail and it is a re-exam (with hints). Everything he says comes
    /// from KanhaLines, so his dialogue can be rewritten in the game.
    /// </summary>
    public class FinalExam : MonoBehaviour
    {
        const int PerSection = 4, PassMark = 8;
        static readonly (string subject, string key, string title)[] SectionsDef =
        {
            ("Physics", "section_a", "SECTION A  ·  PHYSICS"),
            ("Chemistry", "section_b", "SECTION B  ·  CHEMISTRY"),
            ("Biology", "section_c", "SECTION C  ·  BIOLOGY"),
        };
        static readonly string[] Attacks = { "SURPRISE TEST", "NEGATIVE MARKING", "REFERENCE BOOK SLAM", "BOARD EXAM PRESSURE", "TRICK QUESTION", "OUT OF SYLLABUS" };
        static readonly Color Red = new Color(1f, 0.22f, 0.32f);

        ExamHall.Hall hall;
        CharacterVisual kanha;
        Transform kanhaRoot;
        Light aura;
        readonly List<Transform> shards = new List<Transform>();
        MaterialPropertyBlock _mpb;
        MaterialPropertyBlock mpb => _mpb ??= new MaterialPropertyBlock();   // not in a field initializer: Unity forbids it
        Renderer[] kanhaRenderers = new Renderer[0];
        float glitch = 0.15f, hitFlash, dissolve;
        TextMeshProUGUI hSection, hCount, hMarks;
        Image composure;
        CanvasGroup header;
        int marks, total;
        float composureTarget = 1188f;

        IEnumerator Start()
        {
            PauseMenu.Allowed = false;
            var db = Services.Db;
            hall = ExamHall.Find() ?? ExamHall.Build();   // normally built and light-baked in the editor
            Services.Corruption.SetupRegion(ContentDefaults.CreateTheme("ExamHall"), RenderSettings.sun, 0.35f);
            ExamHall.Light(hall);
            var p = PlayerController.Spawn(hall.playerSpot + Vector3.left * 9f);
            p.Visual.Face(1);
            CameraRig.Create(p.transform);
            Companion.Spawn(p.transform);
            Services.UI.Hud.Show(false);
            Services.Audio.Music.Cinematic("opening_room");
            BuildKanha(db);
            BuildHeader();

            // ---------------- the reveal
            CutscenePlayer.Begin();
            p.Lock(this);
            var box = Services.UI.Dialogue;
            if (Services.Game.Data.HasFlag("exam_transfer"))
            {
                // through the rift from the Bio-Zone: it tears open under the ceiling and drops him into the hall
                Services.Game.Data.flags.Remove("exam_transfer");
                CutscenePlayer.AutoAdvance = 1f;
                Vector3 land = hall.playerSpot + Vector3.left * 9f;
                var rift = ChapterPortal.Open(land + Vector3.up * 7.5f, "", "", Red, true);
                rift.enabled = false;   // a way in, not a way out
                foreach (var t in rift.GetComponentsInChildren<TMPro.TextMeshPro>()) t.gameObject.SetActive(false);
                yield return CutscenePlayer.Shot(land + new Vector3(0f, 3.2f, -11f), land + Vector3.up * 7f, 50, 0);
                yield return CutscenePlayer.Wait(1.2f);
                p.Teleport(land + Vector3.up * 9.5f);
                Services.Audio.Play("whoosh");
                yield return CutscenePlayer.Shot(land + new Vector3(0f, 2.4f, -9f), land + Vector3.up * 1.2f, 46, 0.9f);
                for (float t = 0; t < 3f && !p.CC.isGrounded; t += Time.deltaTime) yield return null;
                Services.Audio.Play("boom");
                CameraRig.Instance?.Shake(1.2f);
                VFX.Dust(p.transform.position);
                VFX.Burst(p.transform.position + Vector3.up * 0.5f, Color.white, 40, "spark", 6f, 0.3f, 1.2f);   // loose exam papers
                p.Visual.Trigger("Land");
                yield return CutscenePlayer.Wait(0.5f);
                Destroy(rift.gameObject, 1.5f);
                yield return CutscenePlayer.Say("aarav", "Ow. Desks. Rows of desks. A clock that's way too loud.", "flustered");
                yield return CutscenePlayer.Say("aarav", "No. No no no. This is an EXAM HALL.", "scared");
                CutscenePlayer.AutoAdvance = 0f;
            }
            kanha.FaceYaw(0f);   // back to us, writing on the board
            yield return CutscenePlayer.Shot(new Vector3(2f, 5f, -16f), new Vector3(20f, 2.5f, 8f), 52, 0);
            yield return CutscenePlayer.Shot(new Vector3(8f, 3f, -12f), new Vector3(24f, 2f, 6f), 46, 3.5f);
            yield return CutscenePlayer.Say("aarav", "An exam hall? Inside The Nexus? This is worse than the robots.", "scared");
            yield return CutscenePlayer.Say("pixel", "This place isn't one of mine. Someone built it. On purpose.", "alarm");
            Services.Audio.Play("glitch_big");
            GlitchFX.Pulse(0.5f, 0.4f);
            CameraRig.Instance?.Shake(0.8f);
            kanha.Face(-1);
            kanha.PlayOnce("Taunt", 0.3f);
            yield return CutscenePlayer.IntroCard(kanhaRoot, new Vector3(-1.4f, 0.1f, -2.0f), new Vector3(0, 1.45f, 0), "Kanha the Dard",
                "Class topper. Glitch-maker. Your final exam.", Red, 28f);
            var intro = KanhaLines.Get("intro");
            for (int i = 0; i < intro.Count; i++)
            {
                yield return CutscenePlayer.Say(KanhaLines.Speaker, intro[i]);
                if (i == 0) yield return CutscenePlayer.Say("aarav", "Kanha?! YOU made the Glitch?!", "flustered");
            }
            yield return CutscenePlayer.Say("aarav", "I left my game PAUSED for this.", "smirk");
            foreach (var l in KanhaLines.Get("rules")) yield return CutscenePlayer.Say(KanhaLines.Speaker, l);
            yield return CutscenePlayer.Say("pixel", "Shaurya. Toppers panic too. Read every question twice. You've got this.", "happy");
            yield return CutscenePlayer.Say("aarav", "Physics, Chemistry, Biology. Okay. I've prepared for this my whole life. Mostly by panicking.", "sweating");
            box.Show(false);
            CutscenePlayer.End();
            PauseMenu.Allowed = true;

            // ---------------- the paper
            p.Teleport(hall.playerSpot);
            p.Visual.Face(1);
            CameraRig.Instance?.FrameEncounter(kanhaRoot);
            CameraRig.Instance?.SetExtraDistance(3f);
            Services.Audio.Music.Boss(5);
            Services.Audio.Play("boss_roar");
            Services.UI.Hud.Show(true);
            Services.UI.Hud.SetObjective("Pass Kanha's Final Exam: " + PassMark + " marks out of " + (PerSection * SectionsDef.Length) + ".");
            Tween.Fade(header, 1f, 0.4f);
            int attempt = 0;
            var passedIds = new HashSet<string>();
            while (true)
            {
                var paper = Compose(passedIds, attempt);
                marks = 0; total = paper.Count;
                bool hints = attempt > 0;
                UpdateHeader("", 0);
                string lastSubject = null;
                for (int i = 0; i < paper.Count; i++)
                {
                    var q = paper[i];
                    if (q.subject != lastSubject)
                    {
                        lastSubject = q.subject;
                        foreach (var s in SectionsDef)
                            if (s.subject == q.subject)
                            {
                                Services.UI.Banner(s.title, UIFactory.Theme.cyan, 90, 1.1f, i == 0 ? "THE FINAL EXAM HAS BEGUN" : null);
                                hSection.text = s.title;
                                yield return new WaitForSecondsRealtime(0.9f);
                                yield return KanhaSays(KanhaLines.Random(s.key), 2.2f);
                            }
                    }
                    UpdateHeader(lastSubject, i);
                    // he throws the question at you, from anywhere
                    kanha.PlayOnce("Cast", 0.15f);
                    Services.Audio.Play("whoosh");
                    Vector3 from = Random.value < 0.45f ? kanhaRoot.position + Vector3.up * 1.4f : hall.throwFrom[Random.Range(0, hall.throwFrom.Count)];
                    yield return ThrowPaper(from, p.transform.position + Vector3.up * 1.2f, 0.55f, true);
                    // answer it
                    ChallengeOutcome o = default; bool done = false;
                    string attack = Attacks[Random.Range(0, Attacks.Length)];
                    if (CaptureRunner.Active) { yield return new WaitForSecondsRealtime(1f); o.correct = i % 5 != 2; o.seconds = 6f; done = true; }
                    else ChallengeEngine.Run(q, "", 0, r => { o = r; done = true; },
                        new ChallengeEngine.Options { enemyName = "KANHA THE DARD", attackName = attack, showHint = hints, header = $"Q{i + 1} of {total}  ·  {q.subject.ToUpperInvariant()}" });
                    while (!done) yield return null;
                    Services.Mastery.Record(q, o.correct, o.fast);
                    if (o.correct)
                    {
                        marks++;
                        passedIds.Add(q.id);
                        Services.Audio.Play("correct");
                        yield return ThrowPaper(p.transform.position + Vector3.up * 1.3f, kanhaRoot.position + Vector3.up * 1.3f, 0.35f, false);
                        HitKanha(o.fast);
                        UpdateHeader(lastSubject, i + 1);
                        bool almost = marks == PassMark - 1;
                        yield return KanhaSays(almost ? KanhaLines.Random("almost") : KanhaLines.Random("correct"), 1.6f);
                    }
                    else
                    {
                        Services.Audio.Play("boss_attack");
                        kanha.PlayOnce("Taunt", 0.2f);
                        VFX.Burst(p.transform.position + Vector3.up * 1.2f, Red, 40, "spark", 6f);
                        CameraRig.Instance?.Shake(0.9f);
                        GlitchFX.Pulse(0.45f, 0.25f);
                        p.TakeDamage(28f, kanhaRoot.position, false);
                        UpdateHeader(lastSubject, i + 1);
                        yield return KanhaSays(KanhaLines.Random("wrong"), 1.8f);
                        if (!CaptureRunner.Active) yield return ChallengeEngine.ShowExplanation(q, o, "KANHA: THE DARD");
                        if (p.Stability <= 0f)
                        {
                            p.Visual.Trigger("Defeat");
                            Services.UI.Banner("KNOCKED DOWN", Red, 110, 1f, "HINTS UNLOCKED");
                            yield return new WaitForSecondsRealtime(1f);
                            yield return KanhaSays(KanhaLines.Random("knockdown"), 2.2f);
                            yield return CutscenePlayer.Say("pixel", "Up. Up up up. He only wins if you stop. Hints are on now: use them.", "alarm");
                            box.Show(false);
                            p.RestoreFull();
                            hints = true;
                        }
                    }
                }
                if (marks >= PassMark) break;
                // a re-exam
                attempt++;
                Services.UI.Banner("FAILED", Red, 140, 1.4f, $"{marks} / {total}  ·  RE-EXAM");
                Services.Audio.Play("boss_regen");
                glitch = 0.15f;
                yield return new WaitForSecondsRealtime(1.4f);
                yield return KanhaSays(KanhaLines.Random("fail"), 2.4f);
                yield return CutscenePlayer.Say("pixel", "Okay, that one doesn't count. New paper, and this time the hints are on. Breathe.", "happy");
                box.Show(false);
                p.RestoreFull();
            }

            // ---------------- passed: the result, his end
            PauseMenu.Allowed = false;
            Services.UI.Hud.Show(false);
            Tween.Fade(header, 0f, 0.3f);
            CameraRig.Instance?.EndEncounter();
            CameraRig.Instance?.SetExtraDistance(0);
            Services.Audio.Music.DropOut(0.8f);
            TimeController.SlowMo(0.2f, 0.8f);
            PostFXController.Kick(1f);
            Services.Audio.Play("explosion");
            yield return ResultCard(marks, total);
            CutscenePlayer.Begin();
            yield return CutscenePlayer.Shot(kanhaRoot.position + new Vector3(-3.2f, 1.6f, -4.2f), kanhaRoot.position + Vector3.up * 1.2f, 40, 1.2f);
            kanha.PlayState("Defeat", 0.3f);
            var defeat = KanhaLines.Get("defeat");
            for (int i = 0; i < defeat.Count; i++)
            {
                yield return CutscenePlayer.Say(KanhaLines.Speaker, defeat[i]);
                if (i == 0) yield return CutscenePlayer.Say("aarav", "Pens down, Kanha. Time's up.", "cool");
            }
            box.Show(false);
            // he glitches apart, the papers fall, the hall goes quiet
            Services.Audio.Play("glitch_big");
            kanha.PlayState("Dying", 0.2f);
            for (float t = 0; t < 2.6f; t += Time.deltaTime)
            {
                dissolve = t / 2.6f; glitch = Mathf.Lerp(0.3f, 1f, dissolve);
                if (Random.value < Time.deltaTime * 6f) VFX.GlitchApart(kanhaRoot.position + Vector3.up * Random.Range(0.3f, 1.6f), 0.6f);
                yield return null;
            }
            VFX.Explosion(kanhaRoot.position + Vector3.up, Red, 1.4f);
            kanhaRoot.gameObject.SetActive(false);
            foreach (var paperT in hall.papers) if (paperT != null) { var fp = paperT.GetComponent<FloatingPaper>(); if (fp != null) Destroy(fp); }
            Services.Corruption.Set(0f);
            Services.Audio.Music.Victory();
            VFX.Confetti(p.transform.position + Vector3.up * 2f);
            yield return CutscenePlayer.Shot(p.transform.position + new Vector3(1.6f, 1.7f, -3.4f), p.transform.position + Vector3.up * 1.4f, 38, 1.2f);
            yield return CutscenePlayer.Say("pixel", "Shaurya... you PASSED. The Glitch is gone. The Nexus is free!", "happy");
            yield return CutscenePlayer.Say("aarav", "I can't believe the final boss was an EXAM. Okay. I can, actually.", "smirk");
            box.Show(false);
            CutscenePlayer.End();
            Services.Game.Data.SetFlag("kanha_defeated");
            Services.Game.AddXP(1000);
            Services.Save.Write();
            yield return Services.UI.Fade.FadeOut(1.2f, true);
            Services.Scenes.Load("Ending");
        }

        // ------------------------------------------------------------------ the paper
        List<QuestionData> Compose(HashSet<string> alreadyPassed, int attempt)
        {
            var paper = new List<QuestionData>();
            foreach (var s in SectionsDef)
            {
                var pool = new List<QuestionData>();
                foreach (var q in Services.Questions.All) if (q.arc == "FinalExam" && q.subject == s.subject) pool.Add(q);
                if (pool.Count == 0)   // no exam file: fall back to the hardest questions of that subject in the regions
                    foreach (var q in Services.Questions.All) if (q.subject == s.subject && q.difficulty >= 4) pool.Add(q);
                // shuffle, then a re-exam puts the questions not yet answered right first
                for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
                var fresh = pool.FindAll(q => !alreadyPassed.Contains(q.id));
                fresh.AddRange(pool.FindAll(q => alreadyPassed.Contains(q.id)));
                pool = fresh;
                for (int i = 0; i < pool.Count && paper.Count < (System.Array.IndexOf(SectionsDef, s) + 1) * PerSection; i++) paper.Add(pool[i]);
            }
            return paper;
        }

        IEnumerator KanhaSays(string line, float seconds)
        {
            if (string.IsNullOrEmpty(line)) yield break;
            yield return Services.UI.Dialogue.Say(KanhaLines.Speaker, line, null, seconds);
            Services.UI.Dialogue.Show(false);
        }

        /// <summary>A question paper flies (spinning, on an arc) from a to b; red papers are his, white ones yours.</summary>
        IEnumerator ThrowPaper(Vector3 a, Vector3 b, float seconds, bool his)
        {
            var mb = new MeshBuilder(1);
            mb.BoxC(Vector3.zero, new Vector3(0.21f * 1.6f, 0.006f, 0.297f * 1.6f), 0);
            var mat = his ? CityMaterials.Emissive(new Color(0.95f, 0.92f, 0.9f), new Color(1.4f, 0.25f, 0.35f)) : CityMaterials.Emissive(new Color(0.95f, 0.95f, 0.92f), new Color(0.5f, 1.4f, 1.6f));
            var go = mb.ToObject("ThrownPaper", null, new[] { mat }, false);
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.25f; trail.startWidth = 0.18f; trail.endWidth = 0f;
            trail.material = mat;
            for (float t = 0; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                go.transform.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                go.transform.rotation = Quaternion.Euler(t * 900f, t * 600f, t * 300f);
                yield return null;
            }
            Destroy(go, 0.3f);
            VFX.Burst(b, his ? Red : UIFactory.Theme.cyan, 18, "spark", 4f);
            Services.Audio.Play("hit");
            if (his) { CameraRig.Instance?.Shake(0.35f); GlitchFX.Pulse(0.2f, 0.12f); }
        }

        void HitKanha(bool crit)
        {
            hitFlash = 1f;
            glitch = Mathf.Min(0.85f, 0.15f + marks * 0.06f);   // the more you know, the more he glitches
            kanha.PlayOnce("Hit", 0.08f);
            VFX.Hit(kanhaRoot.position + Vector3.up * 1.3f, crit ? UIFactory.Theme.yellow : UIFactory.Theme.cyan, crit);
            CameraRig.Instance?.Shake(crit ? 0.9f : 0.5f);
            if (crit) { PostFXController.Kick(0.6f); Services.UI.Banner("CRITICAL!", UIFactory.Theme.yellow, 80, 0.5f); }
        }

        // ------------------------------------------------------------------ Kanha himself
        void BuildKanha(GameDatabase db)
        {
            kanhaRoot = new GameObject("Kanha").transform;
            kanhaRoot.position = hall.kanhaSpot + Vector3.up * 0.12f;   // he hovers, just off the stage
            var vgo = new GameObject("Visual"); vgo.transform.SetParent(kanhaRoot, false);
            kanha = vgo.AddComponent<CharacterVisual>();
            kanha.Build(db.Character("kanha"), false);
            kanhaRenderers = kanha.GetComponentsInChildren<Renderer>(true);
            kanha.PlayState("Breathing_Idle", 0f);
            var lgo = new GameObject("Aura"); lgo.transform.SetParent(kanhaRoot, false); lgo.transform.localPosition = new Vector3(0, 2.7f, -0.4f);   // overhead, so it rims him instead of flooding his face
            aura = lgo.AddComponent<Light>(); aura.type = LightType.Point; aura.color = Red; aura.range = 6f; aura.intensity = 2f;
            // glitch shards swirl around his ankles (where the scan ends) and drift up
            var mat = CityMaterials.Emissive(new Color(0.2f, 0.02f, 0.05f), new Color(2.6f, 0.3f, 0.6f));
            for (int i = 0; i < 26; i++)
            {
                var mb = new MeshBuilder(1);
                mb.BoxC(Vector3.zero, Vector3.one * Random.Range(0.03f, 0.09f), 0);
                var s = mb.ToObject("Shard", kanhaRoot, new[] { mat }, false).transform;
                shards.Add(s);
            }
        }

        void Update()
        {
            if (kanhaRoot == null) return;
            float t = Time.time;
            for (int i = 0; i < shards.Count; i++)
            {
                if (shards[i] == null) continue;
                float a = t * (0.9f + i % 5 * 0.25f) + i * 2.4f;
                float h = Mathf.Repeat(t * 0.25f + i * 0.137f, 1f);
                shards[i].localPosition = new Vector3(Mathf.Cos(a) * (0.35f - h * 0.15f), h * 0.6f, Mathf.Sin(a) * (0.3f - h * 0.12f));
                shards[i].localRotation = Quaternion.Euler(a * 70f, a * 50f, 0);
                shards[i].localScale = Vector3.one * (1f - h);
            }
            kanhaRoot.position = new Vector3(kanhaRoot.position.x, hall.kanhaSpot.y + 0.12f + Mathf.Sin(t * 1.3f) * 0.05f, kanhaRoot.position.z);
            // he hovers, but the Animator must think he stands: without Grounded it drops into the mid-air pose
            if (kanha != null) kanha.SetLocomotion(0f, true, 0f);
            if (aura != null) aura.intensity = 1.9f + Mathf.Sin(t * 3.1f) * 0.5f + hitFlash * 4f;   // a menacing glow, not a red wash over his face
            hitFlash = Mathf.MoveTowards(hitFlash, 0f, Time.deltaTime * 3f);
            float g = glitch * (Random.value < 0.04f ? 3f : 1f);
            foreach (var r in kanhaRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_GlitchAmount", Mathf.Clamp01(g));
                mpb.SetFloat("_HitFlash", hitFlash);
                mpb.SetFloat("_Dissolve", dissolve);
                r.SetPropertyBlock(mpb);
            }
            if (composure != null)
            {
                var sd = composure.rectTransform.sizeDelta;
                composure.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(sd.x, composureTarget, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f)), sd.y);
            }
            // the hall: racing clock, turning fans, a stuttering tube light
            if (hall.clockHandFast != null) hall.clockHandFast.localRotation = Quaternion.Euler(0, 0, -t * 220f);
            if (hall.clockHand != null) hall.clockHand.localRotation = Quaternion.Euler(0, 0, -t * 18f);
            foreach (var f in hall.fans) if (f != null) f.Rotate(0, Time.deltaTime * 240f, 0, Space.Self);
            if (hall.tubeLights.Count > 0 && hall.tubeLights[0] != null)
                hall.tubeLights[0].intensity = Random.value < 0.08f ? 1f : ExamHall.TubeIntensity;
        }

        // ------------------------------------------------------------------ UI: the exam header and the result card
        void BuildHeader()
        {
            var th = UIFactory.Theme;
            var root = UIFactory.Rect("ExamHeader", Services.UI.HudLayer);
            UIFactory.Place(root, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1240, 132), new Vector2(0.5f, 1));
            header = root.gameObject.AddComponent<CanvasGroup>(); header.alpha = 0f;
            var bg = UIFactory.Panel(root, new Color(0.96f, 0.95f, 0.9f, 0.96f), 14, "Paper");
            UIFactory.Stretch(bg.rectTransform);
            var ink = new Color(0.1f, 0.1f, 0.14f);
            var t = UIFactory.Text(root, "GLITCHED BOARD EXAMINATION  ·  SCIENCE", 24, ink, TextAlignmentOptions.Left, true);
            UIFactory.Place(t.rectTransform, new Vector2(0, 1), new Vector2(26, -12), new Vector2(760, 36), new Vector2(0, 1));
            hSection = UIFactory.Text(root, "", 30, new Color(0.55f, 0.05f, 0.12f), TextAlignmentOptions.Left, true);
            UIFactory.Place(hSection.rectTransform, new Vector2(0, 1), new Vector2(26, -50), new Vector2(760, 40), new Vector2(0, 1));
            hCount = UIFactory.Text(root, "", 26, ink, TextAlignmentOptions.Right, true);
            UIFactory.Place(hCount.rectTransform, new Vector2(1, 1), new Vector2(-26, -12), new Vector2(420, 36), new Vector2(1, 1));
            hMarks = UIFactory.Text(root, "", 26, ink, TextAlignmentOptions.Right, true);
            UIFactory.Place(hMarks.rectTransform, new Vector2(1, 1), new Vector2(-26, -50), new Vector2(420, 40), new Vector2(1, 1));
            var barBg = UIFactory.Image(root, new Color(0, 0, 0, 0.12f), UISprites.Rounded(6), "ComposureBg");
            UIFactory.Place(barBg.rectTransform, new Vector2(0, 0), new Vector2(26, 14), new Vector2(1188, 16), new Vector2(0, 0));
            composure = UIFactory.Image(barBg.transform, Red, UISprites.Rounded(6), "Composure");
            UIFactory.Place(composure.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(1188, 16), new Vector2(0, 0.5f));
            var label = UIFactory.Text(root, "KANHA'S COMPOSURE", 16, new Color(0.4f, 0.05f, 0.1f), TextAlignmentOptions.Left, true);
            UIFactory.Place(label.rectTransform, new Vector2(0, 0), new Vector2(28, 32), new Vector2(400, 22), new Vector2(0, 0));
        }

        void UpdateHeader(string subject, int answered)
        {
            if (hCount == null) return;
            hCount.text = total > 0 ? $"Q {Mathf.Min(answered + 1, total)} of {total}" : "";
            hMarks.text = $"MARKS  {marks} / {total}     PASS  {PassMark}";
            float left = 1f - Mathf.Clamp01(marks / (float)PassMark);
            composureTarget = 1188f * Mathf.Max(0.02f, left);
        }

        IEnumerator ResultCard(int m, int of)
        {
            var th = UIFactory.Theme;
            float pct = of > 0 ? m * 100f / of : 0f;
            string grade = pct >= 91 ? "A1" : pct >= 81 ? "A2" : pct >= 71 ? "B1" : pct >= 61 ? "B2" : "C1";
            var root = UIFactory.Stretch(UIFactory.Rect("Result", Services.UI.PopupLayer));
            var cg = root.gameObject.AddComponent<CanvasGroup>(); cg.alpha = 0f;
            UIFactory.Blocker(root, 0.55f);
            var card = UIFactory.Panel(root, new Color(0.97f, 0.96f, 0.92f, 1f), 18, "Card");
            UIFactory.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 560));
            var ink = new Color(0.1f, 0.1f, 0.14f);
            void Line(string s, float size, float y, Color c, bool bold = true)
            {
                var tx = UIFactory.Text(card.transform, s, size, c, TextAlignmentOptions.Center, bold);
                UIFactory.Place(tx.rectTransform, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(840, size * 1.6f), new Vector2(0.5f, 1));
            }
            Line("GLITCHED BOARD OF SECONDARY EDUCATION", 26, -36, ink);
            Line("STATEMENT OF MARKS  ·  SCIENCE", 22, -76, new Color(0.35f, 0.35f, 0.4f), false);
            Line(Services.Game.Data.playerName.ToUpperInvariant(), 52, -140, ink);
            Line($"{m} / {of}   ({pct:0}%)", 64, -230, new Color(0.1f, 0.45f, 0.2f));
            Line($"GRADE {grade}   ·   PASSED", 40, -330, new Color(0.1f, 0.45f, 0.2f));
            Line("Invigilator: Kanha the Dard (signature glitched)", 22, -440, new Color(0.45f, 0.1f, 0.15f), false);
            Services.Audio.Play("powerup");
            card.transform.localScale = Vector3.one * 0.85f;
            Tween.Fade(cg, 1f, 0.3f);
            Tween.Scale(card.transform, Vector3.one, 0.35f, Ease.OutBack);
            for (float x = 0; x < (CaptureRunner.Active ? 2.5f : 4.5f); x += Time.unscaledDeltaTime)
            {
                if (x > 1.2f && Services.Input.AdvancePressed) break;
                yield return null;
            }
            Tween.Fade(cg, 0f, 0.3f);
            yield return new WaitForSecondsRealtime(0.3f);
            Destroy(root.gameObject);
        }
    }
}

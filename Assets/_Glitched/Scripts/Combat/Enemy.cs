using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// An enemy built from an EnemyDataSO (model prefab or primitive placeholder). Patrols, starts an encounter
    /// when the player enters its aggro zone, winds up its attack while the challenge timer runs, recoils from
    /// hits, attacks back on wrong answers, and on defeat glitches apart into colour channels.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        public EnemyDataSO data;
        public float aggroRadius = 5f;
        public string id;
        [Tooltip("Optional: start this fight with a specific question id.")]
        public string firstQuestionId;
        public bool tutorialFight;
        [Tooltip("Class XI lesson id (LessonData.id): this enemy asks MCQs on the topic the last lesson terminal taught.")]
        public string topic;

        public int HP { get; private set; }
        public bool Defeated { get; private set; }
        public Transform Model { get; private set; }
        public bool InEncounter { get; set; }

        Vector3 home;
        int dir = 1;
        float windUp;
        Transform eyes;
        RectTransform bar;
        UIBar hpBar;
        RectTransform bubble;
        TextMeshProUGUI bubbleText;
        readonly List<Renderer> renderers = new List<Renderer>();
        MaterialPropertyBlock mpb;
        float flash, glitch, squash;
        Animator anim;
        MachineMotion machine;
        bool realistic;
        bool animHasAttack, animHasHit;

        /// <summary>Bosses: once the animator has run a few frames, rescale and re-seat the model by its real skinned bounds.</summary>
        IEnumerator Renormalize()
        {
            yield return null; yield return null;
            if (Model == null) yield break;
            // converted rigs report bogus mesh bounds; their bones are real transforms, so measure the skeleton
            var bones = new HashSet<Transform>();
            foreach (var smr in Model.GetComponentsInChildren<SkinnedMeshRenderer>()) foreach (var bn in smr.bones) if (bn != null) bones.Add(bn);
            if (bones.Count < 4) yield break;
            Bounds Skeleton() { Bounds bb = default; bool first = true; foreach (var bn in bones) { if (first) { bb = new Bounds(bn.position, Vector3.zero); first = false; } else bb.Encapsulate(bn.position); } return bb; }
            var b = Skeleton();
            if (b.size.y < 0.0001f) yield break;
            // the skin reaches a little past the outermost joints (top of the head, the soles)
            float f = data.height / (b.size.y * 1.12f);
            // scale a holder above the model: the clips of converted rigs write the model root's own transform
            var holder = new GameObject("BossScale").transform;
            holder.SetParent(transform, false);
            holder.position = Model.position;
            var root = Model;
            root.SetParent(holder, true);
            holder.localScale = Vector3.one * f;
            yield return null;
            b = Skeleton();
            holder.position += new Vector3(transform.position.x - b.center.x, transform.position.y - (b.min.y - b.size.y * 0.04f), transform.position.z - b.center.z);
            Debug.Log($"[Enemy] {data.id}: sized by its skeleton x{f:0.###} ({bones.Count} bones, {b.size.y:0.0} m)");
        }

        void Start()
        {
            if (data == null) { Debug.LogWarning($"[Enemy] {name} has no data"); enabled = false; return; }
            if (!string.IsNullOrEmpty(id) && Services.Game.Data.flags.Contains("defeated_" + id) && data.tier != EnemyTier.Boss && data.tier != EnemyTier.MiniBoss)
            {
                gameObject.SetActive(false);
                return;
            }
            HP = data.maxHP;
            home = transform.position;
            realistic = LevelBuilder.Realistic(data.arc);
            if (data.modelPrefab != null)
            {
                Model = Instantiate(data.modelPrefab, transform).transform;
                // converted (Sketchfab) rigs animate their root scale and position: size the model in its first
                // animated pose, not the bind pose
                var a0 = Model.GetComponentInChildren<Animator>();
                if (a0 != null && a0.runtimeAnimatorController != null) a0.Update(0f);
                CharacterVisual.Normalize(Model, data.height);
                if (data.tier >= EnemyTier.MiniBoss)
                {
                    // their importer bounds don't match the scaled bones either: never cull a boss, and check the size again
                    // once the animation is really running
                    foreach (var smr in Model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
                    StartCoroutine(Renormalize());
                }
                anim = Model.GetComponentInChildren<Animator>();
                machine = Model.GetComponentInChildren<MachineMotion>();
                if (anim != null && anim.runtimeAnimatorController != null)
                    foreach (var p in anim.parameters) { if (p.name == "Attack") animHasAttack = true; if (p.name == "Hit") animHasHit = true; }
            }
            else Model = PlaceholderFactory.BuildEnemy(transform, data.placeholderShape, data.tint, data.height, out eyes);
            Model.localRotation = Quaternion.Euler(0, 240, 0);
            renderers.AddRange(Model.GetComponentsInChildren<Renderer>());
            mpb = new MaterialPropertyBlock();
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(aggroRadius * 2, 4, 3);
            col.center = new Vector3(0, 1.5f, 0);
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            ChallengeEngine.TimerProgress += OnTimer;
        }

        void OnDestroy()
        {
            ChallengeEngine.TimerProgress -= OnTimer;
            if (bar) Destroy(bar.gameObject);
            if (bubble) Destroy(bubble.gameObject);
        }

        void OnTimer(float k) { if (InEncounter) windUp = k; }

        void OnTriggerEnter(Collider other)
        {
            if (Defeated || InEncounter || !other.CompareTag("Player")) return;
            if (data.tier == EnemyTier.Boss || data.tier == EnemyTier.MiniBoss) return;   // bosses are started by their arena
            EncounterManager.Instance.Begin(this);
        }

        // ------------------------------------------------------------------ visuals
        void Update()
        {
            if (Model == null) return;
            float t = Time.time;
            if (!InEncounter && !Defeated)
            {
                // patrol
                if (!data.flying)
                {
                    transform.position += Vector3.right * dir * Time.deltaTime * 1.1f;
                    if (Mathf.Abs(transform.position.x - home.x) > data.patrolDistance) { dir = -dir; transform.position = new Vector3(home.x + Mathf.Sign(transform.position.x - home.x) * data.patrolDistance, transform.position.y, 0); }
                }
                FaceTowards(dir);
            }
            else if (InEncounter && PlayerController.Instance != null)
                FaceTowards(PlayerController.Instance.transform.position.x > transform.position.x ? 1 : -1);

            float bob = data.flying ? Mathf.Sin(t * 2.4f) * 0.25f + 0.6f : 0f;
            float wiggle = InEncounter ? windUp : 0f;
            float shake = wiggle > 0.6f ? Mathf.Sin(t * 60f) * 0.05f * wiggle : 0f;
            squash = Mathf.MoveTowards(squash, 0, Time.deltaTime * 3f);
            float sq = 1f + Mathf.Sin(t * 4f) * 0.04f + wiggle * 0.12f - squash * 0.25f;
            if (realistic) sq = 1f;   // real machines and people don't squash and stretch
            Model.localPosition = new Vector3(shake, bob, 0);
            Model.localScale = Vector3.Lerp(Model.localScale, new Vector3(1f / Mathf.Sqrt(sq), sq, 1f / Mathf.Sqrt(sq)) * BaseScale, Time.deltaTime * 12f);
            if (eyes != null) eyes.localScale = Vector3.one * (1f + wiggle * 0.4f);
            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 5f);
            float windFlash = wiggle > 0.75f ? (Mathf.Repeat(t * 6, 1) < 0.5f ? 0.35f : 0f) : 0f;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_HitFlash", Mathf.Max(flash, windFlash));
                mpb.SetFloat("_GlitchAmount", Mathf.Max(glitch, Services.Corruption.Value * 0.12f));
                r.SetPropertyBlock(mpb);
            }
            if (bar != null) hpBar.Set(HP / (float)data.maxHP);
        }

        float baseScale = -1;
        float BaseScale { get { if (baseScale < 0) baseScale = Model.localScale.y; return baseScale; } }

        void FaceTowards(int d)
        {
            float target = d > 0 ? 120f : 240f;
            Model.localRotation = Quaternion.Slerp(Model.localRotation, Quaternion.Euler(0, target, 0), Time.deltaTime * 8f);
        }

        public void ShowBar(bool on)
        {
            if (on && bar == null)
            {
                bar = UIFactory.Rect("EnemyBar", Services.UI.WorldLayer);
                bar.sizeDelta = new Vector2(240, 46);
                var name = UIFactory.Text(bar, data.displayName, 18, UIFactory.Theme.magenta, TextAlignmentOptions.Top, true);
                UIFactory.Place(name.rectTransform, new Vector2(0.5f, 1), Vector2.zero, new Vector2(300, 22), new Vector2(0.5f, 1));
                var lesson = Services.Questions.Lesson(topic);
                if (lesson != null)
                {
                    // which chapter this enemy is made of
                    var tt = UIFactory.Text(bar, lesson.title.ToUpperInvariant(), 14, LessonCard.SubjectColor(lesson.subject), TextAlignmentOptions.Bottom, true);
                    UIFactory.Place(tt.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 18), new Vector2(320, 18), new Vector2(0.5f, 0));
                }
                hpBar = UIFactory.Bar(bar, UIFactory.Theme.magenta, new Color(0, 0, 0, 0.6f), "HP", 6);
                UIFactory.Place(hpBar.GetComponent<RectTransform>(), new Vector2(0.5f, 0), Vector2.zero, new Vector2(220, 14), new Vector2(0.5f, 0));
                hpBar.Set(HP / (float)data.maxHP, true);
                bar.gameObject.AddComponent<WorldFollower>().Init(transform, new Vector3(0, data.height + 0.7f + (data.flying ? 0.6f : 0), 0));
            }
            if (bar != null) bar.gameObject.SetActive(on);
        }

        public void Say(string text, float seconds = 2.2f)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (bubble == null)
            {
                bubble = UIFactory.Rect("EnemySpeech", Services.UI.WorldLayer);
                bubble.sizeDelta = new Vector2(560, 90);
                var p = UIFactory.Panel(bubble, new Color(0.12f, 0.02f, 0.1f, 0.94f), 16, "Bubble");
                UIFactory.Stretch(p.rectTransform);
                bubbleText = UIFactory.Text(p.transform, "", 26, Color.white, TextAlignmentOptions.Center, true);
                UIFactory.Stretch(bubbleText.rectTransform, 14, 6, 14, 6);
                bubbleText.gameObject.AddComponent<GlitchText>().intensityOverride = 0.35f;
                var f = bubble.gameObject.AddComponent<WorldFollower>();
                f.Init(transform, new Vector3(0, data.height + 1.6f + (data.flying ? 0.6f : 0), 0));
                f.clampToScreen = true;
            }
            bubble.gameObject.SetActive(true);
            bubbleText.GetComponent<GlitchText>().Show(text);
            if (!Services.Audio.SpeakLine(VoiceBank.SpeakerOf(data), text)) Services.Audio.PlayBlip("blip", 0.7f);
            bubble.localScale = Vector3.one * 0.6f;
            Tween.Scale(bubble, Vector3.one, 0.2f);
            CancelInvoke(nameof(HideBubble));
            Invoke(nameof(HideBubble), seconds + text.Length / 40f);
        }

        void HideBubble() { if (bubble) bubble.gameObject.SetActive(false); }

        public string RandomOf(List<string> list) => list != null && list.Count > 0 ? list[Random.Range(0, list.Count)] : "";

        // ------------------------------------------------------------------ combat hooks
        public void TakeHit(int damage, bool crit)
        {
            HP = Mathf.Max(0, HP - damage);
            flash = 1f;
            squash = crit ? 1f : 0.6f;
            glitch = crit ? 0.8f : 0.4f;
            Tween.After(0.25f, () => glitch = 0);
            if (anim != null && animHasHit) anim.SetTrigger("Hit");
            if (machine != null) machine.Hit();
            var center = transform.position + Vector3.up * data.height * 0.6f;
            VFX.Hit(center, crit ? UIFactory.Theme.yellow : UIFactory.Theme.cyan, crit);
            Tween.Move(transform, transform.position + Vector3.right * (PlayerController.Instance != null && PlayerController.Instance.transform.position.x < transform.position.x ? 0.6f : -0.6f) * (crit ? 2f : 1f), 0.15f, Ease.OutQuad, false);
        }

        public void Heal(int amount)
        {
            HP = Mathf.Min(data.maxHP, HP + amount);
            VFX.Burst(transform.position + Vector3.up * data.height * 0.5f, UIFactory.Theme.green, 30, "magic", 4f);
        }

        /// <summary>Lunges at the player (ENEMY TURN).</summary>
        public IEnumerator Attack(PlayerController p)
        {
            if (anim != null && animHasAttack) anim.SetTrigger("Attack");
            if (machine != null) machine.Attack();
            Vector3 start = transform.position;
            Vector3 toward = start + Vector3.right * Mathf.Sign(p.transform.position.x - start.x) * Mathf.Min(1.6f, Mathf.Abs(p.transform.position.x - start.x) - 0.8f);
            yield return Tween.Move(transform, toward, 0.12f, Ease.InQuad, false);
            Services.Audio.Play("enemy_attack");
            VFX.Burst(p.transform.position + Vector3.up, UIFactory.Theme.magenta, 24, "spark", 7f);
            p.TakeDamage(data.attackDamage, transform.position);
            yield return Tween.Move(transform, start, 0.3f, Ease.OutQuad, false);
        }

        /// <summary>Glitches, splits into colour channels, and stops existing.</summary>
        public IEnumerator Defeat()
        {
            Defeated = true;
            ShowBar(false);
            Say(RandomOf(data.defeatLines), 1.6f);
            if (!string.IsNullOrEmpty(id)) Services.Game.Data.SetFlag("defeated_" + id);
            glitch = 1f;
            Services.Audio.Play("enemy_defeat");
            // colour-channel ghosts
            var ghosts = new List<Transform>();
            Color[] channels = { new Color(1, 0.1f, 0.3f), new Color(0.1f, 1, 0.5f), new Color(0.2f, 0.4f, 1f) };
            for (int i = 0; i < 3; i++)
            {
                var g = Instantiate(Model.gameObject, transform).transform;
                foreach (var r in g.GetComponentsInChildren<Renderer>())
                {
                    var b = new MaterialPropertyBlock();
                    r.GetPropertyBlock(b);
                    b.SetColor("_Tint", channels[i]);
                    b.SetFloat("_GlitchAmount", 1f);
                    r.SetPropertyBlock(b);
                }
                ghosts.Add(g);
            }
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                for (int i = 0; i < ghosts.Count; i++)
                {
                    ghosts[i].localPosition = Model.localPosition + new Vector3((i - 1) * t * 1.4f, Mathf.Sin(t * 30 + i) * 0.05f, 0);
                    foreach (var r in ghosts[i].GetComponentsInChildren<Renderer>()) { var b = new MaterialPropertyBlock(); r.GetPropertyBlock(b); b.SetFloat("_Dissolve", t / 0.9f); r.SetPropertyBlock(b); }
                }
                foreach (var r in renderers) { if (r == null) continue; r.GetPropertyBlock(mpb); mpb.SetFloat("_Dissolve", t / 0.9f); r.SetPropertyBlock(mpb); }
                yield return null;
            }
            VFX.GlitchApart(transform.position + Vector3.up * data.height * 0.5f, data.height);
            Blast.At(transform.position + Vector3.up * data.height * 0.5f, Mathf.Clamp(data.height * 0.6f, 0.6f, 3f), data.tier >= EnemyTier.MiniBoss ? Blast.StyleFor(data.arc) : BlastStyle.Glitch, true, false, true);
            gameObject.SetActive(false);
        }
    }
}

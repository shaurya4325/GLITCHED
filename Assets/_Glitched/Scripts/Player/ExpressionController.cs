using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    public enum ExpressionSequence { Correct, Wrong, Impossible, Surprised, Victory, Talk }

    /// <summary>
    /// Expression is the feedback system (Section 10.1). Swappable 2D face textures on a face-decal quad:
    ///   correct:    neutral -> surprised -> smirk -> cool
    ///   wrong:      neutral -> flustered -> scared -> defeated
    ///   impossible: neutral -> sweating -> melting
    /// API: Play(ExpressionSequence), SetExpression("smirk").
    /// Replace Resources/Faces/Aarav/face_*.png with your own art (same names) to restyle the face.
    /// </summary>
    public class ExpressionController : MonoBehaviour
    {
        public static readonly string[] Names = { "neutral", "surprised", "smirk", "cool", "flustered", "scared", "defeated", "sweating", "melting", "talk", "blink" };

        CharacterVisual visual;
        readonly Dictionary<string, Texture2D> faces = new Dictionary<string, Texture2D>();
        Renderer decal;
        MaterialPropertyBlock mpb;
        string current = "neutral";
        string held;               // expression held by a sequence
        bool talking;
        float blinkTimer = 3f, blinkUntil;
        Coroutine seq;
        static readonly int IdBase = Shader.PropertyToID("_BaseMap");

        // blend-shape faces (generated characters): expression -> (shape, weight) targets, eased every frame
        SkinnedMeshRenderer face;
        readonly Dictionary<string, int> shapeIndex = new Dictionary<string, int>();
        readonly Dictionary<int, float> target = new Dictionary<int, float>();
        static readonly Dictionary<string, (string, float)[]> Shapes = new Dictionary<string, (string, float)[]>
        {
            { "neutral", new (string, float)[0] },
            { "surprised", new[] { ("open", 55f), ("browsUp", 100f) } },
            { "smirk", new[] { ("smile", 60f), ("browsDown", 15f) } },
            { "cool", new[] { ("smile", 35f), ("blink", 35f) } },
            { "flustered", new[] { ("frown", 40f), ("browsUp", 60f), ("open", 15f) } },
            { "scared", new[] { ("open", 45f), ("browsUp", 90f), ("frown", 30f) } },
            { "defeated", new[] { ("frown", 70f), ("blink", 45f), ("browsUp", 30f) } },
            { "sweating", new[] { ("frown", 30f), ("browsUp", 70f), ("smile", 20f) } },
            { "melting", new[] { ("open", 70f), ("blink", 60f), ("frown", 40f) } },
            { "talk", new[] { ("open", 45f) } },
            { "blink", new[] { ("blink", 100f) } },
        };
        static readonly int IdColor = Shader.PropertyToID("_BaseColor");

        public string Current => held ?? current;

        public void Setup(CharacterVisual v, CharacterProfileSO p)
        {
            visual = v;
            faces.Clear();
            face = null;
            shapeIndex.Clear();
            foreach (var smr in v.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (smr.sharedMesh != null && smr.sharedMesh.GetBlendShapeIndex("smile") >= 0) { face = smr; break; }
            if (face != null)
            {
                for (int i = 0; i < face.sharedMesh.blendShapeCount; i++) shapeIndex[face.sharedMesh.GetBlendShapeName(i)] = i;
                foreach (var n in Names) faces[n] = null;   // every expression is available
                applied = null;
                Apply("neutral");
                return;
            }
            foreach (var n in Names)
            {
                var t = Resources.Load<Texture2D>($"{p.faceFolder}/face_{n}");
                if (t != null) faces[n] = t;
            }
            if (!faces.ContainsKey("neutral")) { Debug.Log($"[Expression] No face textures in Resources/{p.faceFolder}"); return; }
            if (decal != null) PlaceholderFactory.Kill(decal.gameObject);

            var head = v.Head;
            var model = v.Model;
            var go = new GameObject("FaceDecal");
            var t0 = go.transform;
            bool placeholder = v.Proc != null;
            float size = placeholder ? 0.4f : v.HeadSize * 1.7f;
            if (placeholder)
            {
                t0.SetParent(head, false);
                t0.localPosition = new Vector3(0, 0.24f, 0.262f);
                t0.localRotation = Quaternion.Euler(0, 180, 0);
                t0.localScale = new Vector3(size, size, 1);
            }
            else
            {
                t0.position = head.position + model.forward * v.HeadSize * 0.95f + model.up * v.HeadSize * 0.05f;
                t0.rotation = model.rotation * Quaternion.Euler(0, 180, 0);
                t0.localScale = new Vector3(size, size, 1);
                t0.SetParent(head, true);
                // a skin-coloured plate hides the model's painted face
                var plate = Resources.Load<Texture2D>($"{p.faceFolder}/face_plate");
                if (plate != null)
                {
                    var pg = new GameObject("FacePlate");
                    pg.transform.SetParent(t0, false);
                    pg.transform.localPosition = new Vector3(0, 0, 0.004f);
                    pg.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Quad();
                    var pr = pg.AddComponent<MeshRenderer>();
                    pr.sharedMaterial = MaterialCache.Get("face");
                    var pb = new MaterialPropertyBlock();
                    pb.SetTexture(IdBase, plate);
                    pb.SetColor(IdColor, p.skinColor);
                    pr.SetPropertyBlock(pb);
                    pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            go.AddComponent<MeshFilter>().sharedMesh = ProceduralMeshes.Quad();
            decal = go.AddComponent<MeshRenderer>();
            decal.sharedMaterial = MaterialCache.Get("face");
            decal.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decal.receiveShadows = false;
            mpb = new MaterialPropertyBlock();
            Apply("neutral");
        }

        public void SetExpression(string name)
        {
            if (seq != null) { StopCoroutine(seq); seq = null; }
            held = null;
            current = faces.ContainsKey(name) || (face != null && Shapes.ContainsKey(name)) ? name : "neutral";
            Apply(current);
        }

        public void Play(ExpressionSequence s)
        {
            if (seq != null) StopCoroutine(seq);
            seq = StartCoroutine(Run(s));
        }

        IEnumerator Run(ExpressionSequence s)
        {
            (string, float)[] steps;
            switch (s)
            {
                case ExpressionSequence.Correct: steps = new[] { ("neutral", 0.12f), ("surprised", 0.3f), ("smirk", 0.45f), ("cool", 1.4f) }; break;
                case ExpressionSequence.Wrong: steps = new[] { ("neutral", 0.12f), ("flustered", 0.4f), ("scared", 0.5f), ("defeated", 1.3f) }; break;
                case ExpressionSequence.Impossible: steps = new[] { ("neutral", 0.2f), ("sweating", 0.9f), ("melting", 1.8f) }; break;
                case ExpressionSequence.Surprised: steps = new[] { ("surprised", 1.0f) }; break;
                case ExpressionSequence.Victory: steps = new[] { ("smirk", 0.4f), ("cool", 2.2f) }; break;
                default: steps = new[] { ("neutral", 0.1f) }; break;
            }
            if (s == ExpressionSequence.Wrong && visual != null) visual.Trigger("Hit");
            foreach (var (name, time) in steps)
            {
                held = name;
                Apply(name);
                yield return new WaitForSecondsRealtime(time);
            }
            held = null;
            seq = null;
            Apply(current);
        }

        public void SetTalking(bool on) => talking = on;

        void Update()
        {
            if (face != null) EaseShapes();
            if (decal == null && face == null) return;
            string baseFace = held ?? current;
            if (talking && held == null)
            {
                bool open = Mathf.Repeat(Time.unscaledTime * 9f, 1f) < 0.5f;
                Apply(open ? "talk" : baseFace);
                return;
            }
            if (held == null && baseFace == "neutral")
            {
                blinkTimer -= Time.unscaledDeltaTime;
                if (blinkTimer <= 0) { blinkTimer = Random.Range(2f, 5f); blinkUntil = Time.unscaledTime + 0.12f; }
                Apply(Time.unscaledTime < blinkUntil ? "blink" : "neutral");
            }
        }

        string applied;
        void EaseShapes()
        {
            float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
            foreach (var kv in shapeIndex)
            {
                float t = target.TryGetValue(kv.Value, out var w) ? w : 0f;
                face.SetBlendShapeWeight(kv.Value, Mathf.Lerp(face.GetBlendShapeWeight(kv.Value), t, k));
            }
        }

        void Apply(string name)
        {
            if (face != null)
            {
                if (name == applied) return;
                applied = name;
                target.Clear();
                if (Shapes.TryGetValue(name, out var set))
                    foreach (var (shape, w) in set)
                        if (shapeIndex.TryGetValue(shape, out int idx)) target[idx] = w;
                return;
            }
            if (decal == null || name == applied) return;
            if (!faces.TryGetValue(name, out var tex)) tex = faces["neutral"];
            applied = name;
            decal.GetPropertyBlock(mpb);
            mpb.SetTexture(IdBase, tex);
            mpb.SetColor(IdColor, Color.white);
            decal.SetPropertyBlock(mpb);
        }
    }
}

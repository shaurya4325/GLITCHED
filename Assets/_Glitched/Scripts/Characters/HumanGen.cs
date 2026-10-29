using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Generates an original, fully rigged human character in code: a Mixamo-named humanoid skeleton in T-pose (so
    /// every Mixamo / Universal Animation Library clip retargets onto it), one skinned mesh built from smooth rings
    /// along the bones (clothing is part of the same surface, so nothing clips), a modelled face with eyes, lids,
    /// brows, nose, ears and lips, and blend shapes for expressions (smile, frown, open, blink, brows up/down).
    /// The character faces +Z; its left side is -X.
    /// </summary>
    public class HumanGen
    {
        public enum Mat { Skin, Top, TopAccent, Bottom, Shoe, Sole, Hair, EyeWhite, Iris, Pupil, Lips, Pack, Metal, Glass, Count }

        readonly HumanSpec spec;
        readonly float s;   // scale factor (height / 1.7 m)
        readonly float fs;  // head / face scale (teenagers have proportionally larger heads)
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> norms = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<BoneWeight> weights = new List<BoneWeight>();
        readonly List<int>[] tris = new List<int>[(int)Mat.Count];
        readonly List<Transform> bones = new List<Transform>();
        readonly Dictionary<string, int> boneIndex = new Dictionary<string, int>();
        Transform root;

        // face landmarks (bind pose, model space) used to build the expression blend shapes
        Vector3 headC, mouthC, eyeL, eyeR, browCL, browCR;
        float headR;
        readonly List<int> lidL = new List<int>(), lidR = new List<int>();

        public const string P = "mixamorig:";

        public HumanGen(HumanSpec spec)
        {
            this.spec = spec;
            s = spec.height / 1.7f;
            fs = s * (spec.robot ? 1f : 1.14f);
            for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();
        }

        // ================================================================== skeleton
        Transform Bone(string name, Transform parent, Vector3 worldPos)
        {
            var t = new GameObject(P + name).transform;
            t.SetParent(parent, false);
            t.position = worldPos;
            boneIndex[name] = bones.Count;
            bones.Add(t);
            return t;
        }

        int B(string name) => boneIndex[name];

        // key heights (metres at 1.7 m, scaled)
        float Y(float y) => y * s;

        void Skeleton()
        {
            float fem = spec.femininity;
            float shoulderX = Mathf.Lerp(0.185f, 0.165f, fem) * s;
            float hipX = Mathf.Lerp(0.088f, 0.095f, fem) * s;
            var hips = Bone("Hips", root, new Vector3(0, Y(0.98f), 0));
            var spine = Bone("Spine", hips, new Vector3(0, Y(1.08f), 0));
            var spine1 = Bone("Spine1", spine, new Vector3(0, Y(1.2f), 0));
            var spine2 = Bone("Spine2", spine1, new Vector3(0, Y(1.32f), 0));
            var neck = Bone("Neck", spine2, new Vector3(0, Y(1.47f), 0));
            var head = Bone("Head", neck, new Vector3(0, Y(1.555f), 0.005f * s));
            Bone("HeadTop_End", head, new Vector3(0, Y(1.75f), 0.005f * s));
            foreach (int side in new[] { -1, 1 })
            {
                string sd = side < 0 ? "Left" : "Right";
                var sh = Bone(sd + "Shoulder", spine2, new Vector3(side * 0.045f * s, Y(1.405f), 0));
                var arm = Bone(sd + "Arm", sh, new Vector3(side * shoulderX, Y(1.405f), 0));
                var fore = Bone(sd + "ForeArm", arm, new Vector3(side * (shoulderX + 0.27f * s), Y(1.405f), 0));
                var hand = Bone(sd + "Hand", fore, new Vector3(side * (shoulderX + 0.52f * s), Y(1.405f), 0));
                Bone(sd + "HandMiddle1", hand, new Vector3(side * (shoulderX + 0.6f * s), Y(1.405f), 0));
                var up = Bone(sd + "UpLeg", hips, new Vector3(side * hipX, Y(0.93f), 0));
                var leg = Bone(sd + "Leg", up, new Vector3(side * hipX, Y(0.515f), 0.005f * s));
                var foot = Bone(sd + "Foot", leg, new Vector3(side * hipX, Y(0.085f), -0.01f * s));
                var toe = Bone(sd + "ToeBase", foot, new Vector3(side * hipX, Y(0.025f), 0.125f * s));
                Bone(sd + "Toe_End", toe, new Vector3(side * hipX, Y(0.025f), 0.2f * s));
            }
        }

        // ================================================================== geometry primitives
        static BoneWeight W(int a, float wa = 1f, int b = 0, float wb = 0f)
        {
            float sum = wa + wb;
            return new BoneWeight { boneIndex0 = a, weight0 = wa / sum, boneIndex1 = b, weight1 = wb / sum };
        }

        int AddV(Vector3 p, Vector3 n, Vector2 uv, BoneWeight w)
        {
            verts.Add(p); norms.Add(n); uvs.Add(uv); weights.Add(w);
            return verts.Count - 1;
        }

        void Tri(Mat m, int a, int b, int c) { var t = tris[(int)m]; t.Add(a); t.Add(b); t.Add(c); }

        /// <summary>One ring of a loft: centre, ellipse radii along two axes, weights and material for the band above it.</summary>
        struct Ring
        {
            public Vector3 c; public float ra, rb; public BoneWeight w; public Mat mat;
            public Ring(Vector3 c, float ra, float rb, BoneWeight w, Mat mat) { this.c = c; this.ra = ra; this.rb = rb; this.w = w; this.mat = mat; }
        }

        /// <summary>
        /// Lofts rings into a tube. axisA/axisB span each ring's ellipse (ra along A, rb along B); the band between ring i
        /// and i+1 uses ring i's material. Caps close the ends when asked.
        /// </summary>
        void Loft(IList<Ring> rings, Vector3 axisA, Vector3 axisB, int segs, bool capStart, bool capEnd)
        {
            int start = verts.Count;
            float vAcc = 0f;
            for (int r = 0; r < rings.Count; r++)
            {
                if (r > 0) vAcc += (rings[r].c - rings[r - 1].c).magnitude;
                var ring = rings[r];
                float circ = Mathf.PI * (ring.ra + ring.rb);
                for (int i = 0; i <= segs; i++)
                {
                    float a = i / (float)segs * Mathf.PI * 2f;
                    var d = axisA * (Mathf.Cos(a) * ring.ra) + axisB * (Mathf.Sin(a) * ring.rb);
                    var n = (axisA * (Mathf.Cos(a) / Mathf.Max(ring.ra, 1e-4f)) + axisB * (Mathf.Sin(a) / Mathf.Max(ring.rb, 1e-4f))).normalized;
                    AddV(ring.c + d, n, new Vector2(i / (float)segs * circ, vAcc), ring.w);
                }
            }
            // winding: rings advance along the loft axis (cross(axisA, axisB) direction)
            var along = rings.Count > 1 ? (rings[rings.Count - 1].c - rings[0].c).normalized : Vector3.up;
            bool flip = Vector3.Dot(Vector3.Cross(axisA, axisB), along) < 0f;
            for (int r = 0; r < rings.Count - 1; r++)
                for (int i = 0; i < segs; i++)
                {
                    int a = start + r * (segs + 1) + i, b = a + segs + 1;
                    // verified: frame (right, forward) lofted upwards needs (a, b, a+1) to face outwards
                    if (flip) { Tri(rings[r].mat, a, b, a + 1); Tri(rings[r].mat, a + 1, b, b + 1); }
                    else { Tri(rings[r].mat, a, a + 1, b); Tri(rings[r].mat, a + 1, b + 1, b); }
                }
            if (capStart) Cap(rings[0], axisA, axisB, segs, -along, flip);
            if (capEnd) Cap(rings[rings.Count - 1], axisA, axisB, segs, along, !flip);
        }

        void Cap(Ring ring, Vector3 axisA, Vector3 axisB, int segs, Vector3 normal, bool flip)
        {
            int c = AddV(ring.c + normal * Mathf.Min(ring.ra, ring.rb) * 0.35f, normal, Vector2.zero, ring.w);
            int first = verts.Count;
            for (int i = 0; i <= segs; i++)
            {
                float a = i / (float)segs * Mathf.PI * 2f;
                AddV(ring.c + axisA * (Mathf.Cos(a) * ring.ra) + axisB * (Mathf.Sin(a) * ring.rb), normal, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.05f, ring.w);
            }
            for (int i = 0; i < segs; i++)
            {
                if (flip) Tri(ring.mat, c, first + i, first + i + 1);
                else Tri(ring.mat, c, first + i + 1, first + i);
            }
        }

        /// <summary>Ellipsoid (optionally partial in latitude) rigidly weighted to one bone.</summary>
        int Ellipsoid(Vector3 c, Vector3 r, int rings, int segs, Mat m, BoneWeight w, float latFrom = 0f, float latTo = 1f, Quaternion? rot = null)
        {
            int start = verts.Count;
            var q = rot ?? Quaternion.identity;
            for (int i = 0; i <= rings; i++)
            {
                float v = Mathf.Lerp(latFrom, latTo, i / (float)rings);
                float phi = v * Mathf.PI;
                for (int j = 0; j <= segs; j++)
                {
                    float th = j / (float)segs * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    var p = Vector3.Scale(n, r);
                    var nn = new Vector3(n.x / r.x, n.y / r.y, n.z / r.z).normalized;
                    AddV(c + q * p, q * nn, new Vector2(j / (float)segs * 2f, v), w);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++)
                {
                    int a = start + i * (segs + 1) + j, b = a + segs + 1;
                    Tri(m, a, a + 1, b); Tri(m, a + 1, b + 1, b);
                }
            return start;
        }

        // ================================================================== body
        void Torso()
        {
            float fem = spec.femininity, bld = spec.build;
            int hips = B("Hips"), sp = B("Spine"), sp1 = B("Spine1"), sp2 = B("Spine2"), neck = B("Neck");
            Mat top = Mat.Top, bottom = Mat.Bottom;
            float wide = 1f + bld * 0.35f;
            // (height, half-width, half-depth, weights, material)
            var rings = new List<Ring>
            {
                new Ring(new Vector3(0, Y(0.86f), 0), Mathf.Lerp(0.15f, 0.17f, fem) * s * wide, 0.105f * s * wide, W(hips), bottom),
                new Ring(new Vector3(0, Y(0.93f), 0), Mathf.Lerp(0.155f, 0.175f, fem) * s * wide, 0.11f * s * wide, W(hips), bottom),
                new Ring(new Vector3(0, Y(1.0f), 0), Mathf.Lerp(0.15f, 0.16f, fem) * s * wide, 0.105f * s * wide, W(hips, 0.6f, sp, 0.4f), top),
                new Ring(new Vector3(0, Y(1.08f), 0.005f * s), Mathf.Lerp(0.14f, 0.13f, fem) * s * (1f + bld * 0.45f), (0.1f + bld * 0.04f) * s, W(sp), top),
                new Ring(new Vector3(0, Y(1.18f), 0.01f * s), Mathf.Lerp(0.15f, 0.135f, fem) * s * (1f + bld * 0.3f), (0.105f + fem * 0.012f + bld * 0.03f) * s, W(sp, 0.5f, sp1, 0.5f), top),
                new Ring(new Vector3(0, Y(1.28f), 0.012f * s), Mathf.Lerp(0.165f, 0.145f, fem) * s * (1f + bld * 0.2f), (0.112f + fem * 0.02f) * s, W(sp1, 0.5f, sp2, 0.5f), top),
                new Ring(new Vector3(0, Y(1.37f), 0.0f), Mathf.Lerp(0.175f, 0.15f, fem) * s * (1f + bld * 0.12f), 0.1f * s, W(sp2), top),
                new Ring(new Vector3(0, Y(1.43f), -0.005f * s), Mathf.Lerp(0.15f, 0.13f, fem) * s, 0.085f * s, W(sp2, 0.7f, neck, 0.3f), top),
                new Ring(new Vector3(0, Y(1.465f), -0.005f * s), 0.07f * s, 0.06f * s, W(neck, 0.6f, sp2, 0.4f), Mat.Skin),
                new Ring(new Vector3(0, Y(1.54f), 0.0f), 0.052f * s, 0.055f * s, W(neck, 0.6f, B("Head"), 0.4f), Mat.Skin),
                new Ring(new Vector3(0, Y(1.58f), 0.005f * s), 0.05f * s, 0.055f * s, W(B("Head")), Mat.Skin),
            };
            Loft(rings, Vector3.right, Vector3.forward, 20, true, false);
            if (spec.top == HumanSpec.Top.Hoodie)
            {
                // hood folded behind the neck, kangaroo pocket, drawstrings
                Ellipsoid(new Vector3(0, Y(1.47f), -0.075f * s), new Vector3(0.12f, 0.06f, 0.06f) * s, 6, 14, Mat.Top, W(sp2, 0.7f, neck, 0.3f));
                Ellipsoid(new Vector3(0, Y(1.06f), 0.1f * s + bld * 0.03f * s), new Vector3(0.1f, 0.05f, 0.025f) * s, 5, 12, Mat.Top, W(sp));
                foreach (float x in new[] { -0.035f, 0.035f })
                    Rod(new Vector3(x * s, Y(1.43f), 0.085f * s), new Vector3(x * s, Y(1.3f), 0.11f * s), 0.004f * s, Mat.TopAccent, W(sp2));
            }
            if (spec.top == HumanSpec.Top.Shirt || spec.top == HumanSpec.Top.Suit || spec.top == HumanSpec.Top.Jacket)
            {
                // collar
                foreach (int side in new[] { -1, 1 })
                    Ellipsoid(new Vector3(side * 0.04f * s, Y(1.455f), 0.04f * s), new Vector3(0.045f, 0.022f, 0.03f) * s, 4, 10, spec.top == HumanSpec.Top.Shirt ? Mat.Top : Mat.TopAccent, W(sp2, 0.6f, neck, 0.4f));
                if (spec.top == HumanSpec.Top.Suit)
                    Rod(new Vector3(0, Y(1.43f), 0.09f * s), new Vector3(0, Y(1.18f), 0.12f * s), 0.016f * s, Mat.TopAccent, W(sp2, 0.5f, sp1, 0.5f));
            }
            if (spec.bottom == HumanSpec.Bottom.Skirt)
            {
                var skirt = new List<Ring>
                {
                    new Ring(new Vector3(0, Y(0.95f), 0), 0.165f * s * wide, 0.115f * s * wide, W(hips), Mat.Bottom),
                    new Ring(new Vector3(0, Y(0.72f), 0), 0.2f * s * wide, 0.15f * s * wide, W(hips, 0.6f, B("LeftUpLeg"), 0.4f), Mat.Bottom),
                    new Ring(new Vector3(0, Y(0.56f), 0), 0.22f * s * wide, 0.17f * s * wide, W(hips, 0.4f, B("LeftUpLeg"), 0.6f), Mat.Bottom),
                };
                Loft(skirt, Vector3.right, Vector3.forward, 20, false, false);
            }
        }

        void Rod(Vector3 a, Vector3 b, float r, Mat m, BoneWeight w)
        {
            var ax = (b - a).normalized;
            var p1 = Vector3.Cross(ax, Vector3.right).sqrMagnitude > 0.01f ? Vector3.Cross(ax, Vector3.right).normalized : Vector3.Cross(ax, Vector3.up).normalized;
            var p2 = Vector3.Cross(ax, p1);
            Loft(new[] { new Ring(a, r, r, w, m), new Ring(b, r, r, w, m) }, p1, p2, 6, true, true);
        }

        void Arms()
        {
            float bld = spec.build, fem = spec.femininity;
            bool longSleeve = spec.top != HumanSpec.Top.TShirt;
            foreach (int side in new[] { -1, 1 })
            {
                string sd = side < 0 ? "Left" : "Right";
                int shb = B(sd + "Shoulder"), arm = B(sd + "Arm"), fore = B(sd + "ForeArm"), hand = B(sd + "Hand");
                var pArm = bones[arm].position; var pFore = bones[fore].position; var pHand = bones[hand].position;
                float k = (1f + bld * 0.3f) * Mathf.Lerp(1f, 0.85f, fem) * s;
                float sl = longSleeve ? 0.008f * s : 0f;   // sleeves are a touch thicker than skin
                Mat upperM = Mat.Top, lowerM = longSleeve ? Mat.Top : Mat.Skin;
                var ax = Vector3.right * side;
                var rings = new List<Ring>
                {
                    new Ring(pArm - ax * 0.05f * s + Vector3.up * 0.0f, 0.062f * k + sl, 0.058f * k + sl, W(arm, 0.5f, shb, 0.5f), upperM),
                    new Ring(pArm + ax * 0.03f * s, 0.058f * k + sl, 0.056f * k + sl, W(arm), upperM),
                    new Ring(Vector3.Lerp(pArm, pFore, 0.45f), 0.048f * k + sl, 0.05f * k + sl, W(arm), longSleeve ? upperM : Mat.TopAccent),
                    new Ring(Vector3.Lerp(pArm, pFore, 0.55f), (longSleeve ? 0.047f : 0.042f) * k + sl, (longSleeve ? 0.049f : 0.044f) * k + sl, W(arm), lowerM),
                    new Ring(pFore, 0.04f * k + sl, 0.042f * k + sl, W(arm, 0.5f, fore, 0.5f), lowerM),
                    new Ring(Vector3.Lerp(pFore, pHand, 0.35f), 0.039f * k + sl, 0.037f * k + sl, W(fore), lowerM),
                    new Ring(pHand - ax * 0.035f * s, 0.03f * k + sl, 0.026f * k + sl, W(fore, 0.7f, hand, 0.3f), longSleeve ? Mat.TopAccent : Mat.Skin),
                    new Ring(pHand - ax * 0.02f * s, 0.028f * k, 0.022f * k, W(fore, 0.4f, hand, 0.6f), Mat.Skin),
                    new Ring(pHand, 0.026f * k, 0.02f * k, W(hand), Mat.Skin),
                };
                // ring axes: vertical (y) and front-back (z); arm runs along +-x
                Loft(rings, Vector3.up, Vector3.forward, 14, true, false);
                Hand(side, pHand, hand, k);
            }
        }

        void Hand(int side, Vector3 wrist, int hand, float k)
        {
            var ax = Vector3.right * side;
            var w = W(hand);
            // palm
            Loft(new[]
            {
                new Ring(wrist, 0.026f * k, 0.02f * k, w, Mat.Skin),
                new Ring(wrist + ax * 0.04f * s, 0.017f * k, 0.042f * k, w, Mat.Skin),
                new Ring(wrist + ax * 0.085f * s, 0.014f * k, 0.042f * k, w, Mat.Skin),
            }, Vector3.up, Vector3.forward, 12, false, true);
            // four fingers, slightly curled and spread
            for (int f = 0; f < 4; f++)
            {
                float z = (0.028f - f * 0.019f) * k;
                float len = (f == 1 ? 0.085f : f == 0 || f == 2 ? 0.08f : 0.065f) * s;
                var a = wrist + ax * 0.085f * s + new Vector3(0, 0, z);
                var b = a + ax * len * 0.55f + Vector3.down * 0.008f * s;
                var c = b + ax * len * 0.45f + Vector3.down * 0.014f * s;
                float r = 0.0085f * k;
                Loft(new[] { new Ring(a, r, r, w, Mat.Skin), new Ring(b, r * 0.92f, r * 0.92f, w, Mat.Skin), new Ring(c, r * 0.8f, r * 0.8f, w, Mat.Skin) }, Vector3.up, Vector3.forward, 7, false, true);
            }
            // thumb, angled forward
            var t0 = wrist + ax * 0.025f * s + new Vector3(0, -0.005f * s, 0.035f * k);
            var t1 = t0 + (ax * 0.035f + Vector3.forward * 0.03f) * s;
            var t2 = t1 + (ax * 0.03f + Vector3.forward * 0.012f) * s;
            Loft(new[] { new Ring(t0, 0.013f * k, 0.013f * k, w, Mat.Skin), new Ring(t1, 0.011f * k, 0.011f * k, w, Mat.Skin), new Ring(t2, 0.0095f * k, 0.0095f * k, w, Mat.Skin) }, Vector3.up, Vector3.forward, 7, false, true);
        }

        void Legs()
        {
            float bld = spec.build, fem = spec.femininity;
            foreach (int side in new[] { -1, 1 })
            {
                string sd = side < 0 ? "Left" : "Right";
                int up = B(sd + "UpLeg"), leg = B(sd + "Leg"), foot = B(sd + "Foot"), hips = B("Hips");
                var pUp = bones[up].position; var pLeg = bones[leg].position; var pFoot = bones[foot].position;
                float k = (1f + bld * 0.35f) * Mathf.Lerp(1f, 1.05f, fem) * s;
                bool shorts = spec.bottom == HumanSpec.Bottom.Shorts || spec.bottom == HumanSpec.Bottom.Skirt;
                float cloth = shorts ? 0f : 0.008f * s;
                Mat upperM = spec.bottom == HumanSpec.Bottom.Skirt ? Mat.Skin : Mat.Bottom;
                Mat lowerM = shorts ? Mat.Skin : Mat.Bottom;
                var rings = new List<Ring>
                {
                    new Ring(pUp + Vector3.up * 0.04f * s, 0.085f * k + cloth, 0.09f * k + cloth, W(hips, 0.6f, up, 0.4f), upperM),
                    new Ring(Vector3.Lerp(pUp, pLeg, 0.2f), 0.08f * k + cloth, 0.082f * k + cloth, W(up), upperM),
                    new Ring(Vector3.Lerp(pUp, pLeg, 0.62f), 0.062f * k + cloth, 0.064f * k + cloth, W(up), shorts ? Mat.Skin : upperM),
                    new Ring(pLeg + Vector3.up * 0.03f * s, 0.05f * k + cloth, 0.052f * k + cloth, W(up, 0.5f, leg, 0.5f), lowerM),
                    new Ring(Vector3.Lerp(pLeg, pFoot, 0.25f), 0.05f * k + cloth, 0.054f * k + cloth, W(leg), lowerM),
                    new Ring(Vector3.Lerp(pLeg, pFoot, 0.75f), 0.036f * k + cloth, 0.038f * k + cloth, W(leg), lowerM),
                    new Ring(pFoot + Vector3.up * 0.03f * s, 0.036f * k + cloth, 0.04f * k + cloth, W(leg, 0.5f, foot, 0.5f), Mat.Shoe),
                };
                Loft(rings, Vector3.right, Vector3.forward, 16, false, false);
                Shoe(side, pFoot, foot, B(sd + "ToeBase"));
            }
        }

        void Shoe(int side, Vector3 ankle, int foot, int toe)
        {
            float x = ankle.x;
            // upper: rings from heel to toe (running along +z), sole underneath
            var rings = new List<Ring>
            {
                new Ring(new Vector3(x, Y(0.055f), -0.06f * s), 0.042f * s, 0.035f * s, W(foot), Mat.Shoe),
                new Ring(new Vector3(x, Y(0.065f), -0.03f * s), 0.046f * s, 0.055f * s, W(foot), Mat.Shoe),
                new Ring(new Vector3(x, Y(0.055f), 0.05f * s), 0.048f * s, 0.045f * s, W(foot, 0.6f, toe, 0.4f), Mat.Shoe),
                new Ring(new Vector3(x, Y(0.042f), 0.12f * s), 0.046f * s, 0.032f * s, W(toe), Mat.Shoe),
                new Ring(new Vector3(x, Y(0.035f), 0.175f * s), 0.03f * s, 0.022f * s, W(toe), Mat.Shoe),
            };
            Loft(rings, Vector3.right, Vector3.up, 14, true, true);
            // sole
            var sole = new List<Ring>
            {
                new Ring(new Vector3(x, Y(0.012f), -0.075f * s), 0.04f * s, 0.012f * s, W(foot), Mat.Sole),
                new Ring(new Vector3(x, Y(0.012f), 0.06f * s), 0.05f * s, 0.012f * s, W(foot, 0.6f, toe, 0.4f), Mat.Sole),
                new Ring(new Vector3(x, Y(0.012f), 0.185f * s), 0.035f * s, 0.012f * s, W(toe), Mat.Sole),
            };
            Loft(sole, Vector3.right, Vector3.up, 14, true, true);
            // a stripe on the outer side
            Ellipsoid(new Vector3(x + side * 0.046f * s, Y(0.05f), 0.02f * s), new Vector3(0.004f, 0.012f, 0.05f) * s, 3, 8, Mat.Sole, W(foot));
        }

        // ================================================================== head & face
        void Head()
        {
            int head = B("Head");
            var w = W(head);
            headR = 0.1f * fs;
            headC = new Vector3(0, Y(1.655f) + 0.1f * (fs - s), 0.012f * fs);
            // skull + face: a sphere deformed into a head (narrower jaw, chin, cheekbones, flatter sides, nose)
            int rings = 22, segs = 28;
            int start = verts.Count;
            for (int i = 0; i <= rings; i++)
            {
                float v = i / (float)rings;
                float phi = v * Mathf.PI;
                for (int j = 0; j <= segs; j++)
                {
                    float th = j / (float)segs * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    var p = new Vector3(n.x * 0.078f, n.y * 0.112f, n.z * 0.094f);
                    float front = Mathf.Max(0f, n.z);
                    float lower = Mathf.Clamp01(-n.y);
                    // jaw and chin narrow towards the bottom, the back of the skull is rounder
                    p.x *= 1f - lower * lower * 0.35f;
                    p.z *= 1f - lower * 0.12f + (n.z < 0 ? 0.04f : 0f);
                    p.y -= lower * lower * front * 0.012f;                // chin juts down
                    p.z += lower * lower * front * front * 0.012f;        // chin forward
                    // brow ridge and cheekbones
                    float browBand = Mathf.Exp(-Mathf.Pow((n.y - 0.32f) / 0.08f, 2f)) * front;
                    p.z += browBand * 0.006f;
                    float cheek = Mathf.Exp(-Mathf.Pow((n.y + 0.05f) / 0.12f, 2f)) * Mathf.Exp(-Mathf.Pow((Mathf.Abs(n.x) - 0.6f) / 0.2f, 2f)) * front;
                    p.x += Mathf.Sign(n.x) * cheek * 0.005f;
                    // eye sockets
                    float eye = Mathf.Exp(-(Mathf.Pow((Mathf.Abs(n.x) - 0.36f) / 0.12f, 2f) + Mathf.Pow((n.y - 0.16f) / 0.09f, 2f))) * front;
                    p.z -= eye * 0.008f;
                    // nose: a ridge down the centre of the face
                    float nx = n.x / 0.12f;
                    float nose = Mathf.Exp(-nx * nx) * Mathf.Clamp01((0.22f - n.y) / 0.25f) * Mathf.Clamp01((n.y + 0.12f) / 0.1f) * Mathf.Pow(front, 6f);
                    p.z += nose * 0.026f;
                    // mouth groove
                    float mouth = Mathf.Exp(-(Mathf.Pow(n.x / 0.22f, 2f) + Mathf.Pow((n.y + 0.27f) / 0.035f, 2f))) * front;
                    p.z -= mouth * 0.004f;
                    p *= s;
                    AddV(headC + p, n, new Vector2(j / (float)segs, v), w);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++)
                {
                    int a = start + i * (segs + 1) + j, b = a + segs + 1;
                    Tri(Mat.Skin, a, a + 1, b); Tri(Mat.Skin, a + 1, b + 1, b);
                }
            RecalcRegionNormals(start, verts.Count, rings, segs);

            // ears
            foreach (int side in new[] { -1, 1 })
                Ellipsoid(headC + new Vector3(side * 0.079f * fs, 0.005f * fs, -0.008f * fs), new Vector3(0.012f, 0.03f, 0.022f) * fs, 6, 10, Mat.Skin, w);

            // eyes: white, iris, pupil; then lids (blink blend shape) and brows
            foreach (int side in new[] { -1, 1 })
            {
                var ec = headC + new Vector3(side * 0.031f * fs, 0.017f * fs, 0.072f * fs);
                if (side < 0) eyeL = ec; else eyeR = ec;
                float er = 0.0125f * fs;
                Ellipsoid(ec, Vector3.one * er, 8, 12, Mat.EyeWhite, w);
                var irisRot = Quaternion.Euler(90, 0, 0);   // a cap facing +z
                Ellipsoid(ec + Vector3.forward * er * 0.25f, new Vector3(er * 0.62f, er * 0.82f, er * 0.62f), 3, 12, Mat.Iris, w, 0f, 0.28f, irisRot);
                Ellipsoid(ec + Vector3.forward * er * 0.32f, new Vector3(er * 0.3f, er * 0.82f, er * 0.3f), 2, 10, Mat.Pupil, w, 0f, 0.2f, irisRot);
                // upper lid: a skin shell over the top of the eye, rotated down by the "blink" blend shape
                int lid = verts.Count;
                Ellipsoid(ec, Vector3.one * er * 1.12f, 5, 12, Mat.Skin, w, 0.05f, 0.42f, Quaternion.Euler(-8, 0, 0));
                for (int i = lid; i < verts.Count; i++) (side < 0 ? lidL : lidR).Add(i);
                // lower lid
                Ellipsoid(ec, Vector3.one * er * 1.1f, 3, 12, Mat.Skin, w, 0.72f, 0.95f, Quaternion.Euler(8, 0, 0));
                // brow
                var bc = ec + new Vector3(side * 0.003f * fs, 0.019f * fs, 0.008f * fs);
                if (side < 0) browCL = bc; else browCR = bc;
                Ellipsoid(bc, new Vector3(0.017f, 0.0035f, 0.006f) * fs, 3, 10, Mat.Hair, w, 0f, 1f, Quaternion.Euler(0, 0, side * -8f));
            }
            // lips
            mouthC = headC + new Vector3(0, -0.04f * fs, 0.087f * fs);
            Ellipsoid(mouthC + new Vector3(0, 0.0045f * fs, 0), new Vector3(0.021f, 0.0045f, 0.007f) * fs, 4, 14, Mat.Lips, w);
            Ellipsoid(mouthC + new Vector3(0, -0.0045f * fs, -0.001f * fs), new Vector3(0.019f, 0.0055f, 0.007f) * fs, 4, 14, Mat.Lips, w);
            if (spec.glasses) Glasses(w);
        }

        /// <summary>Smooth normals for the head grid (the sphere normals no longer match after deformation).</summary>
        void RecalcRegionNormals(int start, int end, int rings, int segs)
        {
            for (int i = 0; i <= rings; i++)
                for (int j = 0; j <= segs; j++)
                {
                    int idx = start + i * (segs + 1) + j;
                    int jp = start + i * (segs + 1) + (j + 1) % segs, jm = start + i * (segs + 1) + (j - 1 + segs) % segs;
                    int ip = start + Mathf.Min(i + 1, rings) * (segs + 1) + j, im = start + Mathf.Max(i - 1, 0) * (segs + 1) + j;
                    var n = Vector3.Cross(verts[jp] - verts[jm], verts[im] - verts[ip]);
                    if (n.sqrMagnitude > 1e-12f && i > 0 && i < rings) norms[idx] = -n.normalized;
                }
        }

        void Glasses(BoneWeight w)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var c = (side < 0 ? eyeL : eyeR) + Vector3.forward * 0.016f * fs;
                Loft(new[]
                {
                    new Ring(c - Vector3.forward * 0.001f * fs, 0.017f * fs, 0.013f * fs, w, Mat.Metal),
                    new Ring(c + Vector3.forward * 0.001f * fs, 0.017f * fs, 0.013f * fs, w, Mat.Metal),
                }, Vector3.right, Vector3.up, 16, false, false);
                Ellipsoid(c, new Vector3(0.016f, 0.012f, 0.002f) * fs, 3, 12, Mat.Glass, w);
                Rod(c + new Vector3(side * 0.017f * fs, 0, 0), c + new Vector3(side * 0.047f * fs, 0.002f * fs, -0.07f * fs), 0.0015f * fs, Mat.Metal, w);
            }
            Rod((eyeL + eyeR) / 2 + new Vector3(-0.012f * fs, 0.002f * fs, 0.017f * fs), (eyeL + eyeR) / 2 + new Vector3(0.012f * fs, 0.002f * fs, 0.017f * fs), 0.0015f * fs, Mat.Metal, w);
        }

        void Hair()
        {
            var w = W(B("Head"));
            var c = headC;
            var rnd = new System.Random(spec.id.GetHashCode());
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            switch (spec.hair)
            {
                case HumanSpec.HairStyle.Bald: return;
                case HumanSpec.HairStyle.Cap:
                    Ellipsoid(c + new Vector3(0, 0.03f * fs, -0.004f * fs), new Vector3(0.085f, 0.085f, 0.1f) * fs, 8, 18, Mat.TopAccent, w, 0f, 0.5f);
                    Ellipsoid(c + new Vector3(0, 0.035f * fs, 0.1f * fs), new Vector3(0.07f, 0.006f, 0.05f) * fs, 3, 14, Mat.TopAccent, w);
                    return;
            }
            float thick = spec.hair == HumanSpec.HairStyle.Buzz ? 0.003f : spec.hair == HumanSpec.HairStyle.Messy ? 0.016f : 0.011f;
            // hair cap: covers the top/back of the skull, hairline above the forehead and behind the ears
            int start = verts.Count;
            int rings = 12, segs = 26;
            for (int i = 0; i <= rings; i++)
            {
                float v = i / (float)rings * 0.62f;
                float phi = v * Mathf.PI;
                for (int j = 0; j <= segs; j++)
                {
                    float th = j / (float)segs * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    // lower the cap at the back, raise it at the forehead
                    float hairline = n.z > 0 ? 0.5f : 0.68f;
                    float lat = v / 0.62f;
                    var p = new Vector3(n.x * 0.081f, n.y * 0.115f, n.z * 0.097f);
                    if (v > hairline * 0.62f / 0.62f * 0.62f) p *= 1f;   // keep shape
                    float messy = spec.hair == HumanSpec.HairStyle.Messy ? (Mathf.PerlinNoise(j * 0.7f, i * 0.9f) - 0.4f) * 0.012f : 0f;
                    float vol = thick + messy;
                    p += new Vector3(n.x, Mathf.Max(n.y, 0f), n.z) * vol;
                    if (n.z > 0.3f && n.y < 0.45f) p.z -= 0.004f;     // fringe sits close to the forehead
                    AddV(c + p * fs, n, new Vector2(j / (float)segs * 2f, lat), w);
                }
            }
            for (int i = 0; i < rings; i++)
                for (int j = 0; j < segs; j++)
                {
                    int a = start + i * (segs + 1) + j, b = a + segs + 1;
                    // skip the face opening: front quads below the hairline
                    var mid = (verts[a] + verts[b + 1]) * 0.5f - c;
                    if (mid.z > 0.035f * fs && mid.y < 0.06f * fs) continue;
                    Tri(Mat.Hair, a, a + 1, b); Tri(Mat.Hair, a + 1, b + 1, b);
                }
            if (spec.hair == HumanSpec.HairStyle.Messy)
                for (int i = 0; i < 16; i++)
                {
                    float th = R(0f, Mathf.PI * 2f), up = R(0.35f, 0.95f);
                    var dir = new Vector3(Mathf.Cos(th) * Mathf.Sqrt(1 - up * up), up, Mathf.Sin(th) * Mathf.Sqrt(1 - up * up));
                    var b0 = c + Vector3.Scale(dir, new Vector3(0.085f, 0.12f, 0.1f)) * fs;
                    var tip = b0 + (dir + new Vector3(R(-0.3f, 0.3f), 0.2f, R(-0.3f, 0.3f))).normalized * R(0.02f, 0.035f) * fs;
                    Loft(new[] { new Ring(b0, 0.014f * fs, 0.01f * fs, w, Mat.Hair), new Ring(tip, 0.002f * fs, 0.002f * fs, w, Mat.Hair) }, Vector3.right, Vector3.forward, 6, false, true);
                }
            if (spec.hair == HumanSpec.HairStyle.Long)
            {
                int neck = B("Neck"), sp2 = B("Spine2");
                var back = new List<Ring>
                {
                    new Ring(c + new Vector3(0, 0.01f, -0.04f) * fs, 0.085f * fs, 0.06f * fs, w, Mat.Hair),
                    new Ring(c + new Vector3(0, -0.09f, -0.06f) * fs, 0.09f * fs, 0.05f * fs, W(B("Head"), 0.6f, neck, 0.4f), Mat.Hair),
                    new Ring(c + new Vector3(0, -0.2f, -0.075f) * fs, 0.1f * fs, 0.035f * fs, W(neck, 0.5f, sp2, 0.5f), Mat.Hair),
                    new Ring(c + new Vector3(0, -0.28f, -0.085f) * fs, 0.09f * fs, 0.02f * fs, W(sp2), Mat.Hair),
                };
                Loft(back, Vector3.right, Vector3.forward, 16, false, true);
            }
            if (spec.hair == HumanSpec.HairStyle.Bun)
                Ellipsoid(c + new Vector3(0, 0.07f, -0.085f) * fs, new Vector3(0.04f, 0.038f, 0.038f) * fs, 8, 12, Mat.Hair, w);
        }

        // ================================================================== accessories
        void Backpack()
        {
            int sp2 = B("Spine2"), sp1 = B("Spine1");
            var w = W(sp2, 0.7f, sp1, 0.3f);
            var c = new Vector3(0, Y(1.22f), -0.17f * s - spec.build * 0.03f * s);
            // body as a lofted rounded block (rings along y)
            Loft(new[]
            {
                new Ring(c + Vector3.down * 0.19f * s, 0.13f * s, 0.06f * s, w, Mat.Pack),
                new Ring(c + Vector3.down * 0.15f * s, 0.15f * s, 0.075f * s, w, Mat.Pack),
                new Ring(c + Vector3.up * 0.12f * s, 0.15f * s, 0.075f * s, w, Mat.Pack),
                new Ring(c + Vector3.up * 0.18f * s, 0.12f * s, 0.055f * s, w, Mat.Pack),
            }, Vector3.right, Vector3.forward, 16, true, true);
            // front pocket
            Ellipsoid(c + new Vector3(0, -0.07f * s, -0.075f * s), new Vector3(0.1f, 0.07f, 0.03f) * s, 5, 12, Mat.Pack, w);
            // straps over the shoulders
            foreach (int side in new[] { -1, 1 })
            {
                var a = new Vector3(side * 0.08f * s, Y(1.4f), -0.1f * s);
                var top = new Vector3(side * 0.09f * s, Y(1.44f), 0.0f);
                var f = new Vector3(side * 0.085f * s, Y(1.3f), 0.115f * s);
                var low = new Vector3(side * 0.1f * s, Y(1.1f), 0.04f * s);
                Rod(a, top, 0.012f * s, Mat.Metal, W(sp2));
                Rod(top, f, 0.012f * s, Mat.Metal, W(sp2));
                Rod(f, low, 0.011f * s, Mat.Metal, W(sp2, 0.6f, sp1, 0.4f));
            }
        }

        void Headphones()
        {
            var w = W(B("Head"));
            // band arcing over the head from ear to ear, cups on the ears
            var pts = new List<Ring>();
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.Lerp(-1.25f, 1.25f, i / 12f);
                var p = headC + new Vector3(Mathf.Sin(a) * 0.098f, Mathf.Cos(a) * 0.125f, -0.012f) * fs;
                pts.Add(new Ring(p, 0.009f * fs, 0.004f * fs, w, Mat.Pack));
            }
            Loft(pts, Vector3.forward, Vector3.up, 8, true, true);
            foreach (int side in new[] { -1, 1 })
            {
                var c = headC + new Vector3(side * 0.092f * fs, 0.0f, -0.008f * fs);
                Loft(new[]
                {
                    new Ring(c - Vector3.right * side * 0.008f * fs, 0.034f * fs, 0.03f * fs, w, Mat.Metal),
                    new Ring(c + Vector3.right * side * 0.012f * fs, 0.035f * fs, 0.031f * fs, w, Mat.Pack),
                    new Ring(c + Vector3.right * side * 0.02f * fs, 0.028f * fs, 0.025f * fs, w, Mat.Pack),
                }, Vector3.up, Vector3.forward, 14, true, true);
            }
        }

        // ================================================================== expressions
        static float Falloff(Vector3 p, Vector3 c, float r) { float d = (p - c).magnitude / r; return d >= 1f ? 0f : (1f - d * d) * (1f - d * d); }

        void BlendShapes(Mesh mesh)
        {
            int n = verts.Count;
            var d = new Vector3[n];
            float fr = 0.03f * s;
            var cornerL = mouthC + new Vector3(-0.02f * s, 0, -0.004f * s);
            var cornerR = mouthC + new Vector3(0.02f * s, 0, -0.004f * s);
            void Shape(string name, System.Func<Vector3, int, Vector3> f)
            {
                for (int i = 0; i < n; i++) d[i] = f(verts[i], i);
                mesh.AddBlendShapeFrame(name, 100f, d, null, null);
            }
            Shape("smile", (p, i) => (Falloff(p, cornerL, fr) * new Vector3(-0.35f, 1f, -0.2f) + Falloff(p, cornerR, fr) * new Vector3(0.35f, 1f, -0.2f)) * 0.008f * s
                                     + Falloff(p, mouthC + new Vector3(0, 0.03f * s, 0.0f), 0.045f * s) * new Vector3(0, 0.003f, 0.002f) * s);
            Shape("frown", (p, i) => (Falloff(p, cornerL, fr) + Falloff(p, cornerR, fr)) * new Vector3(0, -0.007f, -0.001f) * s);
            Shape("open", (p, i) =>
            {
                float below = Mathf.Clamp01((mouthC.y - p.y) / (0.004f * s) + 0.5f);
                return Falloff(p, mouthC + new Vector3(0, -0.03f * s, 0), 0.06f * s) * below * new Vector3(0, -0.016f, -0.003f) * s;
            });
            Shape("blink", (p, i) =>
            {
                bool l = lidL.Contains(i), r = !l && lidR.Contains(i);
                if (!l && !r) return Vector3.zero;
                var ec = l ? eyeL : eyeR;
                var rel = p - ec;
                return Quaternion.Euler(62f, 0, 0) * rel - rel;
            });
            Shape("browsUp", (p, i) => (Falloff(p, browCL, 0.028f * s) + Falloff(p, browCR, 0.028f * s)) * new Vector3(0, 0.006f, 0) * s);
            Shape("browsDown", (p, i) =>
                Falloff(p, browCL + new Vector3(0.008f * s, 0, 0), 0.022f * s) * new Vector3(0.002f, -0.005f, 0.001f) * s +
                Falloff(p, browCR - new Vector3(0.008f * s, 0, 0), 0.022f * s) * new Vector3(-0.002f, -0.005f, 0.001f) * s);
        }

        // ================================================================== robots / mechs
        /// <summary>Rigid box weighted fully to one bone (hard-surface armour plate).</summary>
        void RBox(Vector3 c, Vector3 size, Mat m, int bone, Quaternion? rot = null)
        {
            var q = rot ?? Quaternion.identity;
            var w = W(bone);
            var h = size * 0.5f;
            Vector3[] faces = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
            foreach (var face in faces)
            {
                var a = Vector3.Cross(face, Mathf.Abs(face.y) > 0.5f ? Vector3.forward : Vector3.up);
                var b = Vector3.Cross(face, a);
                var fc = Vector3.Scale(face, h);
                var ea = Vector3.Scale(a, h); var eb = Vector3.Scale(b, h);
                int i0 = AddV(c + q * (fc - ea - eb), q * face, new Vector2(0, 0), w);
                int i1 = AddV(c + q * (fc + ea - eb), q * face, new Vector2(1, 0), w);
                int i2 = AddV(c + q * (fc + ea + eb), q * face, new Vector2(1, 1), w);
                int i3 = AddV(c + q * (fc - ea + eb), q * face, new Vector2(0, 1), w);
                // Unity: clockwise (seen from outside) faces the viewer; pick the order that matches the face normal
                var nrm = Vector3.Cross(verts[i3] - verts[i0], verts[i1] - verts[i0]);
                if (Vector3.Dot(nrm, q * face) > 0) { Tri(m, i0, i3, i2); Tri(m, i0, i2, i1); }
                else { Tri(m, i0, i1, i2); Tri(m, i0, i2, i3); }
            }
        }

        void RobotBody()
        {
            float k = s * (1f + spec.bulk * 0.6f);   // plates get chunkier with bulk
            int hips = B("Hips"), sp = B("Spine"), sp1 = B("Spine1"), sp2 = B("Spine2"), neck = B("Neck"), head = B("Head");
            Vector3 P0(string b) => bones[B(b)].position;
            // pelvis and abdomen
            RBox(P0("Hips") + Vector3.down * 0.03f * s, new Vector3(0.3f, 0.14f, 0.2f) * k, Mat.Bottom, hips);
            Loft(new[]
            {
                new Ring(P0("Hips") + Vector3.up * 0.05f * s, 0.1f * k, 0.08f * k, W(sp), Mat.Skin),
                new Ring(P0("Spine1"), 0.11f * k, 0.085f * k, W(sp), Mat.Skin),
            }, Vector3.right, Vector3.forward, 10, false, false);
            // chest: armoured, wider at the shoulders, with a glowing core and a back unit with vents
            RBox(P0("Spine1") + new Vector3(0, 0.02f, 0) * s, new Vector3(0.3f, 0.16f, 0.2f) * k, Mat.Top, sp1);
            RBox(P0("Spine2") + new Vector3(0, 0.03f, 0.005f) * s, new Vector3(0.4f, 0.2f, 0.24f) * k, Mat.Top, sp2);
            RBox(P0("Spine2") + new Vector3(0, 0.02f, 0.125f) * k, new Vector3(0.12f, 0.07f, 0.01f) * k, Mat.TopAccent, sp2);
            RBox(P0("Spine2") + new Vector3(0, 0.04f, -0.13f) * k, new Vector3(0.26f, 0.22f, 0.06f) * k, Mat.Bottom, sp2);
            for (int i = 0; i < 3; i++) RBox(P0("Spine2") + new Vector3(-0.08f + i * 0.08f, 0.04f, -0.165f) * k, new Vector3(0.05f, 0.16f, 0.01f) * k, Mat.TopAccent, sp2);
            // neck and head
            Loft(new[] { new Ring(P0("Neck"), 0.045f * k, 0.045f * k, W(neck), Mat.Skin), new Ring(P0("Head"), 0.04f * k, 0.04f * k, W(neck), Mat.Skin) }, Vector3.right, Vector3.forward, 10, false, false);
            float hk = s * (1f + spec.bulk * 0.35f);
            var hc = P0("Head") + new Vector3(0, 0.1f, 0.01f) * hk;
            headC = hc; mouthC = hc; eyeL = eyeR = hc; browCL = browCR = hc;
            RBox(hc, new Vector3(0.2f, 0.2f, 0.22f) * hk, Mat.Top, head);
            if (spec.visor) RBox(hc + new Vector3(0, 0.02f, 0.111f) * hk, new Vector3(0.17f, 0.05f, 0.01f) * hk, Mat.TopAccent, head);
            else foreach (int side in new[] { -1, 1 }) RBox(hc + new Vector3(side * 0.045f, 0.02f, 0.111f) * hk, new Vector3(0.04f, 0.025f, 0.01f) * hk, Mat.TopAccent, head);
            RBox(hc + new Vector3(0, -0.06f, 0.1f) * hk, new Vector3(0.12f, 0.04f, 0.04f) * hk, Mat.Bottom, head);
            foreach (int side in new[] { -1, 1 }) RBox(hc + new Vector3(side * 0.108f, 0f, 0f) * hk, new Vector3(0.02f, 0.08f, 0.08f) * hk, Mat.Bottom, head);
            if (spec.antenna) Rod(hc + new Vector3(0.06f, 0.1f, -0.04f) * hk, hc + new Vector3(0.07f, 0.32f, -0.06f) * hk, 0.006f * hk, Mat.Bottom, W(head));
            if (spec.peakedCap)
            {
                RBox(hc + new Vector3(0, 0.12f, -0.005f) * hk, new Vector3(0.23f, 0.06f, 0.25f) * hk, Mat.Bottom, head);
                RBox(hc + new Vector3(0, 0.1f, 0.15f) * hk, new Vector3(0.2f, 0.015f, 0.08f) * hk, Mat.Bottom, head, Quaternion.Euler(-12, 0, 0));
                RBox(hc + new Vector3(0, 0.125f, 0.12f) * hk, new Vector3(0.05f, 0.03f, 0.01f) * hk, Mat.TopAccent, head);
            }
            // arms: shoulder pads, upper/lower arm plates, elbow joints, blocky hands
            foreach (int side in new[] { -1, 1 })
            {
                string sd = side < 0 ? "Left" : "Right";
                int arm = B(sd + "Arm"), fore = B(sd + "ForeArm"), hand = B(sd + "Hand");
                var pA = bones[arm].position; var pF = bones[fore].position; var pH = bones[hand].position;
                var ax = Vector3.right * side;
                RBox(pA + new Vector3(0, 0.03f, 0) * s, new Vector3(0.16f, 0.12f, 0.17f) * k, Mat.Top, arm);
                RBox((pA + pF) * 0.5f, new Vector3((pF - pA).magnitude * 0.8f, 0.09f * k, 0.09f * k), Mat.Bottom, arm);
                Ellipsoid(pF, Vector3.one * 0.05f * k, 6, 10, Mat.Skin, W(fore));
                RBox((pF + pH) * 0.5f, new Vector3((pH - pF).magnitude * 0.85f, 0.1f * k, 0.1f * k), Mat.Top, fore);
                RBox(pH + ax * 0.06f * s, new Vector3(0.12f * s, 0.05f * k, 0.09f * k), Mat.Bottom, hand);
                RBox(pH + ax * 0.04f * s + new Vector3(0, -0.01f, 0.06f) * s, new Vector3(0.05f, 0.03f, 0.03f) * k, Mat.Bottom, hand);
            }
            // legs: thigh/shin armour, knee joints, heavy boots
            foreach (int side in new[] { -1, 1 })
            {
                string sd = side < 0 ? "Left" : "Right";
                int up = B(sd + "UpLeg"), leg = B(sd + "Leg"), foot = B(sd + "Foot"), toe = B(sd + "ToeBase");
                var pU = bones[up].position; var pL = bones[leg].position; var pF = bones[foot].position;
                RBox((pU + pL) * 0.5f, new Vector3(0.13f * k, (pU - pL).magnitude * 0.85f, 0.14f * k), Mat.Top, up);
                Ellipsoid(pL, Vector3.one * 0.06f * k, 6, 10, Mat.Skin, W(leg));
                RBox((pL + pF) * 0.5f + Vector3.forward * 0.01f * s, new Vector3(0.11f * k, (pL - pF).magnitude * 0.85f, 0.12f * k), Mat.Bottom, leg);
                RBox(new Vector3(pF.x, 0.05f * s, 0.03f * s), new Vector3(0.13f * k, 0.1f * s, 0.26f * s), Mat.Top, foot);
                RBox(new Vector3(pF.x, 0.035f * s, 0.17f * s), new Vector3(0.12f * k, 0.07f * s, 0.07f * s), Mat.Bottom, toe);
            }
        }

        // ================================================================== assembly
        public struct Result { public GameObject root; public Mesh mesh; public Avatar avatar; public SkinnedMeshRenderer renderer; }

        public Result Build(Transform parent, Material[] mats)
        {
            // built at the origin (bone positions are absolute), parented at the end
            root = new GameObject(spec.id).transform;
            var model = new GameObject("Armature").transform;
            model.SetParent(root, false);
            var saveParent = root;
            root = model;
            Skeleton();
            root = saveParent;

            if (spec.robot) RobotBody();
            else
            {
                Torso();
                Arms();
                Legs();
                Head();
                Hair();
                if (spec.backpack) Backpack();
                if (spec.headphones) Headphones();
            }

            var mesh = new Mesh { name = spec.id + "_Body" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            int used = (int)Mat.Count;
            mesh.subMeshCount = used;
            for (int i = 0; i < used; i++) mesh.SetTriangles(tris[i], i, false);
            mesh.boneWeights = weights.ToArray();
            var bind = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++) bind[i] = bones[i].worldToLocalMatrix * root.localToWorldMatrix;
            mesh.bindposes = bind;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (!spec.robot) BlendShapes(mesh);

            var smrGo = new GameObject("Body");
            smrGo.transform.SetParent(root, false);
            var smr = smrGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = bones.ToArray();
            smr.rootBone = bones[B("Hips")];
            var use = new Material[used];
            for (int i = 0; i < used; i++) use[i] = mats[Mathf.Min(i, mats.Length - 1)];
            smr.sharedMaterials = use;
            smr.updateWhenOffscreen = false;
            // localBounds are measured from the root bone (the hips), not from the mesh origin at the feet;
            // CharacterVisual scales and grounds characters by these bounds
            var hipsPos = bones[B("Hips")].position - root.position;
            smr.localBounds = new Bounds(mesh.bounds.center - hipsPos, mesh.bounds.size);

            var avatar = BuildAvatar();
            var anim = root.gameObject.AddComponent<Animator>();
            anim.avatar = avatar;
            anim.applyRootMotion = false;
            if (parent != null) root.SetParent(parent, false);
            return new Result { root = root.gameObject, mesh = mesh, avatar = avatar, renderer = smr };
        }

        Avatar BuildAvatar()
        {
            var human = new List<HumanBone>();
            void Map(HumanBodyBones hb, string bone)
            {
                if (!boneIndex.ContainsKey(bone)) return;
                human.Add(new HumanBone { humanName = HumanTrait.BoneName[(int)hb], boneName = P + bone, limit = new HumanLimit { useDefaultValues = true } });
            }
            Map(HumanBodyBones.Hips, "Hips"); Map(HumanBodyBones.Spine, "Spine"); Map(HumanBodyBones.Chest, "Spine1"); Map(HumanBodyBones.UpperChest, "Spine2");
            Map(HumanBodyBones.Neck, "Neck"); Map(HumanBodyBones.Head, "Head");
            foreach (var sd in new[] { "Left", "Right" })
            {
                bool l = sd == "Left";
                Map(l ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder, sd + "Shoulder");
                Map(l ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm, sd + "Arm");
                Map(l ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm, sd + "ForeArm");
                Map(l ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand, sd + "Hand");
                Map(l ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg, sd + "UpLeg");
                Map(l ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg, sd + "Leg");
                Map(l ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot, sd + "Foot");
                Map(l ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes, sd + "ToeBase");
            }
            var skel = new List<SkeletonBone>();
            foreach (var t in root.GetComponentsInChildren<Transform>())
                skel.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            var desc = new HumanDescription
            {
                human = human.ToArray(), skeleton = skel.ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
            var avatar = AvatarBuilder.BuildHumanAvatar(root.gameObject, desc);
            avatar.name = spec.id + "_Avatar";
            if (!avatar.isValid) Debug.LogWarning($"[HumanGen] Avatar for {spec.id} is not valid");
            return avatar;
        }

        // ================================================================== materials
        public static Material[] Materials(HumanSpec spec)
        {
            var m = new Material[(int)Mat.Count];
            if (spec.robot)
            {
                var plate = CityMaterials.PBR("PaintedMetal004", spec.plate, 0.6f, 0.75f, 0.55f, 0.8f, false);
                var dark = CityMaterials.PBR("Metal009", spec.plateDark, 0.5f, 0.6f, 0.8f, 0.8f, false);
                var joint = CityMaterials.PBR("Rubber004", new Color(0.12f, 0.12f, 0.13f), 0.3f, 1f, 0f, 0.8f, false);
                var glow = CityMaterials.Emissive(spec.glow * 0.4f, spec.glow * 3.5f);
                for (int i = 0; i < m.Length; i++) m[i] = plate;
                m[(int)Mat.Skin] = joint; m[(int)Mat.Bottom] = dark; m[(int)Mat.TopAccent] = glow; m[(int)Mat.Metal] = dark;
                return m;
            }
            m[(int)Mat.Skin] = CityMaterials.Solid(spec.skin, 0.55f, 0f, 0.4f);
            m[(int)Mat.Top] = Fabric(spec.top == HumanSpec.Top.Suit || spec.top == HumanSpec.Top.Jacket ? "Fabric081C" : "Fabric086", spec.topColor);
            m[(int)Mat.TopAccent] = Fabric("Fabric086", spec.topAccent);
            m[(int)Mat.Bottom] = Fabric(spec.bottom == HumanSpec.Bottom.Jeans ? "Fabric081C" : "Fabric086", spec.bottomColor);
            m[(int)Mat.Shoe] = spec.shoeColor.grayscale < 0.4f ? CityMaterials.PBR("Leather008", spec.shoeColor * 1.6f, 0.15f, 0.8f, 0f, 0.4f, false) : Fabric("Fabric086", spec.shoeColor);
            m[(int)Mat.Sole] = CityMaterials.PBR("Rubber004", Color.Lerp(spec.shoeAccent, Color.white, 0.6f), 0.2f, 1f, 0f, 0.4f, false);
            m[(int)Mat.Hair] = CityMaterials.Solid(spec.hairColor, 0.5f, 0f, 0.4f);
            m[(int)Mat.EyeWhite] = CityMaterials.Solid(new Color(0.92f, 0.9f, 0.88f), 0.15f, 0f, 0.3f);
            m[(int)Mat.Iris] = CityMaterials.Solid(spec.eyeColor, 0.1f, 0f, 0.3f);
            m[(int)Mat.Pupil] = CityMaterials.Solid(new Color(0.02f, 0.02f, 0.02f), 0.05f, 0f, 0.3f);
            m[(int)Mat.Lips] = CityMaterials.Solid(Color.Lerp(spec.skin, new Color(0.62f, 0.3f, 0.3f), 0.45f), 0.4f, 0f, 0.4f);
            m[(int)Mat.Pack] = Fabric("Fabric086", spec.backpackColor);
            m[(int)Mat.Metal] = CityMaterials.Solid(new Color(0.16f, 0.16f, 0.17f), 0.45f, 0.4f, 0.4f);
            m[(int)Mat.Glass] = CityMaterials.Glass(new Color(0.6f, 0.7f, 0.75f));
            return m;
        }

        static Material Fabric(string set, Color c) => CityMaterials.PBR(set, c, 0.25f, 1f, 0f, 0.4f, false);
    }
}

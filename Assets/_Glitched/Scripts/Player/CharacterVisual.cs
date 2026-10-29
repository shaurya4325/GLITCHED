using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The visible body of a character. Uses the profile's model prefab (scaled to its height, Animator driven)
    /// or builds a primitive placeholder. Handles facing (the 3D model visibly turns), the swappable face decal,
    /// cosmetics, hit flash and glitch/dissolve.
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        public CharacterProfileSO profile;
        public Transform Model { get; private set; }
        public Transform Head { get; private set; }
        public Animator Anim { get; private set; }
        public ProceduralAnimator Proc { get; private set; }
        public ExpressionController Expression { get; private set; }
        public float HeadSize { get; private set; } = 0.25f;
        /// <summary>Height of the top of the head above the Head transform, in its local units.</summary>
        public float HatOffsetLocal { get; private set; } = 0.52f;

        readonly List<Renderer> renderers = new List<Renderer>();
        MaterialPropertyBlock mpb;
        Transform hat;
        float flash, glitch, dissolve, tintK;
        Color tint = Color.white;
        float yaw, yawTarget;
        bool hasSpeed, hasGrounded, hasVSpeed, hasTalk;
        static readonly int IdFlash = Shader.PropertyToID("_HitFlash");
        static readonly int IdGlitch = Shader.PropertyToID("_GlitchAmount");
        static readonly int IdDissolve = Shader.PropertyToID("_Dissolve");
        static readonly int IdTint = Shader.PropertyToID("_Tint");

        public const float YawRight = 120f, YawLeft = 240f;

        // foot planting: keeps the soles on the floor whatever the animation does (no sinking / floating)
        Transform footL, footR, toeL, toeR, hips;
        float seatHeight;
        float ankleH, toeH, plant;
        Vector3 modelBase;
        bool grounded = true, sitting;
        /// <summary>0..1: a seated shock reaction (leans back hard, head snaps back), added on top of the animation.</summary>
        public float Recoil;

        public void Build(CharacterProfileSO p, bool withFace = true)
        {
            profile = p;
            if (Model != null) PlaceholderFactory.Kill(Model.gameObject);
            Proc = null; Anim = null; Head = null;
            if (p != null && p.modelPrefab != null)
            {
                Model = Instantiate(p.modelPrefab, transform).transform;
                Model.name = "Model";
                Model.localPosition = Vector3.zero;
                Model.localRotation = Quaternion.identity;
                Normalize(Model, p.height);
                Anim = Model.GetComponentInChildren<Animator>();
                if (Anim == null) Anim = Model.gameObject.AddComponent<Animator>();
                if (p.animator != null) Anim.runtimeAnimatorController = p.animator;
                Anim.applyRootMotion = false;
                Anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                // body clips never open the jaw, and MakeHuman teeth poke through the cheeks when the head turns
                foreach (var smr in Model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) if (smr.name.Contains("teeth")) smr.enabled = false;
                FitSkinnedBounds(Model, p.height);
                // the player is framed in close-ups (only the head in view): exact per-frame bounds so nothing gets culled
                if (p.id == "aarav") foreach (var smr in Model.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
                if (Anim.isHuman) Head = Anim.GetBoneTransform(HumanBodyBones.Head);
                if (Head == null) Head = FindDeep(Model, "head") ?? Model;
                HeadSize = p.height * 0.11f;
                HatOffsetLocal = HeadSize * 1.1f / Mathf.Max(0.0001f, Head.lossyScale.y);
                CacheParams();
                SetupFeet();
            }
            else
            {
                Color hoodie = p != null ? p.primaryColor : new Color(0.2f, 0.35f, 0.8f);
                Color skin = p != null ? p.skinColor : new Color(0.86f, 0.66f, 0.52f);
                Model = PlaceholderFactory.BuildHumanoid(transform, hoodie, skin, new Color(0.12f, 0.08f, 0.07f), new Color(0.17f, 0.22f, 0.38f), p == null || p.id == "aarav", p == null || p.id == "aarav", out var rig);
                Proc = Model.gameObject.AddComponent<ProceduralAnimator>();
                Proc.Setup(rig);
                Proc.idleStyle = p != null ? p.idleStyle : "fidget";
                Head = rig.head;
                Normalize(Model, p != null ? p.height : 1.6f);
                HeadSize = 0.25f * Model.localScale.x;
                HatOffsetLocal = 0.52f;
            }
            renderers.Clear();
            renderers.AddRange(Model.GetComponentsInChildren<Renderer>(true));
            mpb = new MaterialPropertyBlock();
            // real models (Mixamo Ty) keep their own painted face; the 2D expression decals are for the placeholder body
            // (dialogue portraits still show the expressions)
            if (withFace && p != null && (p.useFaceDecal && Proc != null || HasFaceShapes()))
            {
                Expression = GetComponent<ExpressionController>() ?? gameObject.AddComponent<ExpressionController>();
                Expression.Setup(this, p);
            }
            yaw = yawTarget = YawRight;
            Model.localRotation = Quaternion.Euler(0, yaw, 0);
        }

        /// <summary>Scales a model so its height matches, with its feet at the parent's origin.</summary>
        public static void Normalize(Transform model, float height)
        {
            var rs = model.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return;
            model.localScale = Vector3.one;
            float h = Mathf.Max(0.01f, WorldBounds(rs).size.y);
            model.localScale = Vector3.one * (height / h);
            var b = WorldBounds(rs);
            Vector3 origin = model.parent != null ? model.parent.position : Vector3.zero;
            model.position += new Vector3(origin.x - b.center.x, origin.y - b.min.y, origin.z - b.center.z);
        }

        static Bounds WorldBounds(Renderer[] rs)
        {
            Bounds b = RendererBounds(rs[0]);
            foreach (var r in rs) b.Encapsulate(RendererBounds(r));
            return b;
        }

        // skinned bounds come from the importer and can be wrong (MakeHuman's Z-up rig): measure the posed vertices instead
        static Mesh bakeMesh;
        static Bounds RendererBounds(Renderer r)
        {
            if (!(r is SkinnedMeshRenderer s) || s.sharedMesh == null) return r.bounds;
            if (bakeMesh == null) bakeMesh = new Mesh();
            s.BakeMesh(bakeMesh, true);   // with the transform scale, so it matches the world
            var v = bakeMesh.vertices;
            if (v.Length == 0) return r.bounds;
            var m = Matrix4x4.TRS(s.transform.position, s.transform.rotation, Vector3.one);
            var b = new Bounds(m.MultiplyPoint3x4(v[0]), Vector3.zero);
            for (int i = 0; i < v.Length; i += 5) b.Encapsulate(m.MultiplyPoint3x4(v[i]));
            return b;
        }

        /// <summary>Gives every skinned mesh culling bounds (in root-bone space) that cover the whole body and its animations,
        /// so an animator set to CullUpdateTransforms never freezes a visible character in its bind pose.</summary>
        static void FitSkinnedBounds(Transform model, float height)
        {
            foreach (var s in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var bone = s.rootBone != null ? s.rootBone : s.transform;
                var wb = RendererBounds(s);
                wb.Expand(height * 0.8f);   // room for sitting, reaching, levitating
                var lb = new Bounds(bone.InverseTransformPoint(wb.center), Vector3.zero);
                for (int i = 0; i < 8; i++)
                    lb.Encapsulate(bone.InverseTransformPoint(wb.center + Vector3.Scale(wb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))));
                s.updateWhenOffscreen = false;
                s.localBounds = lb;
            }
        }

        static Transform FindDeep(Transform t, string contains)
        {
            if (t.name.ToLowerInvariant().Contains(contains)) return t;
            foreach (Transform c in t) { var r = FindDeep(c, contains); if (r != null) return r; }
            return null;
        }

        void ApplyRecoil()
        {
            if (Recoil <= 0.001f || Anim == null || !Anim.isHuman) return;
            var axis = Model.right;
            var chest = Anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? Anim.GetBoneTransform(HumanBodyBones.Chest);
            var head = Anim.GetBoneTransform(HumanBodyBones.Head);
            var la = Anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var ra = Anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (chest != null) chest.rotation = Quaternion.AngleAxis(-22f * Recoil, axis) * chest.rotation;
            if (head != null) head.rotation = Quaternion.AngleAxis(-18f * Recoil, axis) * head.rotation;
            // hands come up in front of the face
            if (la != null) la.rotation = Quaternion.AngleAxis(-55f * Recoil, axis) * la.rotation;
            if (ra != null) ra.rotation = Quaternion.AngleAxis(-55f * Recoil, axis) * ra.rotation;
        }

        bool HasFaceShapes()
        {
            foreach (var smr in Model.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (smr.sharedMesh != null && smr.sharedMesh.GetBlendShapeIndex("smile") >= 0) return true;
            return false;
        }

        void SetupFeet()
        {
            footL = footR = toeL = toeR = hips = null;
            plant = 0f;
            modelBase = Model.localPosition;
            if (Anim == null || !Anim.isHuman) return;
            hips = Anim.GetBoneTransform(HumanBodyBones.Hips);
            footL = Anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            footR = Anim.GetBoneTransform(HumanBodyBones.RightFoot);
            toeL = Anim.GetBoneTransform(HumanBodyBones.LeftToes);
            toeR = Anim.GetBoneTransform(HumanBodyBones.RightToes);
            if (footL == null || footR == null) { footL = footR = null; return; }
            // bind pose: feet stand on the origin, so these are the heights of ankle and toe above the sole
            float baseY = transform.position.y;
            ankleH = Mathf.Min(footL.position.y, footR.position.y) - baseY;
            toeH = toeL != null && toeR != null ? Mathf.Min(toeL.position.y, toeR.position.y) - baseY : ankleH;
        }

        void PlantFeet()
        {
            if (footL == null || Model == null) return;
            float target = 0f;
            if (sitting && seatHeight > 0f && hips != null)
            {
                // seated: slide the body so the hip joint rests just above the seat, whatever the clip's hip height
                target = Mathf.Clamp(plant + (transform.position.y + seatHeight + 0.1f - hips.position.y), -1.2f, 1.2f);
            }
            else if (grounded && !sitting)
            {
                float baseY = transform.position.y;
                float lowest = Mathf.Min(footL.position.y - ankleH, footR.position.y - ankleH);
                if (toeL != null && toeR != null) lowest = Mathf.Min(lowest, Mathf.Min(toeL.position.y, toeR.position.y) - toeH);
                target = Mathf.Clamp(plant - (lowest - baseY), -0.35f, 0.35f);
            }
            plant = Mathf.Lerp(plant, target, 1f - Mathf.Exp(-Time.deltaTime * 14f));
            float scale = transform.lossyScale.y > 1e-4f ? transform.lossyScale.y : 1f;
            Model.localPosition = modelBase + Vector3.up * (plant / scale);
        }

        void CacheParams()
        {
            hasSpeed = hasGrounded = hasVSpeed = hasTalk = false;
            if (Anim == null || Anim.runtimeAnimatorController == null) return;
            foreach (var prm in Anim.parameters)
            {
                if (prm.name == "Speed") hasSpeed = true;
                else if (prm.name == "Grounded") hasGrounded = true;
                else if (prm.name == "VSpeed") hasVSpeed = true;
                else if (prm.name == "Talk") hasTalk = true;
            }
        }

        /// <summary>The speed (m/s) that Speed = 1 means: the sprint clip's real speed on Shaurya's rig (UAL1 Sprint_Loop,
        /// measured from its root-motion version). Blend thresholds use it: walk 1.5, jog 5.06, sprint 7.8 m/s.</summary>
        public const float RunReference = 7.8f;

        /// <summary>Locomotion from a real ground speed in metres per second (NPCs, anyone not the player).</summary>
        public void SetLocomotionVelocity(float metersPerSecond, bool grounded, float vSpeed) =>
            SetLocomotion(Mathf.Clamp01(Mathf.Abs(metersPerSecond) / RunReference), grounded, vSpeed);

        // ------------------------------------------------------------------ animation
        public void SetLocomotion(float speed01, bool grounded, float vSpeed)
        {
            if (Proc != null) { Proc.speed01 = speed01; Proc.grounded = grounded; Proc.verticalSpeed = vSpeed; }
            if (Anim != null && Anim.runtimeAnimatorController != null)
            {
                if (hasSpeed) Anim.SetFloat("Speed", speed01, 0.08f, Time.deltaTime);
                if (hasGrounded) Anim.SetBool("Grounded", grounded);
                this.grounded = grounded;
                if (hasVSpeed) Anim.SetFloat("VSpeed", vSpeed);
            }
        }

        public void Trigger(string name)
        {
            if (name == "Land") landVel = 1.2f;   // the landing dip (UpdateWeight)
            if (Proc != null) Proc.Trigger(name);
            if (Anim != null && Anim.runtimeAnimatorController != null)
                foreach (var prm in Anim.parameters)
                    if (prm.name == name && prm.type == AnimatorControllerParameterType.Trigger) { Anim.SetTrigger(name); break; }
        }

        public void SetTalking(bool on)
        {
            if (Proc != null) Proc.talking = on;
            if (hasTalk) Anim.SetBool("Talk", on);
            if (Expression != null) Expression.SetTalking(on);
        }

        public void SetFrozen(bool on)
        {
            if (Proc != null) Proc.frozen = on;
            if (Anim != null) Anim.speed = on ? 0f : 1f;
        }

        /// <summary>Seated in a cutscene (no foot planting), without forcing the animator's Sit state.</summary>
        /// <summary>Seated poses; seatHeight (above this character's root) puts the hips on the seat, 0 leaves them where the clip has them.</summary>
        public void SetSeated(bool on, float seatHeight = 0f) { sitting = on; this.seatHeight = on ? seatHeight : 0f; }

        /// <summary>Cross-fades to a named animator state (cutscene poses such as "Typing" or "Sitting_Yell"); false if missing.</summary>
        public bool PlayState(string state, float fade = 0.25f)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null) return false;
            int h = Animator.StringToHash(state);
            if (!Anim.HasState(0, h)) return false;
            Anim.CrossFadeInFixedTime(h, fade, 0);
            return true;
        }

        /// <summary>Plays a one-shot state, then blends back to Locomotion when it is nearly done (whatever the clip's loop flag).</summary>
        public void PlayOnce(string state, float fade = 0.25f)
        {
            if (!PlayState(state, fade)) return;
            if (onceRoutine != null) StopCoroutine(onceRoutine);
            onceRoutine = StartCoroutine(BackToLocomotion(Animator.StringToHash(state), fade));
        }

        Coroutine onceRoutine;
        IEnumerator BackToLocomotion(int hash, float fade)
        {
            float t = 0f;
            yield return new WaitForSeconds(fade + 0.05f);
            while (Anim != null && t < 20f)
            {
                var cur = Anim.GetCurrentAnimatorStateInfo(0);
                if (cur.shortNameHash != hash) yield break;   // something else took over
                if (cur.normalizedTime >= 0.85f) { Anim.CrossFadeInFixedTime("Locomotion", 0.35f, 0); break; }
                t += Time.deltaTime;
                yield return null;
            }
            onceRoutine = null;
        }

        public void SetDashing(bool on) { if (Proc != null) Proc.dashing = on; if (on) Trigger("Dash"); }

        public void SetSliding(bool on) { if (Anim != null && Anim.runtimeAnimatorController != null) foreach (var prm in Anim.parameters) if (prm.name == "Slide") Anim.SetBool("Slide", on); }

        // parkour moves: state name -> clip it plays and where in the clip it starts (the roll skips its dive)
        static readonly Dictionary<string, (string clip, float start)> Acts = new Dictionary<string, (string, float)>
        {
            { "Climb", ("ClimbUp_1m", 0.12f) }, { "Vault", ("NinjaJump_Start", 0.1f) }, { "Roll", ("Roll", 0.36f) },
        };
        readonly Dictionary<string, float> actLengths = new Dictionary<string, float>();

        /// <summary>Plays a parkour move (Climb, Vault, Roll) so that it lasts exactly 'duration' seconds.</summary>
        public void PlayAct(string state, float duration)
        {
            if (Anim == null || Anim.runtimeAnimatorController == null || !Acts.TryGetValue(state, out var act)) return;
            int h = Animator.StringToHash(state);
            if (!Anim.HasState(0, h)) return;
            if (!actLengths.TryGetValue(state, out float len))
            {
                len = 1f;
                foreach (var c in Anim.runtimeAnimatorController.animationClips) if (c != null && c.name.EndsWith(act.clip)) { len = c.length; break; }
                actLengths[state] = len;
            }
            Anim.SetFloat("ActSpeed", len * (1f - act.start) / Mathf.Max(0.05f, duration));
            Anim.CrossFadeInFixedTime(h, 0.08f, 0, len * act.start);
        }

        /// <summary>0..1: arms reach straight up along the body (hanging from a bar, a zipline, a ledge).</summary>
        public float Reach;
        float reachK;

        void ApplyReach()
        {
            reachK = Mathf.MoveTowards(reachK, Reach, Time.deltaTime * 8f);
            if (reachK <= 0.001f || Anim == null || !Anim.isHuman) return;
            var up = transform.up;
            foreach (var side in new[] { (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand), (HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand) })
            {
                var ua = Anim.GetBoneTransform(side.Item1); var la = Anim.GetBoneTransform(side.Item2); var hd = Anim.GetBoneTransform(side.Item3);
                if (ua == null || la == null || hd == null) continue;
                // aim the upper arm, then the forearm, straight up (a little apart, like hands on a bar)
                var want = (up + Model.right * (side.Item1 == HumanBodyBones.LeftUpperArm ? -0.12f : 0.12f)).normalized;
                ua.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(la.position - ua.position, want), reachK) * ua.rotation;
                la.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(hd.position - la.position, want), reachK) * la.rotation;
            }
        }
        public void SetSitting(bool on) { sitting = on; if (Proc != null) Proc.sitting = on; if (Anim != null && Anim.runtimeAnimatorController != null) foreach (var prm in Anim.parameters) if (prm.name == "Sit") Anim.SetBool("Sit", on); }

        /// <summary>Face left/right: the model turns smoothly to a 3/4 view toward the camera.</summary>
        public void Face(float dir)
        {
            if (Mathf.Abs(dir) < 0.01f) return;
            yawTarget = dir > 0 ? YawRight : YawLeft;
        }

        public void FaceCamera() => yawTarget = 180f;
        public void FaceYaw(float y) => yawTarget = y;
        public bool FacingRight => Mathf.Abs(Mathf.DeltaAngle(yawTarget, YawRight)) < 1f;

        // ------------------------------------------------------------------ effects
        public void Flash(float amount = 1f) => flash = Mathf.Max(flash, amount);
        public void SetGlitch(float g) => glitch = g;
        public void SetDissolve(float d) => dissolve = d;
        public void SetTint(Color c) { tint = c; tintK = 1; }

        public void ApplyCosmetic(CosmeticSO c)
        {
            if (hat != null) PlaceholderFactory.Kill(hat.gameObject);
            hat = null;
            tint = Color.white;
            if (c == null || Head == null) return;
            if (c.hatPrefab != null)
            {
                hat = Instantiate(c.hatPrefab, Head).transform;
                hat.localPosition = new Vector3(0, HeadSize, 0);
            }
            else hat = PlaceholderFactory.BuildHat(Head, c.hat, c.hatColor, HeadSize / Mathf.Max(0.0001f, Head.lossyScale.y), HatOffsetLocal);
            if (hat != null && Model != null)
            {
                // hats are built in the head's local space; keep them upright relative to the model
                hat.rotation = Model.rotation;
                foreach (var r in hat.GetComponentsInChildren<Renderer>()) renderers.Add(r);
            }
            if (c.tintBody) tint = c.bodyTint;
        }

        // body weight: velocity measured from the root, leaning into the run and into speed changes
        float yawVel, lean, landDip, landVel;
        Vector3 lastPos; float lastVx; bool hasLast;

        void UpdateWeight()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            var pos = transform.position;
            float vx = hasLast ? (pos.x - lastPos.x) / dt : 0f;
            if (!hasLast || Mathf.Abs(vx) > 40f) vx = 0f;   // a teleport, not a run
            float ax = (vx - lastVx) / dt;
            lastPos = pos; lastVx = Mathf.Lerp(lastVx, vx, 0.5f); hasLast = true;
            float target = sitting || !grounded ? 0f : Mathf.Clamp(-vx * 0.75f - ax * 0.05f, -9f, 9f);
            lean = Mathf.Lerp(lean, target, 1f - Mathf.Exp(-dt * 8f));
            // landing: the knees take the weight for a moment (a damped spring on the body height)
            landVel += (-landDip * 260f - landVel * 22f) * dt;
            landDip += landVel * dt;
        }

        void LateUpdate()
        {
            if (Model == null) return;
            UpdateWeight();
            PlantFeet();
            if (landDip > 1e-4f) Model.localPosition += Vector3.down * landDip / Mathf.Max(1e-4f, transform.lossyScale.y);
            ApplyRecoil();
            ApplyReach();
            yaw = Mathf.SmoothDampAngle(yaw, yawTarget, ref yawVel, 0.07f, 1400f);
            Model.localRotation = Quaternion.AngleAxis(lean, Vector3.forward) * Quaternion.Euler(0, yaw, 0);
            if (hat != null) hat.rotation = Model.rotation;
            flash = Mathf.MoveTowards(flash, 0, Time.unscaledDeltaTime * 6f);
            if (mpb == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat(IdFlash, flash);
                mpb.SetFloat(IdGlitch, glitch);
                mpb.SetFloat(IdDissolve, dissolve);
                mpb.SetColor(IdTint, tint);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// 2.5D movement on the X/Y plane (Section 5) at a human pace: walk (analog), jog, and a sprint that opens up
    /// when you keep running; variable-height jump with coyote time and a jump buffer, double jump, dash, wall-jump,
    /// shrink/grow, gravity flips and moving platforms. Parkour: ledge mantles, speed vaults over low obstacles,
    /// slides under low gaps, landing rolls, swing bars, ziplines and springboards. The 3D model turns smoothly to
    /// face the movement direction. Also owns Stability (player HP), interaction and pit respawns.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }
        /// <summary>Test/capture mode: walk automatically in this direction (0 = off).</summary>
        public static float AutoWalk;
        /// <summary>Test bot (ParkourBot): press jump this frame / hold down for this long.</summary>
        public static bool BotJump;
        public static float BotSlide;
        float botHold;

        // speeds match the clips on Shaurya's rig (walk 1.5, jog 5.1, sprint 7.8 m/s), so the feet never skate
        [Header("Run")]
        public float walkSpeed = 1.6f;
        public float runSpeed = 5f;
        public float sprintSpeed = 6.8f;
        [Tooltip("Seconds of running in one direction before the jog opens up into a sprint.")]
        public float sprintBuildTime = 0.7f;
        public float groundAccel = 20f, sprintAccel = 3.2f, groundDecel = 26f, turnAccel = 36f, airAccel = 15f, airDecel = 3f;
        [Header("Jump")]
        public float jumpHeight = 3.1f;
        public float doubleJumpHeight = 2.5f;
        [Tooltip("A running take-off adds this much forward speed (a bound, like a long jumper).")]
        public float jumpBoost = 1.2f;
        public float gravity = 34f;
        public float fallMultiplier = 1.55f;
        public float lowJumpMultiplier = 2.5f;
        public float maxFallSpeed = 24f;
        public float coyoteTime = 0.12f;
        public float jumpBuffer = 0.14f;
        [Header("Dash")]
        public float dashSpeed = 17f, dashTime = 0.17f, dashCooldown = 0.55f;
        [Header("Wall jump")]
        public float wallSlideSpeed = 3f, wallJumpX = 7.5f, wallJumpY = 11.5f;
        [Header("Parkour")]
        [Tooltip("Highest ledge (above the feet) the hands can catch for a mantle.")]
        public float mantleReach = 2.3f;
        public float slideBoost = 1.4f, slideFriction = 4.5f, slideMinSpeed = 2.6f, slideHeight = 0.8f;
        [Header("Shrink")]
        public float shrinkScale = 0.42f;
        [Header("World")]
        public float killY = -30f;
        public float maxStability = 100f;

        public enum Move { Normal, Slide, Mantle, Vault, Swing, Zip }
        public Move State { get; private set; }
        public float Stability { get; private set; }
        public CharacterVisual Visual { get; private set; }
        public CharacterController CC { get; private set; }
        public bool Grounded { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector3 Velocity => vel;
        public bool IsShrunk => shrunk;
        public bool Sprinting => sprintT > sprintBuildTime && Grounded;
        public float GravitySign => gravSign;
        public Checkpoint LastCheckpoint { get; set; }
        public Vector3 SpawnPoint { get; set; }
        public bool Invulnerable => dashTimer > 0 || invulnTimer > 0;

        Vector3 vel;
        bool ballistic;   // an aimed launch (answer pillar): keep the launch speed unless the player steers
        bool launchRise;  // springboards, pillars, let-go: the rise is not cut short by releasing jump
        float coyote, buffer, dashTimer, dashCd, invulnTimer, footTimer, airTime, sprintT, rollT, stunT, airTop, preVy;
        int airJumpsLeft;
        bool wallSliding;
        int wallSide;
        float gravSign = 1f;
        bool shrunk;
        float scaleK = 1f;
        readonly HashSet<object> locks = new HashSet<object>();
        Interactable nearest;
        MovingPlatform platform;
        CollisionFlags lastFlags;
        bool respawning;
        public bool Locked => locks.Count > 0 || respawning;
        const float StandHeight = 1.55f;

        // ------------------------------------------------------------------ creation
        public static PlayerController Spawn(Vector3 position)
        {
            if (Instance != null) { Instance.Teleport(position); return Instance; }
            var go = new GameObject("Player");
            go.tag = "Player";
            go.transform.position = new Vector3(position.x, position.y, 0);
            var cc = go.AddComponent<CharacterController>();
            cc.height = StandHeight; cc.radius = 0.32f; cc.center = new Vector3(0, 0.8f, 0);
            cc.stepOffset = 0.35f; cc.skinWidth = 0.04f; cc.slopeLimit = 50f; cc.minMoveDistance = 0f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            go.AddComponent<PlaneLock>();
            var p = go.AddComponent<PlayerController>();
            p.SpawnPoint = position;
            return p;
        }

        void Awake()
        {
            Instance = this;
            CC = GetComponent<CharacterController>();
            var vGo = new GameObject("Visual");
            vGo.transform.SetParent(transform, false);
            Visual = vGo.AddComponent<CharacterVisual>();
            Visual.Build(Services.Db.Character("aarav"));
            Visual.ApplyCosmetic(Services.Db.Cosmetic(Services.Game.Data.cosmeticEquipped));
            Stability = maxStability;
        }

        void Start() => GameEvents.RaiseStability(Stability, maxStability);
        void OnDestroy() { if (Instance == this) Instance = null; }

        // ------------------------------------------------------------------ locking (encounters, dialogue, cutscenes)
        public void Lock(object owner) { locks.Add(owner); }
        public void Unlock(object owner) { locks.Remove(owner); }
        public void ClearLocks() => locks.Clear();

        public void Teleport(Vector3 p)
        {
            EndParkour();
            CC.enabled = false;
            transform.position = new Vector3(p.x, p.y, 0);
            CC.enabled = true;
            vel = Vector3.zero;
            sprintT = 0; rollT = 0; stunT = 0;
            hasSafeGround = false;   // a new place: remember safe ground afresh
            if (CameraRig.Instance != null) CameraRig.Instance.Snap();
        }

        // ------------------------------------------------------------------ main loop
        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            var input = Services.Input;
            bool locked = Locked;
            float x = locked ? 0f : (AutoWalk != 0 ? AutoWalk : input.Move.x);
            float yIn = locked ? 0f : (BotSlide > 0f ? -1f : input.Move.y);
            if (Mathf.Abs(x) < 0.2f) x = 0f;
            BotSlide -= dt; botHold -= dt;
            if (BotJump) botHold = 0.35f;

            dashCd -= dt; invulnTimer -= dt;
            if (!locked && (input.JumpPressed || BotJump)) buffer = jumpBuffer; else buffer -= dt;
            BotJump = false;

            switch (State)
            {
                case Move.Mantle:
                case Move.Vault: UpdateTraverse(dt); Animate(x, locked); UpdateInteraction(true); return;
                case Move.Swing: UpdateSwing(dt, x, locked); Animate(x, locked); UpdateInteraction(true); return;
                case Move.Zip: UpdateZip(dt, yIn, locked); Animate(x, locked); UpdateInteraction(true); return;
            }

            bool hasDouble = Services.Game.HasAbility("double_jump");
            bool hasDash = Services.Game.HasAbility("dash");
            bool hasWall = Services.Game.HasAbility("wall_jump");

            if (Grounded) { coyote = coyoteTime; airJumpsLeft = hasDouble ? 1 : 0; airTime = 0; }
            else { coyote -= dt; airTime += dt; }
            rollT -= dt; stunT -= dt;

            if (dashTimer > 0)
            {
                dashTimer -= dt;
                vel.x = Facing * dashSpeed;
                vel.y = 0;
                if (dashTimer <= 0) { Visual.SetDashing(false); vel.x = Facing * Mathf.Max(runSpeed, sprintSpeed * 0.9f); sprintT = sprintBuildTime + 0.6f; }
            }
            else if (State == Move.Slide)
            {
                UpdateSlide(dt, x, yIn, locked);
            }
            else
            {
                if (!locked && hasDash && input.DashPressed && dashCd <= 0) StartDash();

                // the pace: tilt the stick a little to walk; keep running one way and the jog opens into a sprint
                bool moving = Mathf.Abs(x) > 0.01f;
                int dir = moving ? (x > 0 ? 1 : -1) : 0;
                if (Grounded)
                {
                    bool sameWay = moving && (Mathf.Abs(vel.x) < 0.3f || Mathf.Sign(vel.x) == dir);
                    if (sameWay && Mathf.Abs(vel.x) > runSpeed * 0.88f && !shrunk) sprintT += dt;
                    else if (!sameWay) sprintT = 0f;
                }
                float top = Mathf.Abs(x) < 0.55f ? walkSpeed : runSpeed;
                if (sprintT > sprintBuildTime) top = Mathf.Lerp(runSpeed, sprintSpeed, Mathf.Clamp01((sprintT - sprintBuildTime) / 0.6f));
                if (shrunk) top *= 0.8f;
                if (stunT > 0f) top *= 0.3f;

                if (rollT > 0f)
                {
                    // a landing roll carries you on at a jog
                    vel.x = Facing * Mathf.Max(Mathf.Abs(vel.x), 4.6f);
                }
                else if (Grounded)
                {
                    float target = dir * top;
                    bool turning = moving && Mathf.Abs(vel.x) > 0.5f && Mathf.Sign(vel.x) != dir;
                    float a = !moving ? groundDecel : turning ? turnAccel : Mathf.Abs(vel.x) < runSpeed ? groundAccel : sprintAccel;
                    if (moving && Mathf.Abs(vel.x) > top + 0.05f && !turning) a = groundDecel * 0.35f;   // ease off a sprint
                    vel.x = Mathf.MoveTowards(vel.x, target, a * dt);
                }
                else
                {
                    // air: momentum is kept; you can steer, but not speed up past your take-off speed
                    float limit = Mathf.Max(top, airTop);
                    if (moving || !ballistic) vel.x = Mathf.MoveTowards(vel.x, moving ? dir * limit : vel.x * 0.98f, (moving ? airAccel : airDecel) * dt);
                }

                // wall slide (wall-jump ability)
                bool sideHit = (lastFlags & CollisionFlags.Sides) != 0;
                wallSliding = false;
                if (hasWall && !Grounded && sideHit && Mathf.Abs(x) > 0.1f && vel.y * gravSign < 0)
                {
                    wallSliding = true;
                    wallSide = x > 0 ? 1 : -1;
                    if (vel.y * gravSign < -wallSlideSpeed) vel.y = -wallSlideSpeed * gravSign;
                }

                // parkour: catch a ledge in the air, or vault / step up onto a low obstacle at a run
                if (!locked && gravSign > 0 && !shrunk && (moving || Mathf.Abs(vel.x) > 1.5f))
                {
                    int toward = moving ? dir : (vel.x > 0 ? 1 : -1);
                    if (!Grounded && vel.y < 5f && airTime > 0.05f && TryMantle(toward, false)) { Animate(x, locked); UpdateInteraction(true); return; }
                    if (Grounded && moving && Mathf.Abs(vel.x) > 2.2f && TryMantle(toward, true)) { Animate(x, locked); UpdateInteraction(true); return; }
                }

                // slide: down at a run
                if (!locked && Grounded && gravSign > 0 && !shrunk && yIn < -0.5f && Mathf.Abs(vel.x) > 3f && rollT <= 0f) StartSlide();

                // jump
                if (buffer > 0 && State == Move.Normal)
                {
                    if (coyote > 0) { DoJump(jumpHeight, x); }
                    else if (wallSliding) { WallJump(); }
                    else if (airJumpsLeft > 0) { airJumpsLeft--; DoJump(doubleJumpHeight, x, true); }
                }

                Gravity(dt, (input.JumpHeld || botHold > 0f) && !locked);
            }

            if (State == Move.Normal || State == Move.Slide) Step(dt, x, locked);
            if (!locked && (State == Move.Normal) && !Grounded) CatchSwingOrZip();

            Animate(x, locked);

            // shrink / grow
            if (!locked && input.ShrinkPressed && Services.Game.HasAbility("shrink") && State == Move.Normal) ToggleShrink();
            float targetScale = shrunk ? shrinkScale : 1f;
            if (!Mathf.Approximately(scaleK, targetScale))
            {
                scaleK = Mathf.MoveTowards(scaleK, targetScale, dt * 3f);
                transform.localScale = Vector3.one * scaleK;
            }

            UpdateInteraction(locked);

            if (transform.position.y < killY || transform.position.y > -killY + 80f) FallRespawn();
        }

        void Gravity(float dt, bool jumpHeld)
        {
            float g = gravity;
            float up = vel.y * gravSign;
            if (up <= 0f) launchRise = false;
            if (up < 0) g *= fallMultiplier;
            else if (up > 0 && !jumpHeld && !launchRise) g *= lowJumpMultiplier;
            vel.y -= g * gravSign * dt;
            if (vel.y * gravSign < -maxFallSpeed) vel.y = -maxFallSpeed * gravSign;
        }

        /// <summary>Moves the character controller (with moving platform carry) and works out what it hit.</summary>
        void Step(float dt, float x, bool locked)
        {
            Vector3 carry = platform != null && Grounded ? platform.Delta : Vector3.zero;
            bool wasGrounded = Grounded;
            platform = null;
            if (!CC.enabled) return;
            preVy = vel.y;
            lastFlags = CC.Move(vel * dt + carry);
            var flags = lastFlags;
            bool hitFeet = gravSign > 0 ? (flags & CollisionFlags.Below) != 0 : (flags & CollisionFlags.Above) != 0;
            bool hitHead = gravSign > 0 ? (flags & CollisionFlags.Above) != 0 : (flags & CollisionFlags.Below) != 0;
            Grounded = hitFeet && vel.y * gravSign <= 0.01f;
            if (Grounded) vel.y = -2f * gravSign;
            if (Grounded && gravSign > 0) RememberSafeGround();
            if (hitHead && vel.y * gravSign > 0) vel.y = 0;
            if ((flags & CollisionFlags.Sides) != 0 && dashTimer <= 0) { vel.x *= 0.5f; sprintT = 0f; }

            if (!wasGrounded && Grounded)
            {
                airTop = 0f;
                ballistic = false;
                if (airTime > 0.12f) Land(x, locked);
            }
            if (!Grounded && wasGrounded) airTop = Mathf.Max(airTop, Mathf.Abs(vel.x));
        }

        void Land(float x, bool locked)
        {
            float impact = -preVy * gravSign;
            if (State == Move.Slide) { Services.Audio.Play("land", 0.5f); return; }
            // a big drop (about 4.5 m or more, e.g. off a springboard or a roof) at a run: roll it out
            if (impact > 21f && Mathf.Abs(vel.x) > 2.5f && !locked && gravSign > 0 && !shrunk)
            {
                // a parkour roll: the fall's energy goes into the ground and forward, not into the knees
                rollT = 0.5f;
                Facing = vel.x > 0 ? 1 : -1;
                Visual.PlayAct("Roll", 0.62f);
                Services.Audio.Play("land", 0.6f);
                VFX.Dust(transform.position);
                return;
            }
            Visual.Trigger("Land");
            Services.Audio.Play("land", Mathf.Clamp01(airTime));
            VFX.Dust(transform.position);
            if (impact > 21f) { stunT = 0.18f; if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.2f); }
        }

        void Animate(float x, bool locked)
        {
            if (Mathf.Abs(x) > 0.01f && !locked && State != Move.Swing && State != Move.Zip) Facing = x > 0 ? 1 : -1;
            if (!locked || Mathf.Abs(vel.x) > 0.1f) Visual.Face(Facing);
            bool ground = Grounded && (State == Move.Normal || State == Move.Slide);
            float speed = State == Move.Normal || State == Move.Slide ? Mathf.Abs(vel.x) : 0f;
            Visual.SetLocomotion(Mathf.Clamp01(speed / CharacterVisual.RunReference), ground, vel.y * gravSign);

            // footsteps: slower steps when walking, quicker when sprinting
            if (Grounded && State == Move.Normal && Mathf.Abs(vel.x) > 1f && rollT <= 0f)
            {
                footTimer -= Time.deltaTime;
                if (footTimer <= 0)
                {
                    footTimer = Mathf.Lerp(0.5f, 0.27f, Mathf.InverseLerp(walkSpeed, sprintSpeed, Mathf.Abs(vel.x)));
                    Services.Audio.Play("footstep", Mathf.Lerp(0.35f, 0.6f, Mathf.Abs(vel.x) / sprintSpeed));
                }
            }
        }

        void DoJump(float height, float x, bool air = false)
        {
            buffer = 0; coyote = 0;
            float v = Mathf.Sqrt(2f * gravity * height * (shrunk ? 0.75f : 1f));
            vel.y = v * gravSign;
            if (!air && Mathf.Abs(x) > 0.5f && (Mathf.Abs(vel.x) < 0.3f || Mathf.Sign(vel.x) == Mathf.Sign(x)))
                vel.x = Mathf.Sign(x) * Mathf.Min(Mathf.Abs(vel.x) + jumpBoost, sprintSpeed + jumpBoost);
            airTop = Mathf.Max(airTop, Mathf.Abs(vel.x));
            Grounded = false;
            rollT = 0f;
            Services.Audio.Play(air ? "double_jump" : "jump");
            if (air) { VFX.Burst(transform.position + Vector3.up * 0.2f, UIFactory.Theme.cyan, 10, "spark"); Visual.Trigger("Jump"); }
            else VFX.Dust(transform.position);
        }

        void WallJump()
        {
            buffer = 0;
            vel.x = -wallSide * wallJumpX;
            vel.y = wallJumpY * gravSign;
            airTop = Mathf.Abs(vel.x);
            Facing = -wallSide;
            Services.Audio.Play("jump");
            VFX.Burst(transform.position + new Vector3(wallSide * 0.3f, 0.8f, 0), UIFactory.Theme.green, 8, "spark");
        }

        void StartDash()
        {
            dashTimer = dashTime;
            dashCd = dashCooldown;
            Visual.SetDashing(true);
            Services.Audio.Play("dash");
            VFX.Trail(transform, UIFactory.Theme.magenta, dashTime);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.15f);
        }

        // ------------------------------------------------------------------ parkour: geometry queries
        static readonly Collider[] overlap = new Collider[16];

        /// <summary>True if a solid (non-trigger, not the player) collider overlaps the capsule.</summary>
        bool Blocked(Vector3 a, Vector3 b, float r)
        {
            int n = Physics.OverlapCapsuleNonAlloc(a, b, r, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (overlap[i] != null && overlap[i].gameObject != gameObject && !overlap[i].transform.IsChildOf(transform)) return true;
            return false;
        }

        bool Ray(Vector3 from, Vector3 dir, float dist, out RaycastHit hit)
        {
            if (!Physics.Raycast(from, dir, out hit, dist, ~0, QueryTriggerInteraction.Ignore)) return false;
            // never climb moving things, or over puzzle doors and ability seals
            var c = hit.collider;
            return c.gameObject != gameObject && c.GetComponentInParent<MovingPlatform>() == null && c.GetComponentInParent<JammedDoor>() == null && c.GetComponentInParent<SealedBarrier>() == null
                   && c.GetComponentInParent<Springboard>() == null;
        }

        bool StandRoom(Vector3 feet) => !Blocked(feet + Vector3.up * (CC.radius + 0.06f), feet + Vector3.up * (StandHeight - CC.radius), CC.radius - 0.02f);

        // ------------------------------------------------------------------ parkour: mantle + vault
        Vector3 travFrom, travMid, travTo;
        float travT, travDur, travExitSpeed;

        /// <summary>A ledge ahead within reach: climb onto it (in the air), or vault / step up onto it (at a run).</summary>
        bool TryMantle(int dir, bool fromGround)
        {
            Vector3 p = transform.position;
            float reach = CC.radius + (fromGround ? 0.45f : 0.38f);
            // a face in front of the body
            float wallX = float.NaN;
            for (float h = 0.3f; h <= 1.7f; h += 0.35f)
                if (Ray(p + Vector3.up * h, Vector3.right * dir, reach, out var hit) && hit.normal.x * dir < -0.7f) { wallX = hit.point.x; break; }
            if (float.IsNaN(wallX)) return false;
            // its top, just beyond the face
            float maxH = fromGround ? 1.35f : mantleReach;
            if (!Ray(new Vector3(wallX + dir * 0.25f, p.y + maxH + 0.05f, 0f), Vector3.down, maxH + 0.05f - 0.25f, out var top)) return false;
            if (top.normal.y < 0.8f) return false;
            float h0 = top.point.y - p.y;
            if (h0 < 0.4f || h0 > maxH) return false;
            // nothing in the way above the ledge on our side (no mantling through a ceiling)
            if (Blocked(new Vector3(wallX - dir * 0.35f, top.point.y + 0.35f, 0f), new Vector3(wallX - dir * 0.35f, top.point.y + 0.9f, 0f), 0.2f)) return false;

            // a short, low obstacle at speed: vault clean over it and land beyond
            if (fromGround && h0 < 1.3f)
            {
                float far = float.NaN;
                for (float d = 0.5f; d <= 1.8f; d += 0.3f)
                    if (!Ray(new Vector3(wallX + dir * d, top.point.y + 0.3f, 0f), Vector3.down, 0.6f, out _)) { far = d; break; }
                if (!float.IsNaN(far))
                {
                    float landX = wallX + dir * (far + 0.55f);
                    float groundY = Ray(new Vector3(landX, top.point.y + 0.3f, 0f), Vector3.down, 6f, out var g) ? g.point.y : p.y;
                    var land = new Vector3(landX, groundY + 0.02f, 0f);
                    if (StandRoom(land) && groundY > top.point.y - 3.5f)
                    {
                        StartTraverse(Move.Vault, new Vector3((p.x + landX) * 0.5f, top.point.y + 0.45f, 0f), land, Mathf.Clamp(Mathf.Abs(landX - p.x) / Mathf.Max(4.5f, Mathf.Abs(vel.x)), 0.32f, 0.5f), Mathf.Abs(vel.x));
                        Visual.PlayAct("Vault", 0.45f);
                        Services.Audio.Play("jump", 0.6f);
                        return true;
                    }
                }
            }

            // climb up: hands on the edge, body rises along the face, then over onto the top
            var stand = new Vector3(wallX + dir * (CC.radius + 0.25f), top.point.y + 0.02f, 0f);
            if (!StandRoom(stand)) return false;
            float dur = Mathf.Lerp(0.3f, 0.62f, Mathf.InverseLerp(0.4f, mantleReach, h0));
            StartTraverse(Move.Mantle, new Vector3(wallX - dir * (CC.radius + 0.05f), top.point.y + 0.15f, 0f), stand, dur, fromGround ? Mathf.Abs(vel.x) * 0.7f : 2.5f);
            Visual.Reach = 1f;
            Visual.PlayAct("Climb", dur);
            Services.Audio.Play("land", 0.4f);
            return true;
        }

        void StartTraverse(Move s, Vector3 mid, Vector3 to, float dur, float exitSpeed)
        {
            State = s;
            travFrom = transform.position; travMid = mid; travTo = to;
            travT = 0f; travDur = dur; travExitSpeed = exitSpeed;
            Facing = to.x > travFrom.x ? 1 : -1;
            CC.enabled = false;
            vel = Vector3.zero;
            Grounded = false;
            buffer = 0f;
        }

        void UpdateTraverse(float dt)
        {
            travT += dt / Mathf.Max(0.05f, travDur);
            float t = Mathf.Clamp01(travT);
            Vector3 p;
            if (State == Move.Vault)
            {
                // a smooth arc over the obstacle (quadratic through the peak)
                Vector3 a = Vector3.Lerp(travFrom, travMid, t), b = Vector3.Lerp(travMid, travTo, t);
                p = Vector3.Lerp(a, b, t);
            }
            else
            {
                // rise along the face first, then roll forward over the edge
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.62f));
                float over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.55f));
                p = new Vector3(Mathf.Lerp(travFrom.x, travMid.x, rise), Mathf.Lerp(travFrom.y, travMid.y, rise), 0f);
                p = new Vector3(Mathf.Lerp(p.x, travTo.x, over), Mathf.Lerp(p.y, travTo.y, over), 0f);
                Visual.Reach = 1f - over;
            }
            transform.position = p;
            if (t >= 1f)
            {
                State = Move.Normal;
                Visual.Reach = 0f;
                CC.enabled = true;
                vel = new Vector3(Facing * travExitSpeed, -2f, 0f);
                Grounded = true;
                coyote = coyoteTime;
                if (travExitSpeed < runSpeed * 0.88f) sprintT = 0f;
                VFX.Dust(transform.position);
            }
        }

        // ------------------------------------------------------------------ parkour: slide
        float slideT;

        void StartSlide()
        {
            State = Move.Slide;
            slideT = 0f;
            int d = vel.x > 0 ? 1 : -1;
            Facing = d;
            vel.x = d * Mathf.Min(Mathf.Abs(vel.x) + slideBoost, sprintSpeed + slideBoost);
            SetCapsule(slideHeight);
            Visual.SetSliding(true);
            Services.Audio.Play("dash", 0.5f);
            VFX.Dust(transform.position);
        }

        void UpdateSlide(float dt, float x, float yIn, bool locked)
        {
            slideT += dt;
            int d = vel.x >= 0 ? 1 : -1;
            float sp = Mathf.Max(0f, Mathf.Abs(vel.x) - slideFriction * dt);
            bool roof = !StandRoom(transform.position);
            if (roof) sp = Mathf.Max(sp, slideMinSpeed);   // can't stand up under a pipe: keep sliding through
            vel.x = d * sp;
            if (Grounded && Mathf.Repeat(slideT, 0.12f) < dt) VFX.Dust(transform.position + Vector3.right * d * 0.3f);
            bool jump = buffer > 0 && Grounded && !roof;
            bool wantUp = slideT > 0.3f && yIn > -0.3f;
            if (jump || (!roof && (sp < slideMinSpeed || wantUp || !Grounded || locked)))
            {
                EndSlide();
                if (!jump && Mathf.Abs(vel.x) >= runSpeed * 0.88f) sprintT = Mathf.Max(sprintT, sprintBuildTime);
                if (jump) DoJump(jumpHeight, d);
            }
            Gravity(dt, Services.Input.JumpHeld);
        }

        void EndSlide()
        {
            if (State != Move.Slide) return;
            State = Move.Normal;
            SetCapsule(StandHeight);
            Visual.SetSliding(false);
        }

        void SetCapsule(float h)
        {
            CC.height = h;
            CC.center = new Vector3(0, h / 2f + 0.025f, 0);
        }

        // ------------------------------------------------------------------ parkour: swing bars + ziplines
        SwingBar swing;
        float swingA, swingW;   // angle from straight down (radians, + = towards +x), angular velocity
        Zipline zip;
        float zipS, zipV;       // distance along the cable, speed along it
        float reattach;
        const float HandHeight = 1.85f;

        void CatchSwingOrZip()
        {
            if ((reattach -= Time.deltaTime) > 0f || gravSign < 0 || shrunk) return;
            Vector3 hands = transform.position + Vector3.up * HandHeight;
            foreach (var b in SwingBar.All)
                if (b != null && Vector2.Distance(hands, b.Pivot) < b.catchRadius) { StartSwing(b); return; }
            foreach (var z in Zipline.All)
            {
                if (z == null) continue;
                float s = z.Project(hands, out float dist);
                if (dist < z.catchRadius && s > 0.3f && s < z.Length - 0.8f) { StartZip(z, s); return; }
            }
        }

        void StartSwing(SwingBar b)
        {
            State = Move.Swing;
            swing = b;
            // caught on the way past: the body trails behind the bar, then swings through
            swingA = -Mathf.Clamp(vel.x * 0.09f, -0.75f, 0.75f);
            // the body's speed becomes swing speed (tangent = (cos a, sin a))
            swingW = (vel.x * Mathf.Cos(swingA) + vel.y * Mathf.Sin(swingA)) / b.length;
            CC.enabled = false;
            vel = Vector3.zero;
            Grounded = false;
            Visual.Reach = 1f;
            b.Catch();
            Services.Audio.Play("land", 0.5f);
        }

        void UpdateSwing(float dt, float x, bool locked)
        {
            float L = swing.length;
            // a pendulum: angular acceleration = -(g/L) sin(angle); leaning with the swing pumps it higher
            float alpha = -(gravity / L) * Mathf.Sin(swingA);
            if (!locked && Mathf.Abs(x) > 0.1f && Mathf.Sign(x) == Mathf.Sign(swingW) && Mathf.Abs(swingA) < 1.2f) alpha += Mathf.Sign(x) * swing.pump;
            swingW += alpha * dt;
            swingW *= 1f - 0.15f * dt;
            swingW = Mathf.Clamp(swingW, -9f, 9f);
            swingA += swingW * dt;
            swingA = Mathf.Clamp(swingA, -2.4f, 2.4f);
            Vector2 piv = swing.Pivot;
            // hands on the bar, feet hanging below along the swing line; the body rotates with the swing
            transform.position = new Vector3(piv.x + Mathf.Sin(swingA) * HandHeight, piv.y - Mathf.Cos(swingA) * HandHeight, 0f);
            Visual.transform.localRotation = Quaternion.Euler(0f, 0f, swingA * Mathf.Rad2Deg);
            if (Mathf.Abs(swingW) > 0.2f) Facing = swingW > 0 ? 1 : -1;
            if (buffer > 0f || locked)
            {
                // let go: the swing's tangential speed, plus a little hop
                float v = swingW * L;
                var rel = new Vector3(Mathf.Cos(swingA) * v * swing.release, Mathf.Sin(swingA) * v * swing.release + 3f, 0f);
                EndSwing();
                vel = rel;
                launchRise = true;
                airTop = Mathf.Abs(vel.x);
                buffer = 0f;
                Services.Audio.Play("jump");
                VFX.Burst(transform.position + Vector3.up * 1.6f, UIFactory.Theme.cyan, 8, "spark");
            }
        }

        void EndSwing()
        {
            if (State != Move.Swing) return;
            State = Move.Normal;
            Visual.transform.localRotation = gravSign < 0 ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
            Visual.Reach = 0f;
            swing = null;
            reattach = 0.35f;
            CC.enabled = true;
            Grounded = false;
            airTime = 0.2f;
        }

        void StartZip(Zipline z, float s)
        {
            State = Move.Zip;
            zip = z;
            zipS = s;
            zipV = Mathf.Max(3f, Vector3.Dot(vel, z.Direction));
            CC.enabled = false;
            vel = Vector3.zero;
            Grounded = false;
            Visual.Reach = 1f;
            Facing = z.Direction.x > 0 ? 1 : -1;
            z.Catch();
            Services.Audio.Play("dash", 0.4f);
        }

        void UpdateZip(float dt, float yIn, bool locked)
        {
            // gravity along the cable, a little friction in the trolley, a top speed so it stays readable
            zipV += gravity * 0.55f * -zip.Direction.y * dt;
            zipV = Mathf.Clamp(zipV - zipV * 0.08f * dt, 3f, zip.maxSpeed);
            zipS += zipV * dt;
            Vector3 hands = zip.Point(zipS);
            transform.position = hands - Vector3.up * HandHeight;
            Visual.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 5f) * 4f);
            zip.Ride(zipS);
            bool end = zipS >= zip.Length - 0.4f;
            if (end || buffer > 0f || yIn < -0.6f || locked)
            {
                var v = zip.Direction * Mathf.Min(zipV, sprintSpeed + 2f);   // let go at a sprint, not a cannonball
                EndZip();
                vel = new Vector3(v.x, (buffer > 0f ? 6f : 0f) + Mathf.Max(v.y, -2f), 0f);
                launchRise = true;
                airTop = Mathf.Abs(vel.x);
                buffer = 0f;
                Services.Audio.Play("jump", 0.6f);
            }
        }

        void EndZip()
        {
            if (State != Move.Zip) return;
            State = Move.Normal;
            Visual.transform.localRotation = Quaternion.identity;
            Visual.Reach = 0f;
            zip.Release();
            zip = null;
            reattach = 0.4f;
            CC.enabled = true;
            Grounded = false;
            airTime = 0.2f;
        }

        /// <summary>Leaves any parkour move at once (teleports, respawns, cutscenes).</summary>
        public void EndParkour()
        {
            if (State == Move.Slide) EndSlide();
            if (State == Move.Swing) EndSwing();
            if (State == Move.Zip) EndZip();
            if (State == Move.Mantle || State == Move.Vault) { State = Move.Normal; Visual.Reach = 0f; CC.enabled = true; }
            reattach = 0f;
        }

        // ------------------------------------------------------------------ springboards
        /// <summary>Launches the player upwards (springboards, bounce pads), clearing any parkour move.</summary>
        public void Launch(float height, float forward = 0f, bool ballisticFlight = false)
        {
            if (State != Move.Normal && State != Move.Slide) return;
            ballistic = ballisticFlight;
            launchRise = true;
            EndSlide();
            vel.y = Mathf.Sqrt(2f * gravity * height) * gravSign;
            if (Mathf.Abs(forward) > 0.01f) vel.x = forward;
            airTop = Mathf.Max(airTop, Mathf.Abs(vel.x));
            Grounded = false;
            coyote = 0f;
            buffer = 0f;
            airJumpsLeft = Services.Game.HasAbility("double_jump") ? 1 : 0;
            Visual.Trigger("Jump");
        }

        void ToggleShrink()
        {
            if (shrunk)
            {
                // need head room to grow
                if (Physics.SphereCast(transform.position + Vector3.up * 0.3f, 0.3f, Vector3.up * gravSign, out _, 1.4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Companion.Say("No room to grow here. Physics says no.");
                    return;
                }
            }
            shrunk = !shrunk;
            Services.Audio.Play(shrunk ? "shrink" : "grow");
            VFX.Burst(transform.position + Vector3.up * 0.5f, UIFactory.Theme.green, 16, "magic");
        }

        public void SetGravity(float sign)
        {
            if (Mathf.Approximately(sign, gravSign)) return;
            EndParkour();
            gravSign = sign;
            vel.y *= 0.3f;
            Visual.transform.localRotation = sign < 0 ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
            Visual.transform.localPosition = sign < 0 ? new Vector3(0, 1.6f, 0) : Vector3.zero;
            Services.Audio.Play("gravity_flip");
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y * gravSign > 0.5f)
            {
                var mp = hit.collider.GetComponentInParent<MovingPlatform>();
                if (mp != null) platform = mp;
                var pad = hit.collider.GetComponentInParent<Springboard>();
                if (pad != null && gravSign > 0) pad.Bounce(this);
                var crumble = hit.collider.GetComponentInParent<CrumblePlatform>();
                if (crumble != null) crumble.Step(this);
                var pillar = hit.collider.GetComponentInParent<AnswerPillar>();
                if (pillar != null && gravSign > 0) pillar.Land(this);
            }
        }

        // ------------------------------------------------------------------ interaction
        void UpdateInteraction(bool locked)
        {
            Interactable best = null;
            float bestD = float.MaxValue;
            Vector3 p = transform.position + Vector3.up * 0.8f;
            if (!locked)
            {
                foreach (var i in Interactable.All)
                {
                    if (i == null || !i.isActiveAndEnabled || !i.CanInteract(this)) continue;
                    float d = Vector2.Distance(p, i.Position);
                    if (d < i.radius && d < bestD) { best = i; bestD = d; }
                }
            }
            nearest = best;
            var hud = Services.UI.Hud;
            if (best != null)
            {
                hud.ShowPrompt($"<color=#40ebff>[E]</color> {best.PromptText}");
                if (Services.Input.InteractPressed) best.Interact(this);
            }
            else hud.ShowPrompt(null);
        }

        // ------------------------------------------------------------------ stability (HP)
        public void TakeDamage(float amount, Vector3 from, bool knockback = true)
        {
            if (amount <= 0) return;
            Stability = Mathf.Max(0, Stability - amount);
            GameEvents.RaiseStability(Stability, maxStability);
            Visual.Flash(1f);
            Visual.Trigger("Hit");
            invulnTimer = 0.4f;
            if (knockback)
            {
                EndParkour();
                float dir = Mathf.Sign(transform.position.x - from.x);
                if (dir == 0) dir = -Facing;
                vel = new Vector3(dir * 6f, 7f * gravSign, 0);
                sprintT = 0f;
            }
            Services.Audio.Play("player_hurt");
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.5f);
            GlitchFX.Pulse(0.45f, 0.35f);
        }

        public void Heal(float amount)
        {
            Stability = Mathf.Min(maxStability, Stability + amount);
            GameEvents.RaiseStability(Stability, maxStability);
        }

        public void RestoreFull() => Heal(maxStability);

        // ------------------------------------------------------------------ pits and checkpoints
        static readonly string[] FallLines =
        {
            "Gravity: 1. You: 0.",
            "I've reset you. You're welcome. That's the third time today. It's been ten minutes.",
            "Fun fact: falling is just very fast sitting down.",
            "Don't worry, the floor here is optional. Unfortunately so are you.",
            "Respawned! Same you. Slightly more embarrassed.",
        };

        // last spot with solid ground on both sides: a fall puts you back there instead of at the checkpoint
        Vector3 safeGround;
        bool hasSafeGround;
        float safeCheck;

        void RememberSafeGround()
        {
            if ((safeCheck -= Time.deltaTime) > 0f) return;
            safeCheck = 0.15f;
            var p = transform.position;
            bool Solid(float dx) => Physics.Raycast(p + new Vector3(dx, 0.5f, 0f), Vector3.down, out var hit, 1.3f, ~0, QueryTriggerInteraction.Ignore)
                                    && hit.collider.GetComponentInParent<MovingPlatform>() == null && hit.collider.GetComponentInParent<PlayerController>() == null
                                    && hit.collider.GetComponentInParent<CrumblePlatform>() == null && hit.collider.GetComponentInParent<AnswerPillar>() == null;
            if (Solid(0f) && Solid(-0.7f) && Solid(0.7f) && State == Move.Normal) { safeGround = p; hasSafeGround = true; }
        }

        public void FallRespawn()
        {
            if (respawning) return;
            StartCoroutine(RespawnRoutine(true, true));
        }

        public void RespawnAtCheckpoint(bool comedic) { if (!respawning) StartCoroutine(RespawnRoutine(comedic)); }

        IEnumerator RespawnRoutine(bool comedic, bool fall = false)
        {
            respawning = true;
            Services.Audio.Play("fall");
            if (Visual.Expression != null) Visual.Expression.Play(ExpressionSequence.Impossible);
            yield return Services.UI.Fade.FadeOut(0.35f, true);
            SetGravity(1f);
            Vector3 p = LastCheckpoint != null ? LastCheckpoint.SpawnPosition : SpawnPoint;
            if (fall && hasSafeGround) p = safeGround + Vector3.up * 0.1f;   // a slip: back to the edge you fell from, not the checkpoint
            Teleport(p);
            if (!fall) Heal(maxStability * 0.25f);
            yield return Services.UI.Fade.FadeIn(0.4f, true);
            respawning = false;
            if (comedic) Companion.Say(FallLines[Random.Range(0, FallLines.Length)]);
            Services.Game.Data.AddStat("falls");
        }
    }
}

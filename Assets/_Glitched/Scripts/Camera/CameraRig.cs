using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Glitched
{
    /// <summary>
    /// The 2.5D camera (Section 3.1): a perspective camera (FOV 40, ~18 units back) driven by a Cinemachine camera
    /// with a Position Composer, horizontal look-ahead, yaw sway, pitch on jumps, camera zones, encounter framing
    /// and impulse screen shake. Create it with CameraRig.Create().
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [Header("Framing")]
        public float baseDistance = 15f;   // a little closer: characters and the city read better
        public float baseFov = 40f;
        public float lookAhead = 2.5f;
        public float lookAheadDamping = 0.55f;
        public float heightOffset = 1.45f;

        public Camera Cam { get; private set; }
        public CinemachineCamera Vcam { get; private set; }
        public CinemachineCamera CutsceneCam { get; private set; }
        public CinemachinePositionComposer Composer { get; private set; }
        public YawSway Sway { get; private set; }
        public Transform Proxy { get; private set; }
        float lastGroundY = -999f;
        public Transform Target { get; set; }

        CinemachineImpulseSource impulse;
        readonly List<CameraZone> zones = new List<CameraZone>();
        Transform encounterTarget;
        float encounterWeight, panelWeight;
        bool panelOpen, quizOpen;
        float quizWeight;
        float lookX, lookVel;
        Vector3 lastTargetPos;
        float smoothVX, smoothVY;
        float punch, punchVel;
        float zoneDistance, zoneFov, zoneYaw, zoneHeight;
        float extraDistance;   // used by bosses
        bool cutscene;

        public static CameraRig Create(Transform target)
        {
            if (Instance != null) { Instance.Target = target; Instance.Snap(); return Instance; }
            var root = new GameObject("CameraRig");
            var rig = root.AddComponent<CameraRig>();
            rig.Build(target);
            return rig;
        }

        void Build(Transform target)
        {
            Instance = this;
            Target = target;
            // remove any stray main camera from the scene
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.CompareTag("MainCamera") && c.transform.root != transform) Destroy(c.gameObject);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.fieldOfView = baseFov;
            Cam.nearClipPlane = 0.3f;
            Cam.farClipPlane = 900f;
            Cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();
            var brain = camGo.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);

            var proxyGo = new GameObject("CameraTarget");
            proxyGo.transform.SetParent(transform, false);
            Proxy = proxyGo.transform;

            var vGo = new GameObject("GameplayCam");
            vGo.transform.SetParent(transform, false);
            Vcam = vGo.AddComponent<CinemachineCamera>();
            Vcam.Target.TrackingTarget = Proxy;
            var lens = LensSettings.Default;
            lens.FieldOfView = baseFov; lens.NearClipPlane = 0.3f; lens.FarClipPlane = 900f;
            Vcam.Lens = lens;
            Composer = vGo.AddComponent<CinemachinePositionComposer>();
            Composer.CameraDistance = baseDistance;
            Composer.Damping = new Vector3(0.35f, 0.45f, 0.4f);
            var comp = ScreenComposerSettings.Default;
            comp.DeadZone.Enabled = true;
            comp.DeadZone.Size = new Vector2(0.04f, 0.08f);
            Composer.Composition = comp;
            Sway = vGo.AddComponent<YawSway>();
            vGo.AddComponent<CinemachineImpulseListener>();

            var cGo = new GameObject("CutsceneCam");
            cGo.transform.SetParent(transform, false);
            CutsceneCam = cGo.AddComponent<CinemachineCamera>();
            CutsceneCam.Lens = lens;
            CutsceneCam.Priority = 0;
            CutsceneCam.enabled = false;

            impulse = gameObject.AddComponent<CinemachineImpulseSource>();
            impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.ImpulseDefinition.ImpulseDuration = 0.22f;

            gameObject.AddComponent<PostFXController>();
            Services.Graphics.ConfigureCamera(Cam);
            GameEvents.TierChanged += OnTier;
            GameEvents.SettingsChanged += OnSettings;
            Snap();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameEvents.TierChanged -= OnTier;
            GameEvents.SettingsChanged -= OnSettings;
        }

        void OnTier(GraphicsTier t) => Services.Graphics.ConfigureCamera(Cam);
        void OnSettings() => Services.Graphics.ConfigureCamera(Cam);

        /// <summary>Jumps the camera to the target with no damping (after spawning / respawning).</summary>
        public void Snap()
        {
            if (Target == null) return;
            lastTargetPos = Target.position;
            lookX = 0;
            Proxy.position = Target.position + Vector3.up * heightOffset;
            Vcam.PreviousStateIsValid = false;
            Vcam.ForceCameraPosition(Proxy.position - Vector3.forward * baseDistance, Quaternion.identity);
        }

        public void PushZone(CameraZone z) { if (!zones.Contains(z)) zones.Add(z); }
        public void PopZone(CameraZone z) => zones.Remove(z);

        public void FrameEncounter(Transform enemy) => encounterTarget = enemy;
        public void EndEncounter() { encounterTarget = null; panelOpen = false; quizOpen = false; }
        public void SetPanelOpen(bool open) => panelOpen = open;
        /// <summary>The quiz card sits at the top of the screen: frame the fight in the lower half.</summary>
        public void SetQuizOpen(bool open) => quizOpen = open;
        public void SetExtraDistance(float d) => extraDistance = d;
        public float ExtraDistance => extraDistance;

        /// <summary>Screen shake through the Cinemachine impulse system (respects the Screen Shake setting).</summary>
        public void Shake(float strength)
        {
            if (!Services.Graphics.ShakeEnabled || impulse == null) return;
            impulse.GenerateImpulseWithVelocity(new Vector3(Random.Range(-1f, 1f), Random.Range(-0.4f, 1f), 0).normalized * strength);
        }

        /// <summary>Camera punch-in (CRITICAL hits): briefly narrows the field of view.</summary>
        public void PunchIn(float degrees)
        {
            punch = Mathf.Max(punch, degrees);
        }

        public void SetCutscene(bool on)
        {
            cutscene = on;
            CutsceneCam.enabled = on;
            CutsceneCam.Priority = on ? 50 : 0;
        }

        void LateUpdate()
        {
            if (Target == null || cutscene) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 tp = Target.position;
            Vector3 vel = (tp - lastTargetPos) / dt;
            lastTargetPos = tp;
            smoothVX = Mathf.Lerp(smoothVX, vel.x, 1f - Mathf.Exp(-dt * 6f));
            smoothVY = Mathf.Lerp(smoothVY, vel.y, 1f - Mathf.Exp(-dt * 4f));

            // look-ahead in the movement direction
            float desiredLook = Mathf.Abs(smoothVX) > 0.6f ? Mathf.Sign(smoothVX) * lookAhead : lookX * 0.98f;
            lookX = Mathf.SmoothDamp(lookX, desiredLook, ref lookVel, lookAheadDamping);

            // zones (highest priority wins, blended smoothly)
            CameraZone zone = null;
            foreach (var z in zones) if (z != null && (zone == null || z.priority >= zone.priority)) zone = z;
            float zb = 1f - Mathf.Exp(-dt * 3f / Mathf.Max(0.1f, zone != null ? zone.blendTime : 1f));
            zoneDistance = Mathf.Lerp(zoneDistance, zone != null ? zone.distance - baseDistance : 0f, zb);
            zoneFov = Mathf.Lerp(zoneFov, zone != null ? zone.fov - baseFov : 0f, zb);
            zoneYaw = Mathf.Lerp(zoneYaw, zone != null ? zone.yawOffset : 0f, zb);
            zoneHeight = Mathf.Lerp(zoneHeight, zone != null ? zone.heightOffset : 0f, zb);

            // encounter framing: both characters in view, above the challenge panel
            encounterWeight = Mathf.MoveTowards(encounterWeight, encounterTarget != null ? 1f : 0f, dt * 2.2f);
            panelWeight = Mathf.MoveTowards(panelWeight, panelOpen ? 1f : 0f, dt * 2.5f);
            Vector3 focus = tp + new Vector3(lookX * (1f - encounterWeight), heightOffset + zoneHeight, 0f);
            float sep = 0f;
            if (encounterTarget != null)
            {
                Vector3 mid = (tp + encounterTarget.position) * 0.5f + Vector3.up * (heightOffset + 0.6f);
                focus = Vector3.Lerp(focus, mid, encounterWeight);
                sep = Mathf.Abs(encounterTarget.position.x - tp.x);
            }
            focus.y -= panelWeight * 2.4f;
            quizWeight = Mathf.MoveTowards(quizWeight, quizOpen ? 1f : 0f, dt * 2.5f);
            focus.y += quizWeight * 2.1f;
            focus.z = 0f;
            // don't dive into pits after a falling player: hold above the last ground they stood on
            var pc = PlayerController.Instance;
            if (pc != null && pc.Grounded) lastGroundY = tp.y;
            if (encounterTarget == null) focus.y = Mathf.Max(focus.y, lastGroundY + heightOffset - 1.5f);
            Proxy.position = focus;

            float dist = baseDistance + zoneDistance + extraDistance + encounterWeight * Mathf.Clamp(sep * 0.45f - 1.5f, -2f, 6f) + panelWeight * 2.5f + quizWeight * 1.5f;
            Composer.CameraDistance = dist;
            punch = Mathf.SmoothDamp(punch, 0f, ref punchVel, 0.18f, Mathf.Infinity, Time.unscaledDeltaTime);
            var lens = Vcam.Lens;
            lens.FieldOfView = Mathf.Clamp(baseFov + zoneFov - punch, 20f, 70f);
            Vcam.Lens = lens;

            Sway.velocityX = encounterTarget != null ? 0 : smoothVX;
            Sway.velocityY = smoothVY;
            Sway.yawOffset = zoneYaw;

            // player screen position for foreground (L0) dither fade
            Vector3 vp = Cam.WorldToViewportPoint(tp + Vector3.up * 0.9f);
            Shader.SetGlobalVector("_PlayerScreenPos", new Vector4(vp.x, vp.y, 0.09f, 1f));
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;

namespace Glitched
{
    /// <summary>
    /// Reflections for a level: one probe covering everything (sky, far city), plus a chain of box-projected local
    /// probes along the play line (High and Ultra), so the street, puddles, cars and shop windows reflect what is
    /// actually next to them, in the right place. All are rendered after load, a few faces per frame, and again when
    /// the region is restored. Resolution follows the graphics tier.
    /// </summary>
    public class SceneReflection : MonoBehaviour
    {
        /// <summary>The level's play range along x (set by LevelBuilder).</summary>
        public float minX, maxX;
        const float Spacing = 26f;
        ReflectionProbe probe;
        readonly System.Collections.Generic.List<ReflectionProbe> locals = new System.Collections.Generic.List<ReflectionProbe>();
        float lastCorruption = -1f;

        void Start()
        {
            probe = gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.size = new Vector3(4000, 1200, 4000);
            probe.boxProjection = false;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.importance = 1;
            probe.intensity = 1f;
            probe.farClipPlane = 1500f;
            probe.shadowDistance = 0f;
            if (maxX > minX)
                for (float x = minX + Spacing * 0.5f; x < maxX + Spacing * 0.5f; x += Spacing)
                {
                    var go = new GameObject("LocalReflection");
                    go.transform.SetParent(transform, false);
                    go.transform.position = new Vector3(x, 2.4f, -1f);   // at the player's eye on the play line
                    var lp = go.AddComponent<ReflectionProbe>();
                    lp.mode = ReflectionProbeMode.Realtime;
                    lp.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                    lp.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                    lp.boxProjection = true;
                    // the street box: the pavement in front, the facades behind, up to the first floors
                    lp.size = new Vector3(Spacing + 8f, 26f, 34f);
                    lp.center = new Vector3(0f, 10.6f, 9f);
                    lp.blendDistance = 4f;
                    lp.importance = 2;
                    lp.clearFlags = ReflectionProbeClearFlags.Skybox;
                    lp.farClipPlane = 400f;
                    lp.shadowDistance = 0f;
                    lp.hdr = true;
                    locals.Add(lp);
                }
            Apply(Services.Graphics != null ? Services.Graphics.Current?.tier ?? GraphicsTier.Medium : GraphicsTier.Medium);
            GameEvents.TierChanged += Apply;
            Invoke(nameof(Render), 0.3f);   // after the sky and corruption state are set up
        }

        void OnDestroy() => GameEvents.TierChanged -= Apply;

        void Apply(GraphicsTier t)
        {
            if (probe == null) return;
            probe.resolution = t >= GraphicsTier.Ultra ? 512 : t >= GraphicsTier.High ? 256 : t >= GraphicsTier.Medium ? 128 : 64;
            foreach (var lp in locals)
            {
                lp.enabled = t >= GraphicsTier.High;
                lp.resolution = t >= GraphicsTier.Ultra ? 256 : 128;
            }
            Render();
        }

        void Render()
        {
            if (probe == null) return;
            lastCorruption = Services.Corruption != null ? Services.Corruption.Value : 0f;
            probe.RenderProbe();
            if (localRoutine != null) StopCoroutine(localRoutine);
            localRoutine = StartCoroutine(RenderLocals());
        }

        Coroutine localRoutine;
        System.Collections.IEnumerator RenderLocals()
        {
            // one probe every few frames (each renders its faces over frames): no hitch on load
            foreach (var lp in locals)
            {
                if (lp == null || !lp.enabled) continue;
                int id = lp.RenderProbe();
                for (int f = 0; f < 30 && !lp.IsFinishedRendering(id); f++) yield return null;
            }
            localRoutine = null;
        }

        void Update()
        {
            // re-render after a big change (restoration), at most every few seconds
            if (Services.Corruption == null || Time.frameCount % 120 != 0) return;
            if (Mathf.Abs(Services.Corruption.Value - lastCorruption) > 0.25f) Render();
        }
    }
}

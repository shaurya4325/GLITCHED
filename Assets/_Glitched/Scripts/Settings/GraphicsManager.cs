using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Glitched
{
    /// <summary>
    /// Applies a GraphicsTierSO plus the player's overrides: URP asset, render scale, MSAA, shadows, fps cap,
    /// LOD bias, texture quality, glitch shader variant and post-processing. Switchable live from Settings.
    /// </summary>
    public class GraphicsManager : MonoBehaviour
    {
        public GraphicsTierSO Current { get; private set; }
        public GraphicsTier Tier => Current != null ? Current.tier : GraphicsTier.Medium;
        UniversalRenderPipelineAsset runtimeAsset;

        public void Init()
        {
            var s = Services.Settings.Data;
            if (!s.tierChosen)
            {
                s.tier = DeviceProfiler.GuessTier();
                Debug.Log($"[Graphics] First launch: device profile suggests tier {s.tier} ({SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize} MB VRAM, {SystemInfo.systemMemorySize} MB RAM)");
            }
            ApplyTier(s.tier);
            // capture runs pick their own window size on the command line
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-capture") < 0 && System.Array.IndexOf(args, "-screen-fullscreen") < 0) ApplyDisplay();
        }

        /// <summary>Fullscreen at the monitor's own resolution, or a 1600x900 window (smaller on small screens).</summary>
        public void ApplyDisplay()
        {
            if (Application.isMobilePlatform || Application.isEditor) return;
            var r = Screen.currentResolution;
            if (Services.Settings.Data.fullscreen)
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            else
            {
                int w = Mathf.Min(1600, r.width - 80), h = Mathf.Min(900, r.height - 120);
                Screen.SetResolution(w, h, FullScreenMode.Windowed);
            }
        }

        public void ApplyTier(GraphicsTier tier)
        {
            var s = Services.Settings.Data;
            s.tier = tier;
            Current = Services.Db.Tier(tier);
            if (Current == null) { Debug.LogWarning("[Graphics] No GraphicsTierSO found."); return; }

            if (QualitySettings.count >= 5) QualitySettings.SetQualityLevel((int)tier, true);

            var source = (Current.urpAsset ?? QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (source != null)
            {
                if (runtimeAsset != null) Destroy(runtimeAsset);
                runtimeAsset = Instantiate(source);
                runtimeAsset.name = source.name + " (runtime)";
                runtimeAsset.renderScale = s.renderScale > 0 ? s.renderScale : Current.renderScale;
                runtimeAsset.msaaSampleCount = Mathf.Max(1, Current.msaa);
                bool shadows = Current.realtimeShadows && s.shadows;
                runtimeAsset.shadowDistance = shadows ? Current.shadowDistance : 0f;
                runtimeAsset.shadowCascadeCount = Mathf.Clamp(Current.shadowCascades, 1, 4);
                runtimeAsset.mainLightShadowmapResolution = Current.shadowResolution;
                runtimeAsset.additionalLightsShadowmapResolution = tier >= GraphicsTier.Ultra ? 4096 : tier >= GraphicsTier.High ? 2048 : 1024;
                runtimeAsset.supportsHDR = tier >= GraphicsTier.Medium;
                QualitySettings.renderPipeline = runtimeAsset;
            }

            int cap = s.fpsCap >= 0 ? s.fpsCap : Current.fpsCap;
            if (cap <= 0) { QualitySettings.vSyncCount = 1; Application.targetFrameRate = -1; }
            else { QualitySettings.vSyncCount = 0; Application.targetFrameRate = cap; }

            QualitySettings.lodBias = Current.lodBias;
            QualitySettings.globalTextureMipmapLimit = Current.textureMipLimit;
            QualitySettings.realtimeReflectionProbes = true;   // SceneReflection renders one probe per level, once
            QualitySettings.softParticles = tier >= GraphicsTier.High;

            if (Current.simpleGlitchShader) Shader.EnableKeyword("GLITCH_LOW"); else Shader.DisableKeyword("GLITCH_LOW");
            Shader.SetGlobalFloat("_HeightFogOn", Current.heightFog ? 1f : 0f);

            var feature = Services.Db.fullscreenGlitchFeature;
            if (feature != null) feature.SetActive(Current.fullscreenGlitch && PostEnabled);
            var ssao = Services.Db.ssaoFeature;
            if (ssao != null) ssao.SetActive(tier >= GraphicsTier.High && PostEnabled);
            WorldLight.SetAll(tier >= GraphicsTier.Medium);

            Services.Settings.SaveSettings();
            GameEvents.RaiseTier(tier);
        }

        // Effective values (tier x player settings)
        public bool PostEnabled => Current != null && Current.postProcessing && Services.Settings.Data.postProcessing;
        public bool ShadowsEnabled => Current != null && Current.realtimeShadows && Services.Settings.Data.shadows;
        public float ParticleMultiplier => (Current != null ? Current.particles : 1f) * Mathf.Clamp01(Services.Settings.Data.particles);
        public float SwayDegrees => (Current != null ? Current.cameraYawSway : 3f) * Mathf.Clamp01(Services.Settings.Data.cameraSway);
        public bool ShakeEnabled => Services.Settings.Data.screenShake;
        public bool UIOverlayGlitch => Current == null || Current.glitchAsUIOverlay || !PostEnabled;

        public float DensityMultiplier
        {
            get
            {
                float d = Current != null ? Current.trafficAndCrowd : 1f;
                switch (Services.Settings.Data.backgroundDetail)
                {
                    case BackgroundDetail.Low: d *= 0.4f; break;
                    case BackgroundDetail.Medium: d *= 0.7f; break;
                }
                return d;
            }
        }

        public bool LayerActive(DepthLayer layer)
        {
            if (Current == null) return true;
            var bg = Services.Settings.Data.backgroundDetail;
            switch (layer)
            {
                case DepthLayer.L0_Foreground: return Current.layerL0 && bg != BackgroundDetail.Low;
                case DepthLayer.L3_Mid: return Current.layerL3 && bg != BackgroundDetail.Low;
                case DepthLayer.L5_SkyEvents: return Current.layerL5Events && bg == BackgroundDetail.High;
                case DepthLayer.L2_NearDetail: return !Current.simpleL2;
                default: return true;
            }
        }

        /// <summary>Configures a gameplay camera for the current tier.</summary>
        public void ConfigureCamera(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = PostEnabled;
                data.renderShadows = ShadowsEnabled;
                data.antialiasing = Tier == GraphicsTier.Low ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None;
                data.stopNaN = false;
                data.dithering = Tier >= GraphicsTier.High;
            }
            cam.allowHDR = Tier >= GraphicsTier.Medium;
            cam.allowMSAA = Current != null && Current.msaa > 1;
        }
    }

    /// <summary>The 2.5D depth layers (Section 3.1).</summary>
    public enum DepthLayer { L0_Foreground, L1_Gameplay, L2_Near, L2_NearDetail, L3_Mid, L4_Far, L5_Sky, L5_SkyEvents }
}

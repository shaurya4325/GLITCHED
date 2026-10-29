using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>Debug overlay (F3 on PC, 3-finger tap on Android): fps, tier, draw calls, triangles and memory.</summary>
    public class DebugOverlay : MonoBehaviour
    {
        TextMeshProUGUI text;
        GameObject panel;
        ProfilerRecorder drawCalls, tris, batches, sysMem, gcMem;
        float refresh;
        readonly StringBuilder sb = new StringBuilder(512);

        public void Build()
        {
            var rt = (RectTransform)transform;
            UIFactory.Place(rt, new Vector2(0, 0), new Vector2(16, 16), new Vector2(560, 300), new Vector2(0, 0));
            var p = UIFactory.Panel(rt, new Color(0, 0, 0, 0.78f), 10, "DebugPanel", false);
            UIFactory.Stretch(p.rectTransform);
            panel = p.gameObject;
            text = UIFactory.Text(p.transform, "", 22, new Color(0.6f, 1f, 0.7f), TextAlignmentOptions.TopLeft);
            text.font = UIFactory.MonoFont;
            UIFactory.Stretch(text.rectTransform, 14, 10, 14, 10);
            panel.SetActive(false);
        }

        public void Toggle()
        {
            bool on = !panel.activeSelf;
            panel.SetActive(on);
            if (on)
            {
                drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
                tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
                sysMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
                gcMem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            }
            else Dispose();
        }

        void Dispose()
        {
            drawCalls.Dispose(); batches.Dispose(); tris.Dispose(); sysMem.Dispose(); gcMem.Dispose();
        }

        void OnDestroy() => Dispose();

        void Update()
        {
            if (!panel.activeSelf) return;
            refresh -= Time.unscaledDeltaTime;
            if (refresh > 0) return;
            refresh = 0.25f;
            sb.Clear();
            var g = Services.Graphics;
            sb.Append($"FPS {FpsMonitor.SmoothedFps:0}  ({FpsMonitor.FrameMs:0.0} ms)\n");
            sb.Append($"TIER {g.Tier}   scale {(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)?.renderScale ?? 1f:0.00}\n");
            sb.Append($"DRAW CALLS {(drawCalls.Valid ? drawCalls.LastValue : -1)}   SETPASS {(batches.Valid ? batches.LastValue : -1)}\n");
            sb.Append($"TRIS {(tris.Valid ? tris.LastValue / 1000 : -1)}k\n");
            sb.Append($"MEM sys {(sysMem.Valid ? sysMem.LastValue / (1024 * 1024) : SystemInfo.systemMemorySize)} MB  gc {(gcMem.Valid ? gcMem.LastValue / (1024 * 1024) : 0)} MB\n");
            sb.Append($"GPU {SystemInfo.graphicsDeviceName}\n");
            sb.Append($"SCENE {SceneManager.GetActiveScene().name}   CORRUPTION {Services.Corruption.Value:0.00}\n");
            sb.Append($"{Screen.width}x{Screen.height}  {Application.platform}");
            text.text = sb.ToString();
        }
    }
}

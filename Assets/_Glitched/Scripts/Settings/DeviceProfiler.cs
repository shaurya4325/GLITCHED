using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Picks a starting graphics tier from the hardware (Section 4.2).
    /// Intel UHD/HD graphics and low-RAM Android phones start at Low. The 3-second benchmark refines the choice.
    /// </summary>
    public static class DeviceProfiler
    {
        public static GraphicsTier GuessTier()
        {
            string gpu = (SystemInfo.graphicsDeviceName ?? "").ToLowerInvariant();
            int vram = SystemInfo.graphicsMemorySize;
            int ram = SystemInfo.systemMemorySize;
            int cores = SystemInfo.processorCount;
            int shaderLevel = SystemInfo.graphicsShaderLevel;

            if (shaderLevel < 35) return GraphicsTier.Potato;

            if (Application.isMobilePlatform)
            {
                bool weakGpu = gpu.Contains("mali-g5") || gpu.Contains("mali-t") || gpu.Contains("powervr") || gpu.Contains("ge8320") || gpu.Contains("adreno (tm) 5") || gpu.Contains("adreno (tm) 6") && ram < 4000;
                if (ram <= 3200) return GraphicsTier.Potato;
                if (ram <= 4600 || weakGpu) return GraphicsTier.Low;
                if (ram <= 6500) return GraphicsTier.Medium;
                return GraphicsTier.High;
            }

            bool integrated = gpu.Contains("intel") && (gpu.Contains("uhd") || gpu.Contains("hd graphics") || gpu.Contains("iris")) || gpu.Contains("vega 8") || gpu.Contains("radeon(tm) graphics");
            if (integrated)
            {
                if (gpu.Contains("iris xe") && ram >= 12000) return GraphicsTier.Medium;
                return GraphicsTier.Low;
            }
            if (ram < 6000 || cores <= 2) return GraphicsTier.Low;
            // any RTX card (even a 4-6 GB laptop RTX 3050) runs the realistic world comfortably at Ultra
            if ((gpu.Contains("rtx") || gpu.Contains("radeon rx 6") || gpu.Contains("radeon rx 7")) && vram >= 3800) return GraphicsTier.Ultra;
            if (vram >= 7500 && cores >= 8) return GraphicsTier.Ultra;
            if (vram >= 3500) return GraphicsTier.High;
            if (vram >= 1800) return GraphicsTier.Medium;
            return GraphicsTier.Low;
        }

        /// <summary>Adjusts the guess with the benchmark's average fps.</summary>
        public static GraphicsTier Refine(GraphicsTier guess, float averageFps)
        {
            int t = (int)guess;
            if (averageFps < 22f) t -= 1;
            if (averageFps < 14f) t -= 1;
            if (averageFps > 110f && guess < GraphicsTier.High) t += 1;
            return (GraphicsTier)Mathf.Clamp(t, 0, 4);
        }
    }
}

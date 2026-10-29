using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A real bit-crush + sample-rate-reduction filter (runs on the audio thread).
    /// The music gets 0.5 s bursts of this when corruption spikes, then snaps back.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BitCrusher : MonoBehaviour
    {
        [Range(0, 1)] public float amount;      // 0 = off
        volatile float shared;
        float hold0, hold1;
        int counter;

        void Update() => shared = amount;

        void OnAudioFilterRead(float[] data, int channels)
        {
            float a = shared;
            if (a <= 0.001f) return;
            float levels = Mathf.Lerp(256f, 6f, a);
            int step = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 14f, a)));
            for (int i = 0; i < data.Length; i += channels)
            {
                if (counter++ % step == 0)
                {
                    hold0 = Mathf.Round(data[i] * levels) / levels;
                    if (channels > 1) hold1 = Mathf.Round(data[i + 1] * levels) / levels;
                }
                data[i] = hold0;
                if (channels > 1) data[i + 1] = hold1;
            }
        }
    }
}

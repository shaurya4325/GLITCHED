using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// "The level is deleting itself behind you": while the player runs from x0 to x1, blasts chase them a few metres
    /// behind (they never hurt; they make you run). Used on the parkour stretches.
    /// </summary>
    public class ChaseBlasts : MonoBehaviour
    {
        public float x0, x1;
        public BlastStyle style;
        public string startLine = "RUN! The level is deleting itself behind us!";
        bool started, finished;
        float t, lastX;

        void Update()
        {
            var p = PlayerController.Instance;
            if (p == null || finished) return;
            float x = p.transform.position.x;
            if (!started) { if (x > x0 && x < x0 + 6f) { started = true; lastX = x; Companion.Say(startLine, 3f); Services.Audio.Play("boom", 0.6f); } return; }
            if (x >= x1) { finished = true; Blast.At(new Vector3(x - 7f, GroundAt(x - 7f) + 0.8f, 2.5f), 3f, style); return; }
            if (p.Locked) return;
            if ((t -= Time.deltaTime) > 0f) return;
            // only while the player keeps moving forward
            if (x < lastX + 0.6f) { t = 0.3f; return; }
            lastX = x;
            t = Random.Range(0.45f, 0.75f);
            float bx = x - Random.Range(6.5f, 9f);
            Blast.At(new Vector3(bx, GroundAt(bx) + Random.Range(0.3f, 2f), Random.Range(1.2f, 3.2f)), Random.Range(1.2f, 2.2f), style, true, Random.value < 0.4f, Random.value < 0.5f);
        }

        static float GroundAt(float x) => Physics.Raycast(new Vector3(x, 30f, 0f), Vector3.down, out var h, 60f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : 0f;
    }
}

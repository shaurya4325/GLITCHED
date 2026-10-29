using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>A prop beside the path (a kiosk, a pressure tank, a transformer, a specimen pod) that sparks, then blows.</summary>
    public class PropBlast : MonoBehaviour
    {
        public float triggerX;
        public BlastStyle style;
        public Transform prop;
        bool done;

        void Update()
        {
            if (done) return;
            var p = PlayerController.Instance;
            if (p == null || p.transform.position.x < triggerX || p.Locked) return;   // not while reading a lesson or talking: it should be seen
            done = true;
            StartCoroutine(Go());
        }

        IEnumerator Go()
        {
            // warning: it fizzes and rattles
            Vector3 home = prop != null ? prop.localPosition : Vector3.zero;
            for (float t = 0f; t < 0.7f; t += Time.deltaTime)
            {
                if (prop != null) prop.localPosition = home + Random.insideUnitSphere * 0.04f;
                if (Random.value < 0.2f) VFX.Burst(transform.position + Vector3.up * Random.Range(0.5f, 2f), Color.white, 6, "spark", 5f, 0.12f, 0.4f);
                yield return null;
            }
            Blast.At(transform.position + Vector3.up * 1.2f, 2f, style, true, true);
            if (prop != null)
            {
                // a charred, toppled wreck stays behind, still burning
                prop.localPosition = home + Vector3.down * 0.3f;
                prop.localRotation = Quaternion.Euler(Random.Range(-14f, 14f), 0f, Random.Range(10f, 22f) * (Random.value < 0.5f ? -1 : 1));
                foreach (var r in prop.GetComponentsInChildren<Renderer>())
                {
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.09f));
                    r.SetPropertyBlock(mpb);
                }
            }
            Blast.Fire(transform.position + Vector3.up * 0.8f, 1f, style, 18f);
        }
    }
}

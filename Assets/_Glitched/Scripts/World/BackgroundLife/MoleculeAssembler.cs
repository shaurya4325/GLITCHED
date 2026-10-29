using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Molecules in the Chemical Chaos sky that assemble and fall apart (they stay together once restored).</summary>
    public class MoleculeAssembler : MonoBehaviour
    {
        readonly List<Transform> atoms = new List<Transform>();
        readonly List<Vector3> basePos = new List<Vector3>();
        readonly List<Vector3> drift = new List<Vector3>();
        float seed;

        void Start()
        {
            seed = Random.value * 10f;
            foreach (Transform c in transform)
            {
                atoms.Add(c); basePos.Add(c.localPosition);
                drift.Add(Random.onUnitSphere * Random.Range(2f, 5f));
            }
        }

        void Update()
        {
            float c = Services.Corruption.Value;
            float apart = Mathf.Clamp01(Mathf.Sin(Time.time * 0.4f + seed) * 1.4f) * c;
            for (int i = 0; i < atoms.Count; i++) atoms[i].localPosition = basePos[i] + drift[i] * apart;
            transform.Rotate(0, 12f * Time.deltaTime, 6f * Time.deltaTime, Space.Self);
        }
    }
}

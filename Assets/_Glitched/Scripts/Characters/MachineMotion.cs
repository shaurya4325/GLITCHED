using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Procedural animation for the generated machine enemies (drones, robot bugs, liquid-metal slimes, delivery
    /// bots): spinning rotors, walking legs, rolling wheels, wobbling bodies, a pulsing eye, plus Attack / Hit reactions
    /// called by Enemy.
    /// </summary>
    public class MachineMotion : MonoBehaviour
    {
        public List<Transform> rotors = new List<Transform>();
        public List<Transform> legs = new List<Transform>();
        public List<Transform> wheels = new List<Transform>();
        public Transform body;
        public Transform blob;
        public Renderer eye;
        public float rotorSpeed = 1800f, legSpeed = 7f, legSwing = 22f, bobAmount = 0.03f;

        float attack, hit;
        Vector3 bodyBase, blobBase;
        readonly List<Quaternion> legBase = new List<Quaternion>();
        MaterialPropertyBlock mpb;

        void Start()
        {
            if (body != null) bodyBase = body.localPosition;
            if (blob != null) blobBase = blob.localScale;
            foreach (var l in legs) legBase.Add(l != null ? l.localRotation : Quaternion.identity);
            mpb = new MaterialPropertyBlock();
        }

        public void Attack() => attack = 1f;
        public void Hit() => hit = 1f;

        void Update()
        {
            float t = Time.time, dt = Time.deltaTime;
            attack = Mathf.MoveTowards(attack, 0f, dt * 2.5f);
            hit = Mathf.MoveTowards(hit, 0f, dt * 4f);
            foreach (var r in rotors) if (r != null) r.Rotate(0f, rotorSpeed * dt, 0f, Space.Self);
            foreach (var w in wheels) if (w != null) w.Rotate(240f * dt, 0f, 0f, Space.Self);   // axle along x
            for (int i = 0; i < legs.Count; i++)
            {
                if (legs[i] == null) continue;
                float phase = (i % 2 == 0 ? 0f : Mathf.PI) + i * 0.4f;
                legs[i].localRotation = legBase[i] * Quaternion.Euler(Mathf.Sin(t * legSpeed + phase) * legSwing, 0f, 0f);
            }
            if (body != null)
            {
                float bob = Mathf.Sin(t * legSpeed * 2f) * bobAmount;
                float lunge = Mathf.Sin(attack * Mathf.PI) * 0.25f;
                float shake = hit > 0 ? Mathf.Sin(t * 70f) * 0.03f * hit : 0f;
                body.localPosition = bodyBase + new Vector3(shake, bob, lunge);
                body.localRotation = Quaternion.Euler(-lunge * 40f, 0f, shake * 120f);
            }
            if (blob != null)
            {
                float w = Mathf.Sin(t * 3.1f) * 0.05f + attack * 0.25f - hit * 0.15f;
                blob.localScale = Vector3.Scale(blobBase, new Vector3(1f - w * 0.5f, 1f + w, 1f - w * 0.5f));
            }
            if (eye != null)
            {
                eye.GetPropertyBlock(mpb);
                mpb.SetFloat("_HitFlash", Mathf.Max(hit, (Mathf.Sin(t * 2.2f) * 0.5f + 0.5f) * 0.15f + attack * 0.5f));
                eye.SetPropertyBlock(mpb);
            }
        }
    }
}

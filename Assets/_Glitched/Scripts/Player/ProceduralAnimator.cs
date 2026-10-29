using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Code-driven animation for primitive placeholder characters: idle breathing, run cycle, jump/fall poses,
    /// hit reactions, victory, defeat, talking, dashing, casting and frozen (glitched NPCs).
    /// Real models use their Animator instead.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        public Transform root, hips, head, armL, armR, legL, legR;
        [HideInInspector] public float speed01;
        [HideInInspector] public bool grounded = true;
        [HideInInspector] public float verticalSpeed;
        [HideInInspector] public bool talking;
        [HideInInspector] public bool frozen;
        [HideInInspector] public bool dashing;
        [HideInInspector] public bool sitting;
        public string idleStyle = "fidget";

        float phase, hitT, victoryT, defeatT, castT, landT;
        Vector3 hipsBase;
        bool init;

        public void Setup(ProceduralRig rig)
        {
            root = rig.root; hips = rig.hips; head = rig.head; armL = rig.armL; armR = rig.armR; legL = rig.legL; legR = rig.legR;
            hipsBase = hips != null ? hips.localPosition : Vector3.zero;
            init = true;
        }

        public void Trigger(string name)
        {
            switch (name)
            {
                case "Hit": hitT = 0.45f; break;
                case "Victory": victoryT = 1.6f; break;
                case "Defeat": defeatT = 1.8f; break;
                case "Cast": castT = 1.2f; break;
                case "Land": landT = 0.18f; break;
            }
        }

        void Update()
        {
            if (!init || hips == null) return;
            if (frozen) return;   // frozen mid-step: glitched NPCs keep their last pose
            float dt = Time.deltaTime;
            hitT -= dt; victoryT -= dt; defeatT -= dt; castT -= dt; landT -= dt;
            float t = Time.time;
            phase += dt * Mathf.Lerp(2f, 11f, speed01);
            float swing = Mathf.Sin(phase);

            Vector3 armLR = Vector3.zero, armRR = Vector3.zero, legLR = Vector3.zero, legRR = Vector3.zero, headR = Vector3.zero, hipsR = Vector3.zero;
            Vector3 hipsPos = hipsBase;

            if (sitting)
            {
                legLR = new Vector3(-85, 0, 0); legRR = new Vector3(-85, 0, 0);
                armLR = new Vector3(-50 + Mathf.Sin(t * 14) * 8, 0, 8); armRR = new Vector3(-50 + Mathf.Sin(t * 14 + 1.5f) * 8, 0, -8);
                hipsPos.y -= 0.32f;
                headR = new Vector3(8 + Mathf.Sin(t * 0.8f) * 3, Mathf.Sin(t * 0.5f) * 6, 0);
            }
            else if (!grounded)
            {
                if (verticalSpeed > 0) { legLR = new Vector3(-55, 0, 0); legRR = new Vector3(10, 0, 0); armLR = new Vector3(-150, 0, 20); armRR = new Vector3(40, 0, -20); }
                else { legLR = new Vector3(-20, 0, 0); legRR = new Vector3(-30, 0, 0); armLR = new Vector3(-160, 0, 35); armRR = new Vector3(-160, 0, -35); }
                hipsR = new Vector3(verticalSpeed > 0 ? -8 : 6, 0, 0);
            }
            else if (speed01 > 0.05f)
            {
                float a = 48f * speed01;
                legLR = new Vector3(swing * a, 0, 0); legRR = new Vector3(-swing * a, 0, 0);
                armLR = new Vector3(-swing * a * 0.9f, 0, 6); armRR = new Vector3(swing * a * 0.9f, 0, -6);
                hipsPos.y += Mathf.Abs(Mathf.Cos(phase)) * 0.06f * speed01;
                hipsR = new Vector3(8 * speed01, 0, 0);
                headR = new Vector3(-4 * speed01, 0, 0);
            }
            else
            {
                float b = Mathf.Sin(t * 2.2f);
                hipsPos.y += b * 0.012f;
                armLR = new Vector3(0, 0, 6 + b * 2); armRR = new Vector3(0, 0, -6 - b * 2);
                if (idleStyle == "fidget")
                {
                    // looks around nervously every few seconds
                    float look = Mathf.Sin(t * 0.7f) > 0.8f ? Mathf.Sin(t * 3f) * 25f : 0f;
                    headR = new Vector3(b * 2, look, 0);
                }
            }

            if (dashing) { hipsR = new Vector3(25, 0, 0); armLR = new Vector3(60, 0, 10); armRR = new Vector3(60, 0, -10); legLR = new Vector3(30, 0, 0); legRR = new Vector3(-40, 0, 0); }
            if (talking) headR += new Vector3(Mathf.Sin(t * 12f) * 4f, Mathf.Sin(t * 3f) * 5f, 0);
            if (landT > 0) hipsPos.y -= 0.08f * (landT / 0.18f);
            if (hitT > 0)
            {
                float k = hitT / 0.45f;
                hipsR = new Vector3(-25 * k, 0, Mathf.Sin(t * 40) * 6 * k);
                armLR = new Vector3(-120 * k, 0, 40 * k); armRR = new Vector3(-120 * k, 0, -40 * k);
                headR = new Vector3(-20 * k, 0, 0);
            }
            if (victoryT > 0)
            {
                armLR = new Vector3(-170, 0, 25); armRR = new Vector3(-170, 0, -25);
                hipsPos.y += Mathf.Abs(Mathf.Sin(t * 8)) * 0.18f;
                headR = new Vector3(-12, 0, 0);
            }
            if (defeatT > 0)
            {
                hipsR = new Vector3(35, 0, 0); headR = new Vector3(35, 0, 0);
                armLR = new Vector3(15, 0, 4); armRR = new Vector3(15, 0, -4);
                hipsPos.y -= 0.12f;
            }
            if (castT > 0)
            {
                armRR = new Vector3(-100 + Mathf.Sin(t * 10) * 10, 0, -10);
                armLR = new Vector3(-30, 0, 10);
            }

            float s = 1f - Mathf.Exp(-dt * 18f);
            hips.localPosition = Vector3.Lerp(hips.localPosition, hipsPos, s);
            Rot(hips, hipsR, s); Rot(head, headR, s); Rot(armL, armLR, s); Rot(armR, armRR, s); Rot(legL, legLR, s); Rot(legR, legRR, s);
        }

        static void Rot(Transform tr, Vector3 euler, float s)
        {
            if (tr == null) return;
            tr.localRotation = Quaternion.Slerp(tr.localRotation, Quaternion.Euler(euler), s);
        }
    }
}

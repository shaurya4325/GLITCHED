using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Capture/test mode only (GLITCHED.exe -capture ... -captureBot 1): plays a level on its own to prove it can be
    /// finished. Runs right, jumps gaps and walls, slides under ducts, lets go of swing bars on the forward swing,
    /// reads lesson terminals and answers quiz cards (correctly, after a beat, so the screenshots show them).
    /// </summary>
    public class ParkourBot : MonoBehaviour
    {
        public static bool Active { get; private set; }
        float swingT, stuckT, lastX, readCd, logT;

        void OnEnable() { Active = true; QuickQuiz.AutoAnswer = true; LessonCard.AutoAdvance = true; }
        void OnDisable() { Active = false; QuickQuiz.AutoAnswer = false; LessonCard.AutoAdvance = false; }

        static bool Ray(Vector3 from, Vector3 dir, float dist) => Physics.Raycast(from, dir, dist, ~0, QueryTriggerInteraction.Ignore);

        void Update()
        {
            var p = PlayerController.Instance;
            if (p == null) return;
            PlayerController.AutoWalk = 1f;
            var pos = p.transform.position;
            float dt = Time.deltaTime;
            readCd -= dt;
            if ((logT -= dt) <= 0f) { logT = 0.5f; Debug.Log($"[Bot] x={pos.x:0.0} y={pos.y:0.0} v=({p.Velocity.x:0.0},{p.Velocity.y:0.0}) {p.State} grounded={p.Grounded} locked={p.Locked}"); }

            // lesson terminals: read each one once
            if (readCd <= 0f && !p.Locked)
                foreach (var i in Interactable.All)
                    if (i is LessonTerminal lt && Vector2.Distance(pos + Vector3.up * 0.8f, lt.Position) < lt.radius && !Services.Game.Data.HasFlag("lesson_" + lt.topic))
                    { lt.Interact(p); readCd = 3f; break; }

            if (p.State == PlayerController.Move.Swing)
            {
                swingT += dt;
                if (swingT > 0.2f) { PlayerController.BotJump = true; swingT = 0f; }
                return;
            }
            swingT = 0f;
            if (p.State != PlayerController.Move.Normal || !p.Grounded || p.Locked) return;

            // stuck against something for a while: jump
            stuckT = Mathf.Abs(pos.x - lastX) < 0.01f ? stuckT + dt : 0f;
            lastX = pos.x;
            bool head = Ray(pos + Vector3.up * 1.3f, Vector3.right, 1.6f);
            bool knee = Ray(pos + Vector3.up * 0.35f, Vector3.right, 1.6f);
            bool gap = !Ray(pos + new Vector3(1.0f, 0.5f, 0f), Vector3.down, 4f);
            if (head && !knee) { PlayerController.BotSlide = 0.5f; return; }   // a duct: slide under it
            if (gap || (knee && head) || stuckT > 0.6f) { PlayerController.BotJump = true; stuckT = 0f; }
        }
    }
}

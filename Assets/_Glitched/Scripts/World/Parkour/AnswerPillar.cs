using UnityEngine;

namespace Glitched
{
    /// <summary>Marks a pillar of an AnswerGate: landing on it gives that pillar's answer.</summary>
    public class AnswerPillar : MonoBehaviour
    {
        public AnswerGate gate;
        public void Land(PlayerController p) { if (gate != null) gate.Land(p, transform); }
    }
}

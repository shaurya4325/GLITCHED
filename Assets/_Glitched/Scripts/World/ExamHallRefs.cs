using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>The hall's moving parts and marks, serialized so a hall built in the editor keeps them.</summary>
    public class ExamHallRefs : MonoBehaviour
    {
        public List<Transform> papers = new List<Transform>();
        public List<Light> tubeLights = new List<Light>();
        public List<Transform> fans = new List<Transform>();
        public List<Vector3> throwFrom = new List<Vector3>();
        public Transform clockHand, clockHandFast;
        public Vector3 playerSpot, kanhaSpot, stageTop;
    }
}

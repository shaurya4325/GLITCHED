using UnityEngine;

namespace Glitched
{
    /// <summary>A named place where the player appears when entering the scene (e.g. "fromL1", "fromMap").</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string id = "default";
        public int facing = 1;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.8f, new Vector3(0.6f, 1.6f, 0.6f));
        }
    }
}

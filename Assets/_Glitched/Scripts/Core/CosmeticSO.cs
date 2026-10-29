using UnityEngine;

namespace Glitched
{
    /// <summary>A ridiculous costume from the shop terminal.</summary>
    [CreateAssetMenu(menuName = "Glitched/Cosmetic", fileName = "Cosmetic")]
    public class CosmeticSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int price = 50;
        [Tooltip("Hat built from primitives: none, cone, crown, propeller, chef, halo, antenna, tophat, duck")]
        public string hat = "none";
        public Color hatColor = Color.white;
        public bool tintBody;
        public Color bodyTint = Color.white;
        [Tooltip("Optional: a prefab attached to the head instead of the primitive hat.")]
        public GameObject hatPrefab;
        [Tooltip("Only unlocked as a reward (not sold).")]
        public bool rewardOnly;
    }
}

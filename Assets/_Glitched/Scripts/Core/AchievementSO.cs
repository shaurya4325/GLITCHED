using UnityEngine;

namespace Glitched
{
    /// <summary>An achievement: unlocks when save stat 'statKey' reaches 'target'.</summary>
    [CreateAssetMenu(menuName = "Glitched/Achievement", fileName = "Achievement")]
    public class AchievementSO : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public string statKey;
        public int target = 1;
        public int coinReward = 25;
        public string icon = "star";
    }
}

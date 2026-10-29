using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    public enum EnemyTier { Normal, Elite, MiniBoss, Boss }

    /// <summary>Data for one kind of enemy. Swap modelPrefab for a real model at any time.</summary>
    [CreateAssetMenu(menuName = "Glitched/Enemy", fileName = "Enemy")]
    public class EnemyDataSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public EnemyTier tier = EnemyTier.Normal;
        public string arc = "CodeCity";
        [Tooltip("Concepts this enemy is built around. Empty = any concept of the arc.")]
        public List<string> concepts = new List<string>();
        [Tooltip("Preferred challenge types (empty = the arc's emphasis types).")]
        public List<ChallengeType> preferredTypes = new List<ChallengeType>();

        public int maxHP = 100;
        public float attackDamage = 15f;
        [Tooltip("Number of challenges needed when every answer is a normal hit.")]
        public int challengesToDefeat = 1;
        public int rewardXP = 40;
        public int rewardCoins = 5;

        [Header("Personality")]
        public List<string> attackNames = new List<string> { "GLITCH STRIKE" };
        public List<string> taunts = new List<string> { "YOU CAN'T FIX MY CODE!" };
        public List<string> hitLines = new List<string> { "OW! THAT WAS CORRECT!" };
        public List<string> winLines = new List<string> { "HA! WRONG!" };
        public List<string> defeatLines = new List<string> { "I... was... deprecated..." };

        [Header("Look")]
        public GameObject modelPrefab;
        public float height = 1.2f;
        public Color tint = new Color(1f, 0.3f, 0.8f);
        public string placeholderShape = "blob";   // blob, robot, spiky, ghost, crystal, cell, virus, mech
        public bool flying;
        public float patrolDistance = 2.5f;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>A Glitch Lord (5 phases) or mini-boss (3 phases).</summary>
    [CreateAssetMenu(menuName = "Glitched/Boss", fileName = "Boss")]
    public class BossDataSO : EnemyDataSO
    {
        [Tooltip("The concept this boss IS (used for the hint, teaching mode and mastery check).")]
        public string bossConcept = "Loops";
        public List<BossPhase> phases = new List<BossPhase>();
        public int regenOnWrong = 40;
        [TextArea] public List<string> introLines = new List<string>();
        public string finalDefeatLine = "NO... YOU UNDERSTOOD THE SYSTEM!";
        [Tooltip("Lines when the player is defeated the first time (the villain mocks the mistake).")]
        public List<string> hintTaunts = new List<string>();
        [Tooltip("Lines the villain says (sighing) while teaching.")]
        public List<string> teachingLines = new List<string>();
        public string rematchLine = "SHOW ME WHAT YOU LEARNED.";
        public CharacterProfileSO profile;
    }

    [Serializable]
    public class BossPhase
    {
        public string name = "RECALL";
        [Range(1, 5)] public int difficulty = 1;
        public List<ChallengeType> types = new List<ChallengeType>();
        public int damage = 50;
        public string line = "";
        [Tooltip("Optional: force a specific concept for this phase.")]
        public string concept = "";
    }
}

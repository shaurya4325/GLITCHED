using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    public enum LevelKind { Hub, Level, BossArena, Cinematic }

    /// <summary>One arc = one region, one subject, one villain (World -> Levels -> Mini-boss -> Boss -> Restoration).</summary>
    [CreateAssetMenu(menuName = "Glitched/Arc", fileName = "Arc")]
    public class ArcDataSO : ScriptableObject
    {
        public string id = "CodeCity";
        public string displayName = "Code City";
        public string subject = "Computer Science";
        public int order;
        public RegionThemeSO theme;
        public List<string> concepts = new List<string>();
        public List<ChallengeType> emphasisTypes = new List<ChallengeType>();
        public List<LevelInfo> levels = new List<LevelInfo>();
        public BossDataSO miniBoss;
        public BossDataSO boss;
        public string abilityReward = "";
        public string cosmeticReward = "";
        [TextArea] public string tagline;
        [TextArea] public List<string> arrivalLines = new List<string>();
        [TextArea] public List<string> restoredLines = new List<string>();

        public LevelInfo Level(string levelId) => levels.Find(l => l.id == levelId);
        public LevelInfo Hub => levels.Find(l => l.kind == LevelKind.Hub);
        public LevelInfo BossLevel => levels.Find(l => l.kind == LevelKind.BossArena);
    }

    [Serializable]
    public class LevelInfo
    {
        public string id;          // e.g. CodeCity_L1
        public string scene;       // scene name in Build Settings
        public string displayName;
        public LevelKind kind;
        public string next;        // level id unlocked after this one
        public string requiredAbility = "";
    }
}

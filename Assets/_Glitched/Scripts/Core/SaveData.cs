using System;
using System.Collections.Generic;

namespace Glitched
{
    /// <summary>Everything that is saved for one save slot (stored as JSON).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int slot;
        public string created;
        public string lastPlayed;
        public float playSeconds;
        public string playerName = "Shaurya";

        public int xp;
        public int coins;
        public int knowledgePoints;
        public int level = 1;

        public List<string> abilities = new List<string>();
        public List<string> cosmeticsOwned = new List<string> { "default" };
        public string cosmeticEquipped = "default";

        public Dictionary<string, ArcProgress> arcs = new Dictionary<string, ArcProgress>();
        public string currentScene = "";
        public string checkpointId = "";

        public Dictionary<string, ConceptMastery> mastery = new Dictionary<string, ConceptMastery>();
        public Dictionary<string, List<string>> usedQuestions = new Dictionary<string, List<string>>();
        public List<int> recentAnswers = new List<int>();      // 1 = correct, 0 = wrong (last 5)
        public Dictionary<string, int> stats = new Dictionary<string, int>();
        public List<string> achievements = new List<string>();
        public List<string> flags = new List<string>();
        public List<string> collected = new List<string>();

        public ArcProgress Arc(string id)
        {
            if (!arcs.TryGetValue(id, out var a)) arcs[id] = a = new ArcProgress();
            return a;
        }

        public int Stat(string key) => stats.TryGetValue(key, out var v) ? v : 0;
        public void AddStat(string key, int amount = 1) => stats[key] = Stat(key) + amount;
        public bool HasFlag(string f) => flags.Contains(f);
        public void SetFlag(string f) { if (!flags.Contains(f)) flags.Add(f); }
    }

    [Serializable]
    public class ArcProgress
    {
        public bool unlocked;
        public List<string> levelsDone = new List<string>();
        public bool miniBossDefeated;
        public bool bossDefeated;
        public bool restored;
        public bool conceptMastered;
        public int bossDefeats;
    }

    [Serializable]
    public class ConceptMastery
    {
        public string subject;
        public float value;     // 0..100
        public int seen;
        public int correct;
        public int fast;
    }
}

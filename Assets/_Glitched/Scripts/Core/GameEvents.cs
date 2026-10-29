using System;
using UnityEngine;

namespace Glitched
{
    /// <summary>Global events that let systems talk to each other without hard references.</summary>
    public static class GameEvents
    {
        public static event Action<float> CorruptionChanged;
        public static event Action<ChallengeOutcome> ChallengeResolved;
        public static event Action<int> XPChanged;
        public static event Action<int> CoinsChanged;
        public static event Action<int> KnowledgeChanged;
        public static event Action<float, float> StabilityChanged;   // current, max
        public static event Action<int> ComboChanged;
        public static event Action<string> AbilityUnlocked;
        public static event Action<string> AchievementUnlocked;
        public static event Action<string, float> MasteryChanged;     // concept, value
        public static event Action<bool> EncounterStateChanged;       // true = started
        public static event Action<Checkpoint> CheckpointReached;
        public static event Action<GraphicsTier> TierChanged;
        public static event Action SettingsChanged;
        public static event Action<string> PlayerLevelUp;
        public static event Action RestorationStarted;
        public static event Action RestorationFinished;

        public static void RaiseCorruption(float v) => CorruptionChanged?.Invoke(v);
        public static void RaiseChallenge(ChallengeOutcome o) => ChallengeResolved?.Invoke(o);
        public static void RaiseXP(int v) => XPChanged?.Invoke(v);
        public static void RaiseCoins(int v) => CoinsChanged?.Invoke(v);
        public static void RaiseKnowledge(int v) => KnowledgeChanged?.Invoke(v);
        public static void RaiseStability(float c, float m) => StabilityChanged?.Invoke(c, m);
        public static void RaiseCombo(int c) => ComboChanged?.Invoke(c);
        public static void RaiseAbility(string id) => AbilityUnlocked?.Invoke(id);
        public static void RaiseAchievement(string id) => AchievementUnlocked?.Invoke(id);
        public static void RaiseMastery(string c, float v) => MasteryChanged?.Invoke(c, v);
        public static void RaiseEncounter(bool started) => EncounterStateChanged?.Invoke(started);
        public static void RaiseCheckpoint(Checkpoint c) => CheckpointReached?.Invoke(c);
        public static void RaiseTier(GraphicsTier t) => TierChanged?.Invoke(t);
        public static void RaiseSettings() => SettingsChanged?.Invoke();
        public static void RaiseLevelUp(string msg) => PlayerLevelUp?.Invoke(msg);
        public static void RaiseRestorationStarted() => RestorationStarted?.Invoke();
        public static void RaiseRestorationFinished() => RestorationFinished?.Invoke();
    }

    /// <summary>Everything that happened when a challenge finished.</summary>
    public struct ChallengeOutcome
    {
        public QuestionData question;
        public bool correct;
        public bool fast;
        public bool timedOut;
        public float seconds;
        public bool rematchGuaranteedCrit;
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>Progression (XP, coins, knowledge points, abilities, arcs) and the current level context.</summary>
    public class GameManager : MonoBehaviour
    {
        public const int DevSlot = 98;

        public SaveData Data
        {
            get
            {
                if (Services.Save.Current == null) DevSetup(Services.Save.NewGame(DevSlot));
                return Services.Save.Current;
            }
        }

        /// <summary>
        /// Pressing Play on a level scene directly (no save loaded): an in-memory test save with every region open
        /// and the abilities of earlier regions, so any scene can be tested on its own. Never written to disk.
        /// </summary>
        static void DevSetup(SaveData d)
        {
            d.SetFlag("opening_seen");
            Services.Db.FindLevelByScene(SceneManager.GetActiveScene().name, out var arc, out _);
            foreach (var a in Services.Db.arcs) if (a != null) d.Arc(a.id).unlocked = true;
            if (arc != null) GiveEarlierAbilities(d, arc);
            d.coins = 200;
        }

        /// <summary>The abilities a player would have earned before reaching this arc (dev test saves, judge copies).</summary>
        static void GiveEarlierAbilities(SaveData d, ArcDataSO arc)
        {
            foreach (var a in Services.Db.arcs)
                if (a != null && a.order < arc.order && !string.IsNullOrEmpty(a.abilityReward) && !d.abilities.Contains(a.abilityReward)) d.abilities.Add(a.abilityReward);
            if (arc.id == "BioZone" && !d.abilities.Contains("shrink")) d.abilities.Add("shrink");
        }

        public ArcDataSO CurrentArc { get; private set; }
        public LevelInfo CurrentLevel { get; private set; }
        public bool IsExpo => Services.Settings.Data.expoMode;
        public bool Paused { get; private set; }

        public void Init()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (Services.Db.FindLevelByScene(s.name, out var arc, out var lvl)) { CurrentArc = arc; CurrentLevel = lvl; }
            else { CurrentArc = null; CurrentLevel = null; }
            if (LicenceAccess.UnlockAll && arc != null && Services.Save.Current != null) GiveEarlierAbilities(Services.Save.Current, arc);
            SetPaused(false);
            TimeController.Reset();
        }

        // ---------------------------------------------------------------- currencies
        public static int LevelForXP(int xp) => 1 + Mathf.FloorToInt(Mathf.Sqrt(xp / 60f));
        public static int XPForLevel(int level) => Mathf.CeilToInt((level - 1) * (level - 1) * 60f);

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            var d = Data;
            d.xp += amount;
            int lvl = LevelForXP(d.xp);
            if (lvl > d.level)
            {
                d.level = lvl;
                GameEvents.RaiseLevelUp($"LEVEL {lvl}");
                Services.Audio.Play("levelup");
            }
            GameEvents.RaiseXP(d.xp);
        }

        public void AddCoins(int amount)
        {
            Data.coins = Mathf.Max(0, Data.coins + amount);
            GameEvents.RaiseCoins(Data.coins);
        }

        public void AddKnowledge(int amount, string concept)
        {
            Data.knowledgePoints += amount;
            GameEvents.RaiseKnowledge(Data.knowledgePoints);
        }

        // ---------------------------------------------------------------- abilities
        public bool HasAbility(string id) => !string.IsNullOrEmpty(id) && Data.abilities.Contains(id);

        public void UnlockAbility(string id)
        {
            if (string.IsNullOrEmpty(id) || HasAbility(id)) return;
            Data.abilities.Add(id);
            GameEvents.RaiseAbility(id);
            Services.Save.Write();
        }

        public static string AbilityName(string id)
        {
            switch (id)
            {
                case "double_jump": return "RECURSIVE LEAP (Double Jump)";
                case "dash": return "MOMENTUM DASH";
                case "wall_jump": return "BOND GRIP (Wall Jump)";
                case "shrink": return "SHRINK / GROW";
                default: return id;
            }
        }

        // ---------------------------------------------------------------- cosmetics
        public bool OwnsCosmetic(string id) => Data.cosmeticsOwned.Contains(id);

        public bool BuyCosmetic(CosmeticSO c)
        {
            if (c == null || OwnsCosmetic(c.id) || c.rewardOnly || Data.coins < c.price) return false;
            AddCoins(-c.price);
            Data.cosmeticsOwned.Add(c.id);
            Data.AddStat("cosmetics_bought");
            Services.Achievements.Check();
            Services.Save.Write();
            return true;
        }

        public void GrantCosmetic(string id)
        {
            if (string.IsNullOrEmpty(id) || OwnsCosmetic(id)) return;
            Data.cosmeticsOwned.Add(id);
        }

        public void EquipCosmetic(string id)
        {
            if (!OwnsCosmetic(id)) return;
            Data.cosmeticEquipped = id;
            var p = PlayerController.Instance;
            if (p != null) p.Visual.ApplyCosmetic(Services.Db.Cosmetic(id));
            Services.Save.Write();
        }

        // ---------------------------------------------------------------- arcs and levels
        public bool IsArcUnlocked(string arcId) => LicenceAccess.UnlockAll || Data.Arc(arcId).unlocked;
        public bool IsLevelDone(string arcId, string levelId) => Data.Arc(arcId).levelsDone.Contains(levelId);

        /// <summary>A level is open if it's the first of its arc or the previous level is done.</summary>
        public bool IsLevelOpen(ArcDataSO arc, LevelInfo lvl)
        {
            if (arc == null || lvl == null) return false;
            if (!IsArcUnlocked(arc.id)) return false;
            if (LicenceAccess.UnlockAll) return true;   // judges can open any level
            int i = arc.levels.IndexOf(lvl);
            if (i <= 0) return true;
            var prev = arc.levels[i - 1];
            return prev.kind == LevelKind.Hub || IsLevelDone(arc.id, prev.id);
        }

        public void CompleteLevel(string arcId, string levelId)
        {
            var a = Data.Arc(arcId);
            if (!a.levelsDone.Contains(levelId)) a.levelsDone.Add(levelId);
            Services.Save.Write();
        }

        public void MarkBossDefeated(ArcDataSO arc, bool mini)
        {
            var a = Data.Arc(arc.id);
            if (mini) a.miniBossDefeated = true;
            else
            {
                a.bossDefeated = true;
                Data.AddStat("boss_" + arc.id);
            }
            Services.Save.Write();
        }

        public void MarkRestored(ArcDataSO arc)
        {
            var a = Data.Arc(arc.id);
            a.restored = true;
            if (arc.BossLevel != null && !a.levelsDone.Contains(arc.BossLevel.id)) a.levelsDone.Add(arc.BossLevel.id);
            Data.AddStat("restored_" + arc.id);
            UnlockAbility(arc.abilityReward);
            GrantCosmetic(arc.cosmeticReward);
            // unlock the next arc
            foreach (var other in Services.Db.arcs)
                if (other != null && other.order == arc.order + 1) Data.Arc(other.id).unlocked = true;
            if (arc.id == "TheCore") Data.AddStat("game_complete");
            Services.Achievements.Check();
            Services.Save.Write();
        }

        public bool IsArcRestored(string arcId) => Data.Arc(arcId).restored;

        /// <summary>Corruption level a scene should start at (restored arcs stay healthy).</summary>
        public float StartCorruptionFor(ArcDataSO arc)
        {
            if (arc == null) return 0f;
            return IsArcRestored(arc.id) ? 0f : 1f;
        }

        // ---------------------------------------------------------------- pause
        public void SetPaused(bool p)
        {
            Paused = p;
            TimeController.SetPaused(p);
            AudioListener.pause = false;
        }
    }
}

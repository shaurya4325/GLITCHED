using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A timed parkour run: the clock starts at the START arch and stops at FINISH. Medals for the time, XP for
    /// quiz pits answered first time, and the best time is saved.
    /// </summary>
    public class ParkourRun : MonoBehaviour
    {
        public static ParkourRun Current { get; private set; }
        public string levelId;
        public float startX, finishX;
        public float gold = 40f, silver = 55f, bronze = 75f;
        bool running, finished;
        float t;
        int quizRight, quizTotal;

        void OnEnable() => Current = this;
        void OnDisable() { if (Current == this) Current = null; }

        string Key => "run_ms_" + levelId;
        int BestMs => Services.Game.Data.Stat(Key);

        /// <summary>A quiz pit was answered (firstTry = right on the first jump).</summary>
        public void QuizAnswered(bool firstTry) { quizTotal++; if (firstTry) quizRight++; }

        static string Clock(float s) => $"{(int)(s / 60f)}:{s % 60f:00.0}";

        void Update()
        {
            var p = PlayerController.Instance;
            if (p == null) return;
            float x = p.transform.position.x;
            if (!running && !finished && x > startX && x < startX + 4f)
            {
                running = true; t = 0f; quizRight = quizTotal = 0;
                Services.Audio.Play("levelup", 0.6f);
                Services.UI.Banner("GO!", UIFactory.Theme.yellow, 110, 0.4f, "parkour run  ·  " + Clock(gold) + " for GOLD");
            }
            if (running && x < startX - 3f) { running = false; Services.UI.Hud.SetRunTimer(null); }   // walked back: reset
            if (!running) return;
            if (!Services.Game.Paused && !EncounterManager.Busy && !LessonCard.Open) t += Time.deltaTime;   // fights and lessons don't count
            Color c = t <= gold ? UIFactory.Theme.yellow : t <= silver ? new Color(0.8f, 0.85f, 0.95f) : t <= bronze ? new Color(0.95f, 0.6f, 0.35f) : UIFactory.Theme.textDim;
            string best = BestMs > 0 ? $"   <size=50%><color=#9fb0cc>BEST {Clock(BestMs / 1000f)}</color></size>" : "";
            Services.UI.Hud.SetRunTimer(Clock(t) + best, c);
            if (x >= finishX) Finish();
        }

        void Finish()
        {
            running = false; finished = true;
            string medal = t <= gold ? "GOLD" : t <= silver ? "SILVER" : t <= bronze ? "BRONZE" : "FINISHED";
            Color c = t <= gold ? UIFactory.Theme.yellow : t <= silver ? new Color(0.85f, 0.9f, 1f) : t <= bronze ? new Color(0.95f, 0.6f, 0.35f) : UIFactory.Theme.cyan;
            int ms = Mathf.RoundToInt(t * 1000f);
            bool record = BestMs == 0 || ms < BestMs;
            if (record) Services.Game.Data.stats[Key] = ms;
            int xp = (medal == "GOLD" ? 150 : medal == "SILVER" ? 100 : medal == "BRONZE" ? 70 : 40) + quizRight * 20;
            Services.Game.AddXP(xp);
            Services.Game.AddCoins(medal == "GOLD" ? 40 : 20);
            Services.UI.Banner(medal + (record ? "  ·  NEW BEST!" : ""), c, 110, 1.6f, $"time {Clock(t)}   ·   quiz {quizRight}/{quizTotal} first try   ·   +{xp} XP");
            Services.Audio.Play("levelup");
            Services.Game.Data.AddStat("parkour_runs");
            Services.Save.Write();
            Tween.After(4f, () => Services.UI.Hud.SetRunTimer(null));
        }
    }
}

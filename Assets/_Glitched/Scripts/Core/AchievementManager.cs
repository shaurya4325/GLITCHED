using UnityEngine;

namespace Glitched
{
    /// <summary>Checks AchievementSO targets against save stats and pops a toast when one unlocks.</summary>
    public class AchievementManager : MonoBehaviour
    {
        public void Check()
        {
            var d = Services.Game.Data;
            foreach (var a in Services.Db.achievements)
            {
                if (a == null || d.achievements.Contains(a.id)) continue;
                if (d.Stat(a.statKey) >= a.target)
                {
                    d.achievements.Add(a.id);
                    Services.Game.AddCoins(a.coinReward);
                    Services.UI.AchievementPopup(a);
                    Services.Audio.Play("achievement");
                    GameEvents.RaiseAchievement(a.id);
                }
            }
        }

        /// <summary>Raises a stat and checks achievements. Use "max_" stats with SetMax.</summary>
        public void Add(string stat, int amount = 1)
        {
            Services.Game.Data.AddStat(stat, amount);
            Check();
        }

        public void SetMax(string stat, int value)
        {
            var d = Services.Game.Data;
            if (value > d.Stat(stat)) { d.stats[stat] = value; Check(); }
        }
    }
}

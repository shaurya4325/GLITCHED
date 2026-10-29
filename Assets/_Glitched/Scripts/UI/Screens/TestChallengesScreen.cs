using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Test scene (Phase 4): cycles through all ten challenge types, one question each, and logs the results.</summary>
    public class TestChallengesScreen : MonoBehaviour
    {
        TextMeshProUGUI log;

        IEnumerator Start()
        {
            PauseMenu.Allowed = false;
            while (!Services.Questions.Loaded) yield return null;
            var cam = FindFirstObjectByType<Camera>();
            if (cam != null) Services.Graphics.ConfigureCamera(cam);
            Services.Corruption.SetupRegion(Services.Db.Arc("CodeCity")?.theme, RenderSettings.sun, 0.6f);
            var root = UIFactory.Stretch(UIFactory.Rect("TestChallenges", Services.UI.SceneLayer));
            var title = UIFactory.Text(root, "CHALLENGE ENGINE TEST: all 10 types", 44, UIFactory.Theme.cyan, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(40, -30), new Vector2(1400, 60), new Vector2(0, 1));
            log = UIFactory.Text(root, "", 24, UIFactory.Theme.text, TextAlignmentOptions.TopLeft);
            UIFactory.Place(log.rectTransform, new Vector2(0, 1), new Vector2(40, -100), new Vector2(1400, 360), new Vector2(0, 1));
            Services.Audio.Music.Cinematic("puzzle");
            while (true)
            {
                foreach (ChallengeType t in System.Enum.GetValues(typeof(ChallengeType)))
                {
                    var pool = new List<QuestionData>();
                    foreach (var q in Services.Questions.All) if (q.type == t) pool.Add(q);
                    if (pool.Count == 0) { log.text += $"\n{t}: no questions!"; continue; }
                    var pick = pool[Random.Range(0, pool.Count)];
                    bool done = false; ChallengeOutcome o = default;
                    ChallengeEngine.Run(pick, $"TEST: {t}", 0, r => { o = r; done = true; }, new ChallengeEngine.Options { header = $"TEST  ·  {t}  ·  {pick.id}" });
                    while (!done) yield return null;
                    log.text += $"\n{t,-12} {pick.id,-14} {(o.correct ? "<color=#7cffa0>correct</color>" : "<color=#ff5a6a>wrong</color>")}  {o.seconds:0.0}s{(o.fast ? "  FAST" : "")}";
                    if (!o.correct) yield return ChallengeEngine.ShowExplanation(pick, o);
                }
                log.text += "\n<color=#ffd640>--- all ten types done, looping ---</color>";
            }
        }
    }
}

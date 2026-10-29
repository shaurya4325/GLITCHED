using System.Collections;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Test scene (Phase 3): a Corruption slider (0 -> 1), a region picker, a tier picker and a restoration-wave button (also R).</summary>
    public class TestCorruptionPanel : LevelScript
    {
        UnityEngine.UI.Slider slider;

        public override IEnumerator Run(RegionDirector director)
        {
            var root = UIFactory.Rect("CorruptionTest", Services.UI.SceneLayer);
            UIFactory.Place(root, new Vector2(1, 0), new Vector2(-30, 30), new Vector2(760, 330), new Vector2(1, 0));
            var p = UIFactory.Panel(root, null, 18, "Panel");
            UIFactory.Stretch(p.rectTransform);
            UIFactory.VLayout(p, 8, 20);
            var t = UIFactory.Text(p.transform, "CORRUPTION TEST   (R = restoration wave, F3 = debug)", 22, UIFactory.Theme.cyan, TextAlignmentOptions.Left, true);
            UIFactory.Size(t, -1, 36);
            slider = UIFactory.Slider(p.transform, "Corruption", 0, 1, Services.Corruption.Value, v => Services.Corruption.Set(v), v => v.ToString("0.00"), false, 200);
            UIFactory.Selector(p.transform, "Tier", new[] { "POTATO", "LOW", "MEDIUM", "HIGH", "ULTRA" }, (int)Services.Graphics.Tier, i => Services.Graphics.ApplyTier((GraphicsTier)i));
            UIFactory.Size(UIFactory.Button(p.transform, "PLAY RESTORATION", Restore, ButtonStyle.Primary, 24), -1, 64);
            UIFactory.Size(UIFactory.Button(p.transform, "CORRUPT AGAIN", () => { Services.Corruption.TweenTo(1f, 1.5f); }, ButtonStyle.Secondary, 22), -1, 56);
            while (true)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame) Restore();
                if (slider != null) slider.SetValueWithoutNotify(Services.Corruption.Value);
                yield return null;
            }
        }

        void Restore()
        {
            var dir = FindFirstObjectByType<RestorationDirector>() ?? new GameObject("RestorationDirector").AddComponent<RestorationDirector>();
            if (!dir.Playing) StartCoroutine(dir.Play(RegionDirector.Current != null ? RegionDirector.Current.Arc : null, true));
        }
    }
}

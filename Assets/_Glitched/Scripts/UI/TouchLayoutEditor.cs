using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Lets the player drag the touch buttons around (and scroll/pinch to resize), then press DONE.</summary>
    public class TouchLayoutEditor : MonoBehaviour
    {
        public static void Begin()
        {
            SubScreen.CloseAll();
            var root = UIFactory.Stretch(UIFactory.Rect("TouchLayoutEditor", Services.UI.PopupLayer));
            root.gameObject.AddComponent<TouchLayoutEditor>();
            var hint = UIFactory.Panel(root, null, 18, "Hint");
            UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1100, 150), new Vector2(0.5f, 1));
            var t = UIFactory.Text(hint.transform, "Drag the buttons to move them. Mouse wheel resizes.\nUse 'Touch button size' in Settings for all buttons.", 26, null, TextAlignmentOptions.Center);
            UIFactory.Stretch(t.rectTransform, 20, 10, 260, 10);
            var done = UIFactory.Button(hint.transform, "DONE", () =>
            {
                Services.UI.Touch.SetEditMode(false);
                Destroy(root.gameObject);
                SettingsScreen.Open();
            });
            UIFactory.Place(done.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(200, 90), new Vector2(1, 0.5f));
            Services.UI.Touch.SetEditMode(true);
        }
    }
}

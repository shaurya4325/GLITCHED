using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>Left/right option picker used in Settings.</summary>
    public class UISelector : MonoBehaviour
    {
        IList<string> options;
        int index;
        TextMeshProUGUI valueText;
        Action<int> onChange;

        public int Index => index;

        public void Init(IList<string> opts, int start, TextMeshProUGUI text, Action<int> changed)
        {
            options = opts; index = Mathf.Clamp(start, 0, opts.Count - 1); valueText = text; onChange = changed;
            Refresh();
        }

        public void Step(int d)
        {
            if (options == null || options.Count == 0) return;
            index = (index + d + options.Count) % options.Count;
            Refresh();
            onChange?.Invoke(index);
        }

        public void SetIndex(int i, bool notify = false)
        {
            index = Mathf.Clamp(i, 0, options.Count - 1);
            Refresh();
            if (notify) onChange?.Invoke(index);
        }

        void Refresh() { if (valueText != null && options != null) valueText.text = options[index]; }
    }
}

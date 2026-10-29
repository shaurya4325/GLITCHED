using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>A vertical list whose rows can be dragged (mouse/touch) or moved with up/down buttons.</summary>
    public class ReorderList : MonoBehaviour
    {
        public readonly List<RectTransform> rows = new List<RectTransform>();
        public readonly List<int> order = new List<int>();     // order[i] = original item index shown at row i
        public float rowHeight = 64f;
        public bool mono;
        public Action onChanged;
        RectTransform rt;
        float spacing = 8f;

        public static ReorderList Create(Transform parent, IList<string> items, List<int> startOrder, bool mono, float rowHeight = 64f)
        {
            var r = UIFactory.Rect("ReorderList", parent);
            var list = r.gameObject.AddComponent<ReorderList>();
            list.rt = r;
            list.mono = mono;
            list.rowHeight = rowHeight;
            UIFactory.Size(r, -1, items.Count * (rowHeight + list.spacing));
            foreach (var idx in startOrder) list.AddRow(items[idx], idx);
            list.Layout(true);
            return list;
        }

        void AddRow(string text, int idx)
        {
            var bg = UIFactory.Panel(rt, new Color(0.07f, 0.09f, 0.15f, 0.98f), 12, "Row", true);
            var row = bg.rectTransform;
            row.anchorMin = new Vector2(0, 1); row.anchorMax = new Vector2(1, 1); row.pivot = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(0, rowHeight);
            var grip = UIFactory.Text(row, "⋮⋮", 26, UIFactory.Theme.textDim, TextAlignmentOptions.Center, true);
            UIFactory.Place(grip.rectTransform, new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(40, rowHeight), new Vector2(0.5f, 0.5f));
            var t = mono ? UIFactory.Mono(row, ChallengeBase.Syntax(text), 26) : UIFactory.Text(row, text, 26);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.textWrappingMode = mono ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
            UIFactory.Stretch(t.rectTransform, 56, 4, 150, 4);
            var up = UIFactory.Button(row, "▲", null, ButtonStyle.Ghost, 22);
            UIFactory.Place(up.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(-80, 0), new Vector2(60, rowHeight - 14), new Vector2(1, 0.5f));
            var down = UIFactory.Button(row, "▼", null, ButtonStyle.Ghost, 22);
            UIFactory.Place(down.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(60, rowHeight - 14), new Vector2(1, 0.5f));
            up.onClick.AddListener(() => Move(row, -1));
            down.onClick.AddListener(() => Move(row, 1));
            var drag = row.gameObject.AddComponent<ReorderRowDrag>();
            drag.list = this;
            rows.Add(row);
            order.Add(idx);
        }

        public void Move(RectTransform row, int dir)
        {
            int i = rows.IndexOf(row);
            int j = Mathf.Clamp(i + dir, 0, rows.Count - 1);
            if (i == j) return;
            (rows[i], rows[j]) = (rows[j], rows[i]);
            (order[i], order[j]) = (order[j], order[i]);
            Services.Audio.Play("drop", 0.6f);
            Layout(false);
            onChanged?.Invoke();
        }

        internal void DragTo(RectTransform row, float localY)
        {
            int i = rows.IndexOf(row);
            int j = Mathf.Clamp(Mathf.FloorToInt(-localY / (rowHeight + spacing) + 0.5f), 0, rows.Count - 1);
            if (i != j)
            {
                var r = rows[i]; var o = order[i];
                rows.RemoveAt(i); order.RemoveAt(i);
                rows.Insert(j, r); order.Insert(j, o);
                Layout(false, row);
                onChanged?.Invoke();
            }
        }

        public void Layout(bool instant, RectTransform skip = null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == skip) continue;
                var target = new Vector2(0, -i * (rowHeight + spacing));
                if (instant) rows[i].anchoredPosition = target;
                else Tween.Anchored(rows[i], target, 0.15f);
            }
        }

        public void Lock()
        {
            foreach (var r in rows)
            {
                foreach (var b in r.GetComponentsInChildren<Button>()) b.interactable = false;
                var d = r.GetComponent<ReorderRowDrag>(); if (d != null) d.enabled = false;
            }
        }

        public RectTransform RowRect => rt;
    }

}

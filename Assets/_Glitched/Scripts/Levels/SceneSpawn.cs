using System;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Hooks that let the level builder run both at runtime and inside the editor: in the editor, prefabs are
    /// instantiated as prefab links (so swapping a model updates every scene) and static scenery is flagged for
    /// static batching.
    /// </summary>
    public static class SceneSpawn
    {
        public static Func<GameObject, Transform, GameObject> InstantiatePrefab = (prefab, parent) => UnityEngine.Object.Instantiate(prefab, parent);
        public static Action<GameObject> MarkStatic = go => { };
        /// <summary>Un-flags scenery that has to move after all (a building the Glitch demolishes).</summary>
        public static Action<GameObject> ClearStatic = go => { };

        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            var go = InstantiatePrefab(prefab, parent);
            return go;
        }

        /// <summary>Instantiates a prefab and scales it uniformly so its height (or largest side) matches.</summary>
        public static GameObject Normalized(GameObject prefab, Transform parent, Vector3 position, float targetSize, float yaw = 0, bool byHeight = true)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, parent);
            var t = go.transform;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            t.localScale = Vector3.one;
            t.position = Vector3.zero;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float size = byHeight ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                float s = targetSize / Mathf.Max(0.01f, size);
                t.localScale = Vector3.one * s;
                // put its base on the target position
                b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                t.position = position + new Vector3(-b.center.x, -b.min.y, -b.center.z);
            }
            else t.position = position;
            foreach (var c in go.GetComponentsInChildren<Collider>()) PlaceholderFactory.Kill(c);
            return go;
        }
    }
}

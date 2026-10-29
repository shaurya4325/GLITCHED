using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Simple GameObject pool. Gameplay code never calls Instantiate/Destroy in loops:
    /// Pool.Get(key, factory) and Pool.Release(go) instead.
    /// </summary>
    public static class Pool
    {
        static readonly Dictionary<string, Stack<GameObject>> free = new Dictionary<string, Stack<GameObject>>();
        static Transform poolRoot;

        static Transform Root
        {
            get
            {
                if (poolRoot == null)
                {
                    var go = new GameObject("[Pool]");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    poolRoot = go.transform;
                }
                return poolRoot;
            }
        }

        public static GameObject Get(string key, Func<GameObject> factory, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            GameObject go = null;
            if (free.TryGetValue(key, out var stack))
            {
                while (stack.Count > 0 && go == null) go = stack.Pop();
            }
            if (go == null)
            {
                go = factory();
                var tag = go.GetComponent<PooledTag>() ?? go.AddComponent<PooledTag>();
                tag.key = key;
            }
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        public static void Release(GameObject go)
        {
            if (go == null) return;
            var tag = go.GetComponent<PooledTag>();
            if (tag == null) { UnityEngine.Object.Destroy(go); return; }
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            if (!free.TryGetValue(tag.key, out var stack)) free[tag.key] = stack = new Stack<GameObject>();
            stack.Push(go);
        }

        public static void ReleaseAfter(GameObject go, float seconds)
        {
            Tween.After(seconds, () => Release(go), false);
        }
    }
}

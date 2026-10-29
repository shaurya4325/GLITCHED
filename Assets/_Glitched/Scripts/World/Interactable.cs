using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>Base for anything the player can press E / Interact on (NPCs, terminals, doors, shop).</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public string prompt = "Interact";
        public float radius = 1.8f;
        public bool once;
        protected bool used;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        public virtual bool CanInteract(PlayerController p) => !(once && used);
        public abstract void Interact(PlayerController p);

        public virtual string PromptText => prompt;
        public Vector3 Position => transform.position;
    }
}

using UnityEngine;

namespace Glitched
{
    /// <summary>A glitched "save terminal". Touching it sets the respawn point and autosaves.</summary>
    public class Checkpoint : MonoBehaviour
    {
        public string id;
        public bool activated;
        Renderer screen;
        public Vector3 SpawnPosition => transform.position + Vector3.up * 0.2f + Vector3.right * 0.6f;

        void Awake()
        {
            screen = transform.Find("Screen")?.GetComponent<Renderer>();
            var col = GetComponent<Collider>();
            if (col == null) { var b = gameObject.AddComponent<BoxCollider>(); b.isTrigger = true; b.size = new Vector3(2.2f, 3f, 2f); b.center = new Vector3(0, 1.5f, 0); }
        }

        void Start()
        {
            if (Services.Game.Data.checkpointId == id && !string.IsNullOrEmpty(id)) SetActive(true, false);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            var p = other.GetComponent<PlayerController>();
            if (p == null) return;
            p.LastCheckpoint = this;
            if (activated) return;
            SetActive(true, true);
            Services.Game.Data.checkpointId = id;
            Services.Game.Data.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            p.Heal(p.maxStability);
            Services.Save.Write();
            GameEvents.RaiseCheckpoint(this);
        }

        public void SetActive(bool on, bool fx)
        {
            activated = on;
            if (screen != null)
            {
                var mpb = new MaterialPropertyBlock();
                screen.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", on ? new Color(0.2f, 2f, 1.2f) : new Color(1.6f, 0.2f, 1.2f));
                screen.SetPropertyBlock(mpb);
            }
            if (fx)
            {
                Services.Audio.Play("checkpoint");
                VFX.Burst(transform.position + Vector3.up * 1.6f, UIFactory.Theme.green, 30, "spark");
                Services.UI.Toast("SAVE TERMINAL REPAIRED   <color=#9fb0cc>(progress saved)</color>");
            }
        }
    }
}

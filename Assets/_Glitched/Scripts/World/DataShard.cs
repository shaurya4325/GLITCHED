using UnityEngine;

namespace Glitched
{
    /// <summary>A collectible data shard (coins for the cosmetic shop). Collected shards stay collected.</summary>
    public class DataShard : MonoBehaviour
    {
        public string id;
        public int value = 5;
        float baseY;
        bool taken;

        void Start()
        {
            baseY = transform.position.y;
            if (!string.IsNullOrEmpty(id) && Services.Game.Data.collected.Contains(id)) gameObject.SetActive(false);
        }

        void Update()
        {
            var p = transform.position;
            p.y = baseY + Mathf.Sin(Time.time * 3f + p.x) * 0.15f;
            transform.position = p;
            transform.Rotate(0, 140 * Time.deltaTime, 0, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            if (taken || !other.CompareTag("Player")) return;
            taken = true;
            Services.Game.AddCoins(value);
            if (!string.IsNullOrEmpty(id)) Services.Game.Data.collected.Add(id);
            Services.Achievements.Add("shards");
            Services.Audio.Play("coin");
            VFX.Burst(transform.position, UIFactory.Theme.yellow, 14, "star", 4f, 0.25f, 0.5f);
            Services.UI.DamageNumber(transform.position, "+" + value, UIFactory.Theme.yellow, 44);
            gameObject.SetActive(false);
        }
    }
}

using UnityEngine;

namespace Glitched
{
    /// <summary>Marks a group as one of the 2.5D depth layers, so graphics tiers can switch whole layers off.</summary>
    public class DepthLayerTag : MonoBehaviour
    {
        public DepthLayer layer = DepthLayer.L2_Near;

        void Start()
        {
            Apply(Services.Graphics.Tier);
            GameEvents.TierChanged += Apply;
        }

        void OnDestroy() => GameEvents.TierChanged -= Apply;

        void Apply(GraphicsTier t)
        {
            bool on = Services.Graphics.LayerActive(layer);
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(on);
        }
    }
}

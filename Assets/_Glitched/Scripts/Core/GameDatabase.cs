using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Glitched
{
    /// <summary>
    /// The one asset (Resources/GlitchedDatabase) that points at every data asset of the game.
    /// Created by the menu Glitched -> Build Everything. If it is missing, defaults are built in memory.
    /// </summary>
    [CreateAssetMenu(menuName = "Glitched/Game Database", fileName = "GlitchedDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public List<GraphicsTierSO> graphicsTiers = new List<GraphicsTierSO>();
        public List<ArcDataSO> arcs = new List<ArcDataSO>();
        public List<CharacterProfileSO> characters = new List<CharacterProfileSO>();
        [Tooltip("Generated citizens (HumanGen) the NPCs pick from, with their heights.")]
        public List<GameObject> citizenModels = new List<GameObject>();
        public List<float> citizenHeights = new List<float>();
        public List<EnemyDataSO> enemies = new List<EnemyDataSO>();
        public List<CosmeticSO> cosmetics = new List<CosmeticSO>();
        public List<AchievementSO> achievements = new List<AchievementSO>();
        public SfxLibrarySO sfx;
        public UIThemeSO uiTheme;
        public MaterialLibrarySO materials;
        public List<EnvironmentKitSO> kits = new List<EnvironmentKitSO>();
        public ScriptableRendererFeature fullscreenGlitchFeature;
        public ScriptableRendererFeature ssaoFeature;   // ambient occlusion (High / Ultra)
        public bool isRuntimeDefault;

        static GameDatabase cached;

        public static GameDatabase Load()
        {
            if (cached != null) return cached;
            cached = Resources.Load<GameDatabase>("GlitchedDatabase");
            if (cached == null)
            {
                Debug.LogWarning("[Glitched] Resources/GlitchedDatabase not found: using built-in defaults. Run Glitched -> Build Everything in the editor.");
                cached = ContentDefaults.CreateRuntimeDatabase();
            }
            return cached;
        }

        public GraphicsTierSO Tier(GraphicsTier t)
        {
            foreach (var g in graphicsTiers) if (g != null && g.tier == t) return g;
            return graphicsTiers.Count > 0 ? graphicsTiers[Mathf.Clamp((int)t, 0, graphicsTiers.Count - 1)] : null;
        }

        public ArcDataSO Arc(string id) => arcs.Find(a => a != null && a.id == id);
        public CharacterProfileSO Character(string id) => characters.Find(c => c != null && c.id == id);
        public EnemyDataSO Enemy(string id) => enemies.Find(e => e != null && e.id == id);
        public CosmeticSO Cosmetic(string id) => cosmetics.Find(c => c != null && c.id == id);
        public EnvironmentKitSO Kit(string id) => kits.Find(k => k != null && k.id == id);

        /// <summary>Finds the arc and level that own a scene name.</summary>
        public bool FindLevelByScene(string scene, out ArcDataSO arc, out LevelInfo level)
        {
            foreach (var a in arcs)
            {
                if (a == null) continue;
                foreach (var l in a.levels)
                    if (l.scene == scene) { arc = a; level = l; return true; }
            }
            arc = null; level = null; return false;
        }
    }
}

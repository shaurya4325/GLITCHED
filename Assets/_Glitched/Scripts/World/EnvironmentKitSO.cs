using System.Collections.Generic;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// The 3D pieces a region is built from (filled automatically from the imported asset packs).
    /// Every list can be edited by hand: drop your own prefabs in and rebuild the scenes.
    /// </summary>
    [CreateAssetMenu(menuName = "Glitched/Environment Kit", fileName = "EnvironmentKit")]
    public class EnvironmentKitSO : ScriptableObject
    {
        public string id;
        [Tooltip("Whole buildings / big structures for the near and mid city (L2, L3).")]
        public List<GameObject> buildings = new List<GameObject>();
        [Tooltip("Facade pieces used to assemble modular buildings.")]
        public List<GameObject> facadePieces = new List<GameObject>();
        public List<GameObject> roofPieces = new List<GameObject>();
        [Tooltip("Small props for the near layer and gameplay plane decoration.")]
        public List<GameObject> props = new List<GameObject>();
        [Tooltip("Foreground occluders (L0): pipes, railings, cables, signs, leaves.")]
        public List<GameObject> foreground = new List<GameObject>();
        public List<GameObject> nature = new List<GameObject>();
        public List<GameObject> platforms = new List<GameObject>();
        public List<GameObject> hazards = new List<GameObject>();
        public List<GameObject> vehicles = new List<GameObject>();
        public List<GameObject> creatures = new List<GameObject>();
        [Tooltip("Textures for procedural towers/walls (world-space mapped).")]
        public List<Texture2D> facadeTextures = new List<Texture2D>();
        [Tooltip("Textures for organic / rocky / lab surfaces in the backdrop.")]
        public List<Texture2D> surfaceTextures = new List<Texture2D>();
        [Tooltip("Ground surface material for gameplay platforms.")]
        public Material groundMaterial;
        public Material groundSideMaterial;
        public Material wallMaterial;
        public Material liquidMaterial;
    }
}

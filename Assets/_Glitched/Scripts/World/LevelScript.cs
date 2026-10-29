using System.Collections;
using UnityEngine;

namespace Glitched
{
    /// <summary>Base for per-level scripted moments (arrival cutscenes, tutorials). Runs once after the level loads.</summary>
    public abstract class LevelScript : MonoBehaviour
    {
        public abstract IEnumerator Run(RegionDirector director);
    }
}

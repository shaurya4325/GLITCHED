using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A named character: personality, look and comedy. Swap modelPrefab for your own 3D model:
    /// no code changes needed. If modelPrefab is empty, a placeholder is built from primitives.
    /// </summary>
    [CreateAssetMenu(menuName = "Glitched/Character Profile", fileName = "Character")]
    public class CharacterProfileSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string personality;
        public string catchphrase;
        [TextArea] public string failureReaction;
        public string idleStyle = "bob";

        [Header("Look")]
        public GameObject modelPrefab;
        [Tooltip("Height in metres the model is scaled to.")]
        public float height = 1.6f;
        public RuntimeAnimatorController animator;
        public bool humanoid = true;
        public Color primaryColor = new Color(0.2f, 0.6f, 1f);
        public Color secondaryColor = Color.white;
        public Color skinColor = new Color(0.86f, 0.66f, 0.52f);
        [Tooltip("Folder in Resources holding face_<expression>.png textures (optional).")]
        public string faceFolder;
        public bool useFaceDecal;
        public bool toonOutline = true;

        [Header("Sound")]
        public AudioClip signatureSound;
        [Tooltip("Pitch of the gibberish talk blips.")]
        public float voicePitch = 1f;
        public string voiceBlipKey = "blip";
    }
}

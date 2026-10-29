using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The Nexus overview: a 3D diorama of floating region islands. Arcs unlock in order; restored islands are healthy,
    /// unrestored ones glitch, locked ones are dark. Pick an island and enter its hub.
    /// </summary>
    public class WorldMapScreen : MonoBehaviour
    {
        [System.Serializable]
        public class Island { public string arcId; public Transform root; }
        public List<Island> islands = new List<Island>();
        public Transform cameraTransform;
        public Light sun;
        int index;
        TextMeshProUGUI title, body, status;
        Button enter;
        Vector3 camVel;
        readonly Dictionary<Renderer, MaterialPropertyBlock> blocks = new Dictionary<Renderer, MaterialPropertyBlock>();

        void Start()
        {
            PauseMenu.Allowed = true;
            Services.Corruption.SetupRegion(Services.Db.Arc("TheCore")?.theme, sun, 1f);
            var cam = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : FindFirstObjectByType<Camera>();
            if (cam != null) Services.Graphics.ConfigureCamera(cam);
            Services.Audio.Music.Cinematic("puzzle");
            // start at the furthest unlocked island
            for (int i = 0; i < islands.Count; i++) if (Services.Game.IsArcUnlocked(islands[i].arcId)) index = i;
            if (SceneLoader.NextSpawnId.StartsWith("arc:")) { string want = SceneLoader.NextSpawnId.Substring(4); index = Mathf.Max(0, islands.FindIndex(x => x.arcId == want)); }
            ApplyIslandStates();
            Build();
            Select(index);
            if (!Services.Game.Data.HasFlag("map_seen"))
            {
                Services.Game.Data.SetFlag("map_seen");
                Companion.Say("This is The Nexus from above. Four regions, four very rude Glitch Lords, one very cool companion.");
            }
        }

        void ApplyIslandStates()
        {
            foreach (var isl in islands)
            {
                if (isl.root == null) continue;
                var p = Services.Game.Data.Arc(isl.arcId);
                bool unlocked = p.unlocked, restored = p.restored;
                foreach (var r in isl.root.GetComponentsInChildren<Renderer>())
                {
                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);
                    mpb.SetFloat("_Glitchiness", restored ? 0f : unlocked ? 0.8f : 1.4f);
                    if (!unlocked) mpb.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.3f));
                    r.SetPropertyBlock(mpb);
                    blocks[r] = mpb;
                }
                var bob = isl.root.GetComponent<FloatingBob>() ?? isl.root.gameObject.AddComponent<FloatingBob>();
                bob.bobHeight = 0.6f; bob.tilt = 1.5f; bob.corruptedLift = Vector3.zero; bob.corruptedTilt = Vector3.zero;
            }
        }

        void Build()
        {
            var th = UIFactory.Theme;
            var root = UIFactory.Stretch(UIFactory.Rect("WorldMap", Services.UI.SceneLayer));
            var head = UIFactory.Text(root, "THE NEXUS", 54, th.cyan, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(head.rectTransform, new Vector2(0, 1), new Vector2(50, -40), new Vector2(800, 80), new Vector2(0, 1));
            head.gameObject.AddComponent<GlitchText>().intensityOverride = 0.3f;
            var panel = UIFactory.Panel(root, null, 22, "Info");
            UIFactory.Place(panel.rectTransform, new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(620, 600), new Vector2(1, 0.5f));
            title = UIFactory.Text(panel.transform, "", 46, th.text, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(36, -30), new Vector2(560, 70), new Vector2(0, 1));
            status = UIFactory.Text(panel.transform, "", 24, th.yellow, TextAlignmentOptions.TopLeft, true);
            UIFactory.Place(status.rectTransform, new Vector2(0, 1), new Vector2(36, -104), new Vector2(560, 40), new Vector2(0, 1));
            body = UIFactory.Text(panel.transform, "", 26, th.textDim, TextAlignmentOptions.TopLeft);
            UIFactory.Place(body.rectTransform, new Vector2(0, 1), new Vector2(36, -156), new Vector2(560, 300), new Vector2(0, 1));
            enter = UIFactory.Button(panel.transform, "ENTER", Enter, ButtonStyle.Primary, 32);
            UIFactory.Place(enter.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(540, 90), new Vector2(0.5f, 0));
            var nav = UIFactory.Rect("Nav", root);
            UIFactory.Place(nav, new Vector2(0.4f, 0), new Vector2(0, 50), new Vector2(560, 90), new Vector2(0.5f, 0));
            UIFactory.HLayout(nav, 20);
            UIFactory.Button(nav, "◀", () => Select(index - 1), ButtonStyle.Secondary, 36);
            UIFactory.Button(nav, "▶", () => Select(index + 1), ButtonStyle.Secondary, 36);
            Services.UI.Hud.Show(false);
            UIFactory.Select(enter);
        }

        void Select(int i)
        {
            if (islands.Count == 0) return;
            index = (i + islands.Count) % islands.Count;
            var isl = islands[index];
            var arc = Services.Db.Arc(isl.arcId);
            var p = Services.Game.Data.Arc(isl.arcId);
            if (arc == null) return;
            title.text = arc.displayName.ToUpper();
            status.text = !p.unlocked ? "<color=#ff5a6a>LOCKED</color>" : p.restored ? "<color=#7cffa0>RESTORED</color>" : "<color=#ff3dc8>CORRUPTED</color>";
            int done = 0; foreach (var l in arc.levels) if (p.levelsDone.Contains(l.id)) done++;
            float avg = 0; int n = 0;
            foreach (var c in arc.concepts) if (Services.Game.Data.mastery.TryGetValue(c, out var m)) { avg += m.value; n++; }
            body.text = $"<b>{arc.subject}</b>\n{arc.tagline}\n\n<color=#9fb0cc>Areas cleared:</color> {done}/{arc.levels.Count}\n<color=#9fb0cc>Mastery:</color> {(n > 0 ? Mathf.RoundToInt(avg / n) + "%" : "not met yet")}" +
                (arc.boss != null ? $"\n<color=#9fb0cc>Glitch Lord:</color> {arc.boss.displayName}" : "");
            enter.interactable = p.unlocked;
            UIFactory.Label(enter).text = p.unlocked ? (p.restored ? "REVISIT" : "ENTER") : "RESTORE THE PREVIOUS REGION FIRST";
            Services.Audio.Play("select");
        }

        void Enter()
        {
            var arc = Services.Db.Arc(islands[index].arcId);
            if (arc == null || !Services.Game.IsArcUnlocked(arc.id)) return;
            var target = arc.Hub ?? arc.levels[0];
            Services.Scenes.Load(target.scene, "fromMap");
        }

        void Update()
        {
            var nav = Services.Input.Move;
            if (Services.Input.Asset != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null)
                {
                    if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Select(index - 1);
                    if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Select(index + 1);
                }
            }
            if (cameraTransform != null && islands.Count > 0 && islands[index].root != null)
            {
                var t = islands[index].root.position;
                Vector3 want = t + new Vector3(-8f, 9f, -26f);
                cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, want, ref camVel, 0.6f);
                var look = Quaternion.LookRotation(t + Vector3.up * 2f - cameraTransform.position);
                cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, look, Time.deltaTime * 3f);
            }
        }
    }
}

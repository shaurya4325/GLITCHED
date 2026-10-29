using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Glitched
{
    /// <summary>
    /// The full-screen verification screen: QR code + pairing code while waiting for the team, "no server" help with
    /// an address box, the blocked / locked screen, the VERIFIED animation, and the small badge on the title screen.
    /// </summary>
    public static class LicenceScreen
    {
        public static bool RetryRequested;

        static RectTransform root, qrBox, helpBox, addressRow;
        static TextMeshProUGUI title, body, status, code, helpText, deviceText, icon;
        static RawImage qrImage;
        static Texture2D qrTex;
        static Button retryBtn;
        static LicenceClient client;
        static GameObject badge;

        static UIThemeSO Th => UIFactory.Theme;

        static void Build()
        {
            if (root != null) return;
            root = UIFactory.Stretch(UIFactory.Rect("LicenceScreen", Services.UI.PopupLayer));
            root.SetAsLastSibling();
            UIFactory.Blocker(root, 0.97f);

            var panel = UIFactory.Panel(root, new Color(0.03f, 0.04f, 0.08f, 0.98f), 26, "Panel");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1560, 820));
            var glow = UIFactory.Image(panel.transform, new Color(Th.cyan.r, Th.cyan.g, Th.cyan.b, 0.6f), null, "TopLine");
            UIFactory.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -2), new Vector2(1500, 3));

            // left column
            var left = UIFactory.Rect("Left", panel.transform);
            UIFactory.Place(left, new Vector2(0f, 0.5f), new Vector2(70, 0), new Vector2(860, 700), new Vector2(0, 0.5f));
            UIFactory.VLayout(left, 18, 0, TextAnchor.UpperLeft);
            var kicker = UIFactory.Mono(left, "// COPY VERIFICATION", 22, Th.cyan);
            UIFactory.Size(kicker, -1, 34);
            title = UIFactory.Text(left, "VERIFY THIS COPY", 64, Th.text, TextAlignmentOptions.TopLeft, true);
            UIFactory.Size(title, -1, 90);
            title.gameObject.AddComponent<GlitchText>().intensityOverride = 0.15f;
            body = UIFactory.Text(left, "", 28, Th.textDim, TextAlignmentOptions.TopLeft);
            UIFactory.Size(body, -1, 200);
            status = UIFactory.Text(left, "", 28, Th.cyan, TextAlignmentOptions.TopLeft);
            UIFactory.Size(status, -1, 80);
            deviceText = UIFactory.Mono(left, $"PC: {SystemInfo.deviceName}   ·   GLITCHED {Application.version}", 20, new Color(1, 1, 1, 0.35f));
            UIFactory.Size(deviceText, -1, 30);

            var row = UIFactory.Rect("Buttons", left);
            UIFactory.Size(row, -1, 80);
            UIFactory.HLayout(row, 16, 0, TextAnchor.MiddleLeft, false);
            retryBtn = UIFactory.Button(row, "RETRY", () => RetryRequested = true, ButtonStyle.Primary, 26);
            UIFactory.Size(retryBtn, 220, 76);
            var addrBtn = UIFactory.Button(row, "SERVER ADDRESS", () => { addressRow.gameObject.SetActive(!addressRow.gameObject.activeSelf); }, ButtonStyle.Secondary, 24);
            UIFactory.Size(addrBtn, 290, 76);
            var quit = UIFactory.Button(row, "QUIT", Application.Quit, ButtonStyle.Ghost, 24);
            UIFactory.Size(quit, 160, 76);

            addressRow = UIFactory.Rect("Address", left);
            UIFactory.Size(addressRow, -1, 70);
            UIFactory.HLayout(addressRow, 12, 0, TextAnchor.MiddleLeft, false);
            var input = UIFactory.Input(addressRow, "e.g. 192.168.137.1:8080", 26);
            UIFactory.Size(input, 470, 66);
            UIFactory.Size(UIFactory.Button(addressRow, "CONNECT", () => { if (client != null) client.SetManualServer(input.text); }, ButtonStyle.Primary, 24), 200, 66);
            input.onSubmit.AddListener(v => { if (client != null) client.SetManualServer(v); });
            addressRow.gameObject.SetActive(false);

            // right column: the QR code
            qrBox = UIFactory.Rect("QR", panel.transform);
            UIFactory.Place(qrBox, new Vector2(1f, 0.5f), new Vector2(-90, 10), new Vector2(480, 640), new Vector2(1, 0.5f));
            var frame = UIFactory.Panel(qrBox, Color.white, 22, "Frame", false);
            UIFactory.Place(frame.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(470, 470), new Vector2(0.5f, 1f));
            var qrRt = UIFactory.Rect("Code", frame.transform);
            UIFactory.Stretch(qrRt, 14, 14, 14, 14);
            qrImage = qrRt.gameObject.AddComponent<RawImage>();
            code = UIFactory.Text(qrBox, "", 64, Th.magenta, TextAlignmentOptions.Center, true);
            UIFactory.Place(code.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(480, 80));
            var scan = UIFactory.Text(qrBox, "Scan with the GLITCHED admin phone", 24, Th.textDim, TextAlignmentOptions.Center);
            UIFactory.Place(scan.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(480, 40));

            // right column: help when there is no QR yet
            helpBox = UIFactory.Rect("Help", panel.transform);
            UIFactory.Place(helpBox, new Vector2(1f, 0.5f), new Vector2(-90, 0), new Vector2(480, 600), new Vector2(1, 0.5f));
            var ring = UIFactory.Image(helpBox, new Color(Th.cyan.r, Th.cyan.g, Th.cyan.b, 0.12f), UISprites.Circle(256), "Ring");
            UIFactory.Place(ring.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(260, 260), new Vector2(0.5f, 1f));
            icon = UIFactory.Text(ring.transform, "?", 150, Th.cyan, TextAlignmentOptions.Center, true);
            UIFactory.Stretch(icon.rectTransform);
            helpText = UIFactory.Text(helpBox, "", 24, Th.textDim, TextAlignmentOptions.Top);
            UIFactory.Place(helpText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(480, 300), new Vector2(0.5f, 0f));
            qrBox.gameObject.SetActive(false);

            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            Tween.Fade(cg, 1, 0.35f);
            UIFactory.Select(retryBtn);
        }

        public static void ShowVerify(LicenceClient c)
        {
            client = c;
            Build();
            title.color = Th.text;
            SetTitle("VERIFY THIS COPY");
            body.text = "Every copy of GLITCHED is verified by the team at the stall.\n" +
                        "Connect this PC to the stall's Wi-Fi and open the game: a QR code appears here. " +
                        "A team member scans it with the admin phone, and the game unlocks by itself.";
            retryBtn.gameObject.SetActive(true);
        }

        public static void ShowQr(int size, string bits, string pairCode, string url)
        {
            if (root == null) return;
            if (qrTex != null) Object.Destroy(qrTex);
            int q = 4, n = size + q * 2;
            qrTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    if (bits[r * size + c] == '1') px[(n - 1 - (r + q)) * n + c + q] = new Color32(5, 6, 13, 255);   // textures start at the bottom row
            qrTex.SetPixels32(px);
            qrTex.Apply();
            qrImage.texture = qrTex;
            code.text = pairCode;
            qrBox.gameObject.SetActive(true);
            helpBox.gameObject.SetActive(false);
            body.text = "Show this screen to a GLITCHED team member.\nThey scan the QR code with the admin phone, check that the code matches and choose your access. " +
                        $"\n<size=80%><color=#5f7390>No phone camera? Approve it on the admin page instead, or open {url}</color></size>";
            Services.Audio.Play("panel_in");
        }

        public static void ShowNoServer(string extra)
        {
            if (root == null) return;
            title.color = Th.text;
            SetTitle("FIND THE STALL");
            qrBox.gameObject.SetActive(false);
            helpBox.gameObject.SetActive(true);
            body.text = (extra != null ? $"<color=#ffd84a>{extra}</color>\n" : "") +
                        "This copy needs to be verified once by the GLITCHED server at the stall, but it can't be found on this network.";
            icon.text = "?"; icon.color = Th.cyan;
            helpText.text = "1. Join the same Wi-Fi as the GLITCHED stall\n   (or its laptop's Mobile Hotspot).\n2. Press RETRY.\n\nStill nothing? Press SERVER ADDRESS and type the address shown on the stall's admin page.";
            status.text = "<color=#ffd84a>No GLITCHED server found.</color> Trying again every few seconds…";
        }

        public static void ShowBlocked(string head, string text, LicenceClient c)
        {
            if (c != null) client = c;
            Build();
            title.color = Th.red;
            SetTitle(head);
            body.text = text + "\n\nIf you think this is a mistake, talk to the GLITCHED team at the stall.";
            qrBox.gameObject.SetActive(false);
            helpBox.gameObject.SetActive(true);
            icon.text = "!"; icon.color = Th.red;
            helpText.text = "Access is managed from the stall's admin page.\nThe game checks again by itself.";
            status.text = "";
            retryBtn.gameObject.SetActive(c != null);
            Services.Audio.Play("error");
        }

        public static void SetStatus(string text)
        {
            if (status != null) status.text = text;
        }

        public static IEnumerator Success(LicenceToken t)
        {
            if (root == null) yield break;
            qrBox.gameObject.SetActive(false);
            helpBox.gameObject.SetActive(true);
            title.color = Th.green;
            SetTitle("COPY VERIFIED");
            body.text = string.IsNullOrEmpty(t.nm) ? "Welcome to GLITCHED." : $"Welcome, <color=#ffffff>{t.nm}</color>.";
            status.text = "Access: <color=#7cffa0>" + LicenceAccess.Label(t.typ) + "</color>";
            icon.text = "✓"; icon.color = Th.green;
            helpText.text = "This PC stays verified.\nYou won't see this screen again.";
            retryBtn.gameObject.SetActive(false);
            Services.Audio.Play("restore");
            yield return new WaitForSecondsRealtime(2.2f);
        }

        public static void Hide()
        {
            if (root == null) return;
            var go = root.gameObject;
            root = null;
            var cg = go.GetComponent<CanvasGroup>();
            Services.UI.StartCoroutine(FadeOut(go, cg));
        }

        static IEnumerator FadeOut(GameObject go, CanvasGroup cg)
        {
            if (cg != null) yield return Tween.Fade(cg, 0, 0.35f);
            Object.Destroy(go);
            if (qrTex != null) { Object.Destroy(qrTex); qrTex = null; }
        }

        static void SetTitle(string text)
        {
            var g = title.GetComponent<GlitchText>();
            if (g != null) g.Show(text); else title.text = text;
        }

        /// <summary>"✓ VERIFIED COPY" in the corner of the title screen.</summary>
        public static void Badge(bool show)
        {
            if (!show) { if (badge != null) badge.SetActive(false); return; }
            if (badge == null)
            {
                var p = UIFactory.Panel(Services.UI.PopupLayer, new Color(0.03f, 0.05f, 0.08f, 0.8f), 12, "LicenceBadge");
                p.transform.SetAsFirstSibling();
                UIFactory.Place(p.rectTransform, new Vector2(0f, 1f), new Vector2(24, -24), new Vector2(820, 46), new Vector2(0, 1));
                var t = UIFactory.Text(p.transform, "", 20, Th.green, TextAlignmentOptions.MidlineLeft);
                UIFactory.Stretch(t.rectTransform, 16, 4, 12, 4);
                badge = p.gameObject;
            }
            badge.GetComponentInChildren<TextMeshProUGUI>().text = "✓ VERIFIED COPY  ·  " + LicenceAccess.Label(LicenceAccess.Type) +
                (string.IsNullOrEmpty(LicenceAccess.Name) ? "" : "  ·  " + LicenceAccess.Name);
            badge.SetActive(true);
        }
    }
}

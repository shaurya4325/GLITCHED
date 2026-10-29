using TMPro;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// Developer mode (main menu, top right, password protected): every question shows its answer — quiz battles,
    /// boss / door challenges and the quiz-jump pillars. Remembered on this PC until switched off.
    /// </summary>
    public static class DevMode
    {
        const string Key = "glitched_devmode";
        // PBKDF2-HMAC-SHA256 of the password (60 000 rounds): the password itself is not in the source.
        static readonly byte[] Salt = System.Convert.FromBase64String("TJXegUJvJmrBnGRWTnRiTg==");
        static readonly byte[] Hash = System.Convert.FromBase64String("r8bqa+iI4TGP3McAoWjOoDURy6BpvBFq9k5/zR3SD1o=");
        const int Rounds = 60000;

        public static bool On
        {
            get => ForceOn || PlayerPrefs.GetInt(Key, 0) == 1;
            set { PlayerPrefs.SetInt(Key, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>Capture runs only (-captureDev 1): on for this run, never saved.</summary>
        public static bool ForceOn;

        public static bool Check(string attempt)
        {
            if (string.IsNullOrEmpty(attempt)) return false;
            var got = Pbkdf2(System.Text.Encoding.UTF8.GetBytes(attempt.Trim()), Salt, Rounds);
            int diff = 0;
            for (int i = 0; i < Hash.Length; i++) diff |= got[i] ^ Hash[i];
            return diff == 0;
        }

        /// <summary>One 32-byte block of PBKDF2 with HMAC-SHA256 (RFC 8018).</summary>
        static byte[] Pbkdf2(byte[] password, byte[] salt, int rounds)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA256(password))
            {
                var block = new byte[salt.Length + 4];
                System.Buffer.BlockCopy(salt, 0, block, 0, salt.Length);
                block[block.Length - 1] = 1;
                var u = hmac.ComputeHash(block);
                var result = (byte[])u.Clone();
                for (int r = 1; r < rounds; r++)
                {
                    u = hmac.ComputeHash(u);
                    for (int i = 0; i < result.Length; i++) result[i] ^= u[i];
                }
                return result;
            }
        }

        /// <summary>The password prompt; onChanged runs after dev mode is switched on or off.</summary>
        public static void Prompt(System.Action onChanged)
        {
            if (On)
            {
                On = false;
                Services.UI.Toast("DEVELOPER MODE OFF");
                onChanged?.Invoke();
                return;
            }
            var th = UIFactory.Theme;
            var root = UIFactory.Rect("DevPrompt", Services.UI.PopupLayer);
            UIFactory.Stretch(root);
            UIFactory.Blocker(root, 0.75f);
            var panel = UIFactory.Panel(root, null, 24, "Panel");
            UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 430));
            UIFactory.VLayout(panel, 14, 34);
            UIFactory.Size(UIFactory.Text(panel.transform, "DEVELOPER MODE", 46, th.yellow, TextAlignmentOptions.Center, true), -1, 60);
            UIFactory.Size(UIFactory.Text(panel.transform, "Enter the password to show the answer to every question.", 24, th.textDim, TextAlignmentOptions.Center), -1, 40);
            var input = UIFactory.Input(panel.transform, "Password", 34);
            input.contentType = TMP_InputField.ContentType.Pin;
            input.asteriskChar = '•';
            input.ForceLabelUpdate();
            UIFactory.Size(input, -1, 70);
            var error = UIFactory.Text(panel.transform, "", 22, th.red, TextAlignmentOptions.Center);
            UIFactory.Size(error, -1, 30);
            var row = UIFactory.Rect("Row", panel.transform);
            UIFactory.Size(row, -1, 70);
            UIFactory.HLayout(row, 16);
            void Try()
            {
                if (Check(input.text))
                {
                    On = true;
                    Object.Destroy(root.gameObject);
                    Services.Audio.Play("correct");
                    Services.UI.Toast("DEVELOPER MODE ON: answers are shown");
                    onChanged?.Invoke();
                }
                else
                {
                    error.text = "Wrong password";
                    input.text = "";
                    Services.Audio.Play("wrong");
                    input.ActivateInputField();
                }
            }
            UIFactory.Button(row, "UNLOCK", Try, ButtonStyle.Primary, 28);
            UIFactory.Button(row, "CANCEL", () => Object.Destroy(root.gameObject), ButtonStyle.Ghost, 26);
            input.onSubmit.AddListener(_ => Try());
            input.Select();
            input.ActivateInputField();
        }
    }
}

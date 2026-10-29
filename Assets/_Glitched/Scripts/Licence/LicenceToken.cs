using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Glitched
{
    /// <summary>
    /// A signed licence from the GLITCHED stall server: base64url(json) + "." + base64url(RSA-SHA256 signature).
    /// Only the server holds the private key; the game checks the signature with the public key in
    /// Resources/Licence/licence_key.json (written by "node server.js --export-game-key").
    /// </summary>
    [Serializable]
    public class LicenceToken
    {
        public int v;
        public string lid;      // licence id on the server
        public string did;      // the PC it belongs to (SystemInfo.deviceUniqueIdentifier)
        public string typ;      // OWNER / FULL / JUDGE / DEMO / BLOCKED
        public long exp;        // expires (unix seconds), 0 = never
        public long off;        // works without the server until (unix seconds), 0 = forever
        public long iat;        // issued at
        public string nm;       // name the team gave this copy
        public string sid;      // server id

        [NonSerialized] public string raw;

        [Serializable] class Key { public string n; public string e; public string fp; }
        static Key key;
        static bool keyLoaded;

        /// <summary>Fingerprint of the embedded public key, or null if the build has none.</summary>
        public static string Fingerprint { get { LoadKey(); return key != null ? key.fp : null; } }

        static void LoadKey()
        {
            if (keyLoaded) return;
            keyLoaded = true;
            var ta = Resources.Load<TextAsset>("Licence/licence_key");
            if (ta == null) { Debug.LogWarning("[Licence] no Resources/Licence/licence_key.json in this build"); return; }
            try { key = JsonUtility.FromJson<Key>(ta.text); } catch (Exception e) { Debug.LogWarning("[Licence] bad key file: " + e.Message); }
        }

        /// <summary>Checks the signature and that it belongs to this PC. Returns null for anything else.</summary>
        public static LicenceToken Verify(string token, string deviceId)
        {
            if (string.IsNullOrEmpty(token)) return null;
            LoadKey();
            if (key == null) return null;
            var parts = token.Split('.');
            if (parts.Length != 2) return null;
            try
            {
                using (var rsa = RSA.Create())
                {
                    rsa.ImportParameters(new RSAParameters { Modulus = B64U(key.n), Exponent = B64U(key.e) });
                    if (!rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0]), B64U(parts[1]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) return null;
                }
                var t = JsonUtility.FromJson<LicenceToken>(Encoding.UTF8.GetString(B64U(parts[0])));
                if (t == null || t.did != deviceId) return null;
                t.raw = token;
                return t;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Licence] could not check a licence: " + e.Message);
                return null;
            }
        }

        public bool Expired(long now) => exp > 0 && now >= exp;
        public bool UsableOffline(long now) => !Expired(now) && typ != "BLOCKED" && (off == 0 || now < off);

        static byte[] B64U(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
            return Convert.FromBase64String(s);
        }
    }
}

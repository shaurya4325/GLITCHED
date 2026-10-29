using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace Glitched
{
    /// <summary>
    /// Copy verification. On the first launch the game finds the GLITCHED stall server on the Wi-Fi (UDP beacon,
    /// the last known address, this PC, or an address typed in), shows its QR code and waits until the team approves
    /// it from the admin phone. The signed licence is then kept in the player data folder and checked again every
    /// minute while playing, so the team can block, remove or change a copy at any time. Without the server a copy
    /// keeps working until its offline date. Editor and development builds skip all of this (unless -licence 1).
    /// </summary>
    public class LicenceClient : MonoBehaviour
    {
        public const int DiscoveryPort = 47777;
        public const int DefaultPort = 8080;
        const float HeartbeatSeconds = 60f;

        public static bool Required => (!Application.isEditor && !Debug.isDebugBuild) || HasArg("-licence");

        public bool Granted { get; private set; }
        public string Server { get; private set; }
        public LicenceToken Token { get; private set; }

        string deviceId;
        Cache cache;
        bool locked;
        float nextBeat;

        [Serializable]
        class Cache { public string token; public string server; public long lastSeen; public int messageId; }
        [Serializable] class Hello { public int glitched; public string fp; }
        [Serializable] class QrData { public int size; public string bits; }
        [Serializable]
        class Reply
        {
            public string status; public string token; public string pairId; public string code; public string url;
            public QrData qr; public string reason; public string type; public Message message; public int expiresIn;
        }
        [Serializable] class Message { public int id; public string text; }

        static string CachePath => Path.Combine(Application.persistentDataPath, "licence.json");
        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        static bool HasArg(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;

        void Awake()
        {
            deviceId = CleanId(SystemInfo.deviceUniqueIdentifier);
            cache = LoadCache();
            SceneManager.sceneLoaded += (s, m) => { if (m == LoadSceneMode.Single) LicenceScreen.Badge(s.name == "Title" && Granted && Required); };
        }

        static string CleanId(string id)
        {
            var sb = new StringBuilder();
            foreach (char c in id ?? "") if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            var s = sb.ToString();
            if (s.Length < 8) s = (s + "pc" + SystemInfo.deviceName.GetHashCode().ToString("x8")).PadRight(8, '0');
            return s.Length > 128 ? s.Substring(0, 128) : s;
        }

        // ------------------------------------------------------------------ the gate (boot, before the title)
        bool gateStarted;

        /// <summary>Waits until this copy may be played (starts the check once; boot and capture runs both wait on it).</summary>
        public IEnumerator WaitGranted()
        {
            if (!gateStarted) { gateStarted = true; StartCoroutine(Gate()); }
            while (!Granted) yield return null;
        }

        /// <summary>Runs until this copy may be played. Shows the verification screen if needed.</summary>
        IEnumerator Gate()
        {
            if (!Required) { Granted = true; yield break; }
            if (string.IsNullOrEmpty(LicenceToken.Fingerprint))
            {
                LicenceScreen.ShowBlocked("THIS BUILD CANNOT BE VERIFIED", "It was built without the GLITCHED licence key.", null);
                while (true) yield return null;
            }
            Token = LicenceToken.Verify(cache.token, deviceId);
            yield return Resolve(true);
            Granted = true;
            nextBeat = Time.unscaledTime + HeartbeatSeconds;
        }

        /// <summary>Finds the server, checks or requests a licence, and loops on the screen until access is granted.</summary>
        IEnumerator Resolve(bool atLaunch)
        {
            bool screen = false;
            while (true)
            {
                if (screen) LicenceScreen.SetStatus("Looking for the GLITCHED server…");
                string found = null;
                yield return FindServer(s => found = s, screen ? 4f : 2.5f);

                if (found != null)
                {
                    Server = found;
                    cache.server = found;
                    SaveCache();
                    if (Token != null)
                    {
                        Reply r = null;
                        yield return Post("/api/v1/check", $"{{\"token\":\"{Token.raw}\",\"deviceId\":\"{deviceId}\",\"version\":\"{Application.version}\",\"launch\":{(atLaunch ? "true" : "false")}}}", x => r = x);
                        if (r != null)
                        {
                            if (r.status == "active" && Accept(r.token)) { ShowMessage(r.message); break; }
                            if (r.status == "blocked") { yield return Blocked("THIS COPY IS BLOCKED", string.IsNullOrEmpty(r.reason) ? "The GLITCHED team has blocked this PC." : r.reason); continue; }
                            if (r.status == "locked") { yield return Blocked("GLITCHED IS LOCKED", "The team has locked every copy for now. It opens again as soon as they unlock it."); continue; }
                            if (Gone(r.status)) { Token = null; cache.token = null; SaveCache(); }   // removed or expired: verify again
                        }
                        else if (Token.UsableOffline(Now) && !ClockRolledBack()) break;   // server went away mid-check
                    }
                    if (Token == null)
                    {
                        screen = true;
                        bool done = false;
                        yield return Pair(ok => done = ok);
                        if (done) break;
                        yield return WaitOrRetry(3f);
                        continue;
                    }
                }
                else if (Token != null && Token.UsableOffline(Now) && !ClockRolledBack())
                {
                    Debug.Log("[Licence] server not found: using the offline licence");
                    break;
                }

                // no server and no usable licence
                if (!screen) { LicenceScreen.ShowVerify(this); screen = true; }
                LicenceScreen.ShowNoServer(Token != null && Token.Expired(Now) ? "This copy's access has ended." : null);
                yield return WaitOrRetry(6f);
            }
            LicenceAccess.Set(Token.typ, Token.nm);
            if (screen) yield return LicenceScreen.Success(Token);
            LicenceScreen.Hide();
        }

        bool Accept(string raw)
        {
            var t = LicenceToken.Verify(raw, deviceId);
            if (t == null) { Debug.LogWarning("[Licence] the server sent a licence that does not verify"); return false; }
            string before = Token != null ? Token.typ : null;
            Token = t;
            cache.token = raw;
            cache.lastSeen = Math.Max(cache.lastSeen, Now);
            SaveCache();
            LicenceAccess.Set(t.typ, t.nm);
            if (Granted && before != null && before != t.typ)
            {
                Services.UI.Toast("Your access changed: " + LicenceAccess.Label(t.typ));
                LicenceScreen.Badge(SceneManager.GetActiveScene().name == "Title");
            }
            return true;
        }

        static bool Gone(string status) => status == "unknown" || status == "expired" || status == "invalid";

        static IEnumerator WaitOrRetry(float seconds)
        {
            float wait = Time.unscaledTime + seconds;
            while (Time.unscaledTime < wait && !LicenceScreen.RetryRequested) yield return null;
            LicenceScreen.RetryRequested = false;
        }

        bool ClockRolledBack() => cache.lastSeen > 0 && Now + 3600 < cache.lastSeen;

        IEnumerator Blocked(string title, string text)
        {
            Debug.Log($"[Licence] {title}: {text}");
            LicenceScreen.ShowBlocked(title, text, this);
            float wait = Time.unscaledTime + 10f;
            while (Time.unscaledTime < wait && !LicenceScreen.RetryRequested) yield return null;
            LicenceScreen.RetryRequested = false;
        }

        /// <summary>Asks the server for a licence and shows the QR until the team approves it.</summary>
        IEnumerator Pair(Action<bool> done)
        {
            LicenceScreen.ShowVerify(this);
            LicenceScreen.SetStatus("Asking the server for a code…");
            Reply r = null;
            string body = "{" +
                $"\"deviceId\":\"{deviceId}\",\"deviceName\":\"{Esc(SystemInfo.deviceName)}\",\"os\":\"{Esc(SystemInfo.operatingSystem)}\"," +
                $"\"gpu\":\"{Esc(SystemInfo.graphicsDeviceName)}\",\"ram\":{SystemInfo.systemMemorySize},\"version\":\"{Esc(Application.version)}\"" + "}";
            yield return Post("/api/v1/pair", body, x => r = x);
            if (r == null) { done(false); yield break; }
            if (r.status == "approved" && Accept(r.token)) { done(true); yield break; }
            if (r.status == "blocked") { yield return Blocked("THIS COPY IS BLOCKED", string.IsNullOrEmpty(r.reason) ? "The GLITCHED team has blocked this PC." : r.reason); done(false); yield break; }
            if (r.status != "pending" || r.qr == null) { done(false); yield break; }

            LicenceScreen.ShowQr(r.qr.size, r.qr.bits, r.code, r.url);
            LicenceScreen.SetStatus("Waiting for the GLITCHED team to scan the code…");
            float until = Time.unscaledTime + Mathf.Max(60, r.expiresIn);
            int misses = 0;
            while (Time.unscaledTime < until)
            {
                float t = Time.unscaledTime + 2f;
                while (Time.unscaledTime < t && !LicenceScreen.RetryRequested) yield return null;
                if (LicenceScreen.RetryRequested) { LicenceScreen.RetryRequested = false; done(false); yield break; }
                Reply p = null;
                yield return Get($"/api/v1/pair/{r.pairId}?d={deviceId}", x => p = x);
                if (p == null) { if (++misses > 5) { done(false); yield break; } LicenceScreen.SetStatus("Lost the server… trying again"); continue; }
                misses = 0;
                if (p.status == "approved" && Accept(p.token)) { done(true); yield break; }
                if (p.status == "denied") { LicenceScreen.SetStatus("<color=#ff5a6a>The team refused this copy.</color> Press RETRY to ask again."); while (!LicenceScreen.RetryRequested) yield return null; LicenceScreen.RetryRequested = false; done(false); yield break; }
                if (p.status == "expired") { done(false); yield break; }
                LicenceScreen.SetStatus("Waiting for the GLITCHED team to scan the code…");
            }
            done(false);
        }

        // ------------------------------------------------------------------ while playing
        void Update()
        {
            if (!Required || !Granted || locked || Time.unscaledTime < nextBeat) return;
            nextBeat = Time.unscaledTime + HeartbeatSeconds;
            StartCoroutine(Heartbeat());
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus && Required && Granted && !locked) nextBeat = Mathf.Min(nextBeat, Time.unscaledTime + 2f);
        }

        IEnumerator Heartbeat()
        {
            if (Token == null) yield break;
            Reply r = null;
            if (!string.IsNullOrEmpty(Server))
                yield return Post("/api/v1/check", $"{{\"token\":\"{Token.raw}\",\"deviceId\":\"{deviceId}\",\"version\":\"{Application.version}\"}}", x => r = x);
            if (r == null && string.IsNullOrEmpty(Server))
            {
                string found = null;
                yield return FindServer(s => found = s, 2f);
                if (found != null) { Server = found; yield break; }   // check on the next beat
            }
            if (r != null && r.status == "active") { if (Accept(r.token)) ShowMessage(r.message); yield break; }
            if (r == null && Token.UsableOffline(Now) && !ClockRolledBack()) yield break;   // offline, still within its days
            if (r != null && Gone(r.status)) { Token = null; cache.token = null; SaveCache(); }
            yield return LockMidGame();
        }

        /// <summary>Access was taken away while playing: freeze the game behind the verification screen until it is back.</summary>
        IEnumerator LockMidGame()
        {
            locked = true;
            Debug.Log("[Licence] access taken away while playing: game paused behind the verification screen");
            float scale = Time.timeScale;
            bool pauseAllowed = PauseMenu.Allowed;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            PauseMenu.Allowed = false;
            Granted = false;
            yield return Resolve(false);
            Granted = true;
            AudioListener.pause = false;
            Time.timeScale = scale <= 0 ? 1f : scale;
            PauseMenu.Allowed = pauseAllowed;
            locked = false;
            Debug.Log("[Licence] access is back: game resumed");
            nextBeat = Time.unscaledTime + HeartbeatSeconds;
            if (!LicenceAccess.CanEnter(SceneManager.GetActiveScene().name)) { LicenceAccess.ShowDemoWall(); Services.Scenes.Load("Title"); }
        }

        void ShowMessage(Message m)
        {
            if (m == null || string.IsNullOrEmpty(m.text) || m.id == cache.messageId) return;
            cache.messageId = m.id;
            SaveCache();
            Services.UI.Toast("<color=#40ebff>GLITCHED TEAM:</color> " + m.text);
        }

        // ------------------------------------------------------------------ finding the server
        /// <summary>Address typed on the verification screen (host or host:port).</summary>
        public void SetManualServer(string address)
        {
            address = (address ?? "").Trim().TrimEnd('/');
            if (address.Length == 0) return;
            if (!address.StartsWith("http://")) address = "http://" + address.Replace("https://", "");
            if (address.LastIndexOf(':') <= 5) address += ":" + DefaultPort;
            cache.server = address;
            SaveCache();
            LicenceScreen.RetryRequested = true;
        }

        IEnumerator FindServer(Action<string> found, float budget)
        {
            var tried = new HashSet<string>();
            string ok = null;
            // 1. last known address (also a typed one)
            if (!string.IsNullOrEmpty(cache.server))
            {
                tried.Add(cache.server);
                yield return Probe(cache.server, s => ok = s);
                if (ok != null) { found(ok); yield break; }
            }
            // 2. ask the Wi-Fi (UDP broadcast) and this PC at the same time
            var replies = new List<string>();
            float seconds = Mathf.Clamp(budget - 0.5f, 1f, 4f);
            var thread = new Thread(() => Discover(replies, seconds)) { IsBackground = true };
            thread.Start();
            float giveUp = Time.unscaledTime + seconds + 2f;
            foreach (var local in new[] { $"http://127.0.0.1:{DefaultPort}" })
            {
                if (!tried.Add(local)) continue;
                yield return Probe(local, s => ok = s);
                if (ok != null) { found(ok); yield break; }
            }
            while (thread.IsAlive && Time.unscaledTime < giveUp) yield return null;
            string[] heard;
            lock (replies) heard = replies.ToArray();
            Debug.Log($"[Licence] discovery: {heard.Length} server(s) answered" + (thread.IsAlive ? " (still searching, gave up waiting)" : ""));
            foreach (var candidate in heard)
            {
                if (!tried.Add(candidate)) continue;
                yield return Probe(candidate, s => ok = s);
                if (ok != null) { found(ok); yield break; }
            }
            found(null);
        }

        /// <summary>Broadcasts "GLITCHED?" on every network and collects the servers that answer.</summary>
        static void Discover(List<string> results, float seconds)
        {
            try
            {
                using (var udp = new UdpClient(0))
                {
                    udp.EnableBroadcast = true;
                    udp.Client.ReceiveTimeout = 400;
                    var msg = Encoding.UTF8.GetBytes("GLITCHED?");
                    var targets = new List<IPAddress> { IPAddress.Broadcast };
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ua.Address.AddressFamily != AddressFamily.InterNetwork || ua.IPv4Mask == null) continue;
                            var ip = ua.Address.GetAddressBytes();
                            var mask = ua.IPv4Mask.GetAddressBytes();
                            var b = new byte[4];
                            for (int i = 0; i < 4; i++) b[i] = (byte)(ip[i] | ~mask[i]);
                            targets.Add(new IPAddress(b));
                        }
                    }
                    var end = DateTime.UtcNow.AddSeconds(seconds);
                    var nextSend = DateTime.MinValue;
                    while (DateTime.UtcNow < end)
                    {
                        if (DateTime.UtcNow >= nextSend)
                        {
                            foreach (var t in targets) { try { udp.Send(msg, msg.Length, new IPEndPoint(t, DiscoveryPort)); } catch { } }
                            nextSend = DateTime.UtcNow.AddSeconds(0.8);
                        }
                        try
                        {
                            var from = new IPEndPoint(IPAddress.Any, 0);
                            var data = udp.Receive(ref from);
                            var reply = JsonUtility.FromJson<BeaconReply>(Encoding.UTF8.GetString(data));
                            if (reply != null && reply.glitched == 1 && reply.port > 0)
                            {
                                string url = $"http://{from.Address}:{reply.port}";
                                lock (results) if (!results.Contains(url)) results.Add(url);
                                if (reply.fp == LicenceToken.Fingerprint) break;
                            }
                        }
                        catch (SocketException) { }
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning("[Licence] discovery: " + e.Message); }
        }
        [Serializable] class BeaconReply { public int glitched; public int port; public string fp; }

        /// <summary>A server only counts if it holds the same licence key this build was made with.</summary>
        IEnumerator Probe(string baseUrl, Action<string> ok)
        {
            using (var req = UnityWebRequest.Get(baseUrl + "/api/v1/hello"))
            {
                req.timeout = 2;
                var op = req.SendWebRequest();
                float giveUp = Time.unscaledTime + 3f;
                while (!op.isDone && Time.unscaledTime < giveUp) yield return null;
                if (!op.isDone) { req.Abort(); Debug.Log($"[Licence] {baseUrl}: no answer"); yield break; }
                if (req.result != UnityWebRequest.Result.Success) { Debug.Log($"[Licence] {baseUrl}: {req.error}"); yield break; }
                Hello h = null;
                try { h = JsonUtility.FromJson<Hello>(req.downloadHandler.text); } catch { }
                if (h != null && h.glitched == 1 && h.fp == LicenceToken.Fingerprint) ok(baseUrl);
                else if (h != null && h.glitched == 1) Debug.LogWarning($"[Licence] {baseUrl} is a GLITCHED server with a different licence key");
            }
        }

        // ------------------------------------------------------------------ http
        IEnumerator Post(string path, string json, Action<Reply> done)
        {
            if (string.IsNullOrEmpty(Server)) { done(null); yield break; }
            using (var req = new UnityWebRequest(Server + path, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 5;
                yield return req.SendWebRequest();
                done(Parse(req));
            }
        }

        IEnumerator Get(string path, Action<Reply> done)
        {
            if (string.IsNullOrEmpty(Server)) { done(null); yield break; }
            using (var req = UnityWebRequest.Get(Server + path))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();
                done(Parse(req));
            }
        }

        static Reply Parse(UnityWebRequest req)
        {
            if (req.result != UnityWebRequest.Result.Success && req.responseCode < 400) return null;
            try
            {
                var r = JsonUtility.FromJson<Reply>(req.downloadHandler.text);
                return r != null && !string.IsNullOrEmpty(r.status) ? r : null;   // errors (429, 500, ...) count as "no answer"
            }
            catch { return null; }
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "'");

        // ------------------------------------------------------------------ cache
        static Cache LoadCache()
        {
            try { if (File.Exists(CachePath)) return JsonUtility.FromJson<Cache>(File.ReadAllText(CachePath)) ?? new Cache(); }
            catch (Exception e) { Debug.LogWarning("[Licence] " + e.Message); }
            return new Cache();
        }

        void SaveCache()
        {
            try { File.WriteAllText(CachePath, JsonUtility.ToJson(cache)); }
            catch (Exception e) { Debug.LogWarning("[Licence] " + e.Message); }
        }
    }
}

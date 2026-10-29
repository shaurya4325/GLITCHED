# The GLITCHED stall server

One small program on the team's laptop does four jobs:

1. **Website.** The 3D GLITCHED site, with every feature and the hold-to-install button.
2. **Downloads.** It serves `GLITCHED_Setup.exe` (and the portable ZIP) over the stall's Wi-Fi.
3. **Copy verification.** Each installed copy shows a QR code on first launch; the team approves it from a phone.
4. **Admin page.** See every copy, change its access, block or remove it, send messages to every game.

Everything works **without internet**: the site, fonts and 3D libraries are all inside `Server/`.

---

## Starting and stopping

**Double-click "GLITCHED Server" on the desktop.** The server starts and the website opens in its own window.
**Close that window and the server stops by itself.**

- No shortcut? Run `Server\launcher\Make-Desktop-Shortcut.ps1` (right-click → Run with PowerShell).
- Without the shortcut: `cd Server` then `node server.js` (Ctrl+C stops it).
- Needs **Node.js 18 or newer** (https://nodejs.org). Nothing else to install.
- The first time, Windows Firewall asks whether Node.js may use the network. Tick **Private networks** and click **Allow**, otherwise phones and other PCs can't reach it.

The window shows the website. The admin page is at **/admin** (footer link "Team login").

### First run: the admin password

Open `http://localhost:8080/admin` **on the laptop itself** and create the admin password (6+ characters). It can only be created on the laptop, never from another device. You'll type it on your phone the first time you scan a QR code (the phone stays logged in for 12 hours).

Forgot it? Run `Server\launcher\Reset-Admin-Password.cmd`, then create a new one. Copies and settings are kept.

---

## Network setup at the exhibition

The laptop, the phones and the PCs that install the game must be on **the same network**:

- **Best:** turn on **Windows Mobile Hotspot** on the laptop (Settings → Network → Mobile hotspot) and connect everything to it. The game finds the server by itself.
- **School Wi-Fi** also works if it lets devices see each other. Some guest networks block that ("client isolation"); use the hotspot then.
- The admin page lists the server's addresses, e.g. `http://192.168.137.1:8080`. If a game can't find the server, press **SERVER ADDRESS** in the game and type it.
- Visitors can scan the QR code on the admin page (or in the website footer) to open the site on their phones.

---

## Approving a copy

1. Someone installs and opens GLITCHED. It finds the server and shows a **QR code** and a 6-letter code such as `77M-TTS`.
2. Scan the QR code with your phone camera. It opens the approve page (log in the first time).
3. Check the code matches, pick the **access** and optionally a name and an expiry, and tap **APPROVE**.
4. The game shows **COPY VERIFIED** and opens.

No phone? Every waiting copy also appears on the admin page's **Overview** with Approve / Refuse buttons. A loud chime plays when a new one arrives.

The game on the **server laptop itself** is approved automatically as **Owner** (switch this off in Settings).

### Access types

| Access | What the copy can do |
|---|---|
| **Owner** | The full game. Works offline forever. For the team's own PCs. |
| **Full game** | Every chapter, in story order. |
| **Judge** | The full game with every chapter and level open from the start, so judges can jump anywhere. |
| **Demo** | Chapter 1 (Code City) only. The portal to Physics Valley shows "End of the demo". |
| **Blocked** | The game shows a blocked screen and won't open. |

Each copy also has:
- **Expires**: never, 1 hour, end of today, 1 day, 7 days or a date. After that the game asks to be verified again.
- **Offline days**: how long it keeps working without seeing the server (default 7). Owner copies never need the server.

### Managing copies (admin page → Copies)

- **Edit**: name, access, expiry, offline days, a note (shown on a blocked PC).
- **Block / Unblock**: takes effect within a minute while the game is running.
- **Remove**: the copy must be scanned again next time.
- The list shows who is **playing now**, when each copy was last seen, its IP, version and number of launches.

### Settings

- **Approve every new copy automatically.** Handy for a rush of visitors; pick the access they get (e.g. Demo). Switch back to QR afterwards.
- **Offline days**, **pre-selected access**, **event name** (shown on the site), **allow downloads**.
- **Message to every game.** A one-line message that pops up in every running copy within a minute.
- **Lock every copy.** Emergency switch: everything except Owner copies shows a lock screen until it is switched off.
- **Download a backup** of the copies list and log.

---

## How the protection works

- The server makes an **RSA-2048 key pair** the first time it runs (`Server/data/keys`). The **public** half is built into the game (`Assets/_Glitched/Resources/Licence/licence_key.json`); the **private** half never leaves the laptop.
- An approval is a small **signed licence** naming the PC (`SystemInfo.deviceUniqueIdentifier`), the access type, the expiry and the offline date. The game checks the signature itself, so a licence can't be edited or forged, and a copied game folder (or a copied licence file) won't run on another PC.
- While playing, the game checks in every minute, so blocking, removing or changing a copy takes effect quickly. Without the server it keeps working until its offline date. Moving the PC's clock backwards doesn't extend that.
- The game only trusts a server holding the same key (it compares the key fingerprint), so a look-alike server can't approve copies.
- Release builds can't skip the check, not even through capture mode. The Unity editor and development builds skip it so the team can work.

**Limits, honestly.** This stops copying at the exhibition: pendrives, copied folders, a friend's install. It is not DRM. Someone determined could decompile a Mono build, and anyone with this public repository and Unity could build their own copy without the check. That takes hours of work and a 2 GB download, which is fine for a school stall.

---

## Back up the key

`Server/data/keys/licence_private.pem` is what makes approvals valid. **If it's lost, every installed copy has to be rebuilt.**

- Copy the whole `Server/data` folder to a pendrive after the first run, and keep it private.
- **Never commit it or share it.** `.gitignore` already excludes `Server/data/` and `*.pem`.
- Moving to another laptop: copy `Server/` including `data/`. The installed games accept the new laptop because the key is the same.
- Made a new key on purpose? Run `Server\tools\export-game-key.cmd`, then rebuild the game and the installer (see [BUILDING.md](BUILDING.md)).

---

## Files

| Path | What |
|---|---|
| `server.js` | The server: HTTP routes, verification API, admin API. |
| `lib/` | Database (`store.js`), keys and passwords (`security.js`), QR codes (`qr.js`), LAN beacon (`net.js`). |
| `public/` | The website (`index.html`, `js/world.js` is the 3D scene), the admin and approve pages. |
| `launcher/` | Desktop shortcut scripts: start, open window, stop when the window closes; reset password. |
| `config.json` | Port (8080), UDP discovery port (47777), paths of the setup and portable files. |
| `data/` | **Private, not in git:** keys, `db.json` (copies, settings, log), logs. |
| `test/smoke.js` | `npm test`: runs the whole copy lifecycle against a throwaway database. |

### API used by the game

| Call | Purpose |
|---|---|
| `UDP 47777 "GLITCHED?"` | Discovery: the server answers with its port and key fingerprint. |
| `GET /api/v1/hello` | Is this a GLITCHED server, and which key does it hold? |
| `POST /api/v1/pair` | Ask to be verified. Returns `approved` + licence, or `pending` + pairing code + QR matrix. |
| `GET /api/v1/pair/:id?d=` | Poll a pending request. |
| `POST /api/v1/check` | Every minute while playing: `active` (fresh licence, message), `blocked`, `locked`, `expired`, `unknown`. |

Admin calls (`/api/admin/*`) need the login cookie and the `X-Glitched: 1` header (which blocks cross-site requests). Wrong passwords are slowed down after 5 tries.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| The shortcut does nothing | Install Node.js. Look in `Server/data/server-errors.log`. |
| Other devices can't open the site | Allow Node.js in Windows Firewall (Private). Same network? Try the hotspot. |
| A game says "No GLITCHED server found" | Same Wi-Fi? Press SERVER ADDRESS and type the address from the admin page. |
| The QR opens a page that doesn't load | The phone is on another network (mobile data). Join the stall Wi-Fi. |
| "Not built yet" on the install button | Build the installer: `Builds/Installer/GLITCHED_Setup.exe` (see BUILDING.md). |
| Port 8080 is busy | The server uses the next free port (8081…); the window and admin page show which. |

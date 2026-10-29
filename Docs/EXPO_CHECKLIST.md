# Exhibition day checklist

## The day before

- [ ] Build the release game and the installer (see [BUILDING.md](BUILDING.md)). The admin page → **Download files** shows the setup's date and SHA-256.
- [ ] Copy `Server/data` (the licence key) to a pendrive as a backup.
- [ ] On the laptop, play once from the desktop shortcut: it should say **✓ VERIFIED COPY · OWNER** on the title screen.
- [ ] **Turn developer mode off** on any PC judges will use (title screen, top right). It shows the answers.
- [ ] Charge the admin phone. Open the admin page on it once and log in.
- [ ] Keep pens and notebooks at the stall (the game's disclaimer asks players to have them).

## Setting up the stall

1. Turn on **Mobile Hotspot** on the laptop (or join the venue Wi-Fi).
2. Double-click **GLITCHED Server** on the desktop. Keep its window open all day: closing it stops the server.
3. First time on a new network: Windows Firewall asks about Node.js → tick **Private** → **Allow**.
4. Admin page (footer "Team login" or `/admin`): check the **This server** addresses and print or show the visitor QR code.
5. Optional: Settings → **Approve automatically** with **Demo** access if a crowd arrives; switch back to QR later.

## When a judge or visitor wants the game

1. They scan the visitor QR (or type the address) on a PC on the same Wi-Fi and **hold the install core**.
2. Setup → *More info → Run anyway* → Next → Install.
3. The game shows a QR code. **Scan it with the admin phone**, check the 6-letter code matches, choose **Judge** (or Full / Demo), name it ("Judge 1") and **APPROVE**.
4. "COPY VERIFIED" appears and the game opens. Judge copies have every chapter open.

On the stall's own PCs the game was already verified as Owner.

## During the day

- Admin → Overview shows who is **playing now** and every copy waiting. A chime plays for new requests.
- **Message to every game**, e.g. "Results at 3 pm, come back to stall B-12!", reaches every running copy within a minute.
- Block a copy you didn't approve on purpose (Copies → Block). Unknown copies can't run anyway.
- Emergency: Settings → **Lock every copy**.

## Afterwards

- Copies keep working for their **offline days** (default 7) without the server; Owner copies forever.
- Admin → Settings → **Download a backup** of the copies list.
- To end access: set an expiry or remove copies before the day ends.

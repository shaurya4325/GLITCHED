# The public website (GitHub Pages)

**https://shaurya4325.github.io/GLITCHED/**

A copy of the stall website that anyone can open from anywhere, on a phone or a PC. Print its QR code at the stall (`Server/pages-dist/img/site-qr.svg`, or scan the one in the site's footer).

| | Stall server (`Server/`, on the laptop) | Public site (GitHub Pages) |
|---|---|---|
| Address | `http://<laptop>:8080` on the stall Wi-Fi | `https://shaurya4325.github.io/GLITCHED/` |
| Downloads come from | the laptop (fast, no internet needed) | the GitHub release `v1.0` |
| Live counters | verified copies, playing now, downloads | game facts instead (no server) |
| Admin page, QR approvals | yes | no (approvals always happen at the stall) |

A copy downloaded from the public site still has to be **verified once at the stall**: the game looks for the stall server on the Wi-Fi and shows its QR code.

## On phones

The site is built for phones first: one column, a menu button, touch-friendly gallery and buttons, a lighter 3D scene, and no sideways scrolling.

- **Android**: if the release has `GLITCHED.apk`, holding the core downloads the Android game.
- **iPhone, or Android without the app**: holding the core opens **Share link / Copy link**, so the visitor can send the link to themselves and open it on a Windows PC.
- Test the phone flow on a PC by adding `?phone=1` to the address.

## Updating it

1. Build the game and the installer (see [BUILDING.md](BUILDING.md)).
2. Upload the files to the release (replace `--clobber` keeps the same links):

   ```bat
   gh release upload v1.0 Builds\Installer\GLITCHED_Setup.exe Builds\Installer\GLITCHED_Portable.zip Builds\Android\GLITCHED.apk --clobber
   ```

3. Publish the site (builds `Server/pages-dist` and pushes it to the `gh-pages` branch):

   ```bat
   Server\tools\publish-pages.cmd
   ```

   GitHub Pages updates within a minute or two. The site shows the size and SHA-256 of each file, so publish **after** uploading new files.

`tools/build-pages.js` copies `Server/public` (without the admin pages), the screenshots, a QR code of the public address and a `js/static-config.js` describing the release files. The same `index.html` works on both: the stall server serves an empty `static-config.js`, so the page uses its live data instead.

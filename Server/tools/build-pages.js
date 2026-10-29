// Builds the GitHub Pages copy of the website (no server needed) into Server/pages-dist:
//   node tools/build-pages.js [--tag v1.0] [--repo owner/name]
// The downloads point at the files of that GitHub release; sizes and SHA-256 come from Builds/Installer.
// Publish with tools/publish-pages.cmd (pushes pages-dist to the gh-pages branch).
'use strict';
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const qr = require('../lib/qr');

const ROOT = path.resolve(__dirname, '..');
const config = JSON.parse(fs.readFileSync(path.join(ROOT, 'config.json'), 'utf8'));
const arg = (n, d) => { const i = process.argv.indexOf(n); return i >= 0 ? process.argv[i + 1] : d; };
const tag = arg('--tag', 'v' + config.gameVersion);
const repo = arg('--repo', 'shaurya4325/GLITCHED');
const [owner, name] = repo.split('/');
const siteUrl = `https://${owner.toLowerCase()}.github.io/${name}/`;
const OUT = path.join(ROOT, 'pages-dist');

function copyDir(src, dst, skip = () => false) {
  fs.mkdirSync(dst, { recursive: true });
  for (const e of fs.readdirSync(src, { withFileTypes: true })) {
    const s = path.join(src, e.name), d = path.join(dst, e.name);
    if (skip(s)) continue;
    if (e.isDirectory()) copyDir(s, d, skip); else fs.copyFileSync(s, d);
  }
}

function fileEntry(rel, publishedName) {
  const p = path.resolve(ROOT, rel);
  if (!fs.existsSync(p)) return { available: false };
  const buf = fs.readFileSync(p);
  return {
    available: true, name: publishedName, size: buf.length,
    sha256: crypto.createHash('sha256').update(buf).digest('hex'),
    url: `https://github.com/${repo}/releases/download/${tag}/${publishedName}`,
  };
}

fs.rmSync(OUT, { recursive: true, force: true });
copyDir(path.join(ROOT, 'public'), OUT, (p) => p.includes(path.sep + 'admin'));
const screensDir = path.resolve(ROOT, config.screensDir);
const screens = fs.existsSync(screensDir) ? fs.readdirSync(screensDir).filter((f) => /\.(png|jpe?g|webp)$/i.test(f)).sort() : [];
copyDir(screensDir, path.join(OUT, 'screens'));
fs.writeFileSync(path.join(OUT, 'img', 'site-qr.svg'), qr.svg(siteUrl, { dark: '#05060d', light: '#e8f7ff' }));

const data = {
  name: 'GLITCHED', event: 'CBSE Skill Expo 2026-27', version: config.gameVersion, downloadsEnabled: true,
  url: siteUrl, qr: 'img/site-qr.svg', stats: null, screens,
  files: {
    setup: fileEntry(config.setupFile, 'GLITCHED_Setup.exe'),
    portable: fileEntry(config.portableFile, 'GLITCHED_Portable.zip'),
    android: fileEntry(config.androidFile || '../Builds/Android/GLITCHED.apk', 'GLITCHED.apk'),
  },
};
fs.writeFileSync(path.join(OUT, 'js', 'static-config.js'),
  '// Written by Server/tools/build-pages.js: the GitHub Pages copy has no server, so the site reads this instead.\n' +
  'window.GLITCHED_STATIC = ' + JSON.stringify(data, null, 1) + ';\n');
fs.writeFileSync(path.join(OUT, '.nojekyll'), '');
fs.writeFileSync(path.join(OUT, '404.html'),
  '<!doctype html><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>404 · GLITCHED</title>' +
  `<link rel="stylesheet" href="${siteUrl}css/site.css"><body class="plain"><main class="plain-card"><h1 class="glitch" data-text="404">404</h1>` +
  `<p>This page fell into the glitch.</p><a class="btn" href="${siteUrl}">Back to GLITCHED</a></main></body>`);

console.log('Pages site in', OUT);
console.log('  url      ', siteUrl);
for (const [k, f] of Object.entries(data.files)) console.log('  ' + k.padEnd(9), f.available ? `${(f.size / 1e6).toFixed(0)} MB -> ${f.url}` : 'not built');

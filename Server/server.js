// GLITCHED stall server.
//   node server.js                     start (the desktop shortcut runs this through launcher/GLITCHED-Server.ps1)
//   node server.js --export-game-key P write the game's copy of the public licence key to P
//   node server.js --reset-password    forget the admin password (set a new one at http://localhost:8080/admin)
// Serves the 3D website, the game download, the copy-verification API the game talks to, and the admin page.
'use strict';
const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { Store } = require('./lib/store');
const sec = require('./lib/security');
const qr = require('./lib/qr');
const net = require('./lib/net');

const ROOT = __dirname;
const config = JSON.parse(fs.readFileSync(path.join(ROOT, 'config.json'), 'utf8'));
const DATA = path.resolve(ROOT, process.env.GLITCHED_DATA || config.dataDir);
const PUBLIC = path.join(ROOT, 'public');
const args = process.argv.slice(2);
const flag = (name) => args.includes(name);
const argAfter = (name) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };

const keys = sec.loadKeys(path.join(DATA, 'keys'));

if (flag('--export-game-key')) {
  const out = path.resolve(argAfter('--export-game-key'));
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, sec.gameKeyJson(keys));
  console.log('wrote', out, 'fingerprint', keys.fingerprint);
  process.exit(0);
}

const store = new Store(DATA);
const db = store.db;

if (flag('--reset-password')) {
  db.admin = null;
  db.sessions = {};
  store.flush();
  console.log('Admin password cleared. Open http://localhost:' + config.port + '/admin on this PC to set a new one.');
  process.exit(0);
}

// ------------------------------------------------------------------ access types (the game knows these names)
const TYPES = {
  OWNER: { label: 'Owner', desc: 'Your own PCs: the full game, works offline forever.' },
  FULL: { label: 'Full game', desc: 'Every chapter, in story order.' },
  JUDGE: { label: 'Judge', desc: 'Full game with every chapter open from the start, so judges can jump anywhere.' },
  DEMO: { label: 'Demo', desc: 'Chapter 1 (Code City) only.' },
  BLOCKED: { label: 'Blocked', desc: 'The game will not open on this PC.' },
};

const START = Date.now();
const online = new Map();          // licenceId -> last check time (ms)
const loginFails = new Map();      // ip -> { n, until }
const pairHits = new Map();        // ip -> [times]
const adminStreams = new Set();
const hostStreams = new Set();
const files = { setup: fileInfo(config.setupFile), portable: fileInfo(config.portableFile), android: fileInfo(config.androidFile || '../Builds/Android/GLITCHED.apk') };
let port = config.port;

// ------------------------------------------------------------------ helpers
function fileInfo(rel) {
  const p = path.resolve(ROOT, rel);
  return { path: p, name: path.basename(p), size: 0, mtime: 0, sha256: null, available: false };
}

/** Refreshes size and SHA-256 of the download files (hashes are cached in data/filehash.json). */
function refreshFiles() {
  const cacheFile = path.join(DATA, 'filehash.json');
  let cache = {};
  try { cache = JSON.parse(fs.readFileSync(cacheFile, 'utf8')); } catch {}
  for (const f of Object.values(files)) {
    let st;
    try { st = fs.statSync(f.path); } catch { f.available = false; continue; }
    f.available = true; f.size = st.size; f.mtime = st.mtimeMs;
    const key = `${f.path}|${st.size}|${st.mtimeMs}`;
    if (cache[key]) { f.sha256 = cache[key]; continue; }
    f.sha256 = null;
    const h = crypto.createHash('sha256');
    fs.createReadStream(f.path).on('error', (e) => { f.sha256 = null; console.warn('[files] ' + f.name + ': ' + e.code + ' (will retry)'); }).on('data', (d) => h.update(d)).on('end', () => {
      f.sha256 = h.digest('hex');
      cache = { ...cache, [key]: f.sha256 };
      fs.writeFileSync(cacheFile, JSON.stringify(cache, null, 1));
      broadcastAdmin('files');
    });
  }
}

const isLoopback = (req) => /^(::1|127\.|::ffff:127\.)/.test(req.socket.remoteAddress || '');
const clientIp = (req) => (req.socket.remoteAddress || '').replace(/^::ffff:/, '');
const now = () => Date.now();
const nowS = () => Math.floor(Date.now() / 1000);

/** The address other devices (phones, other PCs) should use. */
function publicBase(req) {
  const host = (req && req.headers.host) || '';
  if (host && !/^(localhost|127\.|\[::1\])/.test(host)) return 'http://' + host;
  const lan = net.lanAddresses()[0];
  return 'http://' + (lan ? lan.address : 'localhost') + ':' + port;
}

function send(res, code, body, type = 'application/json; charset=utf-8', extra = {}) {
  const data = typeof body === 'string' || Buffer.isBuffer(body) ? body : JSON.stringify(body);
  res.writeHead(code, Object.assign({ 'Content-Type': type, 'Cache-Control': 'no-store' }, securityHeaders, extra));
  res.end(data);
}
const json = (res, body, code = 200) => send(res, code, body);
const fail = (res, code, error) => send(res, code, { error });

const securityHeaders = {
  'X-Content-Type-Options': 'nosniff',
  'Referrer-Policy': 'no-referrer',
  'X-Frame-Options': 'DENY',
  'Content-Security-Policy': "default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; font-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'",
};

function readBody(req, limit = 64 * 1024) {
  return new Promise((resolve, reject) => {
    let size = 0; const chunks = [];
    req.on('data', (c) => { size += c.length; if (size > limit) { reject(new Error('too large')); req.destroy(); } else chunks.push(c); });
    req.on('end', () => {
      if (!chunks.length) return resolve({});
      try { resolve(JSON.parse(Buffer.concat(chunks).toString('utf8'))); } catch { reject(new Error('bad json')); }
    });
    req.on('error', reject);
  });
}

function cookies(req) {
  const out = {};
  for (const part of (req.headers.cookie || '').split(';')) {
    const i = part.indexOf('=');
    if (i > 0) out[part.slice(0, i).trim()] = decodeURIComponent(part.slice(i + 1).trim());
  }
  return out;
}

function session(req) {
  const id = cookies(req).gsid;
  const s = id && db.sessions[id];
  if (!s) return null;
  if (s.expires < now()) { delete db.sessions[id]; store.save(); return null; }
  return s;
}

const str = (v, max = 120) => (typeof v === 'string' ? v : '').replace(/[\u0000-\u001f]/g, '').trim().slice(0, max);
const cleanDeviceId = (v) => (typeof v === 'string' && /^[A-Za-z0-9_-]{8,128}$/.test(v) ? v : null);

// ------------------------------------------------------------------ licences
function licenceExpired(l) { return l.expiresAt && l.expiresAt < now(); }

function tokenFor(l) {
  const forever = l.type === 'OWNER' || l.offlineDays < 0;
  return sec.signToken(keys, {
    v: 1, lid: l.id, did: l.deviceId, typ: l.type,
    exp: l.expiresAt ? Math.floor(l.expiresAt / 1000) : 0,
    off: forever ? 0 : nowS() + Math.round((l.offlineDays ?? db.settings.offlineDays) * 86400),
    iat: nowS(), nm: l.label || l.deviceName, sid: db.serverId,
  });
}

function approve(p, { type, expiresAt = 0, offlineDays, label = '' }, via) {
  if (!TYPES[type]) type = db.settings.defaultType;
  let l = db.licences.find((x) => x.deviceId === p.deviceId);
  if (!l) {
    l = { id: sec.newId(6), deviceId: p.deviceId, createdAt: now(), launches: 0, lastSeen: 0 };
    db.licences.push(l);
  }
  Object.assign(l, {
    deviceName: p.deviceName, os: p.os, gpu: p.gpu, ram: p.ram, version: p.version, lastIp: p.ip,
    type, expiresAt: Number(expiresAt) || 0,
    offlineDays: offlineDays === undefined || offlineDays === null || offlineDays === '' ? db.settings.offlineDays : Number(offlineDays),
    label: str(label, 60) || l.label || '', approvedVia: via, approvedAt: now(),
  });
  p.status = 'approved';
  p.licenceId = l.id;
  store.log('approve', `${l.label || l.deviceName} approved as ${TYPES[type].label}`, { via, lid: l.id });
  return l;
}

function pruneOld() {
  const before = db.pending.length;
  db.pending = db.pending.filter((p) => p.createdAt > now() - 20 * 60 * 1000 || (p.status === 'approved' && p.createdAt > now() - 60 * 60 * 1000));
  if (db.pending.length !== before) store.changed('pending');
  for (const [id, sid] of Object.entries(db.sessions)) if (sid.expires < now()) delete db.sessions[id];
}

function pairPayload(p, req) {
  const url = publicBase(req) + '/a/' + p.code;
  return { status: 'pending', pairId: p.id, code: p.code.slice(0, 3) + '-' + p.code.slice(3), url, qr: qr.matrix(url), expiresIn: Math.max(0, Math.round((p.createdAt + 20 * 60 * 1000 - now()) / 1000)) };
}

function rateLimited(map, ip, max, windowMs) {
  const list = (map.get(ip) || []).filter((t) => t > now() - windowMs);
  list.push(now());
  map.set(ip, list);
  return list.length > max;
}

// ------------------------------------------------------------------ admin view of everything
function stats() {
  const live = [...online.entries()].filter(([, t]) => t > now() - 2.5 * 60 * 1000).length;
  return {
    copies: db.licences.filter((l) => l.type !== 'BLOCKED' && !licenceExpired(l)).length,
    blocked: db.licences.filter((l) => l.type === 'BLOCKED').length,
    pending: db.pending.filter((p) => p.status === 'pending').length,
    downloads: db.downloads.length,
    online: live,
  };
}

function serverInfo() {
  return {
    port, url: 'http://' + ((net.lanAddresses()[0] || {}).address || 'localhost') + ':' + port,
    addresses: net.lanAddresses(), fingerprint: keys.fingerprint, serverId: db.serverId, startedAt: START, version: config.gameVersion,
    files: Object.fromEntries(Object.entries(files).map(([k, f]) => [k, { name: f.name, size: f.size, sha256: f.sha256, available: f.available, mtime: f.mtime }])),
  };
}

function adminState() {
  const t = now();
  return {
    settings: db.settings, types: TYPES, stats: stats(), server: serverInfo(),
    licences: db.licences.map((l) => Object.assign({}, l, { online: (online.get(l.id) || 0) > t - 2.5 * 60 * 1000, expired: !!licenceExpired(l) })),
    pending: db.pending.filter((p) => p.status === 'pending').map((p) => Object.assign({}, p, { code: p.code.slice(0, 3) + '-' + p.code.slice(3) })),
    downloads: db.downloads.slice(-300).reverse(),
    log: db.log.slice(-300).reverse(),
  };
}

function broadcastAdmin(what) {
  for (const res of adminStreams) res.write(`event: changed\ndata: ${JSON.stringify({ what })}\n\n`);
}
store.on('changed', broadcastAdmin);

// ------------------------------------------------------------------ static files
const MIME = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8',
  '.json': 'application/json', '.png': 'image/png', '.jpg': 'image/jpeg', '.svg': 'image/svg+xml', '.ico': 'image/x-icon',
  '.ttf': 'font/ttf', '.woff2': 'font/woff2', '.webp': 'image/webp', '.txt': 'text/plain; charset=utf-8',
};

function serveFile(res, file, extraHeaders = {}) {
  fs.stat(file, (err, st) => {
    if (err || !st.isFile()) return send(res, 404, notFoundPage(), MIME['.html']);
    const ext = path.extname(file).toLowerCase();
    const rel = path.relative(PUBLIC, file).split(path.sep)[0];
    const cache = rel === 'vendor' || rel === 'fonts' || file.startsWith(path.resolve(ROOT, config.screensDir)) ? 'public, max-age=86400' : 'no-cache';
    res.writeHead(200, Object.assign({ 'Content-Type': MIME[ext] || 'application/octet-stream', 'Content-Length': st.size, 'Cache-Control': cache }, securityHeaders, extraHeaders));
    fs.createReadStream(file).on('error', () => res.destroy()).pipe(res);
  });
}

function safeJoin(base, rel) {
  const p = path.normalize(path.join(base, decodeURIComponent(rel)));
  return p.startsWith(base) ? p : null;
}

function notFoundPage() {
  return '<!doctype html><meta charset="utf-8"><title>404 · GLITCHED</title><link rel="stylesheet" href="/css/site.css">' +
    '<body class="plain"><main class="plain-card"><h1 class="glitch" data-text="404">404</h1><p>This page fell into the glitch.</p><a class="btn" href="/">Back to GLITCHED</a></main></body>';
}

function screens() {
  const dir = path.resolve(ROOT, config.screensDir);
  try { return fs.readdirSync(dir).filter((f) => /\.(png|jpe?g|webp)$/i.test(f)).sort(); } catch { return []; }
}

/** Streams a download with resume (Range) support. */
function serveDownload(req, res, key) {
  const f = files[key];
  if (!db.settings.downloadsEnabled) return send(res, 403, '<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="/css/site.css"><body class="plain"><main class="plain-card"><h1>Downloads are paused</h1><p>Ask the GLITCHED team at the stall.</p><a class="btn" href="/">Back</a></main></body>', MIME['.html']);
  let st;
  try { st = fs.statSync(f.path); } catch { return send(res, 404, '<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="/css/site.css"><body class="plain"><main class="plain-card"><h1>Not built yet</h1><p>The setup file is missing on the server PC.</p><a class="btn" href="/">Back</a></main></body>', MIME['.html']); }
  const range = /bytes=(\d*)-(\d*)/.exec(req.headers.range || '');
  let start = 0, end = st.size - 1, code = 200;
  if (range) {
    start = range[1] ? parseInt(range[1], 10) : st.size - parseInt(range[2], 10);
    end = range[1] && range[2] ? Math.min(parseInt(range[2], 10), st.size - 1) : st.size - 1;
    if (start > end || start >= st.size) { res.writeHead(416, { 'Content-Range': `bytes */${st.size}` }); return res.end(); }
    code = 206;
  }
  if (start === 0 && req.method === 'GET') {
    db.downloads.push({ at: now(), ip: clientIp(req), file: key, ua: str(req.headers['user-agent'], 160) });
    store.changed('downloads');
  }
  res.writeHead(code, Object.assign({
    'Content-Type': key === 'android' ? 'application/vnd.android.package-archive' : 'application/octet-stream', 'Content-Length': end - start + 1, 'Accept-Ranges': 'bytes',
    'Content-Disposition': `attachment; filename="${f.name}"`, 'Cache-Control': 'no-store',
  }, code === 206 ? { 'Content-Range': `bytes ${start}-${end}/${st.size}` } : {}, securityHeaders));
  if (req.method === 'HEAD') return res.end();
  fs.createReadStream(f.path, { start, end }).on('error', () => res.destroy()).pipe(res);
}

// ------------------------------------------------------------------ routes
async function route(req, res) {
  const url = new URL(req.url, 'http://x');
  const p = url.pathname;
  const m = req.method;

  // ---------- game API (no login: every answer is signed, and a licence only works on its own PC)
  if (p === '/api/v1/hello') return json(res, { glitched: 1, name: 'GLITCHED', fp: keys.fingerprint, serverId: db.serverId, version: config.gameVersion, time: nowS() });

  if (p === '/api/v1/pair' && m === 'POST') {
    if (rateLimited(pairHits, clientIp(req), 40, 60 * 1000)) return fail(res, 429, 'slow down');
    const b = await readBody(req);
    const deviceId = cleanDeviceId(b.deviceId);
    if (!deviceId) return fail(res, 400, 'deviceId');
    const info = { deviceId, deviceName: str(b.deviceName, 60) || 'PC', os: str(b.os, 120), gpu: str(b.gpu, 120), ram: Number(b.ram) || 0, version: str(b.version, 20), ip: clientIp(req) };
    const existing = db.licences.find((l) => l.deviceId === deviceId);
    if (existing && existing.type === 'BLOCKED') return json(res, { status: 'blocked', reason: existing.note || '' });
    if (existing && !licenceExpired(existing)) {
      Object.assign(existing, { lastIp: info.ip, version: info.version || existing.version });
      store.log('restore', `${existing.label || existing.deviceName} reinstalled: its approval was restored`, { lid: existing.id });
      return json(res, { status: 'approved', token: tokenFor(existing) });
    }
    let pend = db.pending.find((x) => x.deviceId === deviceId && x.status === 'pending');
    if (!pend) {
      pend = Object.assign({ id: sec.newId(10), code: sec.pairCode(), createdAt: now(), status: 'pending' }, info);
      db.pending.push(pend);
      store.log('request', `${info.deviceName} asked to be verified`, { ip: info.ip });
    } else {
      Object.assign(pend, info, { createdAt: now() });
      store.changed('pending');
    }
    if (isLoopback(req) && db.settings.autoApproveLocal) {
      const l = approve(pend, { type: 'OWNER', label: info.deviceName + ' (server PC)' }, 'local');
      return json(res, { status: 'approved', token: tokenFor(l) });
    }
    if (db.settings.approvalMode === 'auto') {
      const l = approve(pend, { type: db.settings.autoType }, 'auto');
      return json(res, { status: 'approved', token: tokenFor(l) });
    }
    return json(res, pairPayload(pend, req));
  }

  let mm;
  if ((mm = /^\/api\/v1\/pair\/([a-f0-9]{20})$/.exec(p)) && m === 'GET') {
    const pend = db.pending.find((x) => x.id === mm[1]);
    if (!pend || pend.deviceId !== url.searchParams.get('d')) return json(res, { status: 'expired' });
    if (pend.status === 'approved') {
      const l = db.licences.find((x) => x.id === pend.licenceId);
      return l ? json(res, { status: 'approved', token: tokenFor(l) }) : json(res, { status: 'expired' });
    }
    if (pend.status === 'denied') return json(res, { status: 'denied' });
    if (pend.createdAt < now() - 20 * 60 * 1000) return json(res, { status: 'expired' });
    return json(res, { status: 'pending' });
  }

  if (p === '/api/v1/check' && m === 'POST') {
    const b = await readBody(req);
    const payload = sec.verifyToken(keys, b.token);
    const deviceId = cleanDeviceId(b.deviceId);
    if (!payload || !deviceId || payload.did !== deviceId) return json(res, { status: 'invalid' });
    const l = db.licences.find((x) => x.id === payload.lid && x.deviceId === deviceId);
    if (!l) return json(res, { status: 'unknown' });
    const first = (online.get(l.id) || 0) < now() - 2.5 * 60 * 1000;
    online.set(l.id, now());
    Object.assign(l, { lastSeen: now(), lastIp: clientIp(req), version: str(b.version, 20) || l.version });
    if (b.launch) l.launches = (l.launches || 0) + 1;
    if (first || b.launch) store.changed('licences'); else store.save();
    if (l.type === 'BLOCKED') return json(res, { status: 'blocked', reason: l.note || '' });
    if (licenceExpired(l)) return json(res, { status: 'expired' });
    if (db.settings.lockdown && l.type !== 'OWNER') return json(res, { status: 'locked' });
    const s = db.settings;
    return json(res, { status: 'active', type: l.type, token: tokenFor(l), message: s.message ? { id: s.messageId, text: s.message } : null, time: nowS() });
  }

  // ---------- public site data
  if (p === '/api/public/info') {
    return json(res, {
      name: 'GLITCHED', event: db.settings.eventName, version: config.gameVersion, downloadsEnabled: db.settings.downloadsEnabled,
      url: publicBase(req), stats: stats(), screens: screens(),
      files: Object.fromEntries(Object.entries(files).map(([k, f]) => [k, { size: f.size, sha256: f.sha256, available: f.available, name: f.name }])),
    });
  }
  if (p === '/api/qr.svg') {
    const text = str(url.searchParams.get('t'), 300) || publicBase(req);
    return send(res, 200, qr.svg(text, { dark: url.searchParams.get('dark') === '1' ? '#05060d' : '#e8f7ff' }), MIME['.svg']);
  }
  if (/^\/download\/(setup|portable|android)$/.test(p)) return serveDownload(req, res, p.split('/')[2]);
  if (p.startsWith('/screens/')) {
    const f = safeJoin(path.resolve(ROOT, config.screensDir), p.slice(9));
    return f ? serveFile(res, f) : fail(res, 404, 'not found');
  }

  // ---------- host window (the desktop shortcut's window): the server stops when it closes
  if (p === '/api/host' && isLoopback(req)) {
    res.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-store', Connection: 'keep-alive' });
    res.write('retry: 2000\n\n');
    hostStreams.add(res);
    clearTimeout(hostTimer);
    req.on('close', () => { hostStreams.delete(res); scheduleHostStop(); });
    return;
  }

  // ---------- the launcher stops the server when its window closes (token given to us at start, this PC only)
  if (p === '/api/local/stop' && m === 'POST' && isLoopback(req)) {
    const token = process.env.GLITCHED_STOP_TOKEN;
    if (!token || req.headers['x-stop-token'] !== token) return fail(res, 403, 'no');
    json(res, { ok: true });
    return setTimeout(() => shutdown('the GLITCHED window was closed'), 100);
  }

  // ---------- admin
  if (p.startsWith('/api/admin/')) return adminRoute(req, res, p, m, url);

  // ---------- pages
  if (p === '/admin' || p === '/admin/') return serveFile(res, path.join(PUBLIC, 'admin', 'index.html'));
  if (/^\/a\/[A-Z0-9]{6}$/.test(p)) return serveFile(res, path.join(PUBLIC, 'admin', 'approve.html'));
  if (p === '/') return serveFile(res, path.join(PUBLIC, 'index.html'));
  const f = safeJoin(PUBLIC, p);
  if (!f) return fail(res, 400, 'bad path');
  return serveFile(res, f);
}

async function adminRoute(req, res, p, m, url) {
  const local = isLoopback(req);
  const ip = clientIp(req);
  // every change needs this header: other websites can't send it, which blocks cross-site requests
  if (m !== 'GET' && req.headers['x-glitched'] !== '1') return fail(res, 403, 'missing header');

  if (p === '/api/admin/whoami') return json(res, { setup: !db.admin, loggedIn: !!session(req), local, event: db.settings.eventName });

  if (p === '/api/admin/setup' && m === 'POST') {
    if (db.admin) return fail(res, 409, 'already set up');
    if (!local) return fail(res, 403, 'Set the admin password on the server PC itself.');
    const b = await readBody(req);
    if (str(b.password, 200).length < 6) return fail(res, 400, 'Use at least 6 characters.');
    db.admin = sec.hashPassword(b.password);
    store.log('admin', 'Admin password created');
    return startSession(res, req);
  }

  if (p === '/api/admin/login' && m === 'POST') {
    const lf = loginFails.get(ip);
    if (lf && lf.until > now()) return fail(res, 429, `Too many tries. Wait ${Math.ceil((lf.until - now()) / 1000)} s.`);
    const b = await readBody(req);
    if (!sec.checkPassword(db.admin, str(b.password, 200))) {
      const n = (lf && lf.last > now() - 10 * 60 * 1000 ? lf.n : 0) + 1;
      loginFails.set(ip, { n, last: now(), until: n >= 5 ? now() + 30 * 1000 * (n - 4) : 0 });
      store.log('security', `Wrong admin password from ${ip}`);
      return fail(res, 401, 'Wrong password.');
    }
    loginFails.delete(ip);
    store.log('admin', `Admin logged in from ${local ? 'this PC' : ip}`);
    return startSession(res, req);
  }

  const s = session(req);
  if (!s) return fail(res, 401, 'login');

  if (p === '/api/admin/logout' && m === 'POST') {
    delete db.sessions[cookies(req).gsid];
    store.save();
    return send(res, 200, { ok: true }, undefined, { 'Set-Cookie': 'gsid=; Path=/; Max-Age=0; HttpOnly; SameSite=Strict' });
  }

  if (p === '/api/admin/state') return json(res, adminState());

  if (p === '/api/admin/events') {
    res.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-store', Connection: 'keep-alive' });
    res.write('retry: 2000\n\n');
    adminStreams.add(res);
    const ping = setInterval(() => res.write(': ping\n\n'), 20000);
    req.on('close', () => { clearInterval(ping); adminStreams.delete(res); });
    return;
  }

  let mm;
  if ((mm = /^\/api\/admin\/pending\/([A-Z0-9]{6})$/.exec(p))) {
    const pend = db.pending.find((x) => x.code === mm[1]);
    if (!pend) return fail(res, 404, 'This code has expired. Press RETRY in the game for a new one.');
    const existing = db.licences.find((l) => l.deviceId === pend.deviceId);
    return json(res, { pending: Object.assign({}, pend, { code: pend.code.slice(0, 3) + '-' + pend.code.slice(3) }), existing: existing || null, types: TYPES, settings: db.settings });
  }

  if (p === '/api/admin/approve' && m === 'POST') {
    const b = await readBody(req);
    const pend = db.pending.find((x) => x.id === b.pairId);
    if (!pend) return fail(res, 404, 'This request has expired.');
    if (pend.status !== 'pending') return json(res, { ok: true, already: pend.status });
    const l = approve(pend, { type: b.type, expiresAt: b.expiresAt, offlineDays: b.offlineDays, label: b.label }, b.via === 'qr' ? 'qr' : 'admin');
    return json(res, { ok: true, licence: l });
  }

  if (p === '/api/admin/deny' && m === 'POST') {
    const b = await readBody(req);
    const pend = db.pending.find((x) => x.id === b.pairId);
    if (!pend) return fail(res, 404, 'This request has expired.');
    pend.status = 'denied';
    store.log('deny', `${pend.deviceName} was refused`);
    return json(res, { ok: true });
  }

  if ((mm = /^\/api\/admin\/licence\/([a-f0-9]{12})$/.exec(p))) {
    const l = db.licences.find((x) => x.id === mm[1]);
    if (!l) return fail(res, 404, 'not found');
    if (m === 'DELETE') {
      db.licences = db.licences.filter((x) => x !== l);
      online.delete(l.id);
      store.log('remove', `${l.label || l.deviceName} was removed: it must be verified again`);
      return json(res, { ok: true });
    }
    if (m === 'POST') {
      const b = await readBody(req);
      const before = l.type;
      if (b.type !== undefined && TYPES[b.type]) l.type = b.type;
      if (b.expiresAt !== undefined) l.expiresAt = Number(b.expiresAt) || 0;
      if (b.offlineDays !== undefined) l.offlineDays = Number(b.offlineDays);
      if (b.label !== undefined) l.label = str(b.label, 60);
      if (b.note !== undefined) l.note = str(b.note, 200);
      store.log('edit', `${l.label || l.deviceName}: ${before !== l.type ? `${TYPES[before].label} → ${TYPES[l.type].label}` : 'details changed'}`, { lid: l.id });
      return json(res, { ok: true, licence: l });
    }
  }

  if (p === '/api/admin/settings' && m === 'POST') {
    const b = await readBody(req);
    const st = db.settings;
    if (typeof b.eventName === 'string') st.eventName = str(b.eventName, 80);
    if (['manual', 'auto'].includes(b.approvalMode)) st.approvalMode = b.approvalMode;
    if (TYPES[b.autoType] && b.autoType !== 'BLOCKED') st.autoType = b.autoType;
    if (TYPES[b.defaultType] && b.defaultType !== 'BLOCKED') st.defaultType = b.defaultType;
    if (b.offlineDays !== undefined && Number.isFinite(Number(b.offlineDays))) st.offlineDays = Math.max(-1, Math.min(365, Number(b.offlineDays)));
    for (const k of ['autoApproveLocal', 'downloadsEnabled', 'lockdown']) if (typeof b[k] === 'boolean') st[k] = b[k];
    if (typeof b.message === 'string') { st.message = str(b.message, 140); st.messageId = (st.messageId || 0) + 1; }
    store.log('settings', 'Settings changed: ' + Object.keys(b).join(', '));
    return json(res, { ok: true, settings: st });
  }

  if (p === '/api/admin/password' && m === 'POST') {
    const b = await readBody(req);
    if (!sec.checkPassword(db.admin, str(b.current, 200))) return fail(res, 401, 'The current password is wrong.');
    if (str(b.next, 200).length < 6) return fail(res, 400, 'Use at least 6 characters.');
    db.admin = sec.hashPassword(b.next);
    const keep = cookies(req).gsid;
    for (const id of Object.keys(db.sessions)) if (id !== keep) delete db.sessions[id];
    store.log('admin', 'Admin password changed (other devices were logged out)');
    return json(res, { ok: true });
  }

  if (p === '/api/admin/export') {
    const backup = { exportedAt: new Date().toISOString(), serverId: db.serverId, settings: db.settings, licences: db.licences, downloads: db.downloads, log: db.log };
    return send(res, 200, JSON.stringify(backup, null, 1), 'application/json', { 'Content-Disposition': `attachment; filename="glitched-backup-${new Date().toISOString().slice(0, 10)}.json"` });
  }

  if (p === '/api/admin/shutdown' && m === 'POST') {
    json(res, { ok: true });
    store.log('admin', 'Server stopped from the admin page');
    return setTimeout(() => shutdown('admin page'), 300);
  }

  return fail(res, 404, 'not found');
}

function startSession(res, req) {
  const id = crypto.randomBytes(24).toString('hex');
  db.sessions[id] = { created: now(), expires: now() + 12 * 3600 * 1000, ip: clientIp(req) };
  store.save();
  return send(res, 200, { ok: true }, undefined, { 'Set-Cookie': `gsid=${id}; Path=/; Max-Age=${12 * 3600}; HttpOnly; SameSite=Strict` });
}

// ------------------------------------------------------------------ lifecycle
let hostTimer = null;
function scheduleHostStop() {
  if (!flag('--stop-with-window') || hostStreams.size) return;
  clearTimeout(hostTimer);
  hostTimer = setTimeout(() => { if (!hostStreams.size) shutdown('the GLITCHED window was closed'); }, 12000);
}

let server, beacon, stopping = false;
function shutdown(why) {
  if (stopping) return;
  stopping = true;
  console.log(`[server] stopping: ${why}`);
  try { store.flush(); } catch (e) { console.error(e); }
  try { fs.unlinkSync(path.join(DATA, 'server.json')); } catch {}
  for (const r of [...adminStreams, ...hostStreams]) try { r.end(); } catch {}
  try { beacon && beacon.close(); } catch {}
  server.close(() => process.exit(0));
  setTimeout(() => process.exit(0), 1500).unref();
}
process.on('SIGINT', () => shutdown('Ctrl+C'));
// the stall server must keep running: log anything unexpected instead of crashing
process.on('uncaughtException', (e) => console.error('[server] unexpected error:', e && e.stack || e));
process.on('unhandledRejection', (e) => console.error('[server] unexpected rejection:', e && e.stack || e));
process.on('SIGTERM', () => shutdown('stop signal'));

function listen(attempt = 0) {
  server = http.createServer((req, res) => {
    route(req, res).catch((e) => {
      console.error('[server]', req.method, req.url, e.message);
      if (!res.headersSent) fail(res, e.message === 'bad json' || e.message === 'too large' ? 400 : 500, e.message);
    });
  });
  server.keepAliveTimeout = 5000;
  server.on('error', (e) => {
    if (e.code === 'EADDRINUSE' && attempt < 10) { port++; return listen(attempt + 1); }
    console.error('[server] cannot start:', e.message);
    process.exit(1);
  });
  server.listen(port, '0.0.0.0', () => {
    const lan = net.lanAddresses();
    fs.writeFileSync(path.join(DATA, 'server.json'), JSON.stringify({ pid: process.pid, port, url: `http://localhost:${port}`, startedAt: START }));
    console.log('');
    console.log('  GLITCHED server is running');
    console.log(`  On this PC:      http://localhost:${port}`);
    for (const a of lan) console.log(`  On the Wi-Fi:    http://${a.address}:${port}   (${a.name})`);
    console.log(`  Admin:           http://localhost:${port}/admin`);
    console.log(`  Licence key:     ${keys.fingerprint}`);
    console.log('');
    beacon = net.startBeacon(config.discoveryPort, () => ({ name: 'GLITCHED', port, fp: keys.fingerprint }));
    refreshFiles();
    setInterval(refreshFiles, 60 * 1000).unref();
    setInterval(pruneOld, 30 * 1000).unref();
    if (flag('--stop-with-window')) hostTimer = setTimeout(() => { if (!hostStreams.size) shutdown('no GLITCHED window opened'); }, 90000);
  });
}

listen();

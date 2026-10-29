// End-to-end check of the verification API against a throwaway database:
//   node test/smoke.js
// Starts its own server on a spare port with a temporary data folder, runs the whole copy lifecycle
// (request -> approve -> check -> copied to another PC -> block -> lockdown -> remove), then stops it.
'use strict';
const { spawn } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');

const data = fs.mkdtempSync(path.join(os.tmpdir(), 'glitched-smoke-'));
const lanIp = Object.values(os.networkInterfaces()).flat().find((a) => a && a.family === 'IPv4' && !a.internal);
let failures = 0;
const ok = (cond, what) => { console.log((cond ? '  PASS ' : '  FAIL ') + what); if (!cond) failures++; };

async function main() {
  const srv = spawn(process.execPath, [path.join(__dirname, '..', 'server.js')], { env: Object.assign({}, process.env, { GLITCHED_DATA: data }), stdio: ['ignore', 'pipe', 'pipe'] });
  srv.stderr.on('data', (d) => process.stderr.write(d));
  try {
    let info;
    for (let i = 0; i < 50 && !info; i++) {
      await new Promise((r) => setTimeout(r, 200));
      try { info = JSON.parse(fs.readFileSync(path.join(data, 'server.json'), 'utf8')); } catch {}
    }
    if (!info) throw new Error('server did not start');
    const L = `http://127.0.0.1:${info.port}`;
    const N = lanIp ? `http://${lanIp.address}:${info.port}` : null;
    let cookie = '';
    const call = async (base, p, body, method) => {
      const r = await fetch(base + p, {
        method: method || (body ? 'POST' : 'GET'),
        headers: { 'Content-Type': 'application/json', 'X-Glitched': '1', Cookie: cookie },
        body: body ? JSON.stringify(body) : undefined,
      });
      const sc = r.headers.get('set-cookie');
      if (sc && sc.startsWith('gsid=')) cookie = sc.split(';')[0];
      return { code: r.status, body: await r.json().catch(() => null) };
    };

    const hello = await call(L, '/api/v1/hello');
    ok(hello.body && hello.body.glitched === 1, 'hello answers with the key fingerprint');

    const local = await call(L, '/api/v1/pair', { deviceId: 'serverpc00000001', deviceName: 'SERVER-PC' });
    ok(local.body.status === 'approved', 'the game on the server PC is approved as owner');

    if (!N) { console.log('  (no LAN address: skipping the remote-device checks)'); return; }
    const pair = await call(N, '/api/v1/pair', { deviceId: 'judgepc000000001', deviceName: 'JUDGE-PC', version: '1.0' });
    ok(pair.body.status === 'pending' && pair.body.qr.size >= 21, 'another PC gets a QR code and waits');
    ok((await call(N, `/api/v1/pair/${pair.body.pairId}?d=judgepc000000001`)).body.status === 'pending', 'polling says pending');
    ok((await call(N, '/api/admin/setup', { password: 'smoke-test-only' })).code === 403, 'the admin password can only be created on the server PC');
    ok((await call(L, '/api/admin/setup', { password: 'smoke-test-only' })).code === 200, 'admin password created on the server PC');
    ok((await call(L, '/api/admin/pending/' + pair.body.code.replace('-', ''))).body.pending.deviceName === 'JUDGE-PC', 'the QR link shows the waiting PC');
    ok((await call(L, '/api/admin/approve', { pairId: pair.body.pairId, type: 'JUDGE', label: 'Judge 1', via: 'qr' })).body.ok, 'approved as Judge');
    const got = await call(N, `/api/v1/pair/${pair.body.pairId}?d=judgepc000000001`);
    ok(got.body.status === 'approved' && got.body.token, 'the game receives a signed licence');
    const token = got.body.token;
    const check = (deviceId) => call(N, '/api/v1/check', { token, deviceId, launch: true }).then((r) => r.body.status);
    ok(await check('judgepc000000001') === 'active', 'check: active');
    ok(await check('stolenpc00000001') === 'invalid', 'the same licence copied to another PC is refused');
    const tampered = token.slice(0, 10) + (token[10] === 'A' ? 'B' : 'A') + token.slice(11);
    ok((await call(N, '/api/v1/check', { token: tampered, deviceId: 'judgepc000000001' })).body.status === 'invalid', 'an edited licence is refused');
    const lid = (await call(L, '/api/admin/state')).body.licences.find((l) => l.deviceName === 'JUDGE-PC').id;
    await call(L, '/api/admin/licence/' + lid, { type: 'BLOCKED', note: 'smoke' });
    ok(await check('judgepc000000001') === 'blocked', 'blocked from the admin page');
    await call(L, '/api/admin/licence/' + lid, { type: 'FULL' });
    await call(L, '/api/admin/settings', { lockdown: true });
    ok(await check('judgepc000000001') === 'locked', 'lockdown locks every copy');
    await call(L, '/api/admin/settings', { lockdown: false });
    ok(await check('judgepc000000001') === 'active', 'lockdown off: active again');
    await call(L, '/api/admin/licence/' + lid, null, 'DELETE');
    ok(await check('judgepc000000001') === 'unknown', 'removed: the game must be verified again');
    cookie = '';
    ok((await call(L, '/api/admin/state')).code === 401, 'admin data needs a login');
    let last;
    for (let i = 0; i < 6; i++) last = await call(N, '/api/admin/login', { password: 'wrong' });
    ok(last.code === 429, 'repeated wrong passwords are slowed down');
    const noHeader = await fetch(L + '/api/admin/settings', { method: 'POST', body: '{}' });
    ok(noHeader.status === 403, 'changes without the X-Glitched header are refused');
    const trav = await fetch(L + '/%2e%2e/config.json');
    ok(trav.status !== 200, 'files outside public/ cannot be read');
  } finally {
    srv.kill();
    setTimeout(() => fs.rmSync(data, { recursive: true, force: true }), 500);
  }
}

main().then(() => {
  console.log(failures ? `\n${failures} check(s) failed` : '\nall checks passed');
  process.exitCode = failures ? 1 : 0;
}).catch((e) => { console.error(e); process.exitCode = 1; });

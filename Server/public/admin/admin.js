// GLITCHED admin page: live dashboard of every copy, approvals, settings.
import { $, $$, api, toast, esc, ago, when, offlineText, expiryFrom, ensureLogin } from './common.js';

let S = null;               // last /api/admin/state
let knownPending = new Set();
let editing = null;

const typeOptions = (sel, skipBlocked) => Object.entries(S.types)
  .filter(([k]) => !(skipBlocked && k === 'BLOCKED'))
  .map(([k, t]) => `<option value="${k}" ${k === sel ? 'selected' : ''}>${esc(t.label)}</option>`).join('');
const typeChip = (t) => `<span class="type ${t}">${esc(S.types[t] ? S.types[t].label : t)}</span>`;

async function load() {
  try { S = await api('/api/admin/state'); }
  catch (e) { if (e.status === 401) location.reload(); return; }
  render();
}

function render() {
  const st = S.stats;
  $('#siteAddr').textContent = S.server.url;
  $('#stats').innerHTML = [
    ['Verified copies', st.copies, 'c'], ['Playing now', st.online, 'g'], ['Waiting', st.pending, 'm'], ['Downloads', st.downloads, 'y'], ['Blocked', st.blocked, 'r'],
  ].map(([k, v, c]) => `<div class="stat ${c}"><dt>${k}</dt><dd>${v}</dd></div>`).join('');
  const badge = st.pending ? `<span class="badge">${st.pending}</span>` : '';
  $('[data-tab="overview"]').innerHTML = 'Overview' + badge;
  document.title = (st.pending ? `(${st.pending}) ` : '') + 'GLITCHED Admin';

  // pending approvals
  const fresh = S.pending.filter((p) => !knownPending.has(p.id));
  if (fresh.length && knownPending.size) { chime(); toast(`${fresh[0].deviceName} is waiting for approval`); }
  if (!knownPending.size && S.pending.length) knownPending.add('_');
  S.pending.forEach((p) => knownPending.add(p.id));
  $('#pendingList').innerHTML = S.pending.length ? S.pending.map((p) => `
    <div class="pending" data-id="${p.id}">
      <div class="code">${esc(p.code)}</div>
      <div class="who"><b>${esc(p.deviceName)}</b><small>${esc(p.ip)} · ${esc(p.os || '')} · asked ${ago(p.createdAt)}</small></div>
      <div class="act">
        <select data-k="type">${typeOptions(S.settings.defaultType, true)}</select>
        <input type="text" data-k="label" placeholder="Name (optional)" maxlength="60">
        <button class="btn sm ok" data-do="approve"><span>Approve</span></button>
        <button class="btn sm danger" data-do="deny"><span>Refuse</span></button>
      </div>
    </div>`).join('') : '<p class="empty">Nobody is waiting. When someone opens the game, their request appears here instantly.</p>';

  // recently active
  const recent = [...S.licences].sort((a, b) => (b.lastSeen || b.approvedAt || 0) - (a.lastSeen || a.approvedAt || 0)).slice(0, 6);
  $('#recentTable').innerHTML = recent.length ? `<tr><th>Copy</th><th>Access</th><th>Seen</th></tr>` + recent.map((l) => `
    <tr><td><span class="dot ${l.online ? 'on' : ''}"></span>${esc(l.label || l.deviceName)}</td><td>${typeChip(l.type)}</td><td class="muted">${l.online ? 'playing now' : ago(l.lastSeen)}</td></tr>`).join('')
    : '<tr><td class="muted">No copies yet.</td></tr>';

  // lockdown banner
  $('#lockBox').innerHTML = S.settings.lockdown
    ? `<h3 style="color:var(--red)">⚠ Every copy is locked</h3><p class="muted">Only Owner copies can play.</p><button class="btn sm ok" id="unlockAll"><span>Unlock all copies</span></button>`
    : `<h3>Approval mode: ${S.settings.approvalMode === 'auto' ? `<span style="color:var(--yellow)">automatic (${esc(S.types[S.settings.autoType].label)})</span>` : '<span style="color:var(--green)">QR scan</span>'}</h3>
       <p class="muted">${S.settings.approvalMode === 'auto' ? 'New copies are approved without a scan. Switch back in Settings once the rush is over.' : 'Every new copy needs a QR scan by the team.'}</p>`;
  const ua = $('#unlockAll'); if (ua) ua.onclick = () => saveSettings({ lockdown: false });

  // server box
  $('#addrList').innerHTML = S.server.addresses.length
    ? S.server.addresses.map((a) => `<li>http://${esc(a.address)}:${S.server.port} <small>(${esc(a.name)})</small></li>`).join('')
    : '<li class="muted">No network. Connect to Wi-Fi or turn on Mobile Hotspot.</li>';
  const qr = $('#visitorQr');
  if (qr.dataset.url !== S.server.url) { qr.src = '/api/qr.svg?dark=1&t=' + encodeURIComponent(S.server.url); qr.dataset.url = S.server.url; }
  const f = S.server.files;
  const fileRow = (name, x) => `<p><b>${name}</b><br><span class="muted">${x.available ? `${(x.size / 1e6).toFixed(0)} MB · ${when(x.mtime)}<br><code style="font-size:11px;word-break:break-all">${x.sha256 || 'calculating SHA-256…'}</code>` : 'missing: build it first (see README)'}</span></p>`;
  $('#fileInfo').innerHTML = fileRow('Setup', f.setup) + fileRow('Portable ZIP', f.portable) + fileRow('Android app', f.android) + `<p class="muted" style="font-size:13px">Licence key ${esc(S.server.fingerprint)}</p>`;

  renderCopies();
  $('#downloadsTable').innerHTML = S.downloads.length ? '<tr><th>When</th><th>File</th><th>From</th><th>Browser</th></tr>' + S.downloads.map((d) =>
    `<tr><td>${when(d.at)}</td><td>${esc(d.file)}</td><td>${esc(d.ip)}</td><td class="muted">${esc(shortUa(d.ua))}</td></tr>`).join('') : '<tr><td class="muted">No downloads yet.</td></tr>';
  $('#logList').innerHTML = S.log.map((l) => `<li><time>${when(l.at)}</time><span class="k ${esc(l.kind)}">${esc(l.kind)}</span><span>${esc(l.text)}</span></li>`).join('') || '<li class="muted">Nothing yet.</li>';
  renderSettings();
}

function shortUa(ua) {
  if (!ua) return '';
  const m = /(Edg|Chrome|Firefox|Safari)\/[\d.]+/.exec(ua);
  const os = /Android|iPhone|Windows|Mac OS X|Linux/.exec(ua);
  return [m ? m[0].replace('Edg', 'Edge') : '', os ? os[0] : ''].filter(Boolean).join(' · ');
}

function renderCopies() {
  const q = ($('#search').value || '').toLowerCase();
  const rows = S.licences.filter((l) => !q || [l.label, l.deviceName, l.lastIp, l.type, l.os].join(' ').toLowerCase().includes(q))
    .sort((a, b) => (b.approvedAt || 0) - (a.approvedAt || 0));
  $('#copiesTable').innerHTML = rows.length ? `<tr><th>Copy</th><th>Access</th><th>Expires</th><th>Offline</th><th>Last seen</th><th>Launches</th><th></th></tr>` + rows.map((l) => `
    <tr data-id="${l.id}">
      <td><span class="dot ${l.online ? 'on' : ''}"></span><b>${esc(l.label || l.deviceName)}</b><br><small class="muted">${esc(l.deviceName)} · ${esc(l.lastIp || '')} · v${esc(l.version || '?')} · via ${esc(l.approvedVia || '?')}</small></td>
      <td>${typeChip(l.type)}${l.expired ? ' <small style="color:var(--red)">expired</small>' : ''}</td>
      <td class="muted">${l.expiresAt ? when(l.expiresAt) : 'never'}</td>
      <td class="muted">${l.type === 'OWNER' ? 'forever' : offlineText(l.offlineDays)}</td>
      <td class="muted">${l.online ? '<span style="color:var(--green)">playing now</span>' : ago(l.lastSeen)}</td>
      <td class="muted">${l.launches || 0}</td>
      <td><div class="row-actions">
        <button class="iconbtn" data-do="edit">Edit</button>
        ${l.type === 'BLOCKED' ? '<button class="iconbtn" data-do="unblock">Unblock</button>' : '<button class="iconbtn red" data-do="block">Block</button>'}
        <button class="iconbtn red" data-do="remove">Remove</button>
      </div></td>
    </tr>`).join('') : '<tr><td class="muted">No copies yet.</td></tr>';
}

function renderSettings() {
  const s = S.settings;
  if (document.activeElement && document.activeElement.closest('[data-pane="settings"]')) return;   // don't overwrite while typing
  $('#sAuto').checked = s.approvalMode === 'auto';
  $('#sAutoType').innerHTML = typeOptions(s.autoType, true);
  $('#sLocal').checked = s.autoApproveLocal;
  $('#sDefault').innerHTML = typeOptions(s.defaultType, true);
  $('#sOffline').value = String(s.offlineDays);
  $('#sEvent').value = s.eventName;
  $('#sDownloads').checked = s.downloadsEnabled;
  $('#sLockdown').checked = s.lockdown;
  $('#sMessage').value = s.message || '';
}

async function saveSettings(patch, quiet) {
  try { await api('/api/admin/settings', patch); if (!quiet) toast('Saved'); await load(); }
  catch (e) { toast(e.message, true); }
}

function chime() {
  try {
    const ctx = new AudioContext();
    [660, 880, 1320].forEach((f, i) => {
      const o = ctx.createOscillator(), g = ctx.createGain();
      o.frequency.value = f; o.type = 'triangle'; o.connect(g); g.connect(ctx.destination);
      g.gain.setValueAtTime(0.0001, ctx.currentTime + i * 0.12);
      g.gain.exponentialRampToValueAtTime(0.2, ctx.currentTime + i * 0.12 + 0.02);
      g.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + i * 0.12 + 0.3);
      o.start(ctx.currentTime + i * 0.12); o.stop(ctx.currentTime + i * 0.12 + 0.35);
    });
  } catch {}
}

function openEdit(l) {
  editing = l;
  $('#editTitle').textContent = 'Edit: ' + (l.label || l.deviceName);
  $('#eLabel').value = l.label || '';
  $('#eType').innerHTML = typeOptions(l.type);
  $('#eExpires').value = l.expiresAt ? new Date(l.expiresAt - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '';
  $('#eOffline').value = String(l.offlineDays);
  $('#eNote').value = l.note || '';
  $('#editDlg').showModal();
}

function wire() {
  $$('#tabs button').forEach((b) => b.addEventListener('click', () => {
    $$('#tabs button').forEach((x) => x.classList.toggle('on', x === b));
    $$('[data-pane]').forEach((p) => (p.hidden = p.dataset.pane !== b.dataset.tab));
    history.replaceState(null, '', '#' + b.dataset.tab);
  }));
  const hash = location.hash.slice(1);
  if (hash) { const b = $(`#tabs [data-tab="${hash}"]`); if (b) b.click(); }

  $('#pendingList').addEventListener('click', async (e) => {
    const btn = e.target.closest('[data-do]'); if (!btn) return;
    const card = btn.closest('.pending'), id = card.dataset.id;
    btn.disabled = true;
    try {
      if (btn.dataset.do === 'approve') {
        await api('/api/admin/approve', { pairId: id, type: card.querySelector('[data-k=type]').value, label: card.querySelector('[data-k=label]').value });
        toast('Approved: the game unlocks in a moment');
      } else {
        await api('/api/admin/deny', { pairId: id });
        toast('Refused');
      }
      await load();
    } catch (err) { toast(err.message, true); btn.disabled = false; }
  });

  $('#copiesTable').addEventListener('click', async (e) => {
    const btn = e.target.closest('[data-do]'); if (!btn) return;
    const l = S.licences.find((x) => x.id === btn.closest('tr').dataset.id); if (!l) return;
    const name = l.label || l.deviceName;
    try {
      if (btn.dataset.do === 'edit') return openEdit(l);
      if (btn.dataset.do === 'block') { if (!confirm(`Block ${name}? The game closes on that PC within a minute.`)) return; await api('/api/admin/licence/' + l.id, { type: 'BLOCKED' }); toast(name + ' blocked'); }
      if (btn.dataset.do === 'unblock') { await api('/api/admin/licence/' + l.id, { type: S.settings.defaultType }); toast(name + ' unblocked'); }
      if (btn.dataset.do === 'remove') { if (!confirm(`Remove ${name}? It will have to be verified again with a QR scan.`)) return; await api('/api/admin/licence/' + l.id, null, 'DELETE'); toast(name + ' removed'); }
      await load();
    } catch (err) { toast(err.message, true); }
  });
  $('#search').addEventListener('input', renderCopies);

  $('#eExpChips').addEventListener('click', (e) => {
    const b = e.target.closest('button'); if (!b) return;
    const ms = expiryFrom(b.dataset.h);
    $('#eExpires').value = ms ? new Date(ms - new Date().getTimezoneOffset() * 60000).toISOString().slice(0, 16) : '';
  });
  $('#editForm').addEventListener('submit', async (e) => {
    if (e.submitter && e.submitter.value === 'cancel') return;
    e.preventDefault();
    const exp = $('#eExpires').value ? new Date($('#eExpires').value).getTime() : 0;
    try {
      await api('/api/admin/licence/' + editing.id, { label: $('#eLabel').value, type: $('#eType').value, expiresAt: exp, offlineDays: Number($('#eOffline').value), note: $('#eNote').value });
      $('#editDlg').close();
      toast('Saved');
      await load();
    } catch (err) { toast(err.message, true); }
  });

  $('#sAuto').addEventListener('change', (e) => saveSettings({ approvalMode: e.target.checked ? 'auto' : 'manual' }));
  $('#sAutoType').addEventListener('change', (e) => saveSettings({ autoType: e.target.value }));
  $('#sLocal').addEventListener('change', (e) => saveSettings({ autoApproveLocal: e.target.checked }));
  $('#sDefault').addEventListener('change', (e) => saveSettings({ defaultType: e.target.value }));
  $('#sOffline').addEventListener('change', (e) => saveSettings({ offlineDays: Number(e.target.value) }));
  $('#sEvent').addEventListener('change', (e) => saveSettings({ eventName: e.target.value }));
  $('#sDownloads').addEventListener('change', (e) => saveSettings({ downloadsEnabled: e.target.checked }));
  $('#sLockdown').addEventListener('change', (e) => {
    if (e.target.checked && !confirm('Lock every copy except Owner copies?')) { e.target.checked = false; return; }
    saveSettings({ lockdown: e.target.checked });
  });
  $('#sendMessage').addEventListener('click', () => saveSettings({ message: $('#sMessage').value }));
  $('#pwForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    try { await api('/api/admin/password', { current: $('#pwCur').value, next: $('#pwNew').value }); toast('Password changed'); e.target.reset(); }
    catch (err) { toast(err.message, true); }
  });
  $('#logout').addEventListener('click', async () => { await api('/api/admin/logout', {}); location.reload(); });
  $('#stop').addEventListener('click', async () => {
    if (!confirm('Stop the GLITCHED server? Verified games keep working offline for their offline days.')) return;
    await api('/api/admin/shutdown', {}).catch(() => {});
    document.body.innerHTML = '<main class="gate"><div class="box"><h1>Server stopped</h1><p class="muted">Start it again from the GLITCHED Server shortcut on the desktop.</p></div></main>';
  });
}

function live() {
  const es = new EventSource('/api/admin/events');
  let t = null;
  es.addEventListener('changed', () => { clearTimeout(t); t = setTimeout(load, 120); });
  es.onopen = () => $('#live').classList.remove('off');
  es.onerror = () => $('#live').classList.add('off');
  setInterval(load, 30000);   // keeps "last seen" fresh
}

(async () => {
  await ensureLogin();
  $('#app').hidden = false;
  wire();
  await load();
  live();
})();

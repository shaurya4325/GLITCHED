// Shared by the admin page and the phone approve page.
export const $ = (s, el = document) => el.querySelector(s);
export const $$ = (s, el = document) => [...el.querySelectorAll(s)];

export async function api(path, body, method) {
  const r = await fetch(path, {
    method: method || (body ? 'POST' : 'GET'),
    headers: { 'Content-Type': 'application/json', 'X-Glitched': '1' },
    body: body ? JSON.stringify(body) : undefined,
    cache: 'no-store',
  });
  let data = null;
  try { data = await r.json(); } catch {}
  if (!r.ok) { const e = new Error((data && data.error) || r.statusText); e.status = r.status; throw e; }
  return data;
}

export function toast(text, err) {
  const t = document.createElement('div');
  t.className = 'toast' + (err ? ' err' : '');
  t.textContent = text;
  document.body.appendChild(t);
  setTimeout(() => t.remove(), 3200);
}

export const esc = (s) => String(s ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

export function ago(ms) {
  if (!ms) return 'never';
  const s = Math.round((Date.now() - ms) / 1000);
  if (s < 60) return 'just now';
  if (s < 3600) return Math.round(s / 60) + ' min ago';
  if (s < 86400) return Math.round(s / 3600) + ' h ago';
  return new Date(ms).toLocaleDateString();
}
export const when = (ms) => (ms ? new Date(ms).toLocaleString([], { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }) : '–');
export const offlineText = (d) => (d < 0 ? 'forever' : d + (d === 1 ? ' day' : ' days'));

/** Expiry chip value -> timestamp (0 = never). */
export function expiryFrom(h) {
  if (h === '0') return 0;
  if (h === 'eod') { const d = new Date(); d.setHours(23, 59, 0, 0); return d.getTime(); }
  return Date.now() + Number(h) * 3600 * 1000;
}

/** Shows the login (or first-time password) gate; resolves once logged in. */
export async function ensureLogin() {
  const who = await api('/api/admin/whoami');
  if (who.loggedIn) return who;
  const gate = $('#gate');
  gate.hidden = false;
  const setup = who.setup;
  if (setup) {
    $('#gateTitle').textContent = 'Create the admin password';
    $('#gateText').textContent = who.local
      ? 'First time here. Choose a password for the GLITCHED team (at least 6 characters). You will need it on your phone to approve copies.'
      : 'The admin password has not been created yet. Create it on the server PC first (open http://localhost:8080/admin there).';
    $('#gateRepeatWrap').hidden = !who.local;
    $('#gateBtn').textContent = 'Create password';
    $('#gatePass').autocomplete = 'new-password';
    if (!who.local) $('#gateForm').hidden = true;
  }
  return new Promise((resolve) => {
    $('#gateForm').addEventListener('submit', async (e) => {
      e.preventDefault();
      $('#gateErr').textContent = '';
      const pw = $('#gatePass').value;
      try {
        if (setup) {
          if (pw !== $('#gateRepeat').value) { $('#gateErr').textContent = 'The two passwords are different.'; return; }
          await api('/api/admin/setup', { password: pw });
        } else {
          await api('/api/admin/login', { password: pw });
        }
        gate.hidden = true;
        resolve(await api('/api/admin/whoami'));
      } catch (err) {
        $('#gateErr').textContent = err.message;
        $('#gatePass').select();
      }
    });
    $('#gatePass').focus();
  });
}

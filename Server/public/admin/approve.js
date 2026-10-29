// The page the admin phone opens when it scans the QR code shown by the game (/a/<code>).
import { $, api, esc, ago, expiryFrom, ensureLogin } from './common.js';

const code = location.pathname.split('/').pop();

async function show() {
  let d;
  try { d = await api('/api/admin/pending/' + code); }
  catch (e) {
    $('#body').innerHTML = `<div class="box"><h3>Code not found</h3><p class="muted">${esc(e.message)}</p><a class="btn ghost" href="/admin">Open the admin page</a></div>`;
    return;
  }
  const p = d.pending;
  if (p.status !== 'pending') return done(p.status === 'approved' ? 'Already approved' : 'This request was refused', p.status === 'approved');
  const types = Object.entries(d.types).filter(([k]) => k !== 'BLOCKED');
  $('#body').innerHTML = `
    <div class="device">
      <span class="code">${esc(p.code)}</span>
      <b>${esc(p.deviceName)}</b>
      <small>${esc(p.os || '')}${p.gpu ? ' · ' + esc(p.gpu) : ''}${p.ram ? ' · ' + Math.round(p.ram / 1024) + ' GB RAM' : ''}</small>
      <small>From ${esc(p.ip)} · asked ${ago(p.createdAt)}</small>
      ${d.existing ? `<small style="color:var(--yellow)">This PC was approved before (${esc(d.existing.label || d.existing.deviceName)}).</small>` : ''}
    </div>
    <p class="muted" style="margin-bottom:8px">Check that the code above matches the one on the game's screen.</p>
    <div class="choices">${types.map(([k, t]) => `
      <label class="choice"><input type="radio" name="type" value="${k}" ${k === d.settings.defaultType ? 'checked' : ''}><b>${esc(t.label)}</b><small>${esc(t.desc)}</small></label>`).join('')}
    </div>
    <label class="field"><span>Name for this copy (optional)</span><input type="text" id="label" maxlength="60" placeholder="e.g. Judge 1, Riya's laptop"></label>
    <p class="muted" style="margin:14px 0 0;font-size:13px">EXPIRES</p>
    <div class="chips" id="exp"><button type="button" data-h="0" class="on">Never</button><button type="button" data-h="1">1 hour</button><button type="button" data-h="eod">End of today</button><button type="button" data-h="24">1 day</button><button type="button" data-h="168">7 days</button></div>
    <div class="big-actions">
      <button class="btn ok" id="yes"><span>APPROVE</span></button>
      <button class="btn danger" id="no"><span>Refuse</span></button>
    </div>`;
  let exp = '0';
  $('#exp').addEventListener('click', (e) => {
    const b = e.target.closest('button'); if (!b) return;
    exp = b.dataset.h;
    [...$('#exp').children].forEach((x) => x.classList.toggle('on', x === b));
  });
  $('#yes').addEventListener('click', async () => {
    $('#yes').disabled = true;
    try {
      const type = document.querySelector('input[name=type]:checked').value;
      await api('/api/admin/approve', { pairId: p.id, type, label: $('#label').value, expiresAt: expiryFrom(exp), via: 'qr' });
      if (navigator.vibrate) navigator.vibrate([20, 30, 60]);
      done(`${esc(p.deviceName)} is verified`, true, d.types[type].label);
    } catch (e) { $('#yes').disabled = false; alertBox(e.message); }
  });
  $('#no').addEventListener('click', async () => {
    try { await api('/api/admin/deny', { pairId: p.id }); done('Refused', false); } catch (e) { alertBox(e.message); }
  });
}

function alertBox(text) {
  const el = document.createElement('p');
  el.className = 'err'; el.style.color = '#ff8a96'; el.textContent = text;
  $('#body').prepend(el);
}

function done(title, ok, type) {
  $('#body').innerHTML = `<div class="success"><div class="tick" style="${ok ? '' : 'border-color:var(--red);color:var(--red);box-shadow:0 0 60px rgba(255,59,78,.5)'}">${ok ? '✓' : '✕'}</div>
    <h2>${title}</h2><p class="muted">${ok ? `Access: ${esc(type || '')}. The game unlocks by itself in a second or two.` : 'The game shows that it was refused.'}</p>
    <a class="btn ghost" href="/admin"><span>Open the admin page</span></a></div>`;
}

(async () => {
  await ensureLogin();
  $('#app').hidden = false;
  show();
})();

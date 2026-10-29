// GLITCHED website: boot screen, live stats, the scroll-driven 3D camera, features, gallery ring and the hold-to-install core.
const $ = (s, el = document) => el.querySelector(s);
const $$ = (s, el = document) => [...el.querySelectorAll(s)];
const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
const low = matchMedia('(max-width: 760px)').matches || (navigator.hardwareConcurrency || 8) <= 4;

let world = null;
let info = null;

// ------------------------------------------------------------------ boot screen
const bootLines = ['> mounting /worlds/code_city', '> calibrating gravity ... 9.8 m/s²', '> balancing equations', '> sequencing DNA', '> scanning for the glitch', '> knowledge.weapon = true'];
async function boot() {
  const log = $('#bootLog'), bar = $('#bootBar');
  let shown = 0;
  const tick = setInterval(() => {
    if (shown < bootLines.length) { log.textContent += bootLines[shown++] + '\n'; bar.style.width = (shown / bootLines.length * 90) + '%'; }
  }, reduced ? 40 : 150);
  const started = performance.now();
  try {
    const { startWorld } = await import('./world.js');
    world = startWorld($('#world'), { low, reducedMotion: reduced });
  } catch (e) { console.warn('3D disabled:', e); }
  if (!world) document.body.classList.add('no-webgl');
  await new Promise((r) => setTimeout(r, Math.max(0, (reduced ? 300 : 1100) - (performance.now() - started))));
  clearInterval(tick);
  bar.style.width = '100%';
  setTimeout(() => $('#boot').classList.add('done'), 200);
  onScroll();
}

// ------------------------------------------------------------------ live info
const fmtSize = (b) => b > 1e9 ? (b / 1e9).toFixed(2) + ' GB' : (b / 1e6).toFixed(0) + ' MB';
function countTo(el, value) {
  const from = parseInt(el.textContent, 10) || 0;
  if (from === value || reduced) { el.textContent = value; return; }
  const t0 = performance.now();
  const step = (t) => { const k = Math.min(1, (t - t0) / 700); el.textContent = Math.round(from + (value - from) * k); if (k < 1) requestAnimationFrame(step); };
  requestAnimationFrame(step);
}
async function loadInfo() {
  try {
    info = await (await fetch('/api/public/info', { cache: 'no-store' })).json();
  } catch { return; }
  for (const el of $$('[data-stat]')) countTo(el, info.stats[el.dataset.stat] ?? 0);
  $('#eventName').textContent = info.event;
  $('#fVersion').textContent = info.version;
  const s = info.files.setup;
  $('#fSize').textContent = s.available ? fmtSize(s.size) : 'not built yet';
  $('#fHash').textContent = s.sha256 || (s.available ? 'calculating…' : '–');
  $('#portableWrap').hidden = !info.files.portable.available;
  $('#siteUrl').textContent = info.url;
  const qr = $('#siteQr');
  if (!qr.dataset.url || qr.dataset.url !== info.url) { qr.src = '/api/qr.svg?dark=1&t=' + encodeURIComponent(info.url); qr.dataset.url = info.url; }
  updateCoreState();
  if (!galleryBuilt) buildGallery(info.screens || []);
}

// ------------------------------------------------------------------ scroll -> camera
let stops = [];
function measure() {
  stops = $$('[data-cam]').map((el) => {
    const r = el.getBoundingClientRect();
    return { el, cam: parseFloat(el.dataset.cam), mid: r.top + window.scrollY + Math.min(r.height, window.innerHeight) / 2, world: el.dataset.world, accent: getComputedStyle(el).getPropertyValue('--accent').trim() };
  }).sort((a, b) => a.mid - b.mid);
}
function onScroll() {
  if (!stops.length) measure();
  const y = window.scrollY + window.innerHeight / 2;
  let cam = stops[0].cam;
  for (let i = 0; i < stops.length - 1; i++) {
    const a = stops[i], b = stops[i + 1];
    if (y >= a.mid && y < b.mid) { cam = a.cam + (b.cam - a.cam) * ((y - a.mid) / (b.mid - a.mid)); break; }
    if (y >= b.mid) cam = b.cam;
  }
  if (world) world.setCamera(cam);
  const max = document.documentElement.scrollHeight - window.innerHeight;
  $('#rail').style.width = (max > 0 ? window.scrollY / max * 100 : 0) + '%';
  $('#nav').classList.toggle('solid', window.scrollY > 40);
  // world dots
  const dots = $$('#worldDots li');
  const worldIdx = Math.round(cam) - 2;
  $('#worldDots').classList.toggle('show', cam > 1.5 && cam < 7.5);
  dots.forEach((d, i) => d.classList.toggle('on', i === worldIdx));
  // nav highlight
  const ids = ['story', 'worlds', 'features', 'gallery', 'verify'];
  let active = null;
  for (const id of ids) { const el = document.getElementById(id); if (el && el.getBoundingClientRect().top < window.innerHeight * 0.5) active = id; }
  $$('.nav nav a').forEach((a) => a.classList.toggle('active', a.getAttribute('href') === '#' + active));
}

function buildDots() {
  const ol = $('#worldDots');
  for (const s of $$('[data-world]')) {
    const li = document.createElement('li');
    li.style.setProperty('--c', getComputedStyle(s).getPropertyValue('--accent'));
    li.title = s.dataset.world;
    ol.appendChild(li);
  }
}

// ------------------------------------------------------------------ features
const ICONS = {
  sword: '<path d="M14.5 17.5 3 6V3h3l11.5 11.5"/><path d="m13 19 6-6"/><path d="m16 16 4 4"/><path d="m19 21 2-2"/>',
  grid: '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>',
  book: '<path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/><path d="M9 7h7M9 11h5"/>',
  flask: '<path d="M9 3h6M10 3v6L4 20a1 1 0 0 0 .9 1.5h14.2A1 1 0 0 0 20 20l-6-11V3"/><path d="M7 15h10"/>',
  brain: '<path d="M12 5a3 3 0 1 0-5.997.125 4 4 0 0 0-2.526 5.77 4 4 0 0 0 .556 6.588A4 4 0 1 0 12 18Z"/><path d="M12 5a3 3 0 1 1 5.997.125 4 4 0 0 1 2.526 5.77 4 4 0 0 1-.556 6.588A4 4 0 1 1 12 18Z"/><path d="M12 5v13"/>',
  calc: '<rect x="4" y="2" width="16" height="20" rx="2"/><path d="M8 6h8M8 10h.01M12 10h.01M16 10h.01M8 14h.01M12 14h.01M16 14h.01M8 18h.01M12 18h4"/>',
  crown: '<path d="m2 4 3 12h14l3-12-6 7-4-7-4 7-6-7z"/><path d="M5 20h14"/>',
  portal: '<circle cx="12" cy="12" r="9"/><path d="M12 7a5 5 0 1 0 5 5"/><path d="M12 10a2 2 0 1 0 2 2"/>',
  mic: '<rect x="9" y="2" width="6" height="12" rx="3"/><path d="M19 10v1a7 7 0 0 1-14 0v-1M12 18v4M8 22h8"/>',
  run: '<circle cx="13" cy="4" r="2"/><path d="m4 17 5 1 2-4 3 3v5"/><path d="m8 9 4-2 3 3 3 1"/>',
  eye: '<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>',
  gauge: '<path d="M12 14l4-4"/><path d="M3.34 19a10 10 0 1 1 17.32 0"/>',
  timer: '<circle cx="12" cy="13" r="8"/><path d="M12 9v4l2 2M9 2h6"/>',
  star: '<path d="m12 2 3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01z"/>',
  shield: '<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/><path d="m9 12 2 2 4-4"/>',
  wifi: '<path d="M5 12.55a11 11 0 0 1 14.08 0M1.42 9a16 16 0 0 1 21.16 0M8.53 16.11a6 6 0 0 1 6.95 0M12 20h.01"/>',
};
const FEATURES = [
  ['sword', '#40ebff', 'Knowledge is your weapon', 'Every battle is won by answering. Answer fast for a CRITICAL, chain answers into a COMBO, and save up for an ULTIMATE.'],
  ['grid', '#ff3dc8', '10 kinds of challenge', 'Drag and drop, logic, numericals, debugging, coding, simulations, matching, prediction, short answers and building things.'],
  ['book', '#7cffa0', '364 questions · CBSE Class XI', 'Computer Science, Physics, Chemistry and Biology, written to the syllabus, plus 20 short lessons you can open from inside the levels.'],
  ['brain', '#ffd84a', 'Losing teaches you', 'Lose once and you get a hint. Lose again and an interactive lab opens: loop sandbox, binary, logic gates, physics sliders, a bond builder, blood-flow tracing. Then a rematch.'],
  ['gauge', '#40ebff', 'It learns what you need', 'Mastery is tracked for every concept. Weak topics come back more often and the difficulty moves with you, one step at a time.'],
  ['calc', '#ffb84a', 'Rough paper + calculator', 'Every question has a scratch pad you can write on and a scientific calculator. The timer pauses while you work.'],
  ['crown', '#ff3b4e', '10 bosses', 'Eight realistic boss models, The Source, and Kanha the Dard waiting in the exam hall. Each fight is a set of tough questions on that chapter.'],
  ['portal', '#b36bff', 'One journey, five worlds', 'Chapter portals link Code City → Physics Valley → Chemical Chaos → Bio-Zone → The Core, opening with a fully animated cutscene.'],
  ['mic', '#ff5fa2', 'Voiced by the team', 'Shaurya, Pixel and the bosses talk with voices we recorded. The Voice Studio lets anyone re-record a line inside the game.'],
  ['run', '#7cffa0', 'Real platforming', 'Run, jump, dash, wall-hop and shrink through 2.5D levels where corrupted worlds heal as you learn.'],
  ['eye', '#40ebff', 'Made for everyone', 'Text size, a dyslexia-friendly font, colour-blind-safe answers, reduced flashing, relaxed timers and camera-sway control.'],
  ['timer', '#ffd84a', 'Expo mode', 'A 10-minute guided demo for exhibitions that resets by itself between visitors.'],
  ['star', '#ff3dc8', 'Progress that matters', 'XP, Knowledge Points for real mastery, coins, a cosmetic shop, achievements and three save slots.'],
  ['gauge', '#7cffa0', 'Runs on school PCs', 'Five quality levels from Potato to Ultra, picked by a quick test on first launch, with fullscreen or windowed play.'],
  ['shield', '#40ebff', 'Verified copies', 'Each install is approved by QR code and signed for its own PC, so copies can\'t be passed around.'],
  ['wifi', '#8fa3c0', 'No internet needed', 'Everything runs offline. Even this website is served from the stall\'s laptop.'],
];
function buildFeatures() {
  const grid = $('#featureGrid');
  FEATURES.forEach(([icon, color, title, text], i) => {
    const el = document.createElement('article');
    el.className = 'feature reveal';
    el.style.setProperty('--fc', color);
    el.style.transitionDelay = (i % 4) * 60 + 'ms';
    el.innerHTML = `<span class="num">${String(i + 1).padStart(2, '0')}</span><div class="ico"><svg viewBox="0 0 24 24">${ICONS[icon]}</svg></div><h3></h3><p></p>`;
    el.querySelector('h3').textContent = title;
    el.querySelector('p').textContent = text;
    grid.appendChild(el);
    if (reduced) return;
    el.addEventListener('pointermove', (e) => {
      const r = el.getBoundingClientRect();
      const x = (e.clientX - r.left) / r.width, y = (e.clientY - r.top) / r.height;
      el.style.setProperty('--ry', (x - 0.5) * 14 + 'deg');
      el.style.setProperty('--rx', (0.5 - y) * 12 + 'deg');
      el.style.setProperty('--mx', x * 100 + '%');
      el.style.setProperty('--my', y * 100 + '%');
    });
    el.addEventListener('pointerleave', () => { el.style.setProperty('--rx', '0deg'); el.style.setProperty('--ry', '0deg'); });
  });
}

// ------------------------------------------------------------------ gallery ring
let galleryBuilt = false;
function buildGallery(list) {
  galleryBuilt = true;
  const ring = $('#ring'), stage = $('#ringStage');
  if (!list.length) { stage.hidden = true; $('#galleryEmpty').hidden = false; return; }
  const n = list.length;
  const caption = (f) => f.replace(/\.[a-z]+$/i, '').replace(/^\d+[_-]?/, '').replace(/[_-]+/g, ' ').toUpperCase();
  list.forEach((f, i) => {
    const fig = document.createElement('figure');
    fig.innerHTML = `<img loading="lazy" alt=""><figcaption></figcaption>`;
    fig.querySelector('img').src = '/screens/' + encodeURIComponent(f);
    fig.querySelector('img').alt = 'GLITCHED screenshot: ' + caption(f).toLowerCase();
    fig.querySelector('figcaption').textContent = caption(f);
    fig.dataset.i = i;
    ring.appendChild(fig);
  });
  let angle = 0, vel = reduced ? 0 : -0.08, dragging = false, lastX = 0, moved = 0;
  const layout = () => {
    const w = ring.getBoundingClientRect().width;
    const radius = Math.round((w / 2) / Math.tan(Math.PI / n) * 1.12);
    $$('figure', ring).forEach((fig, i) => { fig.style.transform = `rotateY(${i * 360 / n}deg) translateZ(${radius}px)`; });
    ring.style.setProperty('--r', radius + 'px');
    ring.dataset.radius = radius;
  };
  layout();
  window.addEventListener('resize', layout);
  const frame = () => {
    if (!dragging) { angle += vel; vel += ((reduced ? 0 : -0.08) - vel) * 0.02; }
    ring.style.transform = `translateZ(-${ring.dataset.radius}px) rotateY(${angle}deg)`;
    const front = ((Math.round(-angle / (360 / n)) % n) + n) % n;
    $$('figure', ring).forEach((fig, i) => { fig.style.filter = i === front ? 'none' : 'brightness(.55) saturate(.7)'; });
    requestAnimationFrame(frame);
  };
  frame();
  stage.addEventListener('pointerdown', (e) => { dragging = true; lastX = e.clientX; moved = 0; stage.setPointerCapture(e.pointerId); });
  stage.addEventListener('pointermove', (e) => { if (!dragging) return; const dx = e.clientX - lastX; lastX = e.clientX; moved += Math.abs(dx); angle += dx * 0.25; vel = dx * 0.25; });
  stage.addEventListener('pointerup', (e) => {
    dragging = false;
    if (moved < 6) {
      const fig = document.elementsFromPoint(e.clientX, e.clientY).find((el) => el.tagName === 'FIGURE');
      if (fig) openLightbox(fig.querySelector('img').src, fig.querySelector('img').alt);
    }
  });
  stage.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowLeft') { angle += 360 / n; vel = 0; }
    if (e.key === 'ArrowRight') { angle -= 360 / n; vel = 0; }
    if (e.key === 'Enter') { const front = ((Math.round(-angle / (360 / n)) % n) + n) % n; const img = $$('figure img', ring)[front]; openLightbox(img.src, img.alt); }
  });
}
function openLightbox(src, alt) {
  const lb = $('#lightbox');
  lb.querySelector('img').src = src;
  lb.querySelector('img').alt = alt;
  lb.hidden = false;
}

// ------------------------------------------------------------------ the hold-to-install core
const HOLD_SECONDS = 1.6;
const core = { p: 0, holding: false, done: false, last: 0 };
let audio = null;
function sound() {
  if (audio) return audio;
  try {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const gain = ctx.createGain(); gain.gain.value = 0; gain.connect(ctx.destination);
    const osc = ctx.createOscillator(); osc.type = 'sawtooth'; osc.frequency.value = 80;
    const filt = ctx.createBiquadFilter(); filt.type = 'lowpass'; filt.frequency.value = 600;
    osc.connect(filt); filt.connect(gain); osc.start();
    audio = { ctx, gain, osc, filt };
  } catch { audio = null; }
  return audio;
}
function boom() {
  const a = sound(); if (!a) return;
  const { ctx } = a;
  const len = ctx.sampleRate * 0.9, buf = ctx.createBuffer(1, len, ctx.sampleRate), d = buf.getChannelData(0);
  for (let i = 0; i < len; i++) d[i] = (Math.random() * 2 - 1) * Math.pow(1 - i / len, 3);
  const src = ctx.createBufferSource(); src.buffer = buf;
  const g = ctx.createGain(); g.gain.value = 0.35;
  const f = ctx.createBiquadFilter(); f.type = 'lowpass'; f.frequency.setValueAtTime(3000, ctx.currentTime); f.frequency.exponentialRampToValueAtTime(80, ctx.currentTime + 0.8);
  src.connect(f); f.connect(g); g.connect(ctx.destination); src.start();
}
function updateCoreState() {
  const btn = $('#coreButton'), label = $('#coreLabel');
  if (!info || core.done) return;
  const ok = info.downloadsEnabled && info.files.setup.available;
  btn.disabled = !ok;
  label.innerHTML = ok ? 'HOLD TO<br>INSTALL' : (info.downloadsEnabled ? 'NOT BUILT<br>YET' : 'DOWNLOADS<br>PAUSED');
}
const SCRAMBLE = '01<>/{}[]#$%&*+=?ABCDEF';
function setupCore() {
  const btn = $('#coreButton'), label = $('#coreLabel'), pct = $('#corePct');
  const start = (e) => {
    if (btn.disabled || core.done) return;
    if (e) e.preventDefault();
    core.holding = true;
    btn.classList.add('charging');
    const a = sound(); if (a && a.ctx.state === 'suspended') a.ctx.resume();
    if (navigator.vibrate) navigator.vibrate(15);
  };
  const stop = () => { core.holding = false; };
  btn.addEventListener('pointerdown', (e) => { btn.setPointerCapture(e.pointerId); start(e); });
  btn.addEventListener('pointerup', stop);
  btn.addEventListener('pointercancel', stop);
  btn.addEventListener('lostpointercapture', stop);
  btn.addEventListener('contextmenu', (e) => e.preventDefault());
  btn.addEventListener('keydown', (e) => { if ((e.key === ' ' || e.key === 'Enter') && !e.repeat) start(e); });
  btn.addEventListener('keyup', (e) => { if (e.key === ' ' || e.key === 'Enter') stop(); });
  btn.addEventListener('click', (e) => e.preventDefault());

  let scrambleT = 0;
  const loop = (t) => {
    const dt = Math.min(0.05, (t - (core.last || t)) / 1000); core.last = t;
    if (!core.done) {
      core.p = core.holding ? Math.min(1, core.p + dt / HOLD_SECONDS) : Math.max(0, core.p - dt * 1.4);
      btn.style.setProperty('--p', core.p.toFixed(3));
      btn.style.setProperty('--jx', (Math.random() * 2 - 1).toFixed(2));
      btn.style.setProperty('--jy', (Math.random() * 2 - 1).toFixed(2));
      if (world) world.charge(core.p);
      const a = audio;
      if (a) {
        a.gain.gain.setTargetAtTime(core.p > 0.01 ? 0.05 + core.p * 0.07 : 0, a.ctx.currentTime, 0.05);
        a.osc.frequency.setTargetAtTime(80 + core.p * core.p * 600, a.ctx.currentTime, 0.05);
        a.filt.frequency.setTargetAtTime(500 + core.p * 3500, a.ctx.currentTime, 0.05);
      }
      if (core.p > 0.02) {
        pct.textContent = Math.round(core.p * 100) + '%';
        scrambleT += dt;
        if (scrambleT > 0.05) {
          scrambleT = 0;
          const words = ['INJECTING', 'CHARGING', 'COMPILING'];
          const w = words[Math.min(2, Math.floor(core.p * 3))];
          label.textContent = [...w].map((c, i) => (Math.random() < 0.35 * (1 - core.p) + 0.05 ? SCRAMBLE[Math.floor(Math.random() * SCRAMBLE.length)] : c)).join('');
        }
      } else if (core.holding === false && pct.textContent) {
        pct.textContent = '';
        btn.classList.remove('charging');
        updateCoreState();
      }
      if (core.p >= 1) complete();
    }
    requestAnimationFrame(loop);
  };
  requestAnimationFrame(loop);

  function complete() {
    core.done = true; core.holding = false;
    btn.classList.remove('charging'); btn.classList.add('done');
    label.innerHTML = 'DOWNLOAD<br>STARTED';
    pct.textContent = '';
    if (audio) audio.gain.gain.setTargetAtTime(0, audio.ctx.currentTime, 0.02);
    boom();
    if (navigator.vibrate) navigator.vibrate([30, 40, 80]);
    if (world) world.burst();
    const r = btn.getBoundingClientRect();
    burst(r.left + r.width / 2, r.top + r.height / 2);
    document.body.animate([{ filter: 'none' }, { filter: 'hue-rotate(90deg) saturate(2) contrast(1.4)' }, { filter: 'none' }], { duration: 450 });
    const a = document.createElement('a');
    a.href = '/download/setup'; a.download = (info && info.files.setup.name) || 'GLITCHED_Setup.exe';
    document.body.appendChild(a); a.click(); a.remove();
    setTimeout(() => openModal('#afterModal'), 900);
    setTimeout(() => { core.done = false; core.p = 0; btn.classList.remove('done'); btn.style.setProperty('--p', 0); updateCoreState(); }, 6000);
  }
}

// particle burst on the 2D overlay canvas
function burst(x, y) {
  const c = $('#burst'), ctx = c.getContext('2d');
  const dpr = Math.min(2, window.devicePixelRatio || 1);
  c.width = innerWidth * dpr; c.height = innerHeight * dpr; ctx.scale(dpr, dpr);
  const parts = [];
  for (let i = 0; i < (reduced ? 40 : 220); i++) {
    const a = Math.random() * Math.PI * 2, s = 2 + Math.random() * 12;
    parts.push({ x, y, vx: Math.cos(a) * s, vy: Math.sin(a) * s, life: 1, size: 1 + Math.random() * 3.5, c: Math.random() < 0.5 ? '64,235,255' : '255,61,200', sq: Math.random() < 0.4 });
  }
  let ring = 0;
  const step = () => {
    ctx.clearRect(0, 0, innerWidth, innerHeight);
    ring += 18;
    if (ring < Math.max(innerWidth, innerHeight)) {
      ctx.strokeStyle = `rgba(64,235,255,${Math.max(0, 0.6 - ring / 1400)})`; ctx.lineWidth = 3;
      ctx.beginPath(); ctx.arc(x, y, ring, 0, Math.PI * 2); ctx.stroke();
    }
    let alive = 0;
    for (const p of parts) {
      if (p.life <= 0) continue;
      alive++;
      p.x += p.vx; p.y += p.vy; p.vx *= 0.96; p.vy = p.vy * 0.96 + 0.08; p.life -= 0.012;
      ctx.fillStyle = `rgba(${p.c},${p.life})`;
      if (p.sq) ctx.fillRect(p.x, p.y, p.size * 2, p.size * 2); else { ctx.beginPath(); ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2); ctx.fill(); }
    }
    if (alive) requestAnimationFrame(step); else ctx.clearRect(0, 0, innerWidth, innerHeight);
  };
  step();
}

// ------------------------------------------------------------------ modals, nav, reveal
function openModal(sel) {
  const m = $(sel); m.hidden = false;
  const close = m.querySelector('[data-close]'); if (close) close.focus();
}
function setupChrome() {
  document.addEventListener('click', (e) => {
    const close = e.target.closest('[data-close]');
    if (close) close.closest('.modal, .lightbox').hidden = true;
    else if (e.target.classList.contains('modal') || e.target.classList.contains('lightbox')) e.target.hidden = true;
  });
  document.addEventListener('keydown', (e) => { if (e.key === 'Escape') $$('.modal, .lightbox').forEach((m) => (m.hidden = true)); });
  const toggle = $('#navToggle'), nav = $('.nav nav');
  toggle.addEventListener('click', () => { const open = nav.classList.toggle('open'); toggle.setAttribute('aria-expanded', open); });
  $$('.nav nav a').forEach((a) => a.addEventListener('click', () => nav.classList.remove('open')));
  const io = new IntersectionObserver((entries) => entries.forEach((en) => { if (en.isIntersecting) { en.target.classList.add('in'); io.unobserve(en.target); } }), { threshold: 0.15 });
  $$('.reveal').forEach((el) => io.observe(el));
  window.addEventListener('scroll', onScroll, { passive: true });
  window.addEventListener('resize', () => { measure(); onScroll(); });
  window.addEventListener('pointermove', (e) => { if (world && !reduced) world.setPointer(e.clientX / innerWidth * 2 - 1, e.clientY / innerHeight * 2 - 1); }, { passive: true });
}

buildFeatures();
buildDots();
setupChrome();
setupCore();
boot();
loadInfo();
setInterval(loadInfo, 15000);
window.addEventListener('load', () => { measure(); onScroll(); });

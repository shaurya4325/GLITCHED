// The desktop shortcut opens the site with ?host=1. While a page of that window is open it holds a connection to
// /api/host; the server stops itself a few seconds after the last one closes (backup for the launcher's own check).
// Touch devices (phones and tablets) get the light rendering path; ?tablet=1 tests it on a PC. Set here so the CSS applies before first paint.
if (matchMedia('(hover: none) and (pointer: coarse)').matches || /[?&]tablet=1/.test(location.search)) document.documentElement.classList.add('touch');
(() => {
  const local = /^(localhost|127\.0\.0\.1|\[::1\])$/.test(location.hostname);
  if (!local) return;
  try {
    if (new URLSearchParams(location.search).get('host') === '1') sessionStorage.setItem('glitchedHost', '1');
    if (sessionStorage.getItem('glitchedHost') !== '1') return;
  } catch { return; }
  new EventSource('/api/host');
})();

// QR codes for the game (a module matrix it draws itself) and the web pages (SVG).
'use strict';
const qrcode = require('./vendor/qrcode.js');

function make(text) {
  const qr = qrcode(0, 'M');      // type 0 = smallest version that fits
  qr.addData(String(text), 'Byte');
  qr.make();
  return qr;
}

/** { size, bits } where bits is size*size characters of '1' (dark) and '0', row by row. */
function matrix(text) {
  const qr = make(text);
  const n = qr.getModuleCount();
  let bits = '';
  for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) bits += qr.isDark(r, c) ? '1' : '0';
  return { size: n, bits };
}

function svg(text, { dark = '#e8f7ff', light = 'transparent', margin = 2 } = {}) {
  const qr = make(text);
  const n = qr.getModuleCount();
  const total = n + margin * 2;
  let d = '';
  for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) if (qr.isDark(r, c)) d += `M${c + margin} ${r + margin}h1v1h-1z`;
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${total} ${total}" shape-rendering="crispEdges">` +
    (light !== 'transparent' ? `<rect width="100%" height="100%" fill="${light}"/>` : '') +
    `<path d="${d}" fill="${dark}"/></svg>`;
}

module.exports = { matrix, svg };

// LAN helpers: this PC's addresses, and the UDP beacon the game uses to find the server on the Wi-Fi.
'use strict';
const os = require('os');
const dgram = require('dgram');

/** IPv4 addresses other devices can reach, best guess first (Wi-Fi / hotspot before virtual adapters). */
function lanAddresses() {
  const out = [];
  for (const [name, list] of Object.entries(os.networkInterfaces())) {
    for (const a of list || []) {
      if (a.family !== 'IPv4' || a.internal) continue;
      const virtual = /vethernet|virtual|vmware|vbox|hyper-v|wsl|docker|loopback|bluetooth/i.test(name);
      const hotspot = a.address.startsWith('192.168.137.');   // Windows Mobile Hotspot
      out.push({ name, address: a.address, score: (virtual ? 0 : 10) + (hotspot ? 5 : 0) + (/wi-?fi|wlan|wireless/i.test(name) ? 3 : 0) });
    }
  }
  return out.sort((a, b) => b.score - a.score).map(({ name, address }) => ({ name, address }));
}

/** Answers "GLITCHED?" broadcasts with where the server is. */
function startBeacon(port, info) {
  const sock = dgram.createSocket({ type: 'udp4', reuseAddr: true });
  sock.on('message', (msg, rinfo) => {
    if (!msg.toString('utf8').startsWith('GLITCHED?')) return;
    const reply = Buffer.from(JSON.stringify(Object.assign({ glitched: 1 }, info())));
    sock.send(reply, rinfo.port, rinfo.address);
  });
  sock.on('error', (e) => console.warn('[beacon] ' + e.message + ' (games can still connect by typing the address)'));
  sock.bind(port, () => console.log(`[beacon] games can find this server on UDP ${port}`));
  return sock;
}

module.exports = { lanAddresses, startBeacon };

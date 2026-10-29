// The server's database: one JSON file (data/db.json), written atomically a moment after each change.
'use strict';
const fs = require('fs');
const path = require('path');
const { EventEmitter } = require('events');

const DEFAULT_SETTINGS = {
  eventName: 'CBSE Skill Expo 2026-27',
  approvalMode: 'manual',      // manual: every copy waits for a QR scan; auto: copies are approved straight away
  autoType: 'DEMO',            // access type given in auto mode
  defaultType: 'FULL',         // pre-selected on the approve screen
  offlineDays: 7,              // how long a copy keeps working without seeing this server
  autoApproveLocal: true,      // the game on this PC (same machine as the server) is approved as OWNER
  downloadsEnabled: true,
  lockdown: false,             // every copy except OWNER is locked until switched off
  message: '',                 // shown once in every running game
  messageId: 0,
};

const LIMITS = { log: 600, downloads: 3000 };

class Store extends EventEmitter {
  constructor(dir) {
    super();
    this.file = path.join(dir, 'db.json');
    fs.mkdirSync(dir, { recursive: true });
    let db = {};
    if (fs.existsSync(this.file)) {
      try { db = JSON.parse(fs.readFileSync(this.file, 'utf8')); }
      catch (e) {
        const bad = this.file + '.corrupt-' + Date.now();
        fs.copyFileSync(this.file, bad);
        console.error(`[store] db.json could not be read (saved a copy as ${path.basename(bad)}); starting from the backup if there is one`);
        try { db = JSON.parse(fs.readFileSync(this.file + '.bak', 'utf8')); } catch { db = {}; }
      }
    }
    this.db = Object.assign({ version: 1, serverId: null, admin: null, sessions: {}, licences: [], pending: [], downloads: [], log: [] }, db);
    this.db.settings = Object.assign({}, DEFAULT_SETTINGS, db.settings || {});
    if (!this.db.serverId) this.db.serverId = require('crypto').randomBytes(6).toString('hex');
    this.timer = null;
    this.save();
  }

  get settings() { return this.db.settings; }

  /** Marks the database changed: saves it shortly and tells the admin pages. */
  changed(what = 'state') {
    this.save();
    this.emit('changed', what);
  }

  save() {
    clearTimeout(this.timer);
    this.timer = setTimeout(() => this.flush(), 150);
  }

  flush() {
    clearTimeout(this.timer);
    const d = this.db;
    if (d.log.length > LIMITS.log) d.log.splice(0, d.log.length - LIMITS.log);
    if (d.downloads.length > LIMITS.downloads) d.downloads.splice(0, d.downloads.length - LIMITS.downloads);
    const tmp = this.file + '.tmp';
    fs.writeFileSync(tmp, JSON.stringify(d, null, 1));
    if (fs.existsSync(this.file)) fs.copyFileSync(this.file, this.file + '.bak');
    fs.renameSync(tmp, this.file);
  }

  log(kind, text, extra) {
    this.db.log.push(Object.assign({ at: Date.now(), kind, text }, extra || {}));
    this.changed('log');
  }
}

module.exports = { Store, DEFAULT_SETTINGS };

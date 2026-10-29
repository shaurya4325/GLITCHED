// Licence signing (RSA-2048, SHA-256, PKCS#1 v1.5), admin passwords (scrypt) and admin sessions.
'use strict';
const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const b64u = (buf) => Buffer.from(buf).toString('base64url');

/** Loads the licence key pair, making one on the very first run. The private key never leaves data/keys. */
function loadKeys(dir) {
  const privFile = path.join(dir, 'licence_private.pem');
  const pubFile = path.join(dir, 'licence_public.pem');
  if (!fs.existsSync(privFile)) {
    fs.mkdirSync(dir, { recursive: true });
    const { privateKey, publicKey } = crypto.generateKeyPairSync('rsa', { modulusLength: 2048 });
    fs.writeFileSync(privFile, privateKey.export({ type: 'pkcs8', format: 'pem' }), { mode: 0o600 });
    fs.writeFileSync(pubFile, publicKey.export({ type: 'spki', format: 'pem' }));
    console.log('[keys] made a new licence key pair in', dir);
  }
  const privateKey = crypto.createPrivateKey(fs.readFileSync(privFile));
  const publicKey = crypto.createPublicKey(fs.readFileSync(pubFile));
  const jwk = publicKey.export({ format: 'jwk' });
  const fingerprint = crypto.createHash('sha256').update(publicKey.export({ type: 'spki', format: 'der' })).digest('hex').slice(0, 16);
  return { privateKey, publicKey, jwk, fingerprint };
}

/** The game's copy of the public key (Assets/_Glitched/Resources/Licence/licence_key.json). */
function gameKeyJson(keys) {
  return JSON.stringify({ n: keys.jwk.n, e: keys.jwk.e, fp: keys.fingerprint }, null, 1);
}

/** token = base64url(json) + "." + base64url(RSA-SHA256 signature of the first part). */
function signToken(keys, payload) {
  const body = b64u(JSON.stringify(payload));
  return body + '.' + b64u(crypto.sign('sha256', Buffer.from(body, 'ascii'), keys.privateKey));
}

function verifyToken(keys, token) {
  const [body, sig] = String(token || '').split('.');
  if (!body || !sig) return null;
  if (!crypto.verify('sha256', Buffer.from(body, 'ascii'), keys.publicKey, Buffer.from(sig, 'base64url'))) return null;
  try { return JSON.parse(Buffer.from(body, 'base64url').toString('utf8')); } catch { return null; }
}

function hashPassword(password, salt = crypto.randomBytes(16).toString('hex')) {
  const hash = crypto.scryptSync(String(password), salt, 64, { N: 16384, r: 8, p: 1 }).toString('hex');
  return { salt, hash };
}

function checkPassword(admin, password) {
  if (!admin || !admin.hash) return false;
  const { hash } = hashPassword(password, admin.salt);
  return crypto.timingSafeEqual(Buffer.from(hash, 'hex'), Buffer.from(admin.hash, 'hex'));
}

const newId = (bytes = 8) => crypto.randomBytes(bytes).toString('hex');

/** A short code people can read off a screen: no 0/O/1/I/L. */
function pairCode() {
  const abc = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
  let s = '';
  for (const b of crypto.randomBytes(6)) s += abc[b % abc.length];
  return s;
}

module.exports = { loadKeys, gameKeyJson, signToken, verifyToken, hashPassword, checkPassword, newId, pairCode };

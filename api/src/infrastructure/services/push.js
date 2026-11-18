const fs = require('fs');
const path = require('path');
const config = require('../../config/env');

let messaging;

const getMessaging = () => {
  if (messaging !== undefined) return messaging;
  messaging = null;
  const ruta = config.firebaseServiceAccountPath;
  if (ruta && fs.existsSync(path.resolve(ruta))) {
    const admin = require('firebase-admin');
    const credenciales = JSON.parse(fs.readFileSync(path.resolve(ruta), 'utf8'));
    admin.initializeApp({ credential: admin.credential.cert(credenciales) });
    messaging = admin.messaging();
  }
  return messaging;
};

module.exports = {
  async enviar(tokens, { titulo, mensaje, data = {} }) {
    if (!tokens.length) return;
    const m = getMessaging();
    if (!m) {
      console.log(`[push-dev] ${titulo}: ${mensaje} (${tokens.length} dispositivo(s))`);
      return;
    }
    await m.sendEachForMulticast({
      tokens,
      notification: { title: titulo, body: mensaje },
      data: Object.fromEntries(Object.entries(data).map(([k, v]) => [k, String(v)])),
    });
  },
};

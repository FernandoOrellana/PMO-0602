const fs = require('fs');
const path = require('path');
const config = require('../../config/env');

const TOKENS_INVALIDOS = new Set([
  'messaging/registration-token-not-registered',
  'messaging/invalid-registration-token',
  'messaging/invalid-argument',
]);

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
  activo: () => Boolean(getMessaging()),

  async enviar(tokens, { titulo, mensaje, data = {} }) {
    if (!tokens.length) return { enviados: 0, invalidos: [] };
    const m = getMessaging();
    if (!m) {
      console.log(`[push-dev] ${titulo}: ${mensaje} (${tokens.length} dispositivo(s))`);
      return { enviados: 0, invalidos: [] };
    }
    const respuesta = await m.sendEachForMulticast({
      tokens,
      notification: { title: titulo, body: mensaje },
      data: Object.fromEntries(Object.entries(data).map(([k, v]) => [k, String(v)])),
      android: {
        priority: 'high',
        notification: { channelId: 'petcare_recordatorios', color: '#1B7F5C', sound: 'default' },
      },
    });
    const invalidos = respuesta.responses
      .map((r, i) => (!r.success && TOKENS_INVALIDOS.has(r.error?.code) ? tokens[i] : null))
      .filter(Boolean);
    return { enviados: respuesta.successCount, invalidos };
  },
};

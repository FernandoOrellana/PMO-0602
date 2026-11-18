const { query } = require('../database/db');

const mapear = (n) => ({
  id: n.id,
  titulo: n.titulo,
  mensaje: n.mensaje,
  leida: Boolean(n.leida),
  fecha: n.fecha,
});

module.exports = {
  async crear({ usuarioId, titulo, mensaje }) {
    await query('INSERT INTO notificaciones (usuario_id, titulo, mensaje) VALUES (?, ?, ?)', [
      usuarioId,
      titulo,
      mensaje,
    ]);
  },

  async listarPorUsuario(usuarioId) {
    const rows = await query(
      `SELECT id, titulo, mensaje, leida, DATE_FORMAT(created_at, '%Y-%m-%d %H:%i:%s') AS fecha
       FROM notificaciones WHERE usuario_id = ? ORDER BY created_at DESC, id DESC LIMIT 100`,
      [usuarioId]
    );
    return rows.map(mapear);
  },

  async marcarLeida(id, usuarioId) {
    const r = await query('UPDATE notificaciones SET leida = 1 WHERE id = ? AND usuario_id = ?', [
      id,
      usuarioId,
    ]);
    return r.affectedRows > 0;
  },

  async tokensDeUsuario(usuarioId) {
    const rows = await query('SELECT fcm_token AS token FROM dispositivos WHERE usuario_id = ?', [
      usuarioId,
    ]);
    return rows.map((r) => r.token);
  },

  async guardarDispositivo(usuarioId, fcmToken) {
    await query(
      `INSERT INTO dispositivos (usuario_id, fcm_token) VALUES (?, ?)
       ON DUPLICATE KEY UPDATE usuario_id = VALUES(usuario_id)`,
      [usuarioId, fcmToken]
    );
  },
};

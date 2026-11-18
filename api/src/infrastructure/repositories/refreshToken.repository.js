const { query } = require('../database/db');

module.exports = {
  async guardar({ usuarioId, tokenHash, expira }) {
    await query('INSERT INTO refresh_tokens (usuario_id, token_hash, expira) VALUES (?, ?, ?)', [
      usuarioId,
      tokenHash,
      expira,
    ]);
  },

  async buscarPorHash(tokenHash) {
    const [row] = await query(
      `SELECT id, usuario_id AS usuarioId, expira, revocado FROM refresh_tokens WHERE token_hash = ?`,
      [tokenHash]
    );
    return row && { ...row, revocado: Boolean(row.revocado) };
  },

  async revocar(id) {
    await query('UPDATE refresh_tokens SET revocado = 1 WHERE id = ?', [id]);
  },

  async revocarTodosDeUsuario(usuarioId) {
    await query('UPDATE refresh_tokens SET revocado = 1 WHERE usuario_id = ?', [usuarioId]);
  },
};

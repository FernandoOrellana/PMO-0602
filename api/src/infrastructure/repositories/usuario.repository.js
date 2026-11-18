const { query } = require('../database/db');

const SELECT_USUARIO = `
  SELECT u.id, u.nombre, u.email, u.telefono, u.password_hash AS passwordHash,
         u.email_verificado AS emailVerificado, u.codigo_verificacion_hash AS codigoHash,
         u.codigo_expira AS codigoExpira, u.clave_temporal_hash AS claveTempHash,
         u.clave_temporal_expira AS claveTempExpira, u.activo, r.nombre AS rol
  FROM usuarios u
  JOIN roles r ON r.id = u.rol_id`;

const mapear = (row) =>
  row && {
    ...row,
    emailVerificado: Boolean(row.emailVerificado),
    activo: Boolean(row.activo),
  };

module.exports = {
  async buscarPorEmail(email) {
    const [row] = await query(`${SELECT_USUARIO} WHERE u.email = ?`, [email]);
    return mapear(row);
  },

  async buscarPorId(id) {
    const [row] = await query(`${SELECT_USUARIO} WHERE u.id = ?`, [id]);
    return mapear(row);
  },

  async crear({ nombre, email, telefono, passwordHash, rol, emailVerificado }) {
    const r = await query(
      `INSERT INTO usuarios (rol_id, nombre, email, telefono, password_hash, email_verificado)
       VALUES ((SELECT id FROM roles WHERE nombre = ?), ?, ?, ?, ?, ?)`,
      [rol, nombre, email, telefono ?? null, passwordHash, emailVerificado ? 1 : 0]
    );
    return r.insertId;
  },

  async guardarCodigoVerificacion(id, codigoHash, expira) {
    await query(
      'UPDATE usuarios SET codigo_verificacion_hash = ?, codigo_expira = ? WHERE id = ?',
      [codigoHash, expira, id]
    );
  },

  async marcarVerificado(id) {
    await query(
      `UPDATE usuarios SET email_verificado = 1, codigo_verificacion_hash = NULL,
       codigo_expira = NULL WHERE id = ?`,
      [id]
    );
  },

  async guardarClaveTemporal(id, claveHash, expira) {
    await query(
      'UPDATE usuarios SET clave_temporal_hash = ?, clave_temporal_expira = ? WHERE id = ?',
      [claveHash, expira, id]
    );
  },

  async actualizarPassword(id, passwordHash) {
    await query(
      `UPDATE usuarios SET password_hash = ?, clave_temporal_hash = NULL,
       clave_temporal_expira = NULL WHERE id = ?`,
      [passwordHash, id]
    );
  },

  async actualizarDatos(id, { nombre, telefono }) {
    await query('UPDATE usuarios SET nombre = ?, telefono = ? WHERE id = ?', [
      nombre,
      telefono ?? null,
      id,
    ]);
  },
};

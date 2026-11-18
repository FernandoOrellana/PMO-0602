const { query } = require('../database/db');

const SELECT_SERVICIO = `
  SELECT id, nombre, precio, duracion_min AS duracionMin, activo FROM servicios`;

module.exports = {
  async especies() {
    return query('SELECT id, nombre FROM especies ORDER BY nombre');
  },

  async especiePorId(id) {
    const [row] = await query('SELECT id, nombre FROM especies WHERE id = ?', [id]);
    return row || null;
  },

  async vacunas() {
    return query('SELECT id, nombre, intervalo_dias AS intervaloDias FROM vacunas ORDER BY nombre');
  },

  async vacunaPorId(id) {
    const [row] = await query(
      'SELECT id, nombre, intervalo_dias AS intervaloDias FROM vacunas WHERE id = ?',
      [id]
    );
    return row || null;
  },

  async servicios({ soloActivos = true } = {}) {
    const rows = await query(
      `${SELECT_SERVICIO} ${soloActivos ? 'WHERE activo = 1' : ''} ORDER BY nombre`
    );
    return rows.map((r) => ({ ...r, activo: Boolean(r.activo) }));
  },

  async servicioPorId(id) {
    const [row] = await query(`${SELECT_SERVICIO} WHERE id = ?`, [id]);
    return row ? { ...row, activo: Boolean(row.activo) } : null;
  },

  async crearServicio({ nombre, precio, duracionMin }) {
    const r = await query('INSERT INTO servicios (nombre, precio, duracion_min) VALUES (?, ?, ?)', [
      nombre,
      precio,
      duracionMin,
    ]);
    return r.insertId;
  },

  async actualizarServicio(id, { nombre, precio, duracionMin }) {
    await query('UPDATE servicios SET nombre = ?, precio = ?, duracion_min = ? WHERE id = ?', [
      nombre,
      precio,
      duracionMin,
      id,
    ]);
  },

  async desactivarServicio(id) {
    await query('UPDATE servicios SET activo = 0 WHERE id = ?', [id]);
  },
};

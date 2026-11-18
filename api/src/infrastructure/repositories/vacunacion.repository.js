const { query } = require('../database/db');

const SELECT_APLICACION = `
  SELECT a.id, a.mascota_id AS mascotaId,
         v.id AS vacunaId, v.nombre AS vacunaNombre,
         DATE_FORMAT(a.fecha_aplicacion, '%Y-%m-%d') AS fechaAplicacion,
         DATE_FORMAT(a.proxima_dosis, '%Y-%m-%d') AS proximaDosis,
         a.lote, u.id AS vetId, u.nombre AS vetNombre
  FROM aplicaciones_vacuna a
  JOIN vacunas v ON v.id = a.vacuna_id
  JOIN usuarios u ON u.id = a.veterinario_id`;

const mapear = (r) =>
  r && {
    id: r.id,
    vacuna: { id: r.vacunaId, nombre: r.vacunaNombre },
    fechaAplicacion: r.fechaAplicacion,
    proximaDosis: r.proximaDosis ?? null,
    lote: r.lote ?? null,
    veterinario: { id: r.vetId, nombre: r.vetNombre },
  };

module.exports = {
  async crearAplicacion({ mascotaId, vacunaId, veterinarioId, citaId, fechaAplicacion, proximaDosis, lote }) {
    const r = await query(
      `INSERT INTO aplicaciones_vacuna
         (mascota_id, vacuna_id, veterinario_id, cita_id, fecha_aplicacion, proxima_dosis, lote)
       VALUES (?, ?, ?, ?, ?, ?, ?)`,
      [mascotaId, vacunaId, veterinarioId, citaId ?? null, fechaAplicacion, proximaDosis ?? null, lote ?? null]
    );
    return r.insertId;
  },

  async buscarAplicacionPorId(id) {
    const [row] = await query(`${SELECT_APLICACION} WHERE a.id = ?`, [id]);
    return mapear(row);
  },

  async listarPorMascota(mascotaId) {
    const rows = await query(
      `${SELECT_APLICACION} WHERE a.mascota_id = ? ORDER BY a.fecha_aplicacion DESC`,
      [mascotaId]
    );
    return rows.map(mapear);
  },

  async pendientesDeRecordatorio(desde, hasta) {
    const rows = await query(
      `SELECT a.id, m.nombre AS mascotaNombre, m.dueno_id AS duenoId, v.nombre AS vacunaNombre,
              DATE_FORMAT(a.proxima_dosis, '%Y-%m-%d') AS proximaDosis
       FROM aplicaciones_vacuna a
       JOIN mascotas m ON m.id = a.mascota_id AND m.activo = 1
       JOIN vacunas v ON v.id = a.vacuna_id
       WHERE a.recordatorio_enviado = 0 AND a.proxima_dosis BETWEEN ? AND ?`,
      [desde, hasta]
    );
    return rows;
  },

  async marcarRecordatorio(id) {
    await query('UPDATE aplicaciones_vacuna SET recordatorio_enviado = 1 WHERE id = ?', [id]);
  },
};

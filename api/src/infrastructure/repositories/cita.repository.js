const { query, transaccion } = require('../database/db');
const { ConflictError } = require('../../shared/errors');
const { paginar } = require('../../shared/mappers');

const SELECT_CITA = `
  SELECT c.id, DATE_FORMAT(c.fecha, '%Y-%m-%d') AS fecha, TIME_FORMAT(c.hora, '%H:%i') AS hora, c.estado,
         m.id AS mascotaId, m.nombre AS mascotaNombre, m.dueno_id AS duenoId,
         s.id AS servicioId, s.nombre AS servicioNombre, s.precio AS servicioPrecio,
         s.duracion_min AS duracionMin,
         v.id AS vetId, v.nombre AS vetNombre,
         d.nombre AS duenoNombre, d.telefono AS duenoTelefono
  FROM citas c
  JOIN mascotas m ON m.id = c.mascota_id
  JOIN servicios s ON s.id = c.servicio_id
  JOIN usuarios v ON v.id = c.veterinario_id
  JOIN usuarios d ON d.id = m.dueno_id`;

const mapear = (r) =>
  r && {
    id: r.id,
    fecha: r.fecha,
    hora: r.hora,
    estado: r.estado,
    mascota: { id: r.mascotaId, nombre: r.mascotaNombre },
    servicio: { id: r.servicioId, nombre: r.servicioNombre, precio: r.servicioPrecio },
    veterinario: { id: r.vetId, nombre: r.vetNombre },
    dueno: { id: r.duenoId, nombre: r.duenoNombre, telefono: r.duenoTelefono ?? null },
  };

const SELECT_CONSULTA = `
  SELECT co.id, co.cita_id AS citaId, DATE_FORMAT(co.fecha, '%Y-%m-%d') AS fecha,
         co.diagnostico, co.tratamiento, co.observaciones, co.peso_kg AS peso,
         u.id AS vetId, u.nombre AS vetNombre
  FROM consultas co
  JOIN usuarios u ON u.id = co.veterinario_id`;

const mapearConsulta = (r) =>
  r && {
    id: r.id,
    citaId: r.citaId,
    fecha: r.fecha,
    diagnostico: r.diagnostico,
    tratamiento: r.tratamiento ?? null,
    observaciones: r.observaciones ?? null,
    peso: r.peso ?? null,
    veterinario: { id: r.vetId, nombre: r.vetNombre },
  };

module.exports = {
  async crear({ mascotaId, servicioId, veterinarioId, fecha, hora }) {
    try {
      const r = await query(
        `INSERT INTO citas (mascota_id, servicio_id, veterinario_id, fecha, hora)
         VALUES (?, ?, ?, ?, ?)`,
        [mascotaId, servicioId, veterinarioId, fecha, hora]
      );
      return r.insertId;
    } catch (err) {
      if (err.code === 'ER_DUP_ENTRY') {
        throw new ConflictError('La hora seleccionada ya no está disponible');
      }
      throw err;
    }
  },

  async buscarPorId(id) {
    const [row] = await query(`${SELECT_CITA} WHERE c.id = ?`, [id]);
    return mapear(row);
  },

  async listar({ duenoId, veterinarioId, mascotaId, fecha, estado, page, limit }) {
    const where = [];
    const params = [];
    if (duenoId) { where.push('m.dueno_id = ?'); params.push(duenoId); }
    if (veterinarioId) { where.push('c.veterinario_id = ?'); params.push(veterinarioId); }
    if (mascotaId) { where.push('c.mascota_id = ?'); params.push(mascotaId); }
    if (fecha) { where.push('c.fecha = ?'); params.push(fecha); }
    if (estado) { where.push('c.estado = ?'); params.push(estado); }
    const whereSql = where.length ? `WHERE ${where.join(' AND ')}` : '';
    const { offset } = paginar({ page, limit });

    const rows = await query(
      `${SELECT_CITA} ${whereSql} ORDER BY c.fecha ASC, c.hora ASC LIMIT ? OFFSET ?`,
      [...params, limit, offset]
    );
    const [{ total }] = await query(
      `SELECT COUNT(*) AS total FROM citas c JOIN mascotas m ON m.id = c.mascota_id ${whereSql}`,
      params
    );
    return { items: rows.map(mapear), total: Number(total) };
  },

  async activasPorVeterinarioYFecha(veterinarioId, fecha) {
    return query(
      `SELECT TIME_FORMAT(c.hora, '%H:%i') AS hora, s.duracion_min AS duracionMin
       FROM citas c JOIN servicios s ON s.id = c.servicio_id
       WHERE c.veterinario_id = ? AND c.fecha = ? AND c.estado IN ('PENDIENTE', 'CONFIRMADA', 'ATENDIDA')`,
      [veterinarioId, fecha]
    );
  },

  async actualizarEstado(id, estado) {
    await query('UPDATE citas SET estado = ? WHERE id = ?', [estado, id]);
  },

  async registrarConsulta({ citaId, mascotaId, veterinarioId, fecha, diagnostico, tratamiento, observaciones, peso }) {
    const id = await transaccion(async (tx) => {
      const r = await tx.query(
        `INSERT INTO consultas (cita_id, mascota_id, veterinario_id, fecha, diagnostico, tratamiento, observaciones, peso_kg)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
        [citaId, mascotaId, veterinarioId, fecha, diagnostico, tratamiento ?? null, observaciones ?? null, peso ?? null]
      );
      await tx.query("UPDATE citas SET estado = 'ATENDIDA' WHERE id = ?", [citaId]);
      return r.insertId;
    });
    const [row] = await query(`${SELECT_CONSULTA} WHERE co.id = ?`, [id]);
    return mapearConsulta(row);
  },

  async consultasPorMascota(mascotaId) {
    const rows = await query(`${SELECT_CONSULTA} WHERE co.mascota_id = ? ORDER BY co.fecha DESC`, [
      mascotaId,
    ]);
    return rows.map(mapearConsulta);
  },

  async pendientesDeRecordatorio(fecha) {
    const rows = await query(
      `${SELECT_CITA} WHERE c.fecha = ? AND c.estado IN ('PENDIENTE', 'CONFIRMADA')
         AND c.recordatorio_enviado = 0`,
      [fecha]
    );
    return rows.map(mapear);
  },

  async marcarRecordatorio(id) {
    await query('UPDATE citas SET recordatorio_enviado = 1 WHERE id = ?', [id]);
  },
};

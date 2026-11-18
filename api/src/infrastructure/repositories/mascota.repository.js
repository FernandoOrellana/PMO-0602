const { query } = require('../database/db');
const { urlPublica, paginar } = require('../../shared/mappers');

const SELECT_MASCOTA = `
  SELECT m.id, m.dueno_id AS duenoId, m.nombre, m.raza,
         DATE_FORMAT(m.fecha_nacimiento, '%Y-%m-%d') AS fechaNacimiento,
         m.peso_kg AS pesoKg, m.foto_ruta AS fotoRuta, m.activo,
         e.id AS especieId, e.nombre AS especieNombre
  FROM mascotas m
  JOIN especies e ON e.id = m.especie_id`;

const mapear = (row) =>
  row && {
    id: row.id,
    duenoId: row.duenoId,
    nombre: row.nombre,
    especie: { id: row.especieId, nombre: row.especieNombre },
    raza: row.raza ?? null,
    fechaNacimiento: row.fechaNacimiento ?? null,
    pesoKg: row.pesoKg ?? null,
    fotoUrl: urlPublica(row.fotoRuta),
    activo: Boolean(row.activo),
  };

module.exports = {
  async listar({ duenoId, page, limit }) {
    const filtro = duenoId ? 'WHERE m.dueno_id = ? AND m.activo = 1' : 'WHERE m.activo = 1';
    const params = duenoId ? [duenoId] : [];
    const { offset } = paginar({ page, limit });
    const rows = await query(
      `${SELECT_MASCOTA} ${filtro} ORDER BY m.nombre LIMIT ? OFFSET ?`,
      [...params, limit, offset]
    );
    const [{ total }] = await query(`SELECT COUNT(*) AS total FROM mascotas m ${filtro}`, params);
    return { items: rows.map(mapear), total: Number(total) };
  },

  async buscarPorId(id) {
    const [row] = await query(`${SELECT_MASCOTA} WHERE m.id = ?`, [id]);
    return mapear(row);
  },

  async rutaFoto(id) {
    const [row] = await query('SELECT foto_ruta AS fotoRuta FROM mascotas WHERE id = ?', [id]);
    return row?.fotoRuta ?? null;
  },

  async crear({ duenoId, nombre, especieId, raza, fechaNacimiento, pesoKg }) {
    const r = await query(
      `INSERT INTO mascotas (dueno_id, especie_id, nombre, raza, fecha_nacimiento, peso_kg)
       VALUES (?, ?, ?, ?, ?, ?)`,
      [duenoId, especieId, nombre, raza ?? null, fechaNacimiento ?? null, pesoKg ?? null]
    );
    return r.insertId;
  },

  async actualizar(id, { nombre, especieId, raza, fechaNacimiento, pesoKg }) {
    await query(
      `UPDATE mascotas SET nombre = ?, especie_id = ?, raza = ?, fecha_nacimiento = ?, peso_kg = ?
       WHERE id = ?`,
      [nombre, especieId, raza ?? null, fechaNacimiento ?? null, pesoKg ?? null, id]
    );
  },

  async desactivar(id) {
    await query('UPDATE mascotas SET activo = 0 WHERE id = ?', [id]);
  },

  async actualizarFoto(id, ruta) {
    await query('UPDATE mascotas SET foto_ruta = ? WHERE id = ?', [ruta, id]);
  },
};

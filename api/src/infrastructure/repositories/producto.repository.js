const { query } = require('../database/db');
const { urlPublica, paginar } = require('../../shared/mappers');

const SELECT_PRODUCTO = `
  SELECT id, nombre, descripcion, categoria, precio, stock, imagen_ruta AS imagenRuta, activo
  FROM productos`;

const mapear = (r) =>
  r && {
    id: r.id,
    nombre: r.nombre,
    descripcion: r.descripcion ?? null,
    categoria: r.categoria,
    precio: r.precio,
    stock: r.stock,
    imagenUrl: urlPublica(r.imagenRuta),
    activo: Boolean(r.activo),
  };

module.exports = {
  async listar({ categoria, page, limit, soloActivos = true }) {
    const where = [];
    const params = [];
    if (soloActivos) where.push('activo = 1');
    if (categoria) { where.push('categoria = ?'); params.push(categoria); }
    const whereSql = where.length ? `WHERE ${where.join(' AND ')}` : '';
    const { offset } = paginar({ page, limit });
    const rows = await query(`${SELECT_PRODUCTO} ${whereSql} ORDER BY nombre LIMIT ? OFFSET ?`, [
      ...params,
      limit,
      offset,
    ]);
    const [{ total }] = await query(`SELECT COUNT(*) AS total FROM productos ${whereSql}`, params);
    return { items: rows.map(mapear), total: Number(total) };
  },

  async buscarPorId(id) {
    const [row] = await query(`${SELECT_PRODUCTO} WHERE id = ?`, [id]);
    return mapear(row);
  },

  async buscarActivosPorIds(ids) {
    if (!ids.length) return [];
    const rows = await query(`${SELECT_PRODUCTO} WHERE activo = 1 AND id IN (?)`, [ids]);
    return rows.map(mapear);
  },

  async rutaImagen(id) {
    const [row] = await query('SELECT imagen_ruta AS imagenRuta FROM productos WHERE id = ?', [id]);
    return row?.imagenRuta ?? null;
  },

  async crear({ nombre, descripcion, categoria, precio, stock }) {
    const r = await query(
      'INSERT INTO productos (nombre, descripcion, categoria, precio, stock) VALUES (?, ?, ?, ?, ?)',
      [nombre, descripcion ?? null, categoria, precio, stock]
    );
    return r.insertId;
  },

  async actualizar(id, { nombre, descripcion, categoria, precio, stock }) {
    await query(
      'UPDATE productos SET nombre = ?, descripcion = ?, categoria = ?, precio = ?, stock = ? WHERE id = ?',
      [nombre, descripcion ?? null, categoria, precio, stock, id]
    );
  },

  async desactivar(id) {
    await query('UPDATE productos SET activo = 0 WHERE id = ?', [id]);
  },

  async actualizarImagen(id, ruta) {
    await query('UPDATE productos SET imagen_ruta = ? WHERE id = ?', [ruta, id]);
  },
};

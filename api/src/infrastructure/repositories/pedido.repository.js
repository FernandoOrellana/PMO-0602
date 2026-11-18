const { query, transaccion } = require('../database/db');
const { ConflictError } = require('../../shared/errors');
const { paginar } = require('../../shared/mappers');
const { ESTADO_PEDIDO } = require('../../domain/constants');

const SELECT_PEDIDO = `
  SELECT p.id, DATE_FORMAT(p.fecha, '%Y-%m-%d %H:%i:%s') AS fecha, p.estado, p.total,
         u.id AS clienteId, u.nombre AS clienteNombre
  FROM pedidos p
  JOIN usuarios u ON u.id = p.cliente_id`;

const itemsDe = async (pedidoIds) => {
  if (!pedidoIds.length) return [];
  return query(
    `SELECT pedido_id AS pedidoId, producto_id AS productoId, nombre_producto AS nombre,
            precio_unitario AS precioUnitario, cantidad, subtotal
     FROM pedido_items WHERE pedido_id IN (?) ORDER BY id`,
    [pedidoIds]
  );
};

const mapear = (p, items = []) => ({
  id: p.id,
  fecha: p.fecha,
  estado: p.estado,
  total: p.total,
  items: items.map(({ productoId, nombre, precioUnitario, cantidad, subtotal }) => ({
    productoId,
    nombre,
    precioUnitario,
    cantidad,
    subtotal,
  })),
  cliente: { id: p.clienteId, nombre: p.clienteNombre },
});

const conItems = async (rows) => {
  const items = await itemsDe(rows.map((r) => r.id));
  return rows.map((p) => mapear(p, items.filter((i) => i.pedidoId === p.id)));
};

module.exports = {
  async crear({ clienteId, total, items }) {
    return transaccion(async (tx) => {
      const pedido = await tx.query('INSERT INTO pedidos (cliente_id, total) VALUES (?, ?)', [
        clienteId,
        total,
      ]);
      for (const item of items) {
        const descuento = await tx.query(
          'UPDATE productos SET stock = stock - ? WHERE id = ? AND stock >= ? AND activo = 1',
          [item.cantidad, item.productoId, item.cantidad]
        );
        if (descuento.affectedRows === 0) {
          throw new ConflictError(`Stock insuficiente para ${item.nombre}`);
        }
        await tx.query(
          `INSERT INTO pedido_items (pedido_id, producto_id, nombre_producto, precio_unitario, cantidad, subtotal)
           VALUES (?, ?, ?, ?, ?, ?)`,
          [pedido.insertId, item.productoId, item.nombre, item.precioUnitario, item.cantidad, item.subtotal]
        );
      }
      return pedido.insertId;
    });
  },

  async buscarPorId(id) {
    const [row] = await query(`${SELECT_PEDIDO} WHERE p.id = ?`, [id]);
    if (!row) return null;
    return (await conItems([row]))[0];
  },

  async listar({ clienteId, page, limit }) {
    const filtro = clienteId ? 'WHERE p.cliente_id = ?' : '';
    const params = clienteId ? [clienteId] : [];
    const { offset } = paginar({ page, limit });
    const rows = await query(`${SELECT_PEDIDO} ${filtro} ORDER BY p.fecha DESC, p.id DESC LIMIT ? OFFSET ?`, [
      ...params,
      limit,
      offset,
    ]);
    const [{ total }] = await query(`SELECT COUNT(*) AS total FROM pedidos p ${filtro}`, params);
    return { items: await conItems(rows), total: Number(total) };
  },

  async actualizarEstado(id, nuevoEstado) {
    await transaccion(async (tx) => {
      const [actual] = await tx.query('SELECT estado FROM pedidos WHERE id = ? FOR UPDATE', [id]);
      await tx.query('UPDATE pedidos SET estado = ? WHERE id = ?', [nuevoEstado, id]);
      if (nuevoEstado === ESTADO_PEDIDO.CANCELADO && actual.estado !== ESTADO_PEDIDO.CANCELADO) {
        const items = await tx.query(
          'SELECT producto_id AS productoId, cantidad FROM pedido_items WHERE pedido_id = ?',
          [id]
        );
        for (const item of items) {
          await tx.query('UPDATE productos SET stock = stock + ? WHERE id = ?', [
            item.cantidad,
            item.productoId,
          ]);
        }
      }
    });
  },
};

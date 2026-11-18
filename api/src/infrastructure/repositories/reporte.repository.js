const { query } = require('../database/db');

module.exports = {
  async ventas(desde, hasta) {
    const [resumen] = await query(
      `SELECT COUNT(*) AS totalPedidos, COALESCE(SUM(total), 0) AS totalVentas
       FROM pedidos WHERE estado <> 'CANCELADO' AND DATE(fecha) BETWEEN ? AND ?`,
      [desde, hasta]
    );
    const productos = await query(
      `SELECT pi.producto_id AS productoId, pi.nombre_producto AS nombre,
              SUM(pi.cantidad) AS cantidad, SUM(pi.subtotal) AS total
       FROM pedido_items pi JOIN pedidos p ON p.id = pi.pedido_id
       WHERE p.estado <> 'CANCELADO' AND DATE(p.fecha) BETWEEN ? AND ?
       GROUP BY pi.producto_id, pi.nombre_producto
       ORDER BY total DESC`,
      [desde, hasta]
    );
    return {
      totalPedidos: Number(resumen.totalPedidos),
      totalVentas: Number(resumen.totalVentas),
      productos: productos.map((p) => ({
        productoId: p.productoId,
        nombre: p.nombre,
        cantidad: Number(p.cantidad),
        total: Number(p.total),
      })),
    };
  },

  async citas(desde, hasta) {
    const porEstado = await query(
      `SELECT estado, COUNT(*) AS cantidad FROM citas WHERE fecha BETWEEN ? AND ? GROUP BY estado`,
      [desde, hasta]
    );
    const porServicio = await query(
      `SELECT s.id AS servicioId, s.nombre, COUNT(c.id) AS cantidad
       FROM servicios s
       LEFT JOIN citas c ON c.servicio_id = s.id AND c.fecha BETWEEN ? AND ?
       GROUP BY s.id, s.nombre ORDER BY cantidad DESC, s.nombre`,
      [desde, hasta]
    );
    const estados = { PENDIENTE: 0, CONFIRMADA: 0, ATENDIDA: 0, CANCELADA: 0 };
    for (const fila of porEstado) estados[fila.estado] = Number(fila.cantidad);
    const total = Object.values(estados).reduce((a, b) => a + b, 0);
    return {
      total,
      porEstado: estados,
      porServicio: porServicio.map((s) => ({
        servicioId: s.servicioId,
        nombre: s.nombre,
        cantidad: Number(s.cantidad),
      })),
    };
  },
};

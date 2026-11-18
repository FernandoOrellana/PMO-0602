const { NotFoundError, ForbiddenError, ValidationError } = require('../../shared/errors');
const { ROLES } = require('../../domain/constants');
const { calcularTotal, transicionPedidoValida, redondear } = require('../../domain/rules');
const { meta } = require('../../shared/mappers');

module.exports = function createTiendaUseCases({ productos, pedidos }) {
  return {
    async listarProductos({ categoria, page, limit }) {
      const { items, total } = await productos.listar({ categoria, page, limit });
      return { items, meta: meta({ page, limit, total }) };
    },

    async obtenerProducto(id) {
      const producto = await productos.buscarPorId(id);
      if (!producto || !producto.activo) throw new NotFoundError('Producto no encontrado');
      return producto;
    },

    async crearPedido(actor, { items }) {
      const cantidades = new Map();
      for (const item of items) {
        cantidades.set(item.productoId, (cantidades.get(item.productoId) || 0) + item.cantidad);
      }

      const encontrados = await productos.buscarActivosPorIds([...cantidades.keys()]);
      const porId = new Map(encontrados.map((p) => [p.id, p]));

      const lineas = [...cantidades].map(([productoId, cantidad]) => {
        const producto = porId.get(productoId);
        if (!producto) throw new ValidationError(`El producto ${productoId} no está disponible`);
        if (producto.stock < cantidad) {
          throw new ValidationError(`Stock insuficiente para ${producto.nombre}`, [
            { campo: 'items', mensaje: `Disponibles: ${producto.stock}` },
          ]);
        }
        return {
          productoId,
          nombre: producto.nombre,
          precioUnitario: producto.precio,
          cantidad,
          subtotal: redondear(producto.precio * cantidad),
        };
      });

      const id = await pedidos.crear({ clienteId: actor.id, items: lineas, total: calcularTotal(lineas) });
      return pedidos.buscarPorId(id);
    },

    async listarPedidos(actor, { page, limit }) {
      const clienteId = actor.rol === ROLES.DUENO ? actor.id : undefined;
      const { items, total } = await pedidos.listar({ clienteId, page, limit });
      return { items, meta: meta({ page, limit, total }) };
    },

    async obtenerPedido(actor, id) {
      const pedido = await pedidos.buscarPorId(id);
      if (!pedido) throw new NotFoundError('Pedido no encontrado');
      if (actor.rol === ROLES.DUENO && pedido.cliente.id !== actor.id) {
        throw new ForbiddenError('Este pedido no pertenece a tu cuenta');
      }
      return pedido;
    },

    async cambiarEstado(id, nuevoEstado) {
      const pedido = await pedidos.buscarPorId(id);
      if (!pedido) throw new NotFoundError('Pedido no encontrado');
      if (!transicionPedidoValida(pedido.estado, nuevoEstado)) {
        throw new ValidationError(`No se puede cambiar el pedido de ${pedido.estado} a ${nuevoEstado}`);
      }
      await pedidos.actualizarEstado(id, nuevoEstado);
      return pedidos.buscarPorId(id);
    },
  };
};

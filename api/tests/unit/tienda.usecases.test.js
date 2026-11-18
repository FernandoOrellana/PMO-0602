const createTiendaUseCases = require('../../src/application/usecases/tienda.usecases');
const { ValidationError, ForbiddenError, NotFoundError } = require('../../src/shared/errors');
const { ROLES, ESTADO_PEDIDO } = require('../../src/domain/constants');

const crearDependencias = () => {
  const productos = {
    buscarActivosPorIds: jest.fn(),
    buscarPorId: jest.fn(),
    listar: jest.fn(),
  };
  const pedidos = {
    crear: jest.fn().mockResolvedValue(900),
    buscarPorId: jest.fn(),
    listar: jest.fn(),
    actualizarEstado: jest.fn(),
  };
  return { productos, pedidos, useCases: createTiendaUseCases({ productos, pedidos }) };
};

const collar = { id: 1, nombre: 'Collar', precio: 150.5, stock: 5, activo: true };
const alimento = { id: 2, nombre: 'Alimento', precio: 1250, stock: 2, activo: true };

describe('casos de uso de tienda', () => {
  test('crearPedido calcula el total en el servidor a partir de los precios actuales', async () => {
    const { productos, pedidos, useCases } = crearDependencias();
    productos.buscarActivosPorIds.mockResolvedValue([collar, alimento]);

    await useCases.crearPedido({ id: 5, rol: ROLES.DUENO }, {
      items: [
        { productoId: 1, cantidad: 2 },
        { productoId: 2, cantidad: 1 },
      ],
    });

    expect(pedidos.crear).toHaveBeenCalledWith({
      clienteId: 5,
      total: 1551,
      items: [
        expect.objectContaining({ productoId: 1, cantidad: 2, subtotal: 301 }),
        expect.objectContaining({ productoId: 2, cantidad: 1, subtotal: 1250 }),
      ],
    });
  });

  test('crearPedido combina líneas repetidas del mismo producto', async () => {
    const { productos, pedidos, useCases } = crearDependencias();
    productos.buscarActivosPorIds.mockResolvedValue([collar]);

    await useCases.crearPedido({ id: 5, rol: ROLES.DUENO }, {
      items: [
        { productoId: 1, cantidad: 1 },
        { productoId: 1, cantidad: 2 },
      ],
    });

    expect(pedidos.crear.mock.calls[0][0].items).toHaveLength(1);
    expect(pedidos.crear.mock.calls[0][0].items[0].cantidad).toBe(3);
  });

  test('crearPedido rechaza un pedido que supera el stock disponible', async () => {
    const { productos, pedidos, useCases } = crearDependencias();
    productos.buscarActivosPorIds.mockResolvedValue([alimento]);

    await expect(
      useCases.crearPedido({ id: 5, rol: ROLES.DUENO }, { items: [{ productoId: 2, cantidad: 3 }] })
    ).rejects.toBeInstanceOf(ValidationError);
    expect(pedidos.crear).not.toHaveBeenCalled();
  });

  test('crearPedido rechaza productos inactivos o inexistentes', async () => {
    const { productos, useCases } = crearDependencias();
    productos.buscarActivosPorIds.mockResolvedValue([]);

    await expect(
      useCases.crearPedido({ id: 5, rol: ROLES.DUENO }, { items: [{ productoId: 99, cantidad: 1 }] })
    ).rejects.toBeInstanceOf(ValidationError);
  });

  test('obtenerPedido impide que un dueño vea el pedido de otro cliente', async () => {
    const { pedidos, useCases } = crearDependencias();
    pedidos.buscarPorId.mockResolvedValue({ id: 900, cliente: { id: 7 }, estado: ESTADO_PEDIDO.PENDIENTE });

    await expect(useCases.obtenerPedido({ id: 5, rol: ROLES.DUENO }, 900)).rejects.toBeInstanceOf(
      ForbiddenError
    );
  });

  test('cambiarEstado respeta el flujo de estados del pedido', async () => {
    const { pedidos, useCases } = crearDependencias();
    pedidos.buscarPorId.mockResolvedValue({ id: 900, estado: ESTADO_PEDIDO.ENTREGADO });

    await expect(useCases.cambiarEstado(900, ESTADO_PEDIDO.CANCELADO)).rejects.toBeInstanceOf(ValidationError);
    expect(pedidos.actualizarEstado).not.toHaveBeenCalled();
  });

  test('cambiarEstado devuelve NotFound si el pedido no existe', async () => {
    const { pedidos, useCases } = crearDependencias();
    pedidos.buscarPorId.mockResolvedValue(null);

    await expect(useCases.cambiarEstado(1, ESTADO_PEDIDO.ENTREGADO)).rejects.toBeInstanceOf(NotFoundError);
  });
});

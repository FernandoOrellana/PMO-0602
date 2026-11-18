const {
  createNotificacionesUseCases,
  crearNotificador,
} = require('../../src/application/usecases/notificaciones.usecases');

const crearRepositorio = (tokens = ['token-a']) => ({
  crear: jest.fn(),
  tokensDeUsuario: jest.fn().mockResolvedValue(tokens),
  eliminarDispositivos: jest.fn(),
  listarPorUsuario: jest.fn(),
  marcarLeida: jest.fn(),
  guardarDispositivo: jest.fn(),
});

describe('notificaciones push', () => {
  test('la notificación de prueba se guarda y se envía a los dispositivos del usuario', async () => {
    const notificaciones = crearRepositorio(['token-a', 'token-b']);
    const push = { enviar: jest.fn().mockResolvedValue({ enviados: 2, invalidos: [] }) };
    const casos = createNotificacionesUseCases({ notificaciones, push });

    const resultado = await casos.probar(7);

    expect(notificaciones.crear).toHaveBeenCalledWith(expect.objectContaining({ usuarioId: 7 }));
    expect(push.enviar).toHaveBeenCalledWith(['token-a', 'token-b'], expect.objectContaining({ data: { tipo: 'PRUEBA' } }));
    expect(resultado).toEqual(expect.objectContaining({ dispositivos: 2, enviados: 2 }));
  });

  test('elimina los tokens que Firebase reporta como inválidos', async () => {
    const notificaciones = crearRepositorio(['token-valido', 'token-viejo']);
    const push = { enviar: jest.fn().mockResolvedValue({ enviados: 1, invalidos: ['token-viejo'] }) };
    const notificar = crearNotificador({ notificaciones, push });

    const resultado = await notificar(3, 'Título', 'Mensaje');

    expect(notificaciones.eliminarDispositivos).toHaveBeenCalledWith(['token-viejo']);
    expect(resultado).toEqual({ dispositivos: 1, enviados: 1 });
  });

  test('sin dispositivos registrados la notificación queda guardada y avisa al usuario', async () => {
    const notificaciones = crearRepositorio([]);
    const push = { enviar: jest.fn().mockResolvedValue({ enviados: 0, invalidos: [] }) };
    const casos = createNotificacionesUseCases({ notificaciones, push });

    const resultado = await casos.probar(9);

    expect(notificaciones.crear).toHaveBeenCalled();
    expect(notificaciones.eliminarDispositivos).not.toHaveBeenCalled();
    expect(resultado.dispositivos).toBe(0);
    expect(resultado.message).toMatch(/No hay dispositivos/);
  });
});

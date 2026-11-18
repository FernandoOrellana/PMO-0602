const { createRecordatoriosUseCases } = require('../../src/application/usecases/notificaciones.usecases');

describe('recordatorios automáticos', () => {
  const crear = () => {
    const vacunacion = {
      pendientesDeRecordatorio: jest.fn().mockResolvedValue([
        { id: 1, duenoId: 5, mascotaNombre: 'Firulais', vacunaNombre: 'Antirrábica', proximaDosis: '2030-01-05' },
      ]),
      marcarRecordatorio: jest.fn(),
    };
    const citas = {
      pendientesDeRecordatorio: jest.fn().mockResolvedValue([
        { id: 9, fecha: '2030-01-02', hora: '09:00', dueno: { id: 5 }, mascota: { nombre: 'Firulais' }, servicio: { nombre: 'Consulta general' } },
      ]),
      marcarRecordatorio: jest.fn(),
    };
    const notificaciones = {
      crear: jest.fn(),
      tokensDeUsuario: jest.fn().mockResolvedValue(['token-fcm']),
    };
    const push = { enviar: jest.fn().mockResolvedValue() };
    const ejecutar = createRecordatoriosUseCases({
      vacunacion,
      citas,
      notificaciones,
      push,
      config: { reminders: { days: 7 } },
    });
    return { ejecutar, vacunacion, citas, notificaciones, push };
  };

  test('notifica vacunas próximas y citas de mañana, y marca cada aviso como enviado', async () => {
    const { ejecutar, vacunacion, citas, notificaciones, push } = crear();

    const resultado = await ejecutar(new Date('2030-01-01T08:00:00'));

    expect(resultado).toEqual({ vacunas: 1, citas: 1 });
    expect(notificaciones.crear).toHaveBeenCalledTimes(2);
    expect(push.enviar).toHaveBeenCalledWith(['token-fcm'], expect.objectContaining({ titulo: expect.any(String) }));
    expect(vacunacion.marcarRecordatorio).toHaveBeenCalledWith(1);
    expect(citas.marcarRecordatorio).toHaveBeenCalledWith(9);
  });

  test('consulta vacunas en la ventana de días configurada', async () => {
    const { ejecutar, vacunacion } = crear();

    await ejecutar(new Date('2030-01-01T08:00:00'));

    expect(vacunacion.pendientesDeRecordatorio).toHaveBeenCalledWith('2030-01-01', '2030-01-08');
  });

  test('un fallo de push no detiene el envío de los demás recordatorios', async () => {
    const { ejecutar, push, vacunacion } = crear();
    push.enviar.mockRejectedValue(new Error('FCM caído'));
    jest.spyOn(console, 'error').mockImplementation(() => {});

    await ejecutar(new Date('2030-01-01T08:00:00'));

    expect(vacunacion.marcarRecordatorio).toHaveBeenCalledWith(1);
    console.error.mockRestore();
  });
});

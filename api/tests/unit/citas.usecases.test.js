const createCitasUseCases = require('../../src/application/usecases/citas.usecases');
const { ForbiddenError, ConflictError, ValidationError } = require('../../src/shared/errors');
const { ROLES, ESTADO_CITA } = require('../../src/domain/constants');

const ahora = new Date('2030-01-01T08:00:00');

const crearDependencias = () => {
  const citas = {
    crear: jest.fn().mockResolvedValue(77),
    buscarPorId: jest.fn(),
    listar: jest.fn(),
    activasPorVeterinarioYFecha: jest.fn().mockResolvedValue([]),
    actualizarEstado: jest.fn(),
    registrarConsulta: jest.fn(),
  };
  const mascotas = { buscarPorId: jest.fn() };
  const catalogo = { servicioPorId: jest.fn() };
  const veterinarios = { buscarPorId: jest.fn() };
  return {
    citas,
    mascotas,
    catalogo,
    veterinarios,
    useCases: createCitasUseCases({ citas, mascotas, catalogo, veterinarios }),
  };
};

const mascotaDeDueno5 = { id: 1, nombre: 'Firulais', duenoId: 5, activo: true };
const servicioConsulta = { id: 1, nombre: 'Consulta general', precio: 350, duracionMin: 30, activo: true };
const veterinarioLunes = {
  id: 3,
  nombre: 'Dra. Ana',
  horarios: [{ diaSemana: 2, horaInicio: '08:00', horaFin: '12:00' }],
};
const datosCita = { mascotaId: 1, servicioId: 1, veterinarioId: 3, fecha: '2030-01-01', hora: '09:00' };

describe('casos de uso de citas', () => {
  test('agendar crea la cita cuando el horario está libre', async () => {
    const { useCases, mascotas, catalogo, veterinarios, citas } = crearDependencias();
    mascotas.buscarPorId.mockResolvedValue(mascotaDeDueno5);
    catalogo.servicioPorId.mockResolvedValue(servicioConsulta);
    veterinarios.buscarPorId.mockResolvedValue(veterinarioLunes);
    citas.buscarPorId.mockResolvedValue({ id: 77, estado: ESTADO_CITA.PENDIENTE });

    const cita = await useCases.agendar({ id: 5, rol: ROLES.DUENO }, datosCita, ahora);

    expect(citas.crear).toHaveBeenCalledWith(datosCita);
    expect(cita.id).toBe(77);
  });

  test('agendar rechaza una mascota que pertenece a otro dueño', async () => {
    const { useCases, mascotas } = crearDependencias();
    mascotas.buscarPorId.mockResolvedValue(mascotaDeDueno5);

    await expect(useCases.agendar({ id: 99, rol: ROLES.DUENO }, datosCita, ahora)).rejects.toBeInstanceOf(
      ForbiddenError
    );
  });

  test('agendar rechaza una hora ya ocupada por otra cita', async () => {
    const { useCases, mascotas, catalogo, veterinarios, citas } = crearDependencias();
    mascotas.buscarPorId.mockResolvedValue(mascotaDeDueno5);
    catalogo.servicioPorId.mockResolvedValue(servicioConsulta);
    veterinarios.buscarPorId.mockResolvedValue(veterinarioLunes);
    citas.activasPorVeterinarioYFecha.mockResolvedValue([{ hora: '09:00', duracionMin: 30 }]);

    await expect(useCases.agendar({ id: 5, rol: ROLES.DUENO }, datosCita, ahora)).rejects.toBeInstanceOf(
      ConflictError
    );
    expect(citas.crear).not.toHaveBeenCalled();
  });

  test('agendar rechaza fechas pasadas', async () => {
    const { useCases, mascotas, catalogo, veterinarios } = crearDependencias();
    mascotas.buscarPorId.mockResolvedValue(mascotaDeDueno5);
    catalogo.servicioPorId.mockResolvedValue(servicioConsulta);
    veterinarios.buscarPorId.mockResolvedValue(veterinarioLunes);

    await expect(
      useCases.agendar({ id: 5, rol: ROLES.DUENO }, { ...datosCita, fecha: '2029-12-31' }, ahora)
    ).rejects.toBeInstanceOf(ValidationError);
  });

  test('cancelar rechaza que un dueño cancele la cita de otro', async () => {
    const { useCases, citas } = crearDependencias();
    citas.buscarPorId.mockResolvedValue({
      id: 77,
      estado: ESTADO_CITA.CONFIRMADA,
      dueno: { id: 5 },
      veterinario: { id: 3 },
    });

    await expect(useCases.cancelar({ id: 8, rol: ROLES.DUENO }, 77)).rejects.toBeInstanceOf(ForbiddenError);
    expect(citas.actualizarEstado).not.toHaveBeenCalled();
  });

  test('cancelar no permite cancelar una cita ya atendida', async () => {
    const { useCases, citas } = crearDependencias();
    citas.buscarPorId.mockResolvedValue({
      id: 77,
      estado: ESTADO_CITA.ATENDIDA,
      dueno: { id: 5 },
      veterinario: { id: 3 },
    });

    await expect(useCases.cancelar({ id: 5, rol: ROLES.DUENO }, 77)).rejects.toBeInstanceOf(ConflictError);
  });

  test('registrarConsulta solo lo puede hacer el veterinario asignado', async () => {
    const { useCases, citas } = crearDependencias();
    citas.buscarPorId.mockResolvedValue({
      id: 77,
      fecha: '2030-01-01',
      estado: ESTADO_CITA.CONFIRMADA,
      mascota: { id: 1 },
      dueno: { id: 5 },
      veterinario: { id: 3 },
    });

    await expect(
      useCases.registrarConsulta({ id: 4, rol: ROLES.VETERINARIO }, 77, { diagnostico: 'Sano' })
    ).rejects.toBeInstanceOf(ForbiddenError);
    expect(citas.registrarConsulta).not.toHaveBeenCalled();
  });

  test('registrarConsulta guarda la consulta con el veterinario de la sesión', async () => {
    const { useCases, citas } = crearDependencias();
    citas.buscarPorId.mockResolvedValue({
      id: 77,
      fecha: '2030-01-01',
      estado: ESTADO_CITA.CONFIRMADA,
      mascota: { id: 1 },
      dueno: { id: 5 },
      veterinario: { id: 3 },
    });
    citas.registrarConsulta.mockResolvedValue({ id: 1 });

    await useCases.registrarConsulta({ id: 3, rol: ROLES.VETERINARIO }, 77, { diagnostico: 'Sano' });

    expect(citas.registrarConsulta).toHaveBeenCalledWith(
      expect.objectContaining({ citaId: 77, mascotaId: 1, veterinarioId: 3, diagnostico: 'Sano' })
    );
  });
});

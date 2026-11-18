const {
  generarHorasDisponibles,
  esFechaHoraFutura,
  haySolapamiento,
  transicionPedidoValida,
  calcularTotal,
  proximasDosis,
  calcularProximaDosis,
} = require('../../src/domain/rules');

describe('reglas del dominio', () => {
  const horarios = [{ diaSemana: 1, horaInicio: '08:00', horaFin: '10:00' }];
  const lunes = '2030-01-07';
  const ahoraLejano = new Date('2030-01-01T10:00:00');

  test('genera horas de 30 minutos dentro del horario del veterinario', () => {
    const horas = generarHorasDisponibles({ fecha: lunes, horarios, citasActivas: [], now: ahoraLejano });
    expect(horas).toEqual(['08:00', '08:30', '09:00', '09:30']);
  });

  test('excluye horas ocupadas por citas activas, considerando su duración', () => {
    const citasActivas = [{ hora: '08:30', duracionMin: 60 }];
    const horas = generarHorasDisponibles({ fecha: lunes, horarios, citasActivas, now: ahoraLejano });
    expect(horas).toEqual(['08:00', '09:30']);
  });

  test('no ofrece horarios con duración que exceden el fin de la jornada', () => {
    const horas = generarHorasDisponibles({
      fecha: lunes,
      horarios,
      citasActivas: [],
      duracionMin: 60,
      now: ahoraLejano,
    });
    expect(horas).toEqual(['08:00', '08:30', '09:00']);
  });

  test('no devuelve horas que ya pasaron el mismo día', () => {
    const ahora = new Date('2030-01-07T08:45:00');
    const horas = generarHorasDisponibles({ fecha: lunes, horarios, citasActivas: [], now: ahora });
    expect(horas).toEqual(['09:00', '09:30']);
  });

  test('rechaza una cita en una fecha u hora pasada', () => {
    const ahora = new Date('2030-01-07T09:00:00');
    expect(esFechaHoraFutura('2030-01-06', '10:00', ahora)).toBe(false);
    expect(esFechaHoraFutura('2030-01-07', '08:00', ahora)).toBe(false);
    expect(esFechaHoraFutura('2030-01-07', '10:00', ahora)).toBe(true);
  });

  test('detecta solapamiento entre citas', () => {
    expect(haySolapamiento('09:00', 30, '09:15', 30)).toBe(true);
    expect(haySolapamiento('09:00', 30, '09:30', 30)).toBe(false);
  });

  test('valida transiciones de estado del pedido', () => {
    expect(transicionPedidoValida('PENDIENTE', 'LISTO_PARA_RECOGER')).toBe(true);
    expect(transicionPedidoValida('ENTREGADO', 'CANCELADO')).toBe(false);
    expect(transicionPedidoValida('CANCELADO', 'PENDIENTE')).toBe(false);
  });

  test('calcula el total del pedido con redondeo a centavos', () => {
    const total = calcularTotal([
      { subtotal: 350.1 },
      { subtotal: 200.2 },
    ]);
    expect(total).toBe(550.3);
  });

  test('calcula la próxima dosis según el intervalo de la vacuna', () => {
    expect(calcularProximaDosis('2026-01-10', 365)).toBe('2027-01-10');
  });

  test('devuelve la próxima dosis más reciente por vacuna', () => {
    const antirrabica = { id: 1, nombre: 'Antirrábica' };
    const aplicaciones = [
      { vacuna: antirrabica, fechaAplicacion: '2025-01-01', proximaDosis: '2026-01-01' },
      { vacuna: antirrabica, fechaAplicacion: '2026-01-02', proximaDosis: '2027-01-02' },
      { vacuna: { id: 2, nombre: 'Séxtuple' }, fechaAplicacion: '2026-03-01', proximaDosis: null },
    ];
    expect(proximasDosis(aplicaciones)).toEqual([{ vacuna: antirrabica, fecha: '2027-01-02' }]);
  });
});

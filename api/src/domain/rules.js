const { ESTADO_CITA, SLOT_MINUTOS, TRANSICIONES_PEDIDO } = require('./constants');

const pad = (n) => String(n).padStart(2, '0');

const aMinutos = (hora) => {
  const [h, m] = hora.slice(0, 5).split(':').map(Number);
  return h * 60 + m;
};

const aHora = (minutos) => `${pad(Math.floor(minutos / 60))}:${pad(minutos % 60)}`;

const hoyLocal = (now = new Date()) =>
  `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;

const addDays = (fecha, dias) => {
  const [y, m, d] = fecha.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d + dias)).toISOString().slice(0, 10);
};

const diaSemana = (fecha) => {
  const [y, m, d] = fecha.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d)).getUTCDay();
};

const haySolapamiento = (horaA, duracionA, horaB, duracionB) => {
  const inicioA = aMinutos(horaA);
  const inicioB = aMinutos(horaB);
  return inicioA < inicioB + duracionB && inicioB < inicioA + duracionA;
};

const esFechaHoraFutura = (fecha, hora, now = new Date()) => {
  const hoy = hoyLocal(now);
  if (fecha > hoy) return true;
  if (fecha < hoy) return false;
  return aMinutos(hora) > now.getHours() * 60 + now.getMinutes();
};

const generarHorasDisponibles = ({
  fecha,
  horarios,
  citasActivas,
  duracionMin = SLOT_MINUTOS,
  now = new Date(),
}) => {
  const dia = diaSemana(fecha);
  const esHoy = fecha === hoyLocal(now);
  const minutosAhora = now.getHours() * 60 + now.getMinutes();
  const horas = new Set();

  for (const horario of horarios.filter((h) => h.diaSemana === dia)) {
    const fin = aMinutos(horario.horaFin);
    for (let t = aMinutos(horario.horaInicio); t + duracionMin <= fin; t += SLOT_MINUTOS) {
      if (esHoy && t <= minutosAhora) continue;
      const hora = aHora(t);
      const ocupada = citasActivas.some((c) =>
        haySolapamiento(hora, duracionMin, c.hora, c.duracionMin)
      );
      if (!ocupada) horas.add(hora);
    }
  }
  return [...horas].sort();
};

const puedeCancelarCita = (estado) =>
  estado === ESTADO_CITA.PENDIENTE || estado === ESTADO_CITA.CONFIRMADA;

const transicionPedidoValida = (desde, hasta) =>
  (TRANSICIONES_PEDIDO[desde] || []).includes(hasta);

const redondear = (valor) => Math.round(valor * 100) / 100;

const calcularTotal = (lineas) =>
  redondear(lineas.reduce((total, linea) => total + linea.subtotal, 0));

const calcularProximaDosis = (fechaAplicacion, intervaloDias) => addDays(fechaAplicacion, intervaloDias);

const proximasDosis = (aplicaciones) => {
  const ultimaPorVacuna = new Map();
  for (const aplicacion of aplicaciones) {
    const previa = ultimaPorVacuna.get(aplicacion.vacuna.id);
    if (!previa || aplicacion.fechaAplicacion > previa.fechaAplicacion) {
      ultimaPorVacuna.set(aplicacion.vacuna.id, aplicacion);
    }
  }
  return [...ultimaPorVacuna.values()]
    .filter((a) => a.proximaDosis)
    .map((a) => ({ vacuna: a.vacuna, fecha: a.proximaDosis }))
    .sort((a, b) => a.fecha.localeCompare(b.fecha));
};

module.exports = {
  aMinutos,
  aHora,
  hoyLocal,
  addDays,
  diaSemana,
  haySolapamiento,
  esFechaHoraFutura,
  generarHorasDisponibles,
  puedeCancelarCita,
  transicionPedidoValida,
  redondear,
  calcularTotal,
  calcularProximaDosis,
  proximasDosis,
};

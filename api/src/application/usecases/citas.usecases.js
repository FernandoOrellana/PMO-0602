const { NotFoundError, ForbiddenError, ValidationError, ConflictError } = require('../../shared/errors');
const { ROLES, ESTADO_CITA } = require('../../domain/constants');
const { esFechaHoraFutura, generarHorasDisponibles, puedeCancelarCita } = require('../../domain/rules');
const { meta } = require('../../shared/mappers');

module.exports = function createCitasUseCases({ citas, mascotas, catalogo, veterinarios }) {
  async function obtenerCita(id) {
    const cita = await citas.buscarPorId(id);
    if (!cita) throw new NotFoundError('Cita no encontrada');
    return cita;
  }

  return {
    async agendar(actor, { mascotaId, servicioId, veterinarioId, fecha, hora }, now = new Date()) {
      const mascota = await mascotas.buscarPorId(mascotaId);
      if (!mascota || !mascota.activo) throw new NotFoundError('Mascota no encontrada');
      if (mascota.duenoId !== actor.id) {
        throw new ForbiddenError('Solo puedes agendar citas para tus mascotas');
      }

      const servicio = await catalogo.servicioPorId(servicioId);
      if (!servicio || !servicio.activo) throw new ValidationError('El servicio seleccionado no está disponible');

      const veterinario = await veterinarios.buscarPorId(veterinarioId);
      if (!veterinario) throw new ValidationError('El veterinario seleccionado no existe');

      if (!esFechaHoraFutura(fecha, hora, now)) {
        throw new ValidationError('La fecha y hora deben ser futuras');
      }

      const disponibles = generarHorasDisponibles({
        fecha,
        horarios: veterinario.horarios,
        citasActivas: await citas.activasPorVeterinarioYFecha(veterinarioId, fecha),
        duracionMin: servicio.duracionMin,
        now,
      });
      if (!disponibles.includes(hora)) {
        throw new ConflictError('La hora seleccionada no está disponible');
      }

      const id = await citas.crear({ mascotaId, servicioId, veterinarioId, fecha, hora });
      return citas.buscarPorId(id);
    },

    async listar(actor, { fecha, estado, page, limit }) {
      const filtros = { fecha, estado, page, limit };
      if (actor.rol === ROLES.DUENO) filtros.duenoId = actor.id;
      if (actor.rol === ROLES.VETERINARIO) filtros.veterinarioId = actor.id;
      const { items, total } = await citas.listar(filtros);
      return { items, meta: meta({ page, limit, total }) };
    },

    async obtener(actor, id) {
      const cita = await obtenerCita(id);
      const esSuya =
        (actor.rol === ROLES.DUENO && cita.dueno.id === actor.id) ||
        (actor.rol === ROLES.VETERINARIO && cita.veterinario.id === actor.id);
      if (actor.rol !== ROLES.ADMINISTRADOR && !esSuya) {
        throw new ForbiddenError('No tienes acceso a esta cita');
      }
      return cita;
    },

    async cancelar(actor, id) {
      const cita = await obtenerCita(id);
      if (cita.dueno.id !== actor.id) throw new ForbiddenError('Solo puedes cancelar tus propias citas');
      if (!puedeCancelarCita(cita.estado)) {
        throw new ConflictError('La cita ya no puede cancelarse');
      }
      await citas.actualizarEstado(id, ESTADO_CITA.CANCELADA);
      return citas.buscarPorId(id);
    },

    async registrarConsulta(actor, citaId, datos) {
      const cita = await obtenerCita(citaId);
      if (cita.veterinario.id !== actor.id) {
        throw new ForbiddenError('Solo el veterinario asignado puede registrar esta consulta');
      }
      if (!puedeCancelarCita(cita.estado)) {
        throw new ConflictError('Esta cita ya fue atendida o cancelada');
      }
      return citas.registrarConsulta({
        ...datos,
        citaId,
        mascotaId: cita.mascota.id,
        veterinarioId: actor.id,
        fecha: cita.fecha,
      });
    },
  };
};

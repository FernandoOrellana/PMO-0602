const { NotFoundError } = require('../../shared/errors');
const { addDays, hoyLocal } = require('../../domain/rules');

module.exports = {
  createNotificacionesUseCases({ notificaciones }) {
    return {
      listar: (usuarioId) => notificaciones.listarPorUsuario(usuarioId),

      async marcarLeida(usuarioId, id) {
        const ok = await notificaciones.marcarLeida(id, usuarioId);
        if (!ok) throw new NotFoundError('Notificación no encontrada');
        return { message: 'Notificación marcada como leída' };
      },

      async registrarDispositivo(usuarioId, fcmToken) {
        await notificaciones.guardarDispositivo(usuarioId, fcmToken);
        return { message: 'Dispositivo registrado' };
      },
    };
  },

  createRecordatoriosUseCases({ vacunacion, citas, notificaciones, push, config }) {
    async function notificar(usuarioId, titulo, mensaje) {
      await notificaciones.crear({ usuarioId, titulo, mensaje });
      try {
        const tokens = await notificaciones.tokensDeUsuario(usuarioId);
        await push.enviar(tokens, { titulo, mensaje });
      } catch (err) {
        console.error('[recordatorios] Error enviando push:', err.message);
      }
    }

    return async function ejecutarRecordatorios(now = new Date()) {
      const hoy = hoyLocal(now);
      const vacunas = await vacunacion.pendientesDeRecordatorio(hoy, addDays(hoy, config.reminders.days));
      for (const v of vacunas) {
        await notificar(
          v.duenoId,
          `Vacuna próxima: ${v.mascotaNombre}`,
          `${v.vacunaNombre} de ${v.mascotaNombre} corresponde el ${v.proximaDosis}. Agenda tu cita.`
        );
        await vacunacion.marcarRecordatorio(v.id);
      }

      const citasManana = await citas.pendientesDeRecordatorio(addDays(hoy, 1));
      for (const c of citasManana) {
        await notificar(
          c.dueno.id,
          'Cita mañana',
          `${c.mascota.nombre} tiene cita de ${c.servicio.nombre} el ${c.fecha} a las ${c.hora}.`
        );
        await citas.marcarRecordatorio(c.id);
      }

      return { vacunas: vacunas.length, citas: citasManana.length };
    };
  },
};

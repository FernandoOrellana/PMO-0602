const { NotFoundError } = require('../../shared/errors');
const { addDays, hoyLocal } = require('../../domain/rules');

function crearNotificador({ notificaciones, push }) {
  return async function notificar(usuarioId, titulo, mensaje, data = {}) {
    await notificaciones.crear({ usuarioId, titulo, mensaje });
    const tokens = await notificaciones.tokensDeUsuario(usuarioId);
    const resultado = (await push.enviar(tokens, { titulo, mensaje, data })) || { enviados: 0, invalidos: [] };
    if (resultado.invalidos?.length) await notificaciones.eliminarDispositivos(resultado.invalidos);
    return { dispositivos: tokens.length - (resultado.invalidos?.length || 0), enviados: resultado.enviados || 0 };
  };
}

module.exports = {
  crearNotificador,

  createNotificacionesUseCases({ notificaciones, push }) {
    const notificar = crearNotificador({ notificaciones, push });

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

      async probar(usuarioId) {
        const resultado = await notificar(
          usuarioId,
          'Notificación de prueba',
          'Las notificaciones de PetCare HN están funcionando en este dispositivo.',
          { tipo: 'PRUEBA' }
        );
        return {
          message: resultado.dispositivos
            ? 'Notificación enviada a tus dispositivos.'
            : 'Notificación guardada. No hay dispositivos registrados para recibir push.',
          ...resultado,
        };
      },
    };
  },

  createRecordatoriosUseCases({ vacunacion, citas, notificaciones, push, config }) {
    const notificar = crearNotificador({ notificaciones, push });

    async function avisar(usuarioId, titulo, mensaje, data) {
      try {
        await notificar(usuarioId, titulo, mensaje, data);
      } catch (err) {
        console.error('[recordatorios] Error enviando push:', err.message);
      }
    }

    return async function ejecutarRecordatorios(now = new Date()) {
      const hoy = hoyLocal(now);
      const vacunas = await vacunacion.pendientesDeRecordatorio(hoy, addDays(hoy, config.reminders.days));
      for (const v of vacunas) {
        await avisar(
          v.duenoId,
          `Vacuna próxima: ${v.mascotaNombre}`,
          `${v.vacunaNombre} de ${v.mascotaNombre} corresponde el ${v.proximaDosis}. Agenda tu cita.`,
          { tipo: 'VACUNA' }
        );
        await vacunacion.marcarRecordatorio(v.id);
      }

      const citasManana = await citas.pendientesDeRecordatorio(addDays(hoy, 1));
      for (const c of citasManana) {
        await avisar(
          c.dueno.id,
          'Cita mañana',
          `${c.mascota.nombre} tiene cita de ${c.servicio.nombre} el ${c.fecha} a las ${c.hora}.`,
          { tipo: 'CITA' }
        );
        await citas.marcarRecordatorio(c.id);
      }

      return { vacunas: vacunas.length, citas: citasManana.length };
    };
  },
};

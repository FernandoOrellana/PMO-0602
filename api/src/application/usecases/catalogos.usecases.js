const { NotFoundError } = require('../../shared/errors');
const { SLOT_MINUTOS } = require('../../domain/constants');
const { generarHorasDisponibles, hoyLocal } = require('../../domain/rules');

module.exports = function createCatalogosUseCases({ catalogo, veterinarios, citas }) {
  return {
    especies: () => catalogo.especies(),
    vacunas: () => catalogo.vacunas(),
    servicios: () => catalogo.servicios({ soloActivos: true }),
    veterinarios: () => veterinarios.listar(),

    async disponibilidad({ veterinarioId, fecha, servicioId }, now = new Date()) {
      const veterinario = await veterinarios.buscarPorId(veterinarioId);
      if (!veterinario) throw new NotFoundError('Veterinario no encontrado');

      let duracionMin = SLOT_MINUTOS;
      if (servicioId) {
        const servicio = await catalogo.servicioPorId(servicioId);
        if (!servicio || !servicio.activo) throw new NotFoundError('Servicio no encontrado');
        duracionMin = servicio.duracionMin;
      }

      const horas =
        fecha < hoyLocal(now)
          ? []
          : generarHorasDisponibles({
              fecha,
              horarios: veterinario.horarios,
              citasActivas: await citas.activasPorVeterinarioYFecha(veterinarioId, fecha),
              duracionMin,
              now,
            });

      return {
        fecha,
        veterinario: { id: veterinario.id, nombre: veterinario.nombre },
        horas,
      };
    },
  };
};

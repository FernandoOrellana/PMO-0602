const { NotFoundError, ForbiddenError, ValidationError } = require('../../shared/errors');
const { ROLES } = require('../../domain/constants');
const { proximasDosis, calcularProximaDosis } = require('../../domain/rules');
const { meta } = require('../../shared/mappers');

module.exports = function createMascotasUseCases({ mascotas, vacunacion, citas, catalogo, storage }) {
  async function obtenerAccesible(actor, id) {
    const mascota = await mascotas.buscarPorId(id);
    if (!mascota || !mascota.activo) throw new NotFoundError('Mascota no encontrada');
    if (actor.rol === ROLES.DUENO && mascota.duenoId !== actor.id) {
      throw new ForbiddenError('Esta mascota no pertenece a tu cuenta');
    }
    return mascota;
  }

  async function asegurarPropietario(actor, id) {
    const mascota = await obtenerAccesible(actor, id);
    if (mascota.duenoId !== actor.id) throw new ForbiddenError('Esta mascota no pertenece a tu cuenta');
    return mascota;
  }

  async function asegurarEspecie(especieId) {
    if (!(await catalogo.especiePorId(especieId))) {
      throw new ValidationError('La especie seleccionada no existe');
    }
  }

  return {
    async listar(actor, { page, limit }) {
      const duenoId = actor.rol === ROLES.DUENO ? actor.id : undefined;
      const { items, total } = await mascotas.listar({ duenoId, page, limit });
      return { items, meta: meta({ page, limit, total }) };
    },

    async crear(actor, datos) {
      await asegurarEspecie(datos.especieId);
      const id = await mascotas.crear({ ...datos, duenoId: actor.id });
      return mascotas.buscarPorId(id);
    },

    async obtener(actor, id) {
      return obtenerAccesible(actor, id);
    },

    async actualizar(actor, id, datos) {
      await asegurarPropietario(actor, id);
      await asegurarEspecie(datos.especieId);
      await mascotas.actualizar(id, datos);
      return mascotas.buscarPorId(id);
    },

    async eliminar(actor, id) {
      await asegurarPropietario(actor, id);
      await mascotas.desactivar(id);
      return { message: 'Mascota eliminada' };
    },

    async actualizarFoto(actor, id, rutaRelativa) {
      await asegurarPropietario(actor, id);
      const anterior = await mascotas.rutaFoto(id);
      await mascotas.actualizarFoto(id, rutaRelativa);
      await storage.eliminar(anterior);
      return mascotas.buscarPorId(id);
    },

    async carne(actor, id) {
      const mascota = await obtenerAccesible(actor, id);
      const vacunas = await vacunacion.listarPorMascota(id);
      return { mascota, vacunas, proximasDosis: proximasDosis(vacunas) };
    },

    async historial(actor, id) {
      const mascota = await obtenerAccesible(actor, id);
      const [consultas, vacunas, citasMascota] = await Promise.all([
        citas.consultasPorMascota(id),
        vacunacion.listarPorMascota(id),
        citas.listar({ mascotaId: id, page: 1, limit: 100 }),
      ]);
      return { mascota, consultas, vacunas, citas: citasMascota.items };
    },

    async registrarVacuna(actor, mascotaId, datos) {
      await obtenerAccesible(actor, mascotaId);
      const vacuna = await catalogo.vacunaPorId(datos.vacunaId);
      if (!vacuna) throw new ValidationError('La vacuna seleccionada no existe');

      const proximaDosis =
        datos.proximaDosis ?? calcularProximaDosis(datos.fechaAplicacion, vacuna.intervaloDias);
      if (proximaDosis < datos.fechaAplicacion) {
        throw new ValidationError('La próxima dosis no puede ser anterior a la aplicación');
      }

      const id = await vacunacion.crearAplicacion({
        ...datos,
        mascotaId,
        veterinarioId: actor.id,
        proximaDosis,
      });
      return vacunacion.buscarAplicacionPorId(id);
    },
  };
};

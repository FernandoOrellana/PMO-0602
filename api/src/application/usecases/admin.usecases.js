const { NotFoundError, ConflictError, ValidationError } = require('../../shared/errors');
const { ROLES } = require('../../domain/constants');
const { hoyLocal } = require('../../domain/rules');

module.exports = function createAdminUseCases({
  productos,
  catalogo,
  veterinarios,
  usuarios,
  security,
  storage,
  reportes,
}) {
  const productoExistente = async (id) => {
    const producto = await productos.buscarPorId(id);
    if (!producto) throw new NotFoundError('Producto no encontrado');
    return producto;
  };

  const servicioExistente = async (id) => {
    const servicio = await catalogo.servicioPorId(id);
    if (!servicio) throw new NotFoundError('Servicio no encontrado');
    return servicio;
  };

  const veterinarioExistente = async (id) => {
    const vet = await veterinarios.buscarPorId(id);
    if (!vet) throw new NotFoundError('Veterinario no encontrado');
    return vet;
  };

  const rangoFechas = ({ desde, hasta }) => {
    const hoy = hoyLocal();
    const hastaFinal = hasta || hoy;
    const desdeFinal = desde || `${hastaFinal.slice(0, 8)}01`;
    if (desdeFinal > hastaFinal) throw new ValidationError('La fecha inicial no puede ser mayor que la final');
    return { desde: desdeFinal, hasta: hastaFinal };
  };

  return {
    productos: {
      async crear(datos) {
        return productos.buscarPorId(await productos.crear(datos));
      },
      async actualizar(id, datos) {
        await productoExistente(id);
        await productos.actualizar(id, datos);
        return productos.buscarPorId(id);
      },
      async desactivar(id) {
        await productoExistente(id);
        await productos.desactivar(id);
        return { message: 'Producto desactivado' };
      },
      async actualizarImagen(id, rutaRelativa) {
        await productoExistente(id);
        const anterior = await productos.rutaImagen(id);
        await productos.actualizarImagen(id, rutaRelativa);
        await storage.eliminar(anterior);
        return productos.buscarPorId(id);
      },
    },

    servicios: {
      async crear(datos) {
        return servicioExistente(await catalogo.crearServicio(datos));
      },
      async actualizar(id, datos) {
        await servicioExistente(id);
        await catalogo.actualizarServicio(id, datos);
        return servicioExistente(id);
      },
      async desactivar(id) {
        await servicioExistente(id);
        await catalogo.desactivarServicio(id);
        return { message: 'Servicio desactivado' };
      },
    },

    veterinarios: {
      async crear({ nombre, email, password, telefono }) {
        if (await usuarios.buscarPorEmail(email)) {
          throw new ConflictError('Ya existe una cuenta con ese correo');
        }
        const id = await usuarios.crear({
          nombre,
          email,
          telefono,
          passwordHash: await security.passwords.hash(password),
          rol: ROLES.VETERINARIO,
          emailVerificado: true,
        });
        return veterinarioExistente(id);
      },
      async actualizar(id, { nombre, telefono }) {
        await veterinarioExistente(id);
        await usuarios.actualizarDatos(id, { nombre, telefono });
        return veterinarioExistente(id);
      },
      async actualizarHorarios(id, horarios) {
        await veterinarioExistente(id);
        await veterinarios.actualizarHorarios(id, horarios);
        return veterinarioExistente(id);
      },
    },

    reportes: {
      async ventas(filtros) {
        const rango = rangoFechas(filtros);
        return { ...rango, ...(await reportes.ventas(rango.desde, rango.hasta)) };
      },
      async citas(filtros) {
        const rango = rangoFechas(filtros);
        return { ...rango, ...(await reportes.citas(rango.desde, rango.hasta)) };
      },
    },
  };
};

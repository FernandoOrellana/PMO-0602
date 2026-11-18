const config = require('./config/env');
const usuarioRepo = require('./infrastructure/repositories/usuario.repository');
const refreshTokenRepo = require('./infrastructure/repositories/refreshToken.repository');
const catalogoRepo = require('./infrastructure/repositories/catalogo.repository');
const veterinarioRepo = require('./infrastructure/repositories/veterinario.repository');
const mascotaRepo = require('./infrastructure/repositories/mascota.repository');
const vacunacionRepo = require('./infrastructure/repositories/vacunacion.repository');
const citaRepo = require('./infrastructure/repositories/cita.repository');
const productoRepo = require('./infrastructure/repositories/producto.repository');
const pedidoRepo = require('./infrastructure/repositories/pedido.repository');
const notificacionRepo = require('./infrastructure/repositories/notificacion.repository');
const reporteRepo = require('./infrastructure/repositories/reporte.repository');
const security = require('./infrastructure/services/security');
const mailer = require('./infrastructure/services/mailer');
const push = require('./infrastructure/services/push');
const storage = require('./infrastructure/services/fileStorage');
const createAuthUseCases = require('./application/usecases/auth.usecases');
const createMascotasUseCases = require('./application/usecases/mascotas.usecases');
const createCitasUseCases = require('./application/usecases/citas.usecases');
const createTiendaUseCases = require('./application/usecases/tienda.usecases');
const createCatalogosUseCases = require('./application/usecases/catalogos.usecases');
const createAdminUseCases = require('./application/usecases/admin.usecases');
const {
  createNotificacionesUseCases,
  createRecordatoriosUseCases,
} = require('./application/usecases/notificaciones.usecases');

const repositorios = {
  usuarios: usuarioRepo,
  refreshTokens: refreshTokenRepo,
  catalogo: catalogoRepo,
  veterinarios: veterinarioRepo,
  mascotas: mascotaRepo,
  vacunacion: vacunacionRepo,
  citas: citaRepo,
  productos: productoRepo,
  pedidos: pedidoRepo,
  notificaciones: notificacionRepo,
  reportes: reporteRepo,
};

const useCases = {
  auth: createAuthUseCases({ ...repositorios, security, mailer, config }),
  mascotas: createMascotasUseCases({ ...repositorios, storage }),
  citas: createCitasUseCases(repositorios),
  tienda: createTiendaUseCases(repositorios),
  catalogos: createCatalogosUseCases(repositorios),
  admin: createAdminUseCases({ ...repositorios, security, storage }),
  notificaciones: createNotificacionesUseCases(repositorios),
  recordatorios: createRecordatoriosUseCases({ ...repositorios, push, config }),
};

module.exports = { config, security, storage, useCases };

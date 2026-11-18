const { Router } = require('express');
const { autenticar, autorizar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { crearUpload } = require('../middlewares/upload');
const { schemas, idParam } = require('../validators');
const { ok } = require('../respuesta');
const { ROLES } = require('../../../domain/constants');

module.exports = function adminRoutes({ useCases, security }) {
  const router = Router();
  const { admin, recordatorios } = useCases;
  const subirImagen = crearUpload('productos', 'imagen');

  router.use(autenticar(security.tokens), autorizar(ROLES.ADMINISTRADOR));

  router.post('/productos', validar({ body: schemas.producto }), async (req, res) =>
    ok(res, await admin.productos.crear(req.input.body), 201)
  );
  router.put(
    '/productos/:id',
    validar({ params: idParam, body: schemas.producto }),
    async (req, res) => ok(res, await admin.productos.actualizar(req.input.params.id, req.input.body))
  );
  router.delete('/productos/:id', validar({ params: idParam }), async (req, res) =>
    ok(res, await admin.productos.desactivar(req.input.params.id))
  );
  router.post(
    '/productos/:id/imagen',
    validar({ params: idParam }),
    subirImagen,
    async (req, res) =>
      ok(res, await admin.productos.actualizarImagen(req.input.params.id, req.file.rutaRelativa))
  );

  router.post('/servicios', validar({ body: schemas.servicio }), async (req, res) =>
    ok(res, await admin.servicios.crear(req.input.body), 201)
  );
  router.put(
    '/servicios/:id',
    validar({ params: idParam, body: schemas.servicio }),
    async (req, res) => ok(res, await admin.servicios.actualizar(req.input.params.id, req.input.body))
  );
  router.delete('/servicios/:id', validar({ params: idParam }), async (req, res) =>
    ok(res, await admin.servicios.desactivar(req.input.params.id))
  );

  router.post('/veterinarios', validar({ body: schemas.veterinarioCrear }), async (req, res) =>
    ok(res, await admin.veterinarios.crear(req.input.body), 201)
  );
  router.put(
    '/veterinarios/:id',
    validar({ params: idParam, body: schemas.veterinarioActualizar }),
    async (req, res) =>
      ok(res, await admin.veterinarios.actualizar(req.input.params.id, req.input.body))
  );
  router.put(
    '/veterinarios/:id/horarios',
    validar({ params: idParam, body: schemas.horarios }),
    async (req, res) =>
      ok(res, await admin.veterinarios.actualizarHorarios(req.input.params.id, req.input.body))
  );

  router.post('/recordatorios/ejecutar', async (_req, res) => ok(res, await recordatorios()));

  router.get('/reportes/ventas', validar({ query: schemas.rangoReporte }), async (req, res) =>
    ok(res, await admin.reportes.ventas(req.input.query))
  );
  router.get('/reportes/citas', validar({ query: schemas.rangoReporte }), async (req, res) =>
    ok(res, await admin.reportes.citas(req.input.query))
  );

  return router;
};

const { Router } = require('express');
const { autenticar, autorizar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { crearUpload } = require('../middlewares/upload');
const { schemas, idParam, paginacion } = require('../validators');
const { ok, lista } = require('../respuesta');
const { ROLES } = require('../../../domain/constants');

module.exports = function mascotasRoutes({ useCases, security }) {
  const router = Router();
  const { mascotas } = useCases;
  const subirFoto = crearUpload('mascotas', 'foto');

  router.use(autenticar(security.tokens));

  router.get('/', validar({ query: paginacion }), async (req, res) =>
    lista(res, await mascotas.listar(req.user, req.input.query))
  );

  router.post('/', autorizar(ROLES.DUENO), validar({ body: schemas.mascota }), async (req, res) =>
    ok(res, await mascotas.crear(req.user, req.input.body), 201)
  );

  router.get('/:id', validar({ params: idParam }), async (req, res) =>
    ok(res, await mascotas.obtener(req.user, req.input.params.id))
  );

  router.put(
    '/:id',
    autorizar(ROLES.DUENO),
    validar({ params: idParam, body: schemas.mascota }),
    async (req, res) =>
      ok(res, await mascotas.actualizar(req.user, req.input.params.id, req.input.body))
  );

  router.delete('/:id', autorizar(ROLES.DUENO), validar({ params: idParam }), async (req, res) =>
    ok(res, await mascotas.eliminar(req.user, req.input.params.id))
  );

  router.post(
    '/:id/foto',
    autorizar(ROLES.DUENO),
    validar({ params: idParam }),
    subirFoto,
    async (req, res) =>
      ok(res, await mascotas.actualizarFoto(req.user, req.input.params.id, req.file.rutaRelativa))
  );

  router.get('/:id/carne', validar({ params: idParam }), async (req, res) =>
    ok(res, await mascotas.carne(req.user, req.input.params.id))
  );

  router.get('/:id/historial', validar({ params: idParam }), async (req, res) =>
    ok(res, await mascotas.historial(req.user, req.input.params.id))
  );

  router.post(
    '/:id/vacunas',
    autorizar(ROLES.VETERINARIO),
    validar({ params: idParam, body: schemas.aplicacionVacuna }),
    async (req, res) =>
      ok(
        res,
        await mascotas.registrarVacuna(req.user, req.input.params.id, req.input.body),
        201
      )
  );

  return router;
};

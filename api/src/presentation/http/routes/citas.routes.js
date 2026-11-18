const { Router } = require('express');
const { autenticar, autorizar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { schemas, idParam } = require('../validators');
const { ok, lista } = require('../respuesta');
const { ROLES } = require('../../../domain/constants');

module.exports = function citasRoutes({ useCases, security }) {
  const router = Router();
  const { citas } = useCases;

  router.use(autenticar(security.tokens));

  router.post('/', autorizar(ROLES.DUENO), validar({ body: schemas.cita }), async (req, res) =>
    ok(res, await citas.agendar(req.user, req.input.body), 201)
  );

  router.get('/', validar({ query: schemas.listaCitas }), async (req, res) =>
    lista(res, await citas.listar(req.user, req.input.query))
  );

  router.get('/:id', validar({ params: idParam }), async (req, res) =>
    ok(res, await citas.obtener(req.user, req.input.params.id))
  );

  router.patch(
    '/:id/cancelar',
    autorizar(ROLES.DUENO),
    validar({ params: idParam }),
    async (req, res) => ok(res, await citas.cancelar(req.user, req.input.params.id))
  );

  router.post(
    '/:id/consulta',
    autorizar(ROLES.VETERINARIO),
    validar({ params: idParam, body: schemas.consulta }),
    async (req, res) =>
      ok(res, await citas.registrarConsulta(req.user, req.input.params.id, req.input.body), 201)
  );

  return router;
};

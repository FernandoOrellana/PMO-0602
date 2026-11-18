const { Router } = require('express');
const { autenticar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { schemas, idParam } = require('../validators');
const { ok } = require('../respuesta');

module.exports = function catalogosRoutes({ useCases, security }) {
  const router = Router();
  const { catalogos } = useCases;
  const auth = autenticar(security.tokens);

  router.get('/especies', auth, async (_req, res) => ok(res, await catalogos.especies()));
  router.get('/vacunas', auth, async (_req, res) => ok(res, await catalogos.vacunas()));
  router.get('/servicios', auth, async (_req, res) => ok(res, await catalogos.servicios()));
  router.get('/veterinarios', auth, async (_req, res) => ok(res, await catalogos.veterinarios()));

  router.get(
    '/veterinarios/:id/disponibilidad',
    auth,
    validar({ params: idParam, query: schemas.disponibilidad }),
    async (req, res) => {
      const { params, query } = req.input;
      return ok(
        res,
        await catalogos.disponibilidad({
          veterinarioId: params.id,
          fecha: query.fecha,
          servicioId: query.servicioId,
        })
      );
    }
  );

  return router;
};

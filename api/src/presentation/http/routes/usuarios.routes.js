const { Router } = require('express');
const { autenticar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { schemas, idParam } = require('../validators');
const { ok } = require('../respuesta');

module.exports = function usuariosRoutes({ useCases, security }) {
  const router = Router();
  const { notificaciones } = useCases;
  const auth = autenticar(security.tokens);

  router.post(
    '/usuarios/me/dispositivos',
    auth,
    validar({ body: schemas.dispositivo }),
    async (req, res) =>
      ok(res, await notificaciones.registrarDispositivo(req.user.id, req.input.body.fcmToken))
  );

  router.get('/notificaciones', auth, async (req, res) =>
    ok(res, await notificaciones.listar(req.user.id))
  );

  router.patch(
    '/notificaciones/:id/leer',
    auth,
    validar({ params: idParam }),
    async (req, res) => ok(res, await notificaciones.marcarLeida(req.user.id, req.input.params.id))
  );

  return router;
};

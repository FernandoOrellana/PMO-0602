const { Router } = require('express');
const { autenticar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { schemas } = require('../validators');
const { ok } = require('../respuesta');

module.exports = function authRoutes({ auth, security }) {
  const router = Router();

  router.post('/register', validar({ body: schemas.registro }), async (req, res) =>
    ok(res, await auth.registrar(req.input.body), 201)
  );
  router.post('/verify-email', validar({ body: schemas.verificarEmail }), async (req, res) =>
    ok(res, await auth.verificarEmail(req.input.body))
  );
  router.post('/resend-code', validar({ body: schemas.email }), async (req, res) =>
    ok(res, await auth.reenviarCodigo(req.input.body))
  );
  router.post('/login', validar({ body: schemas.login }), async (req, res) =>
    ok(res, await auth.login(req.input.body))
  );
  router.post('/refresh', validar({ body: schemas.refrescar }), async (req, res) =>
    ok(res, await auth.refrescar(req.input.body))
  );
  router.post('/forgot-password', validar({ body: schemas.email }), async (req, res) =>
    ok(res, await auth.olvidePassword(req.input.body))
  );
  router.post('/reset-password', validar({ body: schemas.restablecer }), async (req, res) =>
    ok(res, await auth.restablecerPassword(req.input.body))
  );
  router.post('/logout', validar({ body: schemas.refrescar }), async (req, res) =>
    ok(res, await auth.cerrarSesion(req.input.body))
  );
  router.get('/me', autenticar(security.tokens), async (req, res) =>
    ok(res, await auth.perfil(req.user.id))
  );

  return router;
};

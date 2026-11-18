const { Router } = require('express');
const { autenticar, autorizar } = require('../middlewares/auth');
const { validar } = require('../middlewares/validate');
const { schemas, idParam, paginacion } = require('../validators');
const { ok, lista } = require('../respuesta');
const { ROLES } = require('../../../domain/constants');

module.exports = function tiendaRoutes({ useCases, security }) {
  const router = Router();
  const { tienda } = useCases;
  const auth = autenticar(security.tokens);

  router.get(
    '/productos',
    auth,
    validar({ query: schemas.listaProductos }),
    async (req, res) => lista(res, await tienda.listarProductos(req.input.query))
  );

  router.get('/productos/:id', auth, validar({ params: idParam }), async (req, res) =>
    ok(res, await tienda.obtenerProducto(req.input.params.id))
  );

  router.post(
    '/pedidos',
    auth,
    autorizar(ROLES.DUENO),
    validar({ body: schemas.pedido }),
    async (req, res) => ok(res, await tienda.crearPedido(req.user, req.input.body), 201)
  );

  router.get(
    '/pedidos',
    auth,
    autorizar(ROLES.DUENO, ROLES.ADMINISTRADOR),
    validar({ query: paginacion }),
    async (req, res) => lista(res, await tienda.listarPedidos(req.user, req.input.query))
  );

  router.get(
    '/pedidos/:id',
    auth,
    autorizar(ROLES.DUENO, ROLES.ADMINISTRADOR),
    validar({ params: idParam }),
    async (req, res) => ok(res, await tienda.obtenerPedido(req.user, req.input.params.id))
  );

  router.patch(
    '/pedidos/:id/estado',
    auth,
    autorizar(ROLES.ADMINISTRADOR),
    validar({ params: idParam, body: schemas.estadoPedido }),
    async (req, res) =>
      ok(res, await tienda.cambiarEstado(req.input.params.id, req.input.body.estado))
  );

  return router;
};
